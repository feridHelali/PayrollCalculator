# Runs the PayrollCalculator app. Same options as dotnet\run.ps1, e.g. .\run.ps1 -DbPath .\dev.db -ImportLegacy
& (Join-Path $PSScriptRoot 'dotnet\run.ps1') @args
exit $LASTEXITCODE
