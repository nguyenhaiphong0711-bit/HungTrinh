param([Parameter(Mandatory)][string]$BackupFile)
$ErrorActionPreference = 'Stop'
# Run as a normal Windows account, NOT elevated, with no backup/custodian/SQL-service membership.
$destination = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString() + '.bak')
$readDenied = $false
$copyDenied = $false
try {
    $stream = [IO.File]::OpenRead($BackupFile)
    $stream.Dispose()
} catch [UnauthorizedAccessException] { $readDenied = $true }
try {
    [IO.File]::Copy($BackupFile, $destination)
} catch [UnauthorizedAccessException] { $copyDenied = $true }
finally { if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Force } }
if (!$readDenied -or !$copyDenied) { throw "ACL FAIL: ReadDenied=$readDenied CopyDenied=$copyDenied" }
Write-Output 'PASS: unauthorized read and copy were both denied.'
