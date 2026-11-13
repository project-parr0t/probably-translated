[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ManagedRuntime,
    [Parameter(Mandatory = $true)][string]$WorkshopRoot,
    [Parameter(Mandatory = $true)][string]$GameManagedDir,
    [Parameter(Mandatory = $true)][string]$Cecil,
    [Parameter(Mandatory = $true)][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Web.Extensions
Add-Type -Path (Resolve-Path -LiteralPath $Cecil)

function Escape-CSharp([string]$value) {
    return $value.Replace('\', '\\').Replace('"', '\"').Replace("`r", '\r').Replace("`n", '\n')
}

function Runtime-Name($type) {
    return $type.FullName.Replace('/', '+')
}

function Open-CSharp-Type([string]$fullName) {
    $name = $fullName.Replace('/', '.')
    return [regex]::Replace($name, '`(\d+)', {
        param($match)
        $arity = [int]$match.Groups[1].Value
        return '<' + (',' * ($arity - 1)) + '>'
    })
}

function Type-Expression($type) {
    if ($type -is [Mono.Cecil.GenericParameter]) { return $null }
    if ($type -is [Mono.Cecil.ByReferenceType]) {
        $element = Type-Expression $type.ElementType
        if ($null -eq $element) { return $null }
        return '(' + $element + ').MakeByRefType()'
    }
    if ($type -is [Mono.Cecil.PointerType]) {
        $element = Type-Expression $type.ElementType
        if ($null -eq $element) { return $null }
        return '(' + $element + ').MakePointerType()'
    }
    if ($type -is [Mono.Cecil.ArrayType]) {
        $element = Type-Expression $type.ElementType
        if ($null -eq $element) { return $null }
        if ($type.Rank -eq 1) { return '(' + $element + ').MakeArrayType()' }
        return '(' + $element + ').MakeArrayType(' + $type.Rank + ')'
    }
    if ($type -is [Mono.Cecil.GenericInstanceType]) {
        $open = Type-Expression $type.ElementType
        if ($null -eq $open) { return $null }
        $arguments = New-Object System.Collections.Generic.List[string]
        foreach ($argument in $type.GenericArguments) {
            $expression = Type-Expression $argument
            if ($null -eq $expression) { return $null }
            $arguments.Add($expression)
        }
        return '(' + $open + ').MakeGenericType(new Type[] { ' + ($arguments -join ', ') + ' })'
    }
    if ($type -is [Mono.Cecil.OptionalModifierType] -or $type -is [Mono.Cecil.RequiredModifierType] -or
        $type -is [Mono.Cecil.PinnedType] -or $type -is [Mono.Cecil.SentinelType]) {
        return Type-Expression $type.ElementType
    }

    $scopeName = if ($null -ne $type.Scope) { $type.Scope.Name } else { '' }
    $fullName = $type.FullName
    if ($scopeName -eq 'mscorlib' -or $scopeName -eq 'netstandard' -or $scopeName -eq '0Harmony' -or
        $scopeName -eq 'Newtonsoft.Json' -or $scopeName.StartsWith('System', [StringComparison]::Ordinal) -or
        $scopeName.StartsWith('Unity', [StringComparison]::Ordinal) -or $scopeName.StartsWith('UnityEngine', [StringComparison]::Ordinal)) {
        return 'typeof(global::' + (Open-CSharp-Type $fullName) + ')'
    }
    if ($scopeName -eq 'Assembly-CSharp') {
        try {
            $definition = $type.Resolve()
            $visible = $null -ne $definition -and ($definition.IsPublic -or $definition.IsNestedPublic)
            $parent = if ($null -ne $definition) { $definition.DeclaringType } else { $null }
            while ($visible -and $null -ne $parent) {
                $visible = $parent.IsPublic -or $parent.IsNestedPublic
                $parent = $parent.DeclaringType
            }
            if ($visible) { return 'typeof(global::' + (Open-CSharp-Type $fullName) + ')' }
        } catch {
            # Fall through to the game-guarded runtime resolver.
        }
    }
    return 'ModReflect.FindType("' + (Escape-CSharp (Runtime-Name $type)) + '")'
}

function Add-AllTypes($type, $list) {
    $list.Add($type)
    foreach ($nested in $type.NestedTypes) { Add-AllTypes $nested $list }
}

function Route-MatchesType($routeType, [string]$typeName) {
    if ($routeType -eq '*') { return $true }
    if ($routeType.EndsWith('*', [StringComparison]::Ordinal)) {
        return $typeName.StartsWith($routeType.Substring(0, $routeType.Length - 1), [StringComparison]::Ordinal)
    }
    return [StringComparer]::Ordinal.Equals($routeType, $typeName)
}

function Route-MatchesMethod($route, $method) {
    if ($route['method'] -ne '*' -and $route['method'] -ne $method.Name) { return $false }
    $mode = [string]$route['mode']
    if ($mode -eq 'literal' -or $mode -eq 'literal_occurrence') {
        if (-not $method.HasBody) { return $false }
        $sources = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
        foreach ($entry in $route['entries'].Keys) {
            $source = [string]$entry
            if ($mode -eq 'literal_occurrence') {
                $separator = $source.LastIndexOf([char]0x1f)
                if ($separator -ge 0) {
                    $occurrence = 0
                    if ([int]::TryParse($source.Substring($separator + 1), [ref]$occurrence)) {
                        $source = $source.Substring(0, $separator)
                    }
                }
            }
            [void]$sources.Add($source)
        }
        foreach ($instruction in $method.Body.Instructions) {
            if ($instruction.OpCode.Code -eq [Mono.Cecil.Cil.Code]::Ldstr -and
                $sources.Contains([string]$instruction.Operand)) { return $true }
        }
        return $false
    }
    if ($mode -eq 'result_string_array') { return $method.ReturnType.FullName -eq 'System.String[]' }
    return $method.ReturnType.FullName -eq 'System.String'
}

$serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
$serializer.MaxJsonLength = [int]::MaxValue
$root = $serializer.DeserializeObject([IO.File]::ReadAllText((Resolve-Path -LiteralPath $ManagedRuntime), [Text.Encoding]::UTF8))
$fingerprint = [string]$root['inventory_fingerprint']
$targets = New-Object System.Collections.Generic.List[object]
$bindingId = 0
$routeCount = 0
$unsupportedCount = 0
$staticPatchCount = 0
$refreshCount = 0

$assemblyResolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$assemblyResolver.AddSearchDirectory((Resolve-Path -LiteralPath $GameManagedDir))
$readerParameters = New-Object Mono.Cecil.ReaderParameters
$readerParameters.AssemblyResolver = $assemblyResolver

foreach ($targetEntry in $root['targets'].GetEnumerator()) {
    $modId = [string]$targetEntry.Key
    $target = $targetEntry.Value
    $routes = @($target['routes'])
    $refresh = @($target['refresh'])
    $routeCount += $routes.Count
    if ($routes.Count -eq 0 -and $refresh.Count -eq 0) { continue }

    $itemRoot = Join-Path (Resolve-Path -LiteralPath $WorkshopRoot) ([string]$target['workshop_item_id'])
    $matches = New-Object System.Collections.Generic.List[object]
    foreach ($file in Get-ChildItem -LiteralPath $itemRoot -Recurse -File -Filter '*.dll') {
        try {
            $candidate = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($file.FullName, $readerParameters)
            if ($candidate.Name.Name -eq [string]$target['assembly_name']) { $matches.Add($candidate) }
            else { $candidate.Dispose() }
        } catch { }
    }
    if ($matches.Count -ne 1) { throw "Expected one assembly named '$($target['assembly_name'])' under Workshop item $($target['workshop_item_id']); found $($matches.Count)." }
    $assembly = $matches[0]
    try {
        $types = New-Object System.Collections.Generic.List[object]
        foreach ($type in $assembly.MainModule.Types) { Add-AllTypes $type $types }
        $bindings = New-Object System.Collections.Generic.List[object]
        for ($routeIndex = 0; $routeIndex -lt $routes.Count; $routeIndex++) {
            $route = $routes[$routeIndex]
            foreach ($type in $types) {
                $typeName = Runtime-Name $type
                if (-not (Route-MatchesType ([string]$route['type']) $typeName)) { continue }
                foreach ($method in $type.Methods) {
                    if (-not (Route-MatchesMethod $route $method)) { continue }
                    $bindingId++
                    $kind = 'Normal'
                    $issue = $null
                    $unqualifiedTarget = $false
                    $expressions = New-Object System.Collections.Generic.List[string]
                    foreach ($parameter in $method.Parameters) {
                        $expression = Type-Expression $parameter.ParameterType
                        if ($null -eq $expression) {
                            if ($parameter.ParameterType.FullName.StartsWith('System.Reflection.', [StringComparison]::Ordinal)) {
                                $issue = 'reflection metadata parameter types are not materialized by the capability-free runtime'
                            } else {
                                $issue = 'generic parameter signatures cannot be represented by ModReflect.Target Type[]'
                            }
                        }
                        else { $expressions.Add($expression) }
                    }
                    $sameNameCount = @($type.Methods | Where-Object Name -eq $method.Name).Count
                    $staticSignature = $null -eq $issue
                    foreach ($expression in $expressions) {
                        if (-not $expression.StartsWith('typeof(global::', [StringComparison]::Ordinal) -or
                            -not $expression.EndsWith(')', [StringComparison]::Ordinal)) {
                            $staticSignature = $false
                        }
                    }
                    if ($method.Name -eq '.cctor') {
                        $kind = 'StaticPatch'
                        $staticPatchCount++
                    } elseif ($method.Name -eq '.ctor') {
                        if ($sameNameCount -eq 1 -or $staticSignature) {
                            $kind = 'StaticPatch'
                            $staticPatchCount++
                        } else {
                            $kind = 'Unsupported'
                            $issue = 'Harmony constructor patch cannot select an overloaded external signature'
                            $unsupportedCount++
                        }
                    } elseif ($sameNameCount -eq 1 -or $staticSignature) {
                        $kind = 'StaticPatch'
                        $staticPatchCount++
                    } elseif ($null -ne $issue) {
                        $kind = 'Unsupported'
                        $unsupportedCount++
                    }
                    $parameterNames = @($method.Parameters | ForEach-Object { Runtime-Name $_.ParameterType }) -join ', '
                    $bindings.Add([pscustomobject]@{
                        Id = $bindingId; RouteIndex = $routeIndex; TypeName = $typeName; MethodName = [string]$method.Name
                        Kind = $kind; Expressions = @($expressions); Signature = $parameterNames; Issue = $issue
                        UnqualifiedTarget = $unqualifiedTarget
                        UseStaticSignature = $kind -eq 'StaticPatch' -and $sameNameCount -ne 1 -and $method.Name -ne '.cctor'
                    })
                }
            }
        }
        $refreshBindings = New-Object System.Collections.Generic.List[object]
        for ($refreshIndex = 0; $refreshIndex -lt $refresh.Count; $refreshIndex++) {
            $action = $refresh[$refreshIndex]
            $type = $types | Where-Object { (Runtime-Name $_) -eq [string]$action['type'] }
            $methods = @($type.Methods | Where-Object { $_.Name -eq [string]$action['method'] -and $_.IsStatic -and $_.Parameters.Count -eq 0 })
            if ($methods.Count -ne 1) { throw "Refresh $modId/$refreshIndex is not one exact static parameterless method." }
            $refreshCount++
            $refreshBindings.Add([pscustomobject]@{ Index = $refreshIndex; TypeName = [string]$action['type']; MethodName = [string]$action['method']; Stub = $refreshCount })
        }
        $targets.Add([pscustomobject]@{ ModId = $modId; Routes = $routes; Bindings = $bindings.ToArray(); Refresh = $refreshBindings.ToArray() })
    } finally { $assembly.Dispose() }
}

$builder = New-Object Text.StringBuilder
function Line([string]$text = '') { [void]$builder.AppendLine($text) }
Line '// <auto-generated />'
Line '// Generated by tools/Expand-ManagedRoutes.ps1. Contains metadata only; no game or mod text.'
Line 'using System;'
Line 'using System.Collections.Generic;'
Line 'using System.Reflection;'
Line 'using System.Reflection.Emit;'
Line 'using HarmonyLib;'
Line ''
Line 'namespace ProbablyStolenKo'
Line '{'
Line '    internal enum ManagedBindingKind { Normal, StaticPatch, Unsupported }'
Line '    internal sealed class ManagedExactBinding'
Line '    {'
Line '        internal ManagedExactBinding(int id, int routeIndex, string typeName, string methodName, string signature, Type[] parameterTypes, Type patchType, string issue)'
Line '        { Id = id; RouteIndex = routeIndex; TypeName = typeName; MethodName = methodName; Signature = signature; ParameterTypes = parameterTypes; PatchType = patchType; Issue = issue; }'
Line '        internal int Id { get; }'
Line '        internal int RouteIndex { get; }'
Line '        internal string TypeName { get; }'
Line '        internal string MethodName { get; }'
Line '        internal string Signature { get; }'
Line '        internal Type[] ParameterTypes { get; }'
Line '        internal Type PatchType { get; }'
Line '        internal string Issue { get; }'
Line '        internal ManagedBindingKind Kind => Issue != null ? ManagedBindingKind.Unsupported : PatchType != null ? ManagedBindingKind.StaticPatch : ManagedBindingKind.Normal;'
Line '    }'
Line ''
Line '    internal static class GeneratedManagedBindings'
Line '    {'
Line ('        internal const string InventoryFingerprint = "' + (Escape-CSharp $fingerprint) + '";')
Line "        internal const int RouteCount = $routeCount;"
Line "        internal const int BindingCount = $bindingId;"
Line "        internal const int StaticPatchBindingCount = $staticPatchCount;"
Line "        internal const int UnsupportedBindingCount = $unsupportedCount;"
Line "        internal const int RefreshBindingCount = $refreshCount;"
Line '        internal static IReadOnlyList<ManagedExactBinding> Create(string modId)'
Line '        {'
Line '            var bindings = new List<ManagedExactBinding>();'
Line '            switch (modId)'
Line '            {'
for ($targetNumber = 0; $targetNumber -lt $targets.Count; $targetNumber++) {
    $target = $targets[$targetNumber]
    Line ('                case "' + (Escape-CSharp $target.ModId) + '":')
    $chunks = [Math]::Ceiling($target.Bindings.Count / 125.0)
    for ($chunk = 0; $chunk -lt $chunks; $chunk++) { Line "                    Add_${targetNumber}_${chunk}(bindings);" }
    Line '                    break;'
}
Line '            }'
Line '            return bindings;'
Line '        }'
for ($targetNumber = 0; $targetNumber -lt $targets.Count; $targetNumber++) {
    $target = $targets[$targetNumber]
    $chunks = [Math]::Ceiling($target.Bindings.Count / 125.0)
    for ($chunk = 0; $chunk -lt $chunks; $chunk++) {
        Line ''
        Line "        private static void Add_${targetNumber}_${chunk}(ICollection<ManagedExactBinding> bindings)"
        Line '        {'
        $start = $chunk * 125
        $end = [Math]::Min($start + 125, $target.Bindings.Count)
        for ($index = $start; $index -lt $end; $index++) {
            $binding = $target.Bindings[$index]
            $parameters = if ($binding.Kind -eq 'StaticPatch' -or $binding.UnqualifiedTarget) { 'null' } elseif ($binding.Expressions.Count -eq 0) { 'Type.EmptyTypes' } else { 'new Type[] { ' + ($binding.Expressions -join ', ') + ' }' }
            $patchType = if ($binding.Kind -eq 'StaticPatch') { "typeof(GeneratedManagedPatch_$($binding.Id))" } else { 'null' }
            $issue = if ($null -eq $binding.Issue) { 'null' } else { '"' + (Escape-CSharp $binding.Issue) + '"' }
            Line ('            bindings.Add(new ManagedExactBinding(' + $binding.Id + ', ' + $binding.RouteIndex + ', "' + (Escape-CSharp $binding.TypeName) + '", "' + (Escape-CSharp $binding.MethodName) + '", "' + (Escape-CSharp $binding.Signature) + '", ' + $parameters + ', ' + $patchType + ', ' + $issue + '));')
        }
        Line '        }'
    }
}
Line '    }'
Line ''
foreach ($target in $targets) {
    foreach ($binding in $target.Bindings) {
        if ($binding.Kind -ne 'StaticPatch') { continue }
        if ($binding.MethodName -eq '.cctor') {
            Line ('    [HarmonyPatch("' + (Escape-CSharp $binding.TypeName) + '", null, MethodType.StaticConstructor)]')
        } elseif ($binding.MethodName -eq '.ctor') {
            Line ('    [HarmonyPatch("' + (Escape-CSharp $binding.TypeName) + '", null, MethodType.Constructor)]')
        } else {
            Line ('    [HarmonyPatch("' + (Escape-CSharp $binding.TypeName) + '", "' + (Escape-CSharp $binding.MethodName) + '", MethodType.Normal)]')
        }
        if ($binding.UseStaticSignature) {
            Line ('    [HarmonyPatch(new Type[] { ' + ($binding.Expressions -join ', ') + ' })]')
        }
        Line "    internal static class GeneratedManagedPatch_$($binding.Id)"
        Line '    {'
        Line '        [HarmonyPrepare]'
        Line "        internal static bool Prepare() => ManagedLiteralOverlay.PrepareBinding($($binding.Id));"
        $mode = [string]$target.Routes[$binding.RouteIndex]['mode']
        if ($mode -eq 'literal' -or $mode -eq 'literal_occurrence') {
            Line '        [HarmonyTranspiler]'
            Line "        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => ManagedLiteralOverlay.TranspileBinding(instructions, $($binding.Id));"
        } elseif ($mode -eq 'result_by_argument') {
            Line '        [HarmonyPostfix]'
            Line "        internal static void Postfix(object[] __args, ref string __result) => ManagedLiteralOverlay.PostfixByArgumentBinding(__args, ref __result, $($binding.Id));"
        } elseif ($mode -eq 'result_by_arguments') {
            Line '        [HarmonyPostfix]'
            Line "        internal static void Postfix(object[] __args, ref string __result) => ManagedLiteralOverlay.PostfixByArgumentsBinding(__args, ref __result, $($binding.Id));"
        } elseif ($mode -eq 'result_string_array') {
            Line '        [HarmonyPostfix]'
            Line "        internal static void Postfix(ref string[] __result) => ManagedLiteralOverlay.PostfixStringArrayBinding(ref __result, $($binding.Id));"
        } else {
            Line '        [HarmonyPostfix]'
            Line "        internal static void Postfix(ref string __result) => ManagedLiteralOverlay.PostfixStringBinding(ref __result, $($binding.Id));"
        }
        Line '    }'
}
}
Line ''
Line '    internal static class GeneratedManagedRefresh'
Line '    {'
Line '        internal static void Prepare(Harmony harmony, string modId)'
Line '        {'
Line '            switch (modId)'
Line '            {'
foreach ($target in $targets) {
    if ($target.Refresh.Count -eq 0) { continue }
    Line ('                case "' + (Escape-CSharp $target.ModId) + '":')
    foreach ($refreshBinding in $target.Refresh) {
        Line ('                    harmony.CreateReversePatcher(ModReflect.Target("' + (Escape-CSharp $refreshBinding.TypeName) + '", "' + (Escape-CSharp $refreshBinding.MethodName) + '", Type.EmptyTypes), new HarmonyMethod(typeof(GeneratedManagedRefresh), nameof(Refresh_' + $refreshBinding.Stub + '))).Patch();')
    }
    Line '                    break;'
}
Line '            }'
Line '        }'
Line '        internal static void Invoke(string modId)'
Line '        {'
Line '            switch (modId)'
Line '            {'
foreach ($target in $targets) {
    if ($target.Refresh.Count -eq 0) { continue }
    Line ('                case "' + (Escape-CSharp $target.ModId) + '":')
    foreach ($refreshBinding in $target.Refresh) { Line "                    Refresh_$($refreshBinding.Stub)();" }
    Line '                    break;'
}
Line '            }'
Line '        }'
foreach ($target in $targets) {
    foreach ($refreshBinding in $target.Refresh) { Line ('        private static void Refresh_' + $refreshBinding.Stub + '() => throw new InvalidOperationException("Reverse patch was not prepared.");') }
}
Line '    }'
Line '}'

$outputPath = [IO.Path]::GetFullPath($Output)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($outputPath)) | Out-Null
[IO.File]::WriteAllText($outputPath, $builder.ToString(), (New-Object Text.UTF8Encoding($false)))
Write-Output "routes=$routeCount bindings=$bindingId static_patches=$staticPatchCount unsupported=$unsupportedCount refresh=$refreshCount"
