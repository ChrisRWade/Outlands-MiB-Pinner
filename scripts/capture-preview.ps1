[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sourceRoot = Join-Path $projectRoot 'src\WadesMiBPinner'
$artifactRoot = Join-Path $projectRoot 'artifacts'
$fixturePath = Join-Path $artifactRoot 'ui-preview-fixture'
$runnerPath = Join-Path $artifactRoot 'UiSnapshot.exe'
$docsPath = Join-Path $projectRoot 'docs'
$previewPath = Join-Path $docsPath 'app-preview.png'
$scaledPreviewPath = Join-Path $artifactRoot 'app-preview-permission-font-125.png'

$resolvedProjectRoot = [IO.Path]::GetFullPath($projectRoot).TrimEnd('\')
$resolvedFixturePath = [IO.Path]::GetFullPath($fixturePath)
if (-not $resolvedFixturePath.StartsWith($resolvedProjectRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Refusing to clean a fixture folder outside the repository.'
}

if (Test-Path -LiteralPath $fixturePath) {
    Remove-Item -LiteralPath $fixturePath -Recurse -Force
}
New-Item -ItemType Directory -Path $fixturePath -Force | Out-Null
New-Item -ItemType Directory -Path $docsPath -Force | Out-Null

& (Join-Path $PSScriptRoot 'create-icon.ps1')

& $compiler /nologo /target:exe /platform:anycpu /optimize+ /main:UiSnapshot `
    "/win32icon:$(Join-Path $sourceRoot 'app.ico')" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll `
    "/out:$runnerPath" `
    (Join-Path $sourceRoot 'MapMarker.cs') `
    (Join-Path $sourceRoot 'MarkerFileStore.cs') `
    (Join-Path $sourceRoot 'MainForm.Layout.cs') `
    (Join-Path $sourceRoot 'MainForm.Actions.cs') `
    (Join-Path $projectRoot 'tests\UiSnapshot.cs')
if ($LASTEXITCODE -ne 0) {
    throw "Preview compilation failed with exit code $LASTEXITCODE."
}

& $runnerPath $fixturePath $previewPath
if ($LASTEXITCODE -ne 0) {
    throw "Preview capture failed with exit code $LASTEXITCODE."
}

& $runnerPath $fixturePath $scaledPreviewPath '1.25' 'show-permission-button'
if ($LASTEXITCODE -ne 0) {
    throw "Scaled preview capture failed with exit code $LASTEXITCODE."
}

Write-Host "Saved $previewPath"
Write-Host "Saved $scaledPreviewPath"

