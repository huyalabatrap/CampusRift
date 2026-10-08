from b1007_runner import *
prepare();code('UIValidation.SetResolution(1600,1200);return true;');time.sleep(1)
code('var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.ControlMode=CampusRift.Controls.ControlMode.Mobile;CampusRift.UI.SettingsManager.Instance.Apply(s,false);return true;');time.sleep(1)
src='''var h=UnityEngine.Object.FindAnyObjectByType<CampusRift.Controls.MobileControlsHUD>();h.ShowAim();
UIValidation.SetResolution(1920,1080);
int frame=0;var log=new System.Text.StringBuilder();UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{if(h==null){UnityEditor.EditorApplication.update-=tick;return;}frame++;Canvas.ForceUpdateCanvases();
log.AppendLine("Frame "+frame+" screen "+Screen.width+"x"+Screen.height+" canvas "+h.GetComponentInParent<Canvas>().scaleFactor);
foreach(var z in h.Zones){if(z.role==CampusRift.Controls.TouchRole.Look||z.role==CampusRift.Controls.TouchRole.Move||!z.gameObject.activeInHierarchy)continue;var corners=new Vector3[4];((RectTransform)z.transform).GetWorldCorners(corners);log.AppendLine(z.role+" "+corners[0]+" "+corners[2]);}
if(frame>=8){UnityEditor.EditorApplication.update-=tick;System.IO.File.WriteAllText("task/batch-1007/regression/PlayerCombat-layout-diagnostic.txt",log.ToString());}};
UnityEditor.EditorApplication.update+=tick;return true;'''
code(src);time.sleep(2);print((OUT/'PlayerCombat-layout-diagnostic.txt').read_text(encoding='utf-8'));stop()
