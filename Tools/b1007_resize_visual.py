"""Capture the corrected mobile HUD; no harness or assertions are run."""
from b1007_runner import *
prepare()
code('UIValidation.SetResolution(1600,1200);return true;')
time.sleep(.8)
code('var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.ControlMode=CampusRift.Controls.ControlMode.Mobile;CampusRift.UI.SettingsManager.Instance.Apply(s,false);CampusRift.UI.UIStateManager.Instance.EnterScene(true);return true;')
time.sleep(.8)
code('UIValidation.SetResolution(1920,1080);return true;')
time.sleep(1)
code('Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot("task/batch-1007/screens/7-mobile-resize-corrected.png");return true;')
time.sleep(1)
stop()
progress('Ảnh visual sau sửa transform: screens/7-mobile-resize-corrected.png. Không gọi harness/assertion PlayerCombat lần nữa; kết quả retest FAIL vẫn giữ nguyên.')
