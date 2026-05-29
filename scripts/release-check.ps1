$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $root "FFGUITool\FFGUITool.csproj"
$manifestPath = Join-Path $root "FFGUITool\app.manifest"
$installerPath = Join-Path $root "installer\FFGUITool.iss"
$readmePaths = @(
    (Join-Path $root "README.md"),
    (Join-Path $root "README.zh-CN.md")
)
$changelogPath = Join-Path $root "CHANGELOG.md"
$iconPaths = @(
    (Join-Path $root "FFGUITool\Resources\icon.ico"),
    (Join-Path $root "FFGUITool\Resources\icon.png"),
    (Join-Path $root "FFGUITool\Resources\AppIcon.icns"),
    (Join-Path $root "FFGUITool\Resources\AppIcon.png")
)

function Assert-FileExists {
    param([string]$Name, [string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Name is missing: $Path"
    }
}

function Assert-Equal {
    param([string]$Name, [string]$Expected, [string]$Actual)

    if ($Expected -ne $Actual) {
        throw "$Name mismatch. Expected '$Expected', got '$Actual'."
    }
}

function Assert-Contains {
    param([string]$Name, [string]$Text, [string]$Pattern)

    if ($Text -notmatch [regex]::Escape($Pattern)) {
        throw "$Name does not contain '$Pattern'."
    }
}

function Assert-Matches {
    param([string]$Name, [string]$Text, [string]$Pattern)

    if ($Text -notmatch $Pattern) {
        throw "$Name does not match '$Pattern'."
    }
}

Assert-FileExists "Project file" $projectPath
Assert-FileExists "Windows manifest" $manifestPath
Assert-FileExists "Installer script" $installerPath
Assert-FileExists "CHANGELOG" $changelogPath

foreach ($readmePath in $readmePaths) {
    Assert-FileExists "README" $readmePath
}

foreach ($iconPath in $iconPaths) {
    Assert-FileExists "Icon file" $iconPath
}

$project = [xml](Get-Content -LiteralPath $projectPath)
$propertyGroups = @($project.Project.PropertyGroup)
$version = $propertyGroups | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1
$assemblyVersion = $propertyGroups | ForEach-Object { $_.AssemblyVersion } | Where-Object { $_ } | Select-Object -First 1
$fileVersion = $propertyGroups | ForEach-Object { $_.FileVersion } | Where-Object { $_ } | Select-Object -First 1
$informationalVersion = $propertyGroups | ForEach-Object { $_.InformationalVersion } | Where-Object { $_ } | Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($version)) {
    throw "Missing Version in $projectPath."
}

Assert-Matches "Project Version" $version '^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$'
Assert-Equal "AssemblyVersion" "$version.0" $assemblyVersion
Assert-Equal "FileVersion" "$version.0" $fileVersion
Assert-Equal "InformationalVersion" $version $informationalVersion

$manifestText = Get-Content -LiteralPath $manifestPath -Raw
$installerText = Get-Content -LiteralPath $installerPath -Raw
$changelogText = Get-Content -LiteralPath $changelogPath -Raw

Assert-Contains "app.manifest" $manifestText "version=""$version.0"""
Assert-Contains "installer fallback version" $installerText "#define MyAppVersion ""$version"""
Assert-Contains "CHANGELOG current heading" $changelogText "## v$version"

$packagePattern = 'FFGUITool-v(?:x\.x\.x|<version>|\d+\.\d+\.\d+(?:[-.][0-9A-Za-z.-]+)?)-<platform>-(?:Portable|Installer)\.(?:zip|exe|dmg)'
foreach ($readmePath in $readmePaths) {
    $readmeText = Get-Content -LiteralPath $readmePath -Raw
    $readmeName = Split-Path -Leaf $readmePath
    Assert-Contains "$readmeName current portable example" $readmeText "FFGUITool-v$version-<platform>-Portable.zip"
    Assert-Matches "$readmeName package format" $readmeText $packagePattern
}

Write-Host "Release check passed for version $version."
