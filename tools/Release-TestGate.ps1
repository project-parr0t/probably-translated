[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Install", "Remove", "Stage", "Status")]
    [string]$Action,
    [Parameter(Mandatory = $true)][string]$GameRoot,
    [string]$StageDir,
    [string]$WorkshopRoot,
    [string]$Python = "python"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$modsRoot = Join-Path $GameRoot "Mods"
$installRoot = Join-Path $modsRoot "ProbablyTranslated-Test"
$markerName = ".probably-translated-test-install.json"
$markerPath = Join-Path $installRoot $markerName
$receiptRoot = Join-Path $env:LOCALAPPDATA "ProbablyTranslated"
$receiptPath = Join-Path $receiptRoot "test-receipt.json"
$topLevelFiles = @(
    "manifest.xml",
    "ProbablyStolenKo.Localization.dll",
    "managed-ko-runtime.json",
    "workshop-ko-runtime.json",
    "parr0t-release-checkpoint.json",
    "preview.png"
)
$treeNames = @("workshop-overlays", "va11-recipes", "loading-ui")

function Get-PackageFiles([string]$base) {
    $files = New-Object System.Collections.Generic.List[object]
    foreach ($relative in $topLevelFiles) {
        $path = Join-Path $base $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing package file: $path" }
        $files.Add([pscustomobject]@{ Relative = $relative; Path = $path })
    }
    foreach ($treeName in $treeNames) {
        $tree = Join-Path $base $treeName
        if (-not (Test-Path -LiteralPath $tree -PathType Container)) { throw "Missing package tree: $tree" }
        foreach ($file in Get-ChildItem -LiteralPath $tree -Recurse -File) {
            $relative = $file.FullName.Substring($base.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar).Replace("\", "/")
            $files.Add([pscustomobject]@{ Relative = $relative; Path = $file.FullName })
        }
    }
    return @($files | Sort-Object Relative)
}

function Get-PackageDigest([string]$base) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        foreach ($file in Get-PackageFiles $base) {
            $name = [Text.Encoding]::UTF8.GetBytes($file.Relative)
            [void]$sha.TransformBlock($name, 0, $name.Length, $null, 0)
            [void]$sha.TransformBlock([byte[]](0), 0, 1, $null, 0)
            $fileHash = [Text.Encoding]::ASCII.GetBytes((Get-FileHash -LiteralPath $file.Path -Algorithm SHA256).Hash.ToLowerInvariant())
            [void]$sha.TransformBlock($fileHash, 0, $fileHash.Length, $null, 0)
            [void]$sha.TransformBlock([byte[]](10), 0, 1, $null, 0)
        }
        [void]$sha.TransformFinalBlock([byte[]]::new(0), 0, 0)
        return ([BitConverter]::ToString($sha.Hash)).Replace("-", "").ToLowerInvariant()
    } finally {
        $sha.Dispose()
    }
}

function Get-Version([string]$base) {
    [xml]$manifest = Get-Content -LiteralPath (Join-Path $base "manifest.xml") -Raw -Encoding UTF8
    return [string]$manifest.Manifest.ModVersion
}

function Read-Receipt {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { return $null }
    return Get-Content -LiteralPath $receiptPath -Raw -Encoding UTF8 | ConvertFrom-Json
}

function Write-Receipt([string]$status, [string]$digest, [string]$version) {
    New-Item -ItemType Directory -Path $receiptRoot -Force | Out-Null
    [ordered]@{
        schema = "probably-translated.test-gate/v1"
        status = $status
        digest = $digest
        version = $version
        install_path = $installRoot
        updated_utc = [DateTime]::UtcNow.ToString("o")
    } | ConvertTo-Json | Set-Content -LiteralPath $receiptPath -Encoding UTF8
}

function Assert-OwnedInstall {
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "Refusing to modify an unmarked Mods directory: $installRoot"
    }
}

function Assert-GameStopped {
    $gameExecutable = [IO.Path]::GetFullPath((Join-Path $GameRoot "Probably Stolen.exe"))
    foreach ($process in Get-Process -ErrorAction SilentlyContinue) {
        try {
            if ($process.Path -and [IO.Path]::GetFullPath($process.Path) -eq $gameExecutable) {
                throw "Close Probably Stolen before installing or removing the local test package"
            }
        } catch [System.ComponentModel.Win32Exception] {
            continue
        }
    }
}

if (-not (Test-Path -LiteralPath $modsRoot -PathType Container)) { throw "Mods directory does not exist: $modsRoot" }

switch ($Action) {
    "Install" {
        Assert-GameStopped
        $digest = Get-PackageDigest $root
        $version = Get-Version $root
        if (Test-Path -LiteralPath $installRoot) {
            Assert-OwnedInstall
            Remove-Item -LiteralPath $installRoot -Recurse -Force
        }
        $temporary = Join-Path $modsRoot (".probably-translated-test-" + [Guid]::NewGuid().ToString("N"))
        try {
            New-Item -ItemType Directory -Path $temporary | Out-Null
            foreach ($file in Get-PackageFiles $root) {
                $destination = Join-Path $temporary $file.Relative
                New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
                Copy-Item -LiteralPath $file.Path -Destination $destination -Force
            }
            [ordered]@{ schema = "probably-translated.test-install/v1"; digest = $digest; version = $version } |
                ConvertTo-Json | Set-Content -LiteralPath (Join-Path $temporary $markerName) -Encoding UTF8
            Move-Item -LiteralPath $temporary -Destination $installRoot
        } finally {
            if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
        }
        if ((Get-PackageDigest $installRoot) -ne $digest) {
            Remove-Item -LiteralPath $installRoot -Recurse -Force
            throw "Installed test package digest mismatch"
        }
        Write-Receipt "installed" $digest $version
        Write-Output "test-install=ok version=$version digest=$digest path=$installRoot"
    }
    "Remove" {
        Assert-GameStopped
        if (Test-Path -LiteralPath $installRoot) {
            Assert-OwnedInstall
            Remove-Item -LiteralPath $installRoot -Recurse -Force
        }
        Write-Output "test-remove=ok path=$installRoot"
    }
    "Stage" {
        if ([string]::IsNullOrWhiteSpace($WorkshopRoot)) { throw "-WorkshopRoot is required for Stage" }
        & $Python (Join-Path $root "tools/audit_workshop_coverage.py") --root $root --workshop-root $WorkshopRoot
        if ($LASTEXITCODE -ne 0) { throw "Workshop translation coverage audit failed" }
        if ([string]::IsNullOrWhiteSpace($StageDir)) { throw "-StageDir is required for Stage" }
        $receipt = Read-Receipt
        $digest = Get-PackageDigest $root
        $version = Get-Version $root
        if ($null -eq $receipt -or $receipt.schema -ne "probably-translated.test-gate/v1" -or $receipt.status -ne "installed") {
            throw "Install the current package into Mods before Workshop staging"
        }
        if ($receipt.digest -ne $digest -or $receipt.version -ne $version) {
            throw "The package changed after the local Mods test install"
        }
        if (-not (Test-Path -LiteralPath $installRoot -PathType Container)) {
            throw "Keep the exact local Mods test install until Workshop upload completes"
        }
        Assert-OwnedInstall
        if ((Get-PackageDigest $installRoot) -ne $digest) {
            throw "The local Mods test install differs from the package being staged"
        }
        if (Test-Path -LiteralPath $StageDir) { Remove-Item -LiteralPath $StageDir -Recurse -Force }
        New-Item -ItemType Directory -Path $StageDir -Force | Out-Null
        foreach ($file in Get-PackageFiles $root) {
            $destination = Join-Path $StageDir $file.Relative
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            Copy-Item -LiteralPath $file.Path -Destination $destination -Force
        }
        Write-Output "release-stage=ok version=$version digest=$digest path=$StageDir"
    }
    "Status" {
        $receipt = Read-Receipt
        if ($null -eq $receipt) {
            Write-Output "test-status=none"
        } elseif (Test-Path -LiteralPath $installRoot) {
            Assert-OwnedInstall
            $exact = (Get-PackageDigest $root) -eq (Get-PackageDigest $installRoot)
            Write-Output (
                "test-status=$($receipt.status) version=$($receipt.version) " +
                "digest=$($receipt.digest) installed=true exact=$($exact.ToString().ToLowerInvariant())")
        } else {
            Write-Output (
                "test-status=$($receipt.status) version=$($receipt.version) " +
                "digest=$($receipt.digest) installed=false")
        }
    }
}
