[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$InputDll,
    [Parameter(Mandatory = $true)][string]$OutputDll,
    [Parameter(Mandatory = $true)][string]$WorkshopRoot,
    [Parameter(Mandatory = $true)][string]$GameManagedDir,
    [Parameter(Mandatory = $true)][string]$Cecil
)

$ErrorActionPreference = 'Stop'
Add-Type -Path $Cecil

$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory((Resolve-Path -LiteralPath $GameManagedDir))
$readerParameters = New-Object Mono.Cecil.ReaderParameters
$readerParameters.AssemblyResolver = $resolver
$typeMap = New-Object 'System.Collections.Generic.Dictionary[string,Mono.Cecil.TypeDefinition]' ([StringComparer]::Ordinal)

foreach ($file in Get-ChildItem -LiteralPath (Resolve-Path -LiteralPath $WorkshopRoot) -Recurse -File -Filter '*.dll') {
    try { $candidate = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($file.FullName, $readerParameters) }
    catch { continue }
    foreach ($rootType in $candidate.MainModule.Types) {
        $stack = New-Object 'System.Collections.Generic.Stack[Mono.Cecil.TypeDefinition]'
        $stack.Push($rootType)
        while ($stack.Count -ne 0) {
            $type = $stack.Pop()
            $runtimeName = $type.FullName.Replace('/', '+')
            if (-not $typeMap.ContainsKey($runtimeName)) { $typeMap.Add($runtimeName, $type) }
            foreach ($nested in $type.NestedTypes) { $stack.Push($nested) }
        }
    }
}

$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $InputDll), $readerParameters)
$module = $assembly.MainModule
$patchTypes = @($module.Types | Where-Object Name -like 'GeneratedManagedPatch_*')
$prototype = $patchTypes | ForEach-Object { $_.CustomAttributes } | Where-Object {
    $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' -and
    $_.ConstructorArguments.Count -eq 3 -and
    $_.ConstructorArguments[0].Type.FullName -eq 'System.String'
} | Select-Object -First 1
if ($null -eq $prototype) { throw 'No generated string target attributes were found.' }

$harmonyPatchType = $prototype.AttributeType
$harmonyTargetMethodType = New-Object Mono.Cecil.TypeReference('HarmonyLib', 'HarmonyTargetMethod', $module, $harmonyPatchType.Scope)
$emptyPatchCtor = New-Object Mono.Cecil.MethodReference('.ctor', $module.TypeSystem.Void, $harmonyPatchType)
$emptyPatchCtor.HasThis = $true
$targetMethodCtor = New-Object Mono.Cecil.MethodReference('.ctor', $module.TypeSystem.Void, $harmonyTargetMethodType)
$targetMethodCtor.HasThis = $true
$frameworkScope = $module.AssemblyReferences | Where-Object Name -eq 'netstandard' | Select-Object -First 1
if ($null -eq $frameworkScope) { throw 'The netstandard assembly reference was not found.' }
$methodBaseType = New-Object Mono.Cecil.TypeReference('System.Reflection', 'MethodBase', $module, $frameworkScope)
$moduleType = New-Object Mono.Cecil.TypeReference('System.Reflection', 'Module', $module, $frameworkScope)
$systemType = New-Object Mono.Cecil.TypeReference('System', 'Type', $module, $frameworkScope)
$runtimeTypeHandleType = New-Object Mono.Cecil.TypeReference('System', 'RuntimeTypeHandle', $module, $frameworkScope, $true)
$getTypeFromHandle = New-Object Mono.Cecil.MethodReference('GetTypeFromHandle', $systemType, $systemType)
$getTypeFromHandle.Parameters.Add((New-Object Mono.Cecil.ParameterDefinition($runtimeTypeHandleType)))
$getModule = New-Object Mono.Cecil.MethodReference('get_Module', $moduleType, $systemType)
$getModule.HasThis = $true
$resolveMethod = New-Object Mono.Cecil.MethodReference('ResolveMethod', $methodBaseType, $moduleType)
$resolveMethod.HasThis = $true
$resolveMethod.Parameters.Add((New-Object Mono.Cecil.ParameterDefinition($module.TypeSystem.Int32)))

$converted = 0
foreach ($patchType in $patchTypes) {
    $old = $patchType.CustomAttributes | Where-Object {
        $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' -and
        $_.ConstructorArguments.Count -eq 3 -and
        $_.ConstructorArguments[0].Type.FullName -eq 'System.String'
    } | Select-Object -First 1
    if ($null -eq $old) { continue }

    $targetName = [string]$old.ConstructorArguments[0].Value
    [Mono.Cecil.TypeDefinition]$targetType = $null
    if (-not $typeMap.TryGetValue($targetName, [ref]$targetType)) { throw "Target type not found: $targetName" }

    $methodName = $old.ConstructorArguments[1].Value
    $methodKind = [int]$old.ConstructorArguments[2].Value
    if ($methodKind -eq 3) { $candidates = @($targetType.Methods | Where-Object Name -eq '.ctor') }
    elseif ($methodKind -eq 4) { $candidates = @($targetType.Methods | Where-Object Name -eq '.cctor') }
    else { $candidates = @($targetType.Methods | Where-Object Name -eq $methodName) }

    $parameterPatch = $patchType.CustomAttributes | Where-Object {
        $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' -and
        $_.ConstructorArguments.Count -eq 1 -and
        $_.ConstructorArguments[0].Type.FullName -eq 'System.Type[]'
    } | Select-Object -First 1
    if ($null -ne $parameterPatch) {
        $parameterNames = @($parameterPatch.ConstructorArguments[0].Value | ForEach-Object { $_.Value.FullName })
        $candidates = @($candidates | Where-Object {
            $actual = @($_.Parameters | ForEach-Object { $_.ParameterType.FullName })
            $actual.Count -eq $parameterNames.Count -and -not (Compare-Object $actual $parameterNames -SyncWindow 0)
        })
    }
    if ($candidates.Count -ne 1) { throw "Target method is not unique: $targetName::$methodName ($($candidates.Count) candidates)" }

    foreach ($attribute in @($patchType.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' })) {
        [void]$patchType.CustomAttributes.Remove($attribute)
    }
    $patchType.CustomAttributes.Add((New-Object Mono.Cecil.CustomAttribute($emptyPatchCtor)))

    $methodAttributes = [Mono.Cecil.MethodAttributes](([int][Mono.Cecil.MethodAttributes]::Private) -bor ([int][Mono.Cecil.MethodAttributes]::Static) -bor ([int][Mono.Cecil.MethodAttributes]::HideBySig))
    $targetMethod = New-Object Mono.Cecil.MethodDefinition -ArgumentList @('TargetMethod', $methodAttributes, $methodBaseType)
    $targetMethod.CustomAttributes.Add((New-Object Mono.Cecil.CustomAttribute($targetMethodCtor)))
    $il = $targetMethod.Body.GetILProcessor()
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ldtoken, $module.ImportReference($targetType)))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Call, $getTypeFromHandle))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Callvirt, $getModule))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ldc_I4, $candidates[0].MetadataToken.ToInt32()))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Callvirt, $resolveMethod))
    $il.Append($il.Create([Mono.Cecil.Cil.OpCodes]::Ret))
    $patchType.Methods.Add($targetMethod)
    $converted++
}
$refreshType = $module.Types | Where-Object Name -eq 'GeneratedManagedRefresh' | Select-Object -First 1
$refreshPrepare = $refreshType.Methods | Where-Object Name -eq 'Prepare' | Select-Object -First 1
$refreshCalls = @($refreshPrepare.Body.Instructions | Where-Object {
    $null -ne $_.Operand -and $_.Operand.ToString().Contains('ModReflect::Target(System.String,System.String,System.Type[])')
})
$refreshConverted = 0
foreach ($call in $refreshCalls) {
    $methodNameInstruction = $call.Previous.Previous
    $typeNameInstruction = $methodNameInstruction.Previous
    $targetName = [string]$typeNameInstruction.Operand
    $refreshMethodName = [string]$methodNameInstruction.Operand
    [Mono.Cecil.TypeDefinition]$refreshTargetType = $null
    if (-not $typeMap.TryGetValue($targetName, [ref]$refreshTargetType)) { throw "Refresh target type not found: $targetName" }
    $refreshCandidates = @($refreshTargetType.Methods | Where-Object {
        $_.Name -eq $refreshMethodName -and $_.Parameters.Count -eq 0
    })
    if ($refreshCandidates.Count -ne 1) { throw "Refresh target method is not unique: $targetName::$refreshMethodName" }

    $resolverMethod = New-Object Mono.Cecil.MethodDefinition -ArgumentList @("RefreshTarget_$refreshConverted", $methodAttributes, $methodBaseType)
    $resolverIl = $resolverMethod.Body.GetILProcessor()
    $resolverIl.Append($resolverIl.Create([Mono.Cecil.Cil.OpCodes]::Ldtoken, $module.ImportReference($refreshTargetType)))
    $resolverIl.Append($resolverIl.Create([Mono.Cecil.Cil.OpCodes]::Call, $getTypeFromHandle))
    $resolverIl.Append($resolverIl.Create([Mono.Cecil.Cil.OpCodes]::Callvirt, $getModule))
    $resolverIl.Append($resolverIl.Create([Mono.Cecil.Cil.OpCodes]::Ldc_I4, $refreshCandidates[0].MetadataToken.ToInt32()))
    $resolverIl.Append($resolverIl.Create([Mono.Cecil.Cil.OpCodes]::Callvirt, $resolveMethod))
    $resolverIl.Append($resolverIl.Create([Mono.Cecil.Cil.OpCodes]::Ret))
    $refreshType.Methods.Add($resolverMethod)

    foreach ($obsolete in @($typeNameInstruction, $methodNameInstruction, $call.Previous)) {
        $obsolete.OpCode = [Mono.Cecil.Cil.OpCodes]::Nop
        $obsolete.Operand = $null
    }
    $call.OpCode = [Mono.Cecil.Cil.OpCodes]::Call
    $call.Operand = $resolverMethod
    $refreshConverted++
}
if ($refreshConverted -ne 6) { throw "Converted $refreshConverted of 6 generated refresh targets." }


foreach ($typeReference in $module.GetTypeReferences()) {
    if ($typeReference.Scope -is [Mono.Cecil.AssemblyNameReference] -and $typeReference.Scope.Name -eq 'mscorlib') {
        $typeReference.Scope = $frameworkScope
    }
}
foreach ($assemblyReference in @($module.AssemblyReferences | Where-Object Name -eq 'mscorlib')) {
    [void]$module.AssemblyReferences.Remove($assemblyReference)
}

if ($converted -ne $patchTypes.Count) { throw "Converted $converted of $($patchTypes.Count) generated Harmony targets." }
$assembly.Write($OutputDll)
Write-Output "exact_method_handles=$converted"
