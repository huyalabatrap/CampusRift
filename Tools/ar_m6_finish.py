from ar_session import *
print(code('return UnityEditor.EditorApplication.isCompiling;'),flush=True)
print(console('task/ar/m6/compile-console.json'),flush=True)
call('manage_editor',{'action':'play'});background()
time.sleep(2)
for step in [0,1]:
 for language in ['Vietnamese','English']:
  code('var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.Language=CampusRift.Localization.GameLanguage.'+language+';s.TextSize=2;s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;CampusRift.UI.SettingsManager.Instance.Apply(s,false);var b=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARSessionBootstrap>();b.GetType().GetMethod("Refresh",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(b,null);return true;')
  time.sleep(.6)
  print(code('var r=CampusRift.UI.ComicTextAudit.Scan("AR consent130");CampusRift.UI.ComicTextAudit.Save(r,"task/ar/m6/consent'+str(step)+'-'+language+'-130.json");return new {issues=r.issues.Count,details=r.issues};'),flush=True)
 if step==0:code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARSessionBootstrap>().Continue();return true;')
code('var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.Language=CampusRift.Localization.GameLanguage.Vietnamese;s.TextSize=0;CampusRift.UI.SettingsManager.Instance.Apply(s,false);return true;')
exec(Path('Tools/ar_m6_gate.py').read_text(encoding='utf-8-sig'))
exec(Path('Tools/ar_m6_audit.py').read_text(encoding='utf-8-sig'))
