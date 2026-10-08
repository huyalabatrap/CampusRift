from ar_session import *
for language in ['Vietnamese','English']:
 code('var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.Language=CampusRift.Localization.GameLanguage.'+language+';s.TextSize=2;CampusRift.UI.SettingsManager.Instance.Apply(s,false);var h=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattleHUD>();var go=h.gameObject;var child=go.transform.Find("AR Battle HUD");if(child!=null)UnityEngine.Object.DestroyImmediate(child.gameObject);UnityEngine.Object.DestroyImmediate(h);go.AddComponent<CampusRift.AR.ARBattleHUD>();return true;')
 for width in [1920,2340]:
  code('UIValidation.SetResolution('+str(width)+',1080);return true;');time.sleep(1)
  result=code('var r=CampusRift.UI.ComicTextAudit.Scan("AR HUD 130%");CampusRift.UI.ComicTextAudit.Save(r,"task/ar/m6/audit-'+language+'-'+str(width)+'-130.json");return new {count=r.issues.Count,issues=r.issues};')
  print(language,width,result,flush=True)
code('var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.Language=CampusRift.Localization.GameLanguage.Vietnamese;s.TextSize=0;CampusRift.UI.SettingsManager.Instance.Apply(s,false);UIValidation.SetResolution(1920,1080);var mock=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.MockGestureSource>();mock.enabled=false;mock.Emit("Victory",new UnityEngine.Vector2(.5f,.5f));return true;')
time.sleep(1)
code('UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/m6-hud.png");return true;')
console('task/ar/m6/final-console.json')
