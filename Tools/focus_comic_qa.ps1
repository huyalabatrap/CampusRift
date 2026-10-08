param([int]$ComicQaEditorId)
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class ComicQaWindow {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr handle,int mode);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr handle,out uint processId);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint from,uint to,bool attach);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr handle);
}
'@
$comicQaEditor=Get-Process -Id $ComicQaEditorId
[ComicQaWindow]::ShowWindowAsync($comicQaEditor.MainWindowHandle,9) | Out-Null
$comicQaFocused=[ComicQaWindow]::SetForegroundWindow($comicQaEditor.MainWindowHandle)
if(-not $comicQaFocused) {
    [uint32]$comicQaWindowPid=0
    $comicQaForegroundThread=[ComicQaWindow]::GetWindowThreadProcessId([ComicQaWindow]::GetForegroundWindow(),[ref]$comicQaWindowPid)
    $comicQaUnityThread=[ComicQaWindow]::GetWindowThreadProcessId($comicQaEditor.MainWindowHandle,[ref]$comicQaWindowPid)
    [ComicQaWindow]::AttachThreadInput($comicQaUnityThread,$comicQaForegroundThread,$true) | Out-Null
    try {
        [ComicQaWindow]::BringWindowToTop($comicQaEditor.MainWindowHandle) | Out-Null
        $comicQaFocused=[ComicQaWindow]::SetForegroundWindow($comicQaEditor.MainWindowHandle)
    } finally { [ComicQaWindow]::AttachThreadInput($comicQaUnityThread,$comicQaForegroundThread,$false) | Out-Null }
}
[uint32]$comicQaForegroundId=0
[ComicQaWindow]::GetWindowThreadProcessId([ComicQaWindow]::GetForegroundWindow(),[ref]$comicQaForegroundId) | Out-Null
Write-Output "Unity focus=$comicQaFocused; foreground process=$comicQaForegroundId; Unity process=$($comicQaEditor.Id)"
