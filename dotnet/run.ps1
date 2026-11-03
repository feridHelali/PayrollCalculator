<#
.SYNOPSIS
    Builds and runs the PayrollCalculator WPF app.

.DESCRIPTION
    Wraps `dotnet run` for src/PayrollCalculator.Wpf. By default the app uses
    %LOCALAPPDATA%\PayrollCalculator\payroll.db, which it creates and migrates on startup.

.PARAMETER DbPath
    Use another database file (sets PAYROLL_DB_PATH for this run only).

.PARAMETER ImportLegacy
    Import the legacy Electron data into the database at startup. The target database must be empty.

.PARAMETER LegacyDb
    The legacy SQLite file to import. Defaults to ..\payroll.sqlite (the repository root).

.PARAMETER Release
    Build and run the Release configuration instead of Debug.

.EXAMPLE
    .\run.ps1
.EXAMPLE
    .\run.ps1 -DbPath .\dev.db -ImportLegacy
.EXAMPLE
    .\run.ps1 -Release
#>
[CmdletBinding()]
param(
    [string]$DbPath,
    [switch]$ImportLegacy,
    [string]$LegacyDb = (Join-Path $PSScriptRoot '..\payroll.sqlite'),
    [switch]$Release
)

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'src\PayrollCalculator.Wpf\PayrollCalculator.Wpf.csproj'
$configuration = if ($Release) { 'Release' } else { 'Debug' }

$appArgs = @()
if ($ImportLegacy) {
    if (-not (Test-Path $LegacyDb -PathType Leaf)) { throw "Legacy database not found: $LegacyDb" }
    $appArgs += '--import-legacy', (Resolve-Path $LegacyDb).Path
}

$previousDbPath = $env:PAYROLL_DB_PATH
try {
    if ($DbPath) {
        $env:PAYROLL_DB_PATH = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($DbPath)
        Write-Host "Database: $env:PAYROLL_DB_PATH"
    }

    $dotnetArgs = @('run', '--project', $project, '-c', $configuration)
    if ($appArgs.Count -gt 0) { $dotnetArgs += @('--') + $appArgs }

    & dotnet @dotnetArgs
    if ($LASTEXITCODE -ne 0) { throw "The app exited with code $LASTEXITCODE." }
}
finally {
    $env:PAYROLL_DB_PATH = $previousDbPath
}
