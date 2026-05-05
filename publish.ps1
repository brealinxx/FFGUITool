param(
    [ValidateSet("all", "win-x86", "win-x64", "win-arm64", "osx-x64", "osx-arm64")]
    [string]$Runtime = "all",

    [string]$Configuration = "Release",

    [bool]$SelfContained = $true
)

$ErrorActionPreference = "Stop"

$ProjectPath = Join-Path $PSScriptRoot "FFGUITool\FFGUITool.csproj"
$PublishRoot = Join-Path $PSScriptRoot "FFGUITool\bin\publish"

$RuntimeMap = [ordered]@{
    "win-x86" = "FFGUITool-win-x86"
    "win-x64" = "FFGUITool-win-x64"
    "win-arm64" = "FFGUITool-win-arm64"
    "osx-x64" = "FFGUITool-osx-x64"
    "osx-arm64" = "FFGUITool-osx-arm64"
}

if ($Runtime -eq "all") {
    $Targets = $RuntimeMap.Keys
} else {
    $Targets = @($Runtime)
}

Write-Host "Publishing FFGUITool ($Configuration)..." -ForegroundColor Cyan

foreach ($rid in $Targets) {
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
}

Write-Host ""
Write-Host "Publish complete. Outputs are in: $PublishRoot" -ForegroundColor Cyan
