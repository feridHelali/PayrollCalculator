<#
.SYNOPSIS
    Builds a release of PayrollCalculator: installer, portable zip and checksums.

.DESCRIPTION
    1. Runs the tests (unless -SkipTests).
    2. Publishes the WPF app self-contained for win-x64 (the .NET runtime, WPF and SQLite ship inside: nothing to install).
    3. Builds the installer with Inno Setup 6 (installer\PayrollCalculator.iss): per user, no admin rights.
    4. Zips the same folder as a portable copy (data\ is created beside the exe on first start).
    5. Writes SHA256SUMS.txt.

    Output: artifacts\release\<version>\ (version from Directory.Build.props).

.PARAMETER SkipTests
    Skip the test run (for a quick packaging check only; never for a release sent out).

.PARAMETER Iscc
    Path of ISCC.exe, if Inno Setup isn't in its default folder.

.EXAMPLE
    .\release.ps1
#>
[CmdletBinding()]
param(
    [switch]$SkipTests,
    [string]$Iscc = (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe')
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

[xml]$props = Get-Content (Join-Path $root 'Directory.Build.props')
$version = ($props.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
if (-not $version) { throw 'No <Version> in Directory.Build.props.' }
if (-not (Test-Path $Iscc -PathType Leaf)) { throw "Inno Setup not found: $Iscc (install Inno Setup 6 or pass -Iscc)." }

$out = Join-Path $root "artifacts\release\$version"
$app = Join-Path $out 'app'
if (Test-Path $out) { Remove-Item $out -Recurse -Force -Confirm:$false }
New-Item -ItemType Directory -Force $app | Out-Null

function Invoke-Step([string]$name, [scriptblock]$step) {
    Write-Host "== $name" -ForegroundColor Cyan
    & $step
    if ($LASTEXITCODE -ne 0) { throw "$name failed (exit code $LASTEXITCODE)." }
}

if (-not $SkipTests) {
    Invoke-Step 'Tests' { dotnet test (Join-Path $root 'PayrollCalculator.slnx') -c Release }
}

Invoke-Step 'Publish (self-contained, win-x64)' {
    dotnet publish (Join-Path $root 'src\PayrollCalculator.Wpf\PayrollCalculator.Wpf.csproj') -c Release -r win-x64 `
        --self-contained true -p:PublishReadyToRun=true -p:DebugType=none -p:SatelliteResourceLanguages=ar%3Bfr%3Ben -o $app
}

Invoke-Step 'Installer (Inno Setup)' {
    & $Iscc /Qp "/DAppVersion=$version" "/DSourceDir=$app" "/DOutputDir=$out" (Join-Path $root 'installer\PayrollCalculator.iss')
}

Write-Host '== Portable zip' -ForegroundColor Cyan
$portable = Join-Path $out "PayrollCalculator-Portable-$version.zip"
$staging = Join-Path $out 'portable\PayrollCalculator'
New-Item -ItemType Directory -Force $staging | Out-Null
Copy-Item (Join-Path $app '*') $staging -Recurse
New-Item -ItemType Directory -Force (Join-Path $staging 'data') | Out-Null
Set-Content (Join-Path $staging 'data\README.txt') -Encoding UTF8 -Value @(
    'This folder holds the data of PayrollCalculator: payroll.db (the database), backups\ and license.json.',
    'Keep it when you copy or update the program. Back it up regularly.')
Compress-Archive -Path $staging -DestinationPath $portable -CompressionLevel Optimal
Remove-Item (Join-Path $out 'portable') -Recurse -Force -Confirm:$false

Copy-Item (Join-Path (Split-Path $root -Parent) 'design_docs\08_Installer_MVP_Release\expert_guide_ar.md') (Join-Path $out 'دليل_الخبير.md')

Write-Host '== Checksums' -ForegroundColor Cyan
$files = Get-ChildItem $out -File | Where-Object { $_.Extension -in '.exe', '.zip' }
$files | ForEach-Object { "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name } |
    Set-Content (Join-Path $out 'SHA256SUMS.txt') -Encoding ascii

Write-Host ''
Write-Host "Release $version ready in $out" -ForegroundColor Green
$files | ForEach-Object { Write-Host ("  {0,-45} {1,8:N1} MB" -f $_.Name, ($_.Length / 1MB)) }
