param([switch]$SkipRelease, [switch]$ReleaseOnly)
$ErrorActionPreference = 'Stop'
$workspaceP17 = (Resolve-Path -LiteralPath '.').Path
$devExeP17 = (Resolve-Path -LiteralPath 'Artifacts/P17-Builds/Windows/dev/CampusRift.exe').Path
$devFolderP17 = Split-Path -Parent $devExeP17
function Run-P17Player([string]$flagP17, [string]$logNameP17) {
    $logP17 = Join-Path $devFolderP17 $logNameP17
    $argsP17 = @($flagP17, '-screen-fullscreen', '0', '-logFile', ('"' + $logP17 + '"'))
    $procP17 = Start-Process -FilePath $devExeP17 -WorkingDirectory $devFolderP17 -ArgumentList $argsP17 -WindowStyle Hidden -PassThru
    if (-not $procP17.WaitForExit(75000)) {
        $liveP17 = Get-Process -Id $procP17.Id
        if ($liveP17.Path -ne $devExeP17) { throw 'Unexpected player PID/path' }
        $liveP17.CloseMainWindow() | Out-Null
        if (-not $liveP17.WaitForExit(5000)) { Stop-Process -Id $liveP17.Id }
        throw "Native smoke timeout: $flagP17; inspect log"
    }
    Write-Output "$flagP17 exit=$($procP17.ExitCode)"
}
if (-not $ReleaseOnly) {
Run-P17Player '-p17-smoke' 'native-smoke.log'
Run-P17Player '-p17-persistence' 'native-reopen.log'
$qaFolderP17 = Join-Path $devFolderP17 'P17-Smoke'
Get-Content -LiteralPath (Join-Path $qaFolderP17 'DONE.txt')
Get-Content -LiteralPath (Join-Path $qaFolderP17 'study-buy.txt')
Get-Content -LiteralPath (Join-Path $qaFolderP17 'PERSISTENCE.txt')
foreach ($imageP17 in @('windows-hub.png','windows-level1.png')) {
    Copy-Item -LiteralPath (Join-Path $qaFolderP17 $imageP17) -Destination (Join-Path $workspaceP17 ('task/p17/screens/' + $imageP17))
}
}
if ($SkipRelease) { exit 0 }
$releaseExeP17 = (Resolve-Path -LiteralPath 'Artifacts/P17-Builds/Windows/release/CampusRift.exe').Path
$releaseFolderP17 = Split-Path -Parent $releaseExeP17
$releaseLogP17 = Join-Path $releaseFolderP17 'native-startup.log'
$releaseProcP17 = Start-Process -FilePath $releaseExeP17 -WorkingDirectory $releaseFolderP17 -ArgumentList @('-screen-fullscreen','0','-logFile',('"'+$releaseLogP17+'"')) -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 6
$releaseLiveP17 = Get-Process -Id $releaseProcP17.Id -ErrorAction SilentlyContinue
if ($releaseLiveP17) {
    if ($releaseLiveP17.Path -ne $releaseExeP17) { throw 'Unexpected release PID/path' }
    $releaseLiveP17.CloseMainWindow() | Out-Null
    if (-not $releaseLiveP17.WaitForExit(5000)) { Stop-Process -Id $releaseLiveP17.Id }
    Set-Content -LiteralPath (Join-Path $releaseFolderP17 'startup-smoke.txt') -Encoding UTF8 -Value 'Release ran 6s; closed by smoke; inspect native-startup.log. No gameplay automation in release.'
} else { throw 'Release exited before startup observation finished; inspect log' }
