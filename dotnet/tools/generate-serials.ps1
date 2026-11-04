<#
.SYNOPSIS
    Generates licence serials for PayrollCalculator.

.DESCRIPTION
    Makes -Count random serials (20 characters of Crockford base 32, shown XXXXX-XXXXX-XXXXX-XXXXX: 100 random bits).

    - The serials go to a private CSV in licences\ at the repository root (git-ignored). Keep it: it is the only place
      the serials exist. Give one serial per expert or machine and note who got which.
    - Their SHA-256 hashes are appended to src\PayrollCalculator.Infrastructure\Licensing\serials.sha256, which is
      embedded in the app. Rebuild and release the app for new serials to be accepted. Earlier serials stay valid.

    The hash is SHA-256 of "PayrollCalculator.serial.v1:" + the serial without dashes, in lower-case hex. It must
    match SerialKey.Hash in the Application project (a test checks a known value).

.PARAMETER Count
    How many serials to make (default 100).

.PARAMETER Note
    A label written in the CSV and in the hash file, for example "Expert MVP 2026-11".

.EXAMPLE
    .\tools\generate-serials.ps1
.EXAMPLE
    .\tools\generate-serials.ps1 -Count 10 -Note "Cabinet X"
#>
[CmdletBinding()]
param(
    [ValidateRange(1, 10000)]
    [int]$Count = 100,
    [string]$Note = "batch"
)

$ErrorActionPreference = 'Stop'

$alphabet = '0123456789ABCDEFGHJKMNPQRSTVWXYZ'
$prefix = 'PayrollCalculator.serial.v1:'
$dotnet = Split-Path $PSScriptRoot -Parent
$repository = Split-Path $dotnet -Parent
$hashFile = Join-Path $dotnet 'src\PayrollCalculator.Infrastructure\Licensing\serials.sha256'
$outDir = Join-Path $repository 'licences'

function Get-SerialHash([string]$serial) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($prefix + $serial))
        return -join ($bytes | ForEach-Object { $_.ToString('x2') })
    }
    finally { $sha.Dispose() }
}

$known = @{}
if (Test-Path $hashFile) {
    Get-Content $hashFile | Where-Object { $_ -match '^[0-9a-f]{64}$' } | ForEach-Object { $known[$_] = $true }
}

$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rows = New-Object System.Collections.Generic.List[object]
try {
    while ($rows.Count -lt $Count) {
        $bytes = New-Object byte[] 20
        $rng.GetBytes($bytes)
        $serial = -join ($bytes | ForEach-Object { $alphabet[$_ -band 31] }) # 256 = 8 x 32: uniform
        $hash = Get-SerialHash $serial
        if ($known.ContainsKey($hash)) { continue }
        $known[$hash] = $true
        $display = ($serial -split '(.{5})' | Where-Object { $_ }) -join '-'
        $rows.Add([pscustomobject]@{ Serial = $display; Sha256 = $hash; Note = $Note; Recipient = '' })
    }
}
finally { $rng.Dispose() }

New-Item -ItemType Directory -Force $outDir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$csv = Join-Path $outDir "serials-$stamp.csv"
$rows | Export-Csv -Path $csv -NoTypeInformation -Encoding UTF8

$lines = @("# $stamp $Note ($Count)") + ($rows | ForEach-Object { $_.Sha256 })
Add-Content -Path $hashFile -Value $lines -Encoding ascii

Write-Host "$Count serials written to $csv (private: do not commit or share the file)."
Write-Host "Hashes appended to $hashFile. Rebuild the app so they are accepted."
