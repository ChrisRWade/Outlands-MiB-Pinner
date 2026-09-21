[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testOutput = Join-Path $projectRoot 'artifacts\tests\MarkerFileStoreTests.exe'
$testDirectory = Split-Path -Parent $testOutput
$sourceRoot = Join-Path $projectRoot 'src\WadesMiBPinner'

New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null

& $compiler /nologo /target:exe /platform:anycpu /optimize+ `
    /reference:System.dll /reference:System.Core.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll `
    "/out:$testOutput" `
    (Join-Path $sourceRoot 'MapMarker.cs') `
    (Join-Path $sourceRoot 'MarkerFileStore.cs') `
    (Join-Path $projectRoot 'tests\MarkerFileStoreTests.cs')
if ($LASTEXITCODE -ne 0) {
    throw "Test compilation failed with exit code $LASTEXITCODE."
}

& $testOutput
if ($LASTEXITCODE -ne 0) {
    throw "Marker-file tests failed with exit code $LASTEXITCODE."
}

$uiTestOutput = Join-Path $testDirectory 'UiSmokeTests.exe'
& $compiler /nologo /target:exe /platform:anycpu /optimize+ /main:UiSmokeTests `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll /reference:System.Xml.dll /reference:System.Xml.Linq.dll `
    "/out:$uiTestOutput" `
    (Join-Path $sourceRoot 'MapMarker.cs') `
    (Join-Path $sourceRoot 'MarkerFileStore.cs') `
    (Join-Path $sourceRoot 'MainForm.Layout.cs') `
    (Join-Path $sourceRoot 'MainForm.Actions.cs') `
    (Join-Path $projectRoot 'tests\UiSmokeTests.cs')
if ($LASTEXITCODE -ne 0) {
    throw "UI smoke-test compilation failed with exit code $LASTEXITCODE."
}

& $uiTestOutput
if ($LASTEXITCODE -ne 0) {
    throw "UI smoke tests failed with exit code $LASTEXITCODE."
}
