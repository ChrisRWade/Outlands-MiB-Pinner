[CmdletBinding()]
param(
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$artifactRoot = Join-Path $projectRoot 'artifacts'
$packageName = "Wades-MiB-Pinner-v$Version"
$stagePath = Join-Path $artifactRoot $packageName
$exePath = Join-Path $stagePath 'Wades-MiB-Pinner.exe'
$zipPath = Join-Path $artifactRoot "$packageName.zip"
$checksumPath = Join-Path $artifactRoot 'SHA256SUMS.txt'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw ".NET Framework 4.8 C# compiler not found at $compiler"
}

$resolvedProjectRoot = [IO.Path]::GetFullPath($projectRoot).TrimEnd('\')
$resolvedStagePath = [IO.Path]::GetFullPath($stagePath)
if (-not $resolvedStagePath.StartsWith($resolvedProjectRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Refusing to clean a build folder outside the repository.'
}

if (Test-Path -LiteralPath $stagePath) {
    Remove-Item -LiteralPath $stagePath -Recurse -Force
}
New-Item -ItemType Directory -Path $stagePath -Force | Out-Null

& (Join-Path $PSScriptRoot 'create-icon.ps1')

$sourceRoot = Join-Path $projectRoot 'src\WadesMiBPinner'
$sources = @(
    (Join-Path $sourceRoot 'MapMarker.cs'),
    (Join-Path $sourceRoot 'MarkerFileStore.cs'),
    (Join-Path $sourceRoot 'MainForm.Layout.cs'),
    (Join-Path $sourceRoot 'MainForm.Actions.cs'),
    (Join-Path $sourceRoot 'Program.cs'),
    (Join-Path $sourceRoot 'Properties\AssemblyInfo.cs')
)

& $compiler /nologo /target:winexe /platform:anycpu /optimize+ `
    "/win32manifest:$(Join-Path $sourceRoot 'app.manifest')" `
    "/win32icon:$(Join-Path $sourceRoot 'app.ico')" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll `
    "/out:$exePath" @sources
if ($LASTEXITCODE -ne 0) {
    throw "Application compilation failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $stagePath
Copy-Item -LiteralPath (Join-Path $projectRoot 'QUICK-START.txt') -Destination $stagePath

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -Path (Join-Path $stagePath '*') -DestinationPath $zipPath -CompressionLevel Optimal

$exeHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $exePath).Hash.ToLowerInvariant()
$zipHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash.ToLowerInvariant()
@(
    "$exeHash  $packageName/Wades-MiB-Pinner.exe"
    "$zipHash  $packageName.zip"
) | Set-Content -LiteralPath $checksumPath -Encoding ASCII

Write-Host "Built $exePath"
Write-Host "Packaged $zipPath"
Write-Host "Checksums $checksumPath"
