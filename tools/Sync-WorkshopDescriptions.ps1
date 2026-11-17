[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$DescriptionPath,
    [Parameter(Mandatory = $true)][string]$ContentPath,
    [string]$DotNet = "dotnet",
    [uint32]$AppId = 4348910,
    [uint64]$ItemId = 3838981739,
    [switch]$Submit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "steam-workshop-cli/src/SteamWorkshopCli/SteamWorkshopCli.csproj"
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "Initialize repository submodules before synchronizing Workshop descriptions."
}

$description = (Resolve-Path -LiteralPath $DescriptionPath).Path
$content = (Resolve-Path -LiteralPath $ContentPath).Path
if (-not (Test-Path -LiteralPath $content -PathType Container)) {
    throw "Workshop content directory does not exist: $content"
}

$languages = @(
    "arabic", "bulgarian", "schinese", "tchinese", "czech", "danish", "dutch", "english",
    "finnish", "french", "german", "greek", "hungarian", "indonesian", "italian", "japanese",
    "koreana", "malay", "norwegian", "polish", "portuguese", "brazilian", "romanian", "russian",
    "spanish", "latam", "swedish", "thai", "turkish", "ukrainian", "vietnamese"
)

$temp = Join-Path ([IO.Path]::GetTempPath()) ("probably-translated-description-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    $manifestPath = Join-Path $temp "manifest.json"
    [ordered]@{
        appId = $AppId
        publishedFileId = $ItemId
        contentFolder = $content
        visibility = "public"
        defaultLanguage = "english"
        languages = @($languages | ForEach-Object {
            [ordered]@{ lang = $_; descriptionFile = $description }
        })
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

    $arguments = @(
        "run", "--configuration", "Release", "--project", $project, "--",
        "publish", "--manifest", $manifestPath
    )
    if (-not $Submit) { $arguments += "--dry-run" }

    & $DotNet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Workshop description synchronization failed" }
} finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
