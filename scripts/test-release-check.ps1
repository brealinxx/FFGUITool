$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = [xml](Get-Content (Join-Path $root 'FFGUITool/FFGUITool.csproj'))
$version = $project.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1
$check = Join-Path $PSScriptRoot 'release-check.ps1'
$alias = Join-Path $PSScriptRoot 'check-release-metadata.ps1'
$previousTag = $env:RELEASE_TAG

function Assert-TagRejected {
    param([scriptblock]$Action)
    try { & $Action }
    catch {
        if ($_.Exception.Message -like 'Release tag mismatch.*') { return }
        throw
    }
    throw 'Expected a release tag mismatch, but validation succeeded.'
}

try {
    $env:RELEASE_TAG = $null
    & $check
    & $check -Tag "v$version"
    & $alias -Tag "v$version"
    foreach ($invalid in @('', ' ', 'main', "V$version", "v$version-wrong", "refs/tags/v$version")) {
        Assert-TagRejected { & $check -Tag $invalid }
    }
    Assert-TagRejected { & $alias -Tag 'main' }

    $env:RELEASE_TAG = "v$version"
    & $check
    & $alias
    $env:RELEASE_TAG = 'main'
    Assert-TagRejected { & $check }
    Assert-TagRejected { & $alias }
    Write-Host 'Release tag validation tests passed.'
}
finally { $env:RELEASE_TAG = $previousTag }
