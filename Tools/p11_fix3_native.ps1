$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$benchExe = Join-Path $workspace 'Builds/P11Fix3Benchmark/CampusRift.exe'
$benchRoot = Join-Path $workspace 'Artifacts/Reactions/fix3'
$startedAt = [DateTime]::UtcNow
$arguments = '-batchmode -force-d3d12 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile "' + (Join-Path $benchRoot 'Player.log') + '"'
$bench = Start-Process -FilePath $benchExe -WorkingDirectory $workspace -WindowStyle Hidden -ArgumentList $arguments -PassThru
$deadline = [DateTime]::UtcNow.AddMinutes(3)
while (-not $bench.WaitForExit(1000)) {
    if ([DateTime]::UtcNow -gt $deadline) { throw "P11 native benchmark timed out (PID $($bench.Id))." }
}
$reportPath = Join-Path $benchRoot 'Performance-Player.json'
if ((Get-Item -LiteralPath $reportPath).LastWriteTimeUtc -lt $startedAt) { throw 'No fresh native result.' }
$report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
$manifest = [ordered]@{
    startedAt = $startedAt.ToString('o')
    arguments = $arguments
    assemblySHA256 = (Get-FileHash -LiteralPath (Join-Path $workspace 'Builds/P11Fix3Benchmark/CampusRift_Data/Managed/Assembly-CSharp.dll') -Algorithm SHA256).Hash
    passed = $report.passed
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $benchRoot 'Native-run.json') -Encoding UTF8
$report | ConvertTo-Json
if (-not $report.passed -or $report.error) { throw 'P11 native benchmark failed. Preserve raw artifacts before changing code.' }
