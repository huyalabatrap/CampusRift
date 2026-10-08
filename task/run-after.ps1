# Waits for a process, checks that a prerequisite report exists, then runs one Codex job via run-codex-job.ps1.
param([int]$WaitPid = 0, [string]$Require = "", [Parameter(Mandatory)][string]$Prompt, [Parameter(Mandatory)][string]$Report,
      [Parameter(Mandatory)][string]$Tag, [string]$Context = "", [string]$OnlyAgent = "", [string]$Model = "", [string]$Effort = "")
$p = "D:\Projects\Campus Rift\Campus Rift"
$queueLog = "$p\task\queue-log.txt"
function Note($s) { "$(Get-Date -Format s) $s" | Out-File $queueLog -Append -Encoding utf8 }

if ($WaitPid) { Note "$Tag waiting for pid $WaitPid"; while (Get-Process -Id $WaitPid -ErrorAction SilentlyContinue) { Start-Sleep 30 } }
if ($Require -and -not (Test-Path (Join-Path $p $Require))) { Note "$Tag not started: $Require missing"; exit 1 }
if (Test-Path (Join-Path $p $Report)) { Note "$Tag skipped (report exists)"; exit 0 }
Note "$Tag start"
$a = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "$p\task\run-codex-job.ps1", '-Prompt', $Prompt, '-Report', $Report, '-Tag', $Tag)
if ($Context) { $a += @('-Context', $Context) }
if ($OnlyAgent) { $a += @('-OnlyAgent', $OnlyAgent) }
if ($Model) { $a += @('-Model', $Model) }
if ($Effort) { $a += @('-Effort', $Effort) }
& powershell.exe @a
if (Test-Path (Join-Path $p $Report)) { Note "$Tag done" } else { Note "$Tag ended without report" }
