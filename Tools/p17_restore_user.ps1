$ErrorActionPreference = 'Stop'
$workspaceRestoreP17 = (Resolve-Path -LiteralPath '.').Path
$backupRestoreP17 = (Get-Content -LiteralPath 'task/p17/BACKUP.txt' -Encoding UTF8).Trim()
$originalRestoreP17 = (Resolve-Path -LiteralPath (Join-Path $backupRestoreP17 'persistent-user')).Path
$targetRestoreP17 = 'C:\Users\Admin\AppData\LocalLow\DefaultCompany\Campus Rift'
if ((Get-Item -LiteralPath $targetRestoreP17).FullName -ne $targetRestoreP17) { throw 'Unexpected persistent target' }
$archiveRestoreP17 = [IO.Path]::GetFullPath((Join-Path $backupRestoreP17 ('persistent-after-qa-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))))
if (-not $archiveRestoreP17.StartsWith($workspaceRestoreP17 + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Archive outside workspace' }
if (-not $originalRestoreP17.StartsWith($workspaceRestoreP17 + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Backup outside workspace' }
Move-Item -LiteralPath $targetRestoreP17 -Destination $archiveRestoreP17
Copy-Item -LiteralPath $originalRestoreP17 -Destination $targetRestoreP17 -Recurse
$checkedRestoreP17 = 0
foreach ($fileRestoreP17 in Get-ChildItem -LiteralPath $originalRestoreP17 -File -Recurse) {
    $relativeRestoreP17 = $fileRestoreP17.FullName.Substring($originalRestoreP17.Length + 1)
    $restoredFileP17 = Join-Path $targetRestoreP17 $relativeRestoreP17
    if ((Get-FileHash -LiteralPath $fileRestoreP17.FullName).Hash -ne (Get-FileHash -LiteralPath $restoredFileP17).Hash) { throw "Restored hash mismatch: $relativeRestoreP17" }
    $checkedRestoreP17++
}
Add-Content -LiteralPath 'task/p17/PROGRESS.md' -Encoding UTF8 -Value "`n- User persistent restored from pre-P17 snapshot: $checkedRestoreP17 files SHA256 match; QA data archived at $archiveRestoreP17. No recursive delete."
Write-Output "Restored user persistent: $checkedRestoreP17 files matched."
