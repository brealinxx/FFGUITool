param(
    [switch]$Windows,

    [switch]$MacOS,

    [switch]$All,

    [ValidateSet("zip", "7z")]
    [string]$Archive = "zip",

    [string]$Configuration = "Release",

    [bool]$SelfContained = $true
)

$ErrorActionPreference = "Stop"

$ProjectPath = Join-Path $PSScriptRoot "FFGUITool\FFGUITool.csproj"
$PublishRoot = Join-Path $PSScriptRoot "FFGUITool\bin\publish"
$ArchiveRoot = Join-Path $PublishRoot "archives"

$RuntimeMap = [ordered]@{
    "win-x64" = "FFGUITool-win-x64"
    "win-x86" = "FFGUITool-win-x86"
    "win-arm64" = "FFGUITool-win-arm64"
    "osx-x64" = "FFGUITool-osx-x64"
    "osx-arm64" = "FFGUITool-osx-arm64"
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

    New-PublishArchive -SourcePath $outputPath -ArchiveName $outputName
}

Write-Host ""
Write-Host "Publish complete. Outputs are in: $PublishRoot" -ForegroundColor Cyan
Write-Host "Archives are in: $ArchiveRoot" -ForegroundColor Cyan
