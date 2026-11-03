<#
.SYNOPSIS
    Builds and runs the PayrollCalculator WPF app.

.DESCRIPTION
    Wraps `dotnet run` for src/PayrollCalculator.Wpf. By default the app uses
    %LOCALAPPDATA%\PayrollCalculator\payroll.db, which it creates and migrates on startup.

.PARAMETER DbPath
    Use another database file (sets PAYROLL_DB_PATH for this run only).

.PARAMETER Reset
    Delete the database first (the default one, or -DbPath). The app then recreates it on startup: migrations, the
    shipped reference data (data/seed) and the first-run admin account. Use it for a fast, clean development cycle.

.PARAMETER ImportLegacy
    Import the legacy Electron data into the database at startup. The target database must be empty.

.PARAMETER LegacyDb
    The legacy SQLite file to import. Defaults to tests\fixtures\legacy-sample.sqlite (the Electron sample data).

.PARAMETER Release
    Build and run the Release configuration instead of Debug.

.EXAMPLE
    .\run.ps1
.EXAMPLE
    .\run.ps1 -DbPath .\dev.db -ImportLegacy
.EXAMPLE
    .\run.ps1 -Reset
.EXAMPLE
    .\run.ps1 -Release
#>
[CmdletBinding()]
param(
    [string]$DbPath,
    [switch]$Reset,
    [switch]$ImportLegacy,
    [string]$LegacyDb = (Join-Path $PSScriptRoot 'tests\fixtures\legacy-sample.sqlite'),
    [switch]$Release
)

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'src\PayrollCalculator.Wpf\PayrollCalculator.Wpf.csproj'
$configuration = if ($Release) { 'Release' } else { 'Debug' }

$database = if ($DbPath) { $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($DbPath) }
            else { Join-Path $env:LOCALAPPDATA 'PayrollCalculator\payroll.db' }
if ($Reset) {
    foreach ($file in @($database, "$database-wal", "$database-shm")) {
        if (Test-Path $file -PathType Leaf) { Remove-Item $file -Force -Confirm:$false }
    }
    Write-Host "Reset: $database deleted; it is recreated from the seed on startup."
}

$appArgs = @()
if ($ImportLegacy) {
    if (-not (Test-Path $LegacyDb -PathType Leaf)) { throw "Legacy database not found: $LegacyDb" }
    $appArgs += '--import-legacy', (Resolve-Path $LegacyDb).Path
}

$previousDbPath = $env:PAYROLL_DB_PATH
try {
    if ($DbPath) {
        $env:PAYROLL_DB_PATH = $database
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
