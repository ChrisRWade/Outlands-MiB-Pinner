[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$toolOutput = Join-Path $projectRoot 'artifacts\tools\IconBuilder.exe'
$toolDirectory = Split-Path -Parent $toolOutput
$iconPath = Join-Path $projectRoot 'src\WadesMiBPinner\app.ico'
$previewPath = Join-Path $projectRoot 'docs\boat-icon.png'

New-Item -ItemType Directory -Path $toolDirectory -Force | Out-Null

& $compiler /nologo /target:exe /platform:anycpu /optimize+ `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    "/out:$toolOutput" `
    (Join-Path $projectRoot 'tools\IconBuilder.cs')
if ($LASTEXITCODE -ne 0) {
    throw "Icon builder compilation failed with exit code $LASTEXITCODE."
}

& $toolOutput $iconPath $previewPath
if ($LASTEXITCODE -ne 0) {
    throw "Icon generation failed with exit code $LASTEXITCODE."
}

Write-Host "Generated $iconPath"

