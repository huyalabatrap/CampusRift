from pathlib import Path
import json,time
from p13_cli import call
root=Path('task/p21')
def code(s):return call('execute_code',dict(action='execute',code=s))
def save(name,value):(root/name).write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
call('manage_editor',dict(action='stop'))
old=code('return UnityEditor.EditorSettings.enterPlayModeOptionsEnabled;')['data']['result']
try:
    code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");return "menu scene";')
    call('manage_editor',dict(action='play'));time.sleep(2)
    code('UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.UIManager>().OpenCredits();return "actual menu action";');time.sleep(.5)
    audit=code('var menu=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.P21CreditsMenu>();var reports=new System.Collections.Generic.List<string>();for(int i=0;i<13;i++){menu.Show(i);reports.Add(UnityEngine.JsonUtility.ToJson(CampusRift.UI.ComicTextAudit.Scan("credits-menu-page"+(i+1))));}menu.Show(1);return string.Join("|P21_PAGE|",reports.ToArray());')['data']['result']
    state=code('return CampusRift.UI.UIStateManager.Instance.State.ToString();')['data']['result']
    save('credits-menu-audit.json',dict(state=state,pages=[json.loads(p) for p in audit.split('|P21_PAGE|')]));time.sleep(.3)
    code('UnityEngine.ScreenCapture.CaptureScreenshot("task/p21/screens/credits-menu.png");return "capture requested";');time.sleep(.3)
    result=code('var menu=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.P21CreditsMenu>();menu.transform.Find("P21 credits catalog/Next").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();var text=menu.transform.Find("P21 credits catalog/Credits content").GetComponent<TMPro.TMP_Text>().text;menu.transform.Find("P21 credits catalog/Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();return new {nextChangesPage=text.Contains("Lava Golem"),backReturnsMenu=CampusRift.UI.UIStateManager.Instance.State==CampusRift.UI.UIState.Menu};')['data']['result']
    save('credits-menu-buttons.json',result)
    save('credits-menu-console.json',call('read_console',dict(action='get',types=['error'],count=20,format='detailed')))
    print(result,flush=True)
finally:
    call('manage_editor',dict(action='stop'))
    code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(old).lower()+';UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return "Edit SampleScene restored";')
