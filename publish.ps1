param(
    [switch]$Windows,

    [switch]$MacOS,

    [switch]$All,

    [ValidateSet("zip", "7z")]
    [string]$Archive = "zip",

    [string]$Configuration = "Release",

    [bool]$SelfContained = $true,

    [switch]$Installer
)

$ErrorActionPreference = "Stop"

$ProjectPath = Join-Path $PSScriptRoot "FFGUITool\FFGUITool.csproj"
$PublishRoot = Join-Path $PSScriptRoot "FFGUITool\bin\publish"
$ArchiveRoot = Join-Path $PublishRoot "archives"
$InstallerScript = Join-Path $PSScriptRoot "installer\FFGUITool.iss"
$InstallerOutput = Join-Path $PublishRoot "installer"
$ProjectXml = [xml](Get-Content $ProjectPath)

function Get-ProjectVersion {
    $propertyGroups = @($ProjectXml.Project.PropertyGroup)
    $version = $propertyGroups |
        ForEach-Object { $_.Version } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 1

    if (-not [string]::IsNullOrWhiteSpace($version)) {
        return $version
    }

    if ([string]::IsNullOrWhiteSpace($version)) {
        $version = $propertyGroups |
            ForEach-Object { $_.InformationalVersion } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Select-Object -First 1
    }

    if (-not [string]::IsNullOrWhiteSpace($version)) {
        return $version
    }

    if ([string]::IsNullOrWhiteSpace($version)) {
        $version = $propertyGroups |
            ForEach-Object { $_.AssemblyVersion } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Select-Object -First 1
    }

    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "Could not find Version, InformationalVersion, or AssemblyVersion in $ProjectPath."
    }

    return ($version -replace '\.0$', '')
}

$PackageVersion = Get-ProjectVersion

$RuntimeMap = [ordered]@{
    "win-x64" = "FFGUITool-win-x64"
    "win-x86" = "FFGUITool-win-x86"
    "win-arm64" = "FFGUITool-win-arm64"
    "osx-x64" = "FFGUITool-osx-x64"
    "osx-arm64" = "FFGUITool-osx-arm64"
}

function Get-PackagePlatformName {
    param(
        [string]$RuntimeId
    )

    switch ($RuntimeId) {
        "win-x64" { "windows-x64" }
        "win-x86" { "windows-x86" }
        "win-arm64" { "windows-arm64" }
        "osx-x64" { "macos-intel" }
        "osx-arm64" { "macos-arm64" }
        default { $RuntimeId }
    }
}

function Get-CurrentPlatformGroup {
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        return "windows"
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) {
        return "macos"
    }

    throw "Unsupported OS. Use -Windows, -MacOS, or -All explicitly."
}

function New-PublishArchive {
    param(
        [string]$SourcePath,
        [string]$ArchiveName
    )

    New-Item -ItemType Directory -Force -Path $ArchiveRoot | Out-Null
    $archivePath = Join-Path $ArchiveRoot "$ArchiveName.$Archive"

    if (Test-Path $archivePath) {
        Remove-Item $archivePath -Force
    }

    if ($Archive -eq "zip") {
        Compress-Archive -Path (Join-Path $SourcePath "*") -DestinationPath $archivePath -Force
        return
    }

    $sevenZip = Get-Command 7z -ErrorAction SilentlyContinue
    if ($null -eq $sevenZip) {
        throw "7z was not found in PATH. Install 7-Zip or use -Archive zip."
    }

    Push-Location $SourcePath
    try {
        & $sevenZip.Source a -t7z $archivePath ".\*" | Out-Host
    }
    finally {
        Pop-Location
    }
}

function New-InnoInstaller {
    param(
        [string]$SourcePath,
        [string]$RuntimeId
    )

    $iscc = Get-Command ISCC -ErrorAction SilentlyContinue
    $isccPath = if ($null -ne $iscc) { $iscc.Source } else { $null }
    if (-not $isccPath) {
        $defaultIsccPaths = @(
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
            "F:\Program Files (x86)\Inno Setup 6\ISCC.exe",
            "F:\Program Files\Inno Setup 6\ISCC.exe"
        )
        $isccPath = $defaultIsccPaths | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
    }

    if (-not $isccPath) {
        throw "Inno Setup Compiler (ISCC) was not found in PATH. Install Inno Setup or run without -Installer."
    }

    New-Item -ItemType Directory -Force -Path $InstallerOutput | Out-Null
    $packagePlatform = Get-PackagePlatformName -RuntimeId $RuntimeId
    & $isccPath $InstallerScript "/DSourceDir=$SourcePath" "/DOutputDir=$InstallerOutput" "/DRuntimeId=$packagePlatform" "/DMyAppVersion=$PackageVersion" | Out-Host
}

if ($All) {
    $targets = @("win-x64", "win-x86", "win-arm64", "osx-x64", "osx-arm64")
}
else {
    $group = if ($Windows) {
        "windows"
    }
    elseif ($MacOS) {
        "macos"
    }
    else {
        Get-CurrentPlatformGroup
    }

    $targets = switch ($group) {
        "windows" { @("win-x64", "win-x86", "win-arm64") }
        "macos" { @("osx-x64", "osx-arm64") }
        default { throw "Unsupported platform group: $group" }
    }
}

Write-Host "Publishing FFGUITool ($Configuration)..." -ForegroundColor Cyan
Write-Host "Targets: $($targets -join ', ')" -ForegroundColor Cyan
Write-Host "Archive: .$Archive" -ForegroundColor Cyan

foreach ($rid in $targets) {
    $outputName = $RuntimeMap[$rid]
    $outputPath = Join-Path $PublishRoot $outputName

    Write-Host ""
    Write-Host "=> $rid -> $outputPath" -ForegroundColor Green

    dotnet publish $ProjectPath `
        --configuration $Configuration `
        --runtime $rid `
        --self-contained $SelfContained `
        --output $outputPath `
        -p:PublishSingleFile=false `
        -p:DebugType=None `
        -p:DebugSymbols=false

    $packagePlatform = Get-PackagePlatformName -RuntimeId $rid
    $portableName = "FFGUITool-v$PackageVersion-$packagePlatform-Portable"
    New-PublishArchive -SourcePath $outputPath -ArchiveName $portableName

    if ($Installer -and $rid.StartsWith("win-")) {
        New-InnoInstaller -SourcePath $outputPath -RuntimeId $rid
    }
}

Write-Host ""
Write-Host "Publish complete. Outputs are in: $PublishRoot" -ForegroundColor Cyan
Write-Host "Portable archives are in: $ArchiveRoot" -ForegroundColor Cyan
if ($Installer) {
    Write-Host "Installer output is in: $InstallerOutput" -ForegroundColor Cyan
}
