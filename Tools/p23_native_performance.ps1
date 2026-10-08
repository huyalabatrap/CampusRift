$ErrorActionPreference='Stop'
$exeP23=(Resolve-Path -LiteralPath 'Releases/2026-10-04-v1.0/Windows/dev/CampusRift.exe').Path
$folderP23=Split-Path -Parent $exeP23
$logP23=Join-Path (Get-Location).Path 'task/p23/windows-dev-player.log'
$startedP23=Get-Date
$procP23=Start-Process -FilePath $exeP23 -WorkingDirectory $folderP23 -ArgumentList @('-p23-perf','-screen-fullscreen','0','-logFile',('"'+$logP23+'"')) -WindowStyle Hidden -PassThru
Write-Output "Native development performance PID $($procP23.Id)"
$deadlineP23=(Get-Date).AddMinutes(3)
$doneP23=Join-Path $folderP23 'P23-Perf/DONE.txt'
while((Get-Date)-lt $deadlineP23){
    if((Test-Path -LiteralPath $doneP23)-and (Get-Item -LiteralPath $doneP23).LastWriteTime -ge $startedP23){break}
    Start-Sleep -Seconds 1
}
if(-not(Test-Path -LiteralPath $doneP23)){throw 'No fresh native performance completion'}
$textP23=Get-Content -LiteralPath $doneP23 -Raw -Encoding UTF8
if($textP23 -notmatch '^Completed 3 heavy samples'){throw $textP23}
if(-not $procP23.WaitForExit(10000)){throw 'Native fixture did not exit'}
$evidenceP23=Join-Path (Get-Location).Path 'task/p23/perf-windows'
New-Item -ItemType Directory -Path $evidenceP23 -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $folderP23 'P23-Perf') -Destination $evidenceP23 -Recurse -Force
Write-Output $textP23
