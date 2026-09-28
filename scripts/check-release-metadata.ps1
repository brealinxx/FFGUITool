[CmdletBinding()]
param([string]$Tag = $env:RELEASE_TAG)

$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "release-check.ps1") @PSBoundParameters
