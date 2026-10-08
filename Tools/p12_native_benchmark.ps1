$ErrorActionPreference = 'Stop'
$p12Workspace = Split-Path -Parent $PSScriptRoot
$p12Exe = Join-Path $p12Workspace 'Builds/P12Benchmark/CampusRift.exe'
$p12Root = Join-Path $p12Workspace 'Artifacts/P12/performance'
$p12Archive = Join-Path $p12Root ('before-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $p12Archive -Force | Out-Null
foreach ($p12File in @('PC.json','PC-render.png','PC-warmup-render.png','Player.log','DONE.txt','Native-run.json','Measurement.md')) {
    $p12Source = Join-Path $p12Root $p12File
    if (Test-Path -LiteralPath $p12Source) { Copy-Item -LiteralPath $p12Source -Destination $p12Archive -Force }
}
$p12Started = [DateTime]::UtcNow
$p12Args = '-batchmode -force-d3d12 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile "' + (Join-Path $p12Root 'Player.log') + '"'
$p12Process = Start-Process -FilePath $p12Exe -WorkingDirectory $p12Workspace -WindowStyle Hidden -ArgumentList $p12Args -PassThru
$p12Deadline = [DateTime]::UtcNow.AddMinutes(3)
while (-not $p12Process.WaitForExit(1000)) {
    if ([DateTime]::UtcNow -gt $p12Deadline) { throw "P12 native benchmark timed out (PID $($p12Process.Id))." }
}
$p12ReportPath = Join-Path $p12Root 'PC.json'
if ((Get-Item -LiteralPath $p12ReportPath).LastWriteTimeUtc -lt $p12Started) { throw 'No fresh P12 native report.' }
$p12Report = Get-Content -LiteralPath $p12ReportPath -Raw -Encoding UTF8 | ConvertFrom-Json
$p12Errors = @(Select-String -LiteralPath (Join-Path $p12Root 'Player.log') -Pattern 'Exception:|NullReferenceException|MissingReferenceException|UnityException|InvalidOperationException')
$p12Manifest = [ordered]@{
    startedAt = $p12Started.ToString('o')
    arguments = $p12Args
    assemblySHA256 = (Get-FileHash -LiteralPath (Join-Path $p12Workspace 'Builds/P12Benchmark/CampusRift_Data/Managed/Assembly-CSharp.dll') -Algorithm SHA256).Hash
    passed = $p12Report.passed
    playerLogExceptions = $p12Errors.Count
}
$p12Manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $p12Root 'Native-run.json') -Encoding UTF8
$p12Report | ConvertTo-Json
if (-not $p12Report.passed -or $p12Report.error -or $p12Errors.Count) { throw 'P12 native benchmark failed; preserve all raw evidence.' }
