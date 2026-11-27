<#
.SYNOPSIS
    Builds dist\Companian.exe with the C# compiler that ships with Windows (.NET Framework 4.x).
    No Visual Studio, .NET SDK or admin rights needed.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\build.ps1
#>
param(
    [switch]$RegenerateIcon
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw 'csc.exe (.NET Framework 4.x) was not found.' }

$assets = Join-Path $root 'assets'
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $assets, $dist | Out-Null

$ico = Join-Path $assets 'companian.ico'
if ($RegenerateIcon -or -not (Test-Path $ico)) {
    Write-Host 'Generating icon...'
    $tmp = Join-Path ([IO.Path]::GetTempPath()) 'Companian-IconGen.exe'
    & $csc /nologo /target:exe /codepage:65001 "/out:$tmp" /r:System.Drawing.dll (Join-Path $root 'tools\IconGen.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Icon generator failed to compile.' }
    & $tmp $ico (Join-Path $assets 'logo.png')
    if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed.' }
    Remove-Item $tmp -Force
}

Write-Host 'Compiling Companian.exe...'
$sources = Get-ChildItem (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object { $_.FullName }
$exe = Join-Path $dist 'Companian.exe'
& $csc /nologo /target:winexe /platform:anycpu /optimize+ /debug- /codepage:65001 /utf8output /warn:4 `
    "/out:$exe" "/win32icon:$ico" "/win32manifest:$(Join-Path $root 'src\app.manifest')" `
    "/resource:$ico,Companian.app.ico" `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    $sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$size = [math]::Round((Get-Item $exe).Length / 1KB)
Write-Host "Done: $exe ($size KB)"
