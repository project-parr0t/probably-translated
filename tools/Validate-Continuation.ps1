[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameManagedDir,
    [Parameter(Mandatory = $true)][string]$Parr0tRoot,
    [string]$WorkshopRoot,
    [string]$Cecil,
    [string]$DotNet = "dotnet",
    [string]$Python = "python",
    [switch]$VerifyGeneratedBindings
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$state = Get-Content -LiteralPath (Join-Path $root "parr0t-continuation-state.json") -Raw -Encoding UTF8 | ConvertFrom-Json

if (-not (Test-Path -LiteralPath $GameManagedDir -PathType Container)) { throw "GameManagedDir does not exist: $GameManagedDir" }
if (-not (Test-Path -LiteralPath $Parr0tRoot -PathType Container)) { throw "Parr0tRoot does not exist: $Parr0tRoot" }
$pythonVersion = (& $Python -c "import sys; print('.'.join(map(str, sys.version_info[:3])))").Trim()
if ($LASTEXITCODE -ne 0 -or [Version]$pythonVersion -lt [Version]"3.11") {
    throw "Python 3.11 or newer is required by Parr0t; actual=$pythonVersion"
}
$parr0tCommit = (& git -C $Parr0tRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $parr0tCommit -ne $state.parr0t.commit) {
    throw "Parr0t checkout must be pinned to $($state.parr0t.commit); actual=$parr0tCommit"
}

if ($VerifyGeneratedBindings) {
    if ([string]::IsNullOrWhiteSpace($WorkshopRoot) -or -not (Test-Path -LiteralPath $WorkshopRoot -PathType Container)) {
        throw "A complete WorkshopRoot is required with -VerifyGeneratedBindings"
    }
    if ([string]::IsNullOrWhiteSpace($Cecil)) {
        $cache = Join-Path ([IO.Path]::GetTempPath()) "probably-translated-tools/mono.cecil.0.11.6"
        $package = Join-Path $cache "mono.cecil.0.11.6.nupkg"
        $Cecil = Join-Path $cache "lib/net40/Mono.Cecil.dll"
        if (-not (Test-Path -LiteralPath $Cecil -PathType Leaf)) {
            New-Item -ItemType Directory -Path $cache -Force | Out-Null
            Invoke-WebRequest -UseBasicParsing -Uri "https://www.nuget.org/api/v2/package/Mono.Cecil/0.11.6" -OutFile $package
            $actualPackageHash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($actualPackageHash -ne $state.dependencies.mono_cecil_nupkg_sha256) {
                Remove-Item -LiteralPath $package -Force
                throw "Mono.Cecil package digest mismatch"
            }
            Expand-Archive -LiteralPath $package -DestinationPath $cache -Force
        }
    }
    if (-not (Test-Path -LiteralPath $Cecil -PathType Leaf)) { throw "Mono.Cecil.dll does not exist: $Cecil" }
}

& $Python (Join-Path $root "tools/validate_continuation.py") --root $root --parr0t-root $Parr0tRoot
if ($LASTEXITCODE -ne 0) { throw "Continuation contract validation failed" }

$temp = Join-Path ([IO.Path]::GetTempPath()) ("probably-translated-verify-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    if ($VerifyGeneratedBindings) {
        $generated = Join-Path $temp "GeneratedManagedBindings.cs"
        $generatorOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "tools/Expand-ManagedRoutes.ps1") `
            -ManagedRuntime (Join-Path $root "managed-ko-runtime.json") `
            -WorkshopRoot $WorkshopRoot `
            -GameManagedDir $GameManagedDir `
            -Cecil $Cecil `
            -Output $generated
        if ($LASTEXITCODE -ne 0) { throw "Managed binding generation failed" }
        $generatorLine = ($generatorOutput | Select-Object -Last 1).ToString()
        $expectedLine = "routes=$($state.contracts.generated_routes) bindings=$($state.contracts.generated_bindings) static_patches=$($state.contracts.generated_static_patches) unsupported=$($state.contracts.generated_unsupported) refresh=$($state.contracts.generated_refresh)"
        if ($generatorLine -ne $expectedLine) { throw "Generated binding contract mismatch: $generatorLine" }
        $trackedGenerated = Join-Path $root "src/ProbablyStolenKo.Localization/GeneratedManagedBindings.cs"
        $generatedText = (Get-Content -LiteralPath $generated -Raw -Encoding UTF8).Replace("`r`n", "`n")
        $trackedText = (Get-Content -LiteralPath $trackedGenerated -Raw -Encoding UTF8).Replace("`r`n", "`n")
        if ($generatedText -cne $trackedText) { throw "Tracked generated bindings are stale" }
    }

    $buildOutput = Join-Path $temp "build"
    & $DotNet build (Join-Path $root "src/ProbablyStolenKo.Localization/ProbablyStolenKo.Localization.csproj") `
        --configuration Release `
        --output $buildOutput `
        "-p:GameManagedDir=$GameManagedDir"
    if ($LASTEXITCODE -ne 0) { throw "Release build failed" }
} finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Output "continuation-build=ok release=$($state.release.version) commit=$($state.release.commit)"
