# Runs Codex jobs one after another (each via run-codex-job.ps1), optionally after a process exits.
param([int]$WaitPid = 0)
$p = "D:\Projects\Campus Rift\Campus Rift"
$job = "$p\task\run-codex-job.ps1"
$queueLog = "$p\task\queue-log.txt"
function Note($s) { "$(Get-Date -Format s) $s" | Out-File $queueLog -Append -Encoding utf8 }

if ($WaitPid) { Note "waiting for pid $WaitPid"; while (Get-Process -Id $WaitPid -ErrorAction SilentlyContinue) { Start-Sleep 30 } }

$jobs = @(
  @{ Prompt = 'task/p10/PROMPT-P10-fix2.md'; Report = 'task/p10/REPORT-P10-fix2.md'; Tag = 'p10fix2'; Session = '01a0f853-3c08-7893-b9df-f6026a3dca7b'; Context = '' },
  @{ Prompt = 'task/p11/PROMPT-P11.md'; Report = 'task/p11/REPORT-P11.md'; Tag = 'p11'; Session = ''; Context = 'task/p10/REPORT-P10.md; task/p10/REPORT-P10-fix2.md; task/p10/PROMPT-P10-fix1.md' }
)
foreach ($j in $jobs) {
  if (Test-Path (Join-Path $p $j.Report)) { Note "$($j.Tag) skipped (report exists)"; continue }
  Note "$($j.Tag) start"
  $a = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $job, '-Prompt', $j.Prompt, '-Report', $j.Report, '-Tag', $j.Tag)
  if ($j.Session) { $a += @('-Session', $j.Session) }
  if ($j.Context) { $a += @('-Context', $j.Context) }
  & powershell.exe @a
  if (-not (Test-Path (Join-Path $p $j.Report))) { Note "$($j.Tag) ended without report -> queue stopped"; break }
  Note "$($j.Tag) done"
}
Note "QUEUE DONE"
