# Generic Codex job runner for Campus Rift.
#   -Prompt   repo-relative path of the UTF-8 brief codex should read and carry out
#   -Report   repo-relative path of the report that marks the job as done
#   -Context  extra files a fresh session should read first ("a; b; c")
#   -WaitPid  wait for this process to exit before starting
#
# Account policy (user, 2026-10-02):
#   codex -> codex2 -> codex3 with gpt-6.1-sol xhigh, full access; an account that hits its usage
#   limit (5-hour window) is skipped until it resets; when all three are exhausted use codex4 with
#   gpt-6.1-sol high. If codex4 is exhausted too, wait for the earliest reset.
#   Model at capacity / network drop -> wait and resume the same account's session.
# Limit state is shared across jobs in task/codex-accounts.json.
param([Parameter(Mandatory)][string]$Prompt, [Parameter(Mandatory)][string]$Report, [string]$Tag = "job",
      [int]$WaitPid = 0, [string]$Context = "", [string]$Session = "", [string]$SessionAgent = "",
      [string]$OnlyAgent = "", [string]$Model = "gpt-6.1-sol", [string]$Effort = "", [int]$MaxTransient = 6, [int]$MaxUnknown = 3)
$p = "D:\Projects\Campus Rift\Campus Rift"
Set-Location $p
$utf8 = New-Object System.Text.UTF8Encoding $false
$OutputEncoding = $utf8
[Console]::OutputEncoding = $utf8
$logDir = Join-Path $p (Split-Path $Prompt -Parent)
$chainLog = "$logDir\chain-$Tag.txt"
$stateFile = "$p\task\codex-accounts.json"
$agents = @(
  @{ Name = 'codex';  Effort = 'xhigh' },
  @{ Name = 'codex2'; Effort = 'xhigh' },
  @{ Name = 'codex3'; Effort = 'xhigh' },
  @{ Name = 'codex4'; Effort = 'high' }
)
# A job pinned to one account/model (e.g. AR on codex4 + gpt-6-astra) waits for that account instead of switching.
if ($OnlyAgent) { $agents = @($OnlyAgent -split ',' | ForEach-Object { @{ Name = $_.Trim(); Effort = $(if ($Effort) { $Effort } else { 'high' }) } }) }
$limitRx = 'usage.?limit|hit your limit|reached your limit|quota|insufficient_quota|out of credits|Too Many Requests|rate.?limit'
$transientRx = 'at capacity|Reconnecting|stream disconnected|os error|timed out|503|502|500 Internal|overloaded|Service Unavailable'

function Note($s) { "$(Get-Date -Format s) $s" | Out-File $chainLog -Append -Encoding utf8 }

function Load-State {
  $s = @{}
  if (Test-Path $stateFile) {
    try { (Get-Content $stateFile -Raw | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $s[$_.Name] = [datetime]$_.Value } } catch {}
  }
  return $s
}
function Save-Exhausted($name, [datetime]$until) {
  $s = Load-State
  $s[$name] = $until
  $o = New-Object PSObject
  foreach ($k in $s.Keys) { $o | Add-Member NoteProperty $k ($s[$k].ToString('s')) }
  $o | ConvertTo-Json | Out-File $stateFile -Encoding utf8
}

# Codex's own error lines (not tool output) decide limit vs transient.
function Error-Tail($log) {
  $t = Get-Content $log -Encoding UTF8 -ErrorAction SilentlyContinue
  if (-not $t) { return "" }
  $tail = $t | Select-Object -Last 80
  return (($tail | Where-Object { $_ -match '^\s*(ERROR|error|warning:)|Reconnecting|usage limit|rate limit' }) -join "`n")
}

# "try again in 2 hours 13 minutes" / "in 45 minutes" / "at 3:45 PM"; default: recheck in 60 minutes.
function Reset-Time($text) {
  $now = Get-Date
  if ($text -match '\bin\s+(\d+)\s*h(?:ours?|rs?)?\b(?:\s*(?:and\s*)?(\d+)\s*m)?') {
    $m = 0; if ($Matches[2]) { $m = [int]$Matches[2] }
    return $now.AddHours([int]$Matches[1]).AddMinutes($m + 2)
  }
  if ($text -match '\bin\s+(\d+)\s*m(?:in|inutes?)?\b') { return $now.AddMinutes([int]$Matches[1] + 2) }
  # Weekly limits: "try again at Oct 11th, 2026 11:52 AM".
  if ($text -match 'at\s+([A-Z][a-z]{2,8})\s+(\d{1,2})(?:st|nd|rd|th)?,\s*(\d{4})\s+(\d{1,2}:\d{2}\s*[AP]M)') {
    try { return [datetime]::Parse("$($Matches[1]) $($Matches[2]) $($Matches[3]) $($Matches[4])", [Globalization.CultureInfo]::InvariantCulture).AddMinutes(2) } catch {}
  }
  if ($text -match 'at\s+(\d{1,2}:\d{2}\s*[AP]M)') {
    try { $t = [datetime]::Parse($Matches[1]); if ($t -lt $now) { $t = $t.AddDays(1) }; return $t.AddMinutes(2) } catch {}
  }
  return $now.AddMinutes(60)
}

if ($WaitPid) {
  Note "waiting for pid $WaitPid"
  while (Get-Process -Id $WaitPid -ErrorAction SilentlyContinue) { Start-Sleep 30 }
}

$resume = "Read the file $Prompt (UTF-8, Vietnamese) in full and carry out every instruction in it. Before redoing anything, check what is already done on disk (and any PROGRESS/REPORT files next to the brief) and continue from there."
$fresh = "You may be taking over this job from another agent that was interrupted. $resume"
if ($Context) { $fresh += " Read these files first for context: $Context." }
$fresh += " Keep the PROGRESS file next to the brief updated after every milestone, because another agent may have to take over. Write the final report to $Report only when the job is complete."

$sessions = @{}      # agent -> session id within this job
if ($Session -and $SessionAgent) { $sessions[$SessionAgent] = $Session }
$skip = @{}          # agent -> true when it failed too often in this job
$done = $false
for ($round = 1; $round -le 200 -and -not $done; $round++) {
  $state = Load-State
  $now = Get-Date
  $agent = $agents | Where-Object { -not $skip[$_.Name] -and (-not $state.ContainsKey($_.Name) -or $state[$_.Name] -le $now) } | Select-Object -First 1
  if (-not $agent) {
    $usable = $agents | Where-Object { -not $skip[$_.Name] }
    if (-not $usable) { Note "${Tag}: every account failed repeatedly in this job -> stop"; break }
    $next = ($usable | ForEach-Object { $state[$_.Name] } | Sort-Object | Select-Object -First 1)
    $wait = [Math]::Max(60, [Math]::Min(1800, ($next - $now).TotalSeconds))
    Note "${Tag}: all accounts at their limit; earliest reset $($next.ToString('s')) -> sleeping $([int]$wait) s"
    Start-Sleep ([int]$wait)
    continue
  }
  $a = $agent.Name
  $n = 1 + @(Get-ChildItem "$logDir\codex-log-$Tag-$a-try*.txt" -ErrorAction SilentlyContinue).Count
  $log = "$logDir\codex-log-$Tag-$a-try$n.txt"
  $args2 = @('-m', $Model, '-c', "model_reasoning_effort=`"$($agent.Effort)`"", '--dangerously-bypass-approvals-and-sandbox', '--skip-git-repo-check', '-o', "$logDir\codex-last-$Tag.md")
  "START $(Get-Date -Format s) agent=$a effort=$($agent.Effort) try=$n" | Out-File $log -Encoding utf8
  if ($sessions[$a]) {
    Note "$Tag -> $a $($agent.Effort) (try $n, resume $($sessions[$a]))"
    & $a exec resume @args2 $sessions[$a] $resume 2>&1 | Out-File $log -Append -Encoding utf8 -Width 4096
  } else {
    Note "$Tag -> $a $($agent.Effort) (try $n, fresh session)"
    & $a exec @args2 -C $p $fresh 2>&1 | Out-File $log -Append -Encoding utf8 -Width 4096
  }
  $code = $LASTEXITCODE
  "EXIT $code $(Get-Date -Format s)" | Out-File $log -Append -Encoding utf8
  if (-not $sessions[$a]) {
    $m = Select-String -Path $log -Pattern 'session id: *([0-9a-f-]{36})' | Select-Object -First 1
    if ($m) { $sessions[$a] = $m.Matches[0].Groups[1].Value; Note "$Tag $a session $($sessions[$a])" }
  }
  $err = Error-Tail $log
  $raw = ((Get-Content $log -Encoding UTF8 -ErrorAction SilentlyContinue | Select-Object -Last 40) -join ' ')
  if ($code -eq 0 -and (Test-Path (Join-Path $p $Report))) { Note "$Tag finished on $a (exit 0, report present)"; $done = $true; break }
  if ($err -match $limitRx) {
    $until = Reset-Time $raw
    Save-Exhausted $a $until
    Note "${Tag}: $a hit its usage limit (exit $code) -> unavailable until $($until.ToString('s'))"
    continue
  }
  $key = "$a-transient"; $ukey = "$a-unknown"
  if ($err -match $transientRx) {
    $sessions[$key] = 1 + [int]$sessions[$key]
    if ($sessions[$key] -gt $MaxTransient) { $skip[$a] = $true; Note "${Tag}: $a transient errors x$($sessions[$key]) -> skip for this job"; continue }
    Note "${Tag}: $a transient error (exit $code) -> retry in 180 s"; Start-Sleep 180; continue
  }
  $sessions[$ukey] = 1 + [int]$sessions[$ukey]
  if ($sessions[$ukey] -gt $MaxUnknown) { $skip[$a] = $true; Note "${Tag}: $a ended without report x$($sessions[$ukey]) -> skip for this job"; continue }
  Note "${Tag}: $a ended exit $code without report and no known error -> resume in 60 s"; Start-Sleep 60
}
if (-not $done) { Note "${Tag}: FAILED" }
Note "$Tag DONE"
