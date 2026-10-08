from pathlib import Path
import json,time
from p13_cli import call
root=Path('task/p21')
def code(s):return call('execute_code',dict(action='execute',code=s))
call('manage_editor',dict(action='stop'))
old=code('return UnityEditor.EditorSettings.enterPlayModeOptionsEnabled;')['data']['result']
try:
    code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;return "capture setup";')
    call('manage_editor',dict(action='play'));time.sleep(2)
    code('CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());CampusRift.UI.UIStateManager.Instance.EnterScene(true);var d=CampusRift.Levels.LevelDirector.Ensure();d.enabled=false;d.StartCoroutine(CampusRift.SkyBeast.P21StoryCinematic.Play(true));return "isolated ending capture / transient profile";');time.sleep(.3)
    code('CampusRift.SkyBeast.P21StoryCinematic.Active.SeekForCapture(1);return "credits shown";');time.sleep(.3)
    for page,name in [(0,'credits-title'),(1,'credits-sources')]:
        code('CampusRift.SkyBeast.P21StoryCinematic.Active.SetCreditPage('+str(page)+');return "page";');time.sleep(.3)
        code('UnityEngine.ScreenCapture.CaptureScreenshot("task/p21/screens/'+name+'.png");System.IO.File.WriteAllText("task/p21/screens/'+name+'-text-audit.json",UnityEngine.JsonUtility.ToJson(CampusRift.UI.ComicTextAudit.Scan("'+name+'"),true));return "capture requested";');time.sleep(.3)
    (root/'credits-caption-console.json').write_text(json.dumps(call('read_console',dict(action='get',types=['error'],count=20,format='detailed')),indent=2),encoding='utf-8')
    print('Credits caption captures refreshed from Unity; no harness or FPS rerun.',flush=True)
finally:
    try:code('CampusRift.SkyBeast.P21StoryCinematic.CancelActive();return "restored";')
    finally:
        call('manage_editor',dict(action='stop'))
        code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(old).lower()+';return "Edit restored";')
