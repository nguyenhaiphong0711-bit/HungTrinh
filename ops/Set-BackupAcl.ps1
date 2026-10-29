[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)][string]$BackupDirectory,
    [Parameter(Mandatory)][string]$CertificateDirectory,
    [Parameter(Mandatory)][string]$SqlServiceAccount,
    [Parameter(Mandatory)][string]$BackupOperatorGroup,
    [Parameter(Mandatory)][string]$CertificateCustodianGroup
)
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Windows NTFS is required.' }
$backup = [IO.Path]::GetFullPath($BackupDirectory).TrimEnd('\')
$certificate = [IO.Path]::GetFullPath($CertificateDirectory).TrimEnd('\')
if ($backup -eq $certificate -or $backup.StartsWith($certificate + '\', [StringComparison]::OrdinalIgnoreCase) -or $certificate.StartsWith($backup + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Backup and certificate directories must be separate, non-overlapping paths.'
}
function Set-RestrictedAcl([string]$Path, [string]$ReaderGroup) {
    if (!(Test-Path -LiteralPath $Path)) { throw "Create and inspect an empty dedicated directory first: $Path" }
    $item = Get-Item -LiteralPath $Path
    if (!$item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Use a real directory, not a junction/symlink.' }
    if (Get-ChildItem -LiteralPath $Path -Force | Select-Object -First 1) { throw 'Apply this script only to empty dedicated directories; migrate existing contents separately.' }
    $acl = New-Object Security.AccessControl.DirectorySecurity
    $acl.SetAccessRuleProtection($true, $false)
    $inheritance = [Security.AccessControl.InheritanceFlags]'ContainerInherit, ObjectInherit'
    $propagation = [Security.AccessControl.PropagationFlags]::None
    $allow = [Security.AccessControl.AccessControlType]::Allow
    foreach ($entry in @(
        @('S-1-5-18', 'FullControl'), # SYSTEM
        @('S-1-5-32-544', 'FullControl'), # Built-in Administrators
        @($SqlServiceAccount, 'Modify'),
        @($ReaderGroup, 'ReadAndExecute')
    )) {
        $identity = if ($entry[0] -like 'S-1-*') { [Security.Principal.SecurityIdentifier]::new($entry[0]) } else { [Security.Principal.NTAccount]::new($entry[0]) }
        $rule = [Security.AccessControl.FileSystemAccessRule]::new($identity, $entry[1], $inheritance, $propagation, $allow)
        $acl.AddAccessRule($rule)
    }
    if ($PSCmdlet.ShouldProcess($Path, 'Replace ACL with dedicated least-privilege rules')) { Set-Acl -LiteralPath $Path -AclObject $acl }
}
Set-RestrictedAcl $backup $BackupOperatorGroup
Set-RestrictedAcl $certificate $CertificateCustodianGroup
