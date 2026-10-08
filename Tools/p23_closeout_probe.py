"""Related branch checks after raw failures; no full suite rerun or weakened assertion.

Checks the corrected mobile page-down target, final Settings wording and original
VoidWall sprint/Q assertion on a clear existing campus point. Disposable profile.
"""
from pathlib import Path
import json,time
import unity_mcp as m

root=Path.cwd();out=root/'task/p23/closeout-probe';out.mkdir(parents=True,exist_ok=True)
m.initialize()
def call(name,args):
    for _ in range(40):
        raw=m.call(name,args);v=raw.get('result',{}).get('structuredContent',raw.get('result',raw))
        if v.get('success') is not False:return v
        if v.get('message')=='Compilation failed':raise RuntimeError(v)
        transient=v.get('data',{}).get('reason') in ('no_unity_session','unity_session_not_ready')
        if not transient and not any(s in str(v.get('message','')).lower() for s in ('retry','not ready','compiling','domain reload','busy')):raise RuntimeError(v)
        time.sleep(2)
    raise RuntimeError(v)
def code(s):
    r=call('execute_code',{'action':'execute','code':s})
    return r.get('data',{}).get('result',r)
def audit(label):
    return code('var r=CampusRift.UI.ComicTextAudit.Scan("'+label+'");System.IO.File.WriteAllText("task/p23/closeout-probe/'+label+'.json",UnityEngine.JsonUtility.ToJson(r,true));return r.issues.Count;')
rows=json.loads((out/'result.json').read_text(encoding='utf-8'))['checks'] if (out/'result.json').exists() else []
done={r['label'] for r in rows if r['pass']}
def check(label,ok,evidence=None):
    if label in done:return
    rows[:]=[r for r in rows if r['label']!=label]
    rows.append({'label':label,'pass':bool(ok),'evidence':evidence})
    (out/'result.json').write_text(json.dumps({'checks':rows,'passed':sum(r['pass'] for r in rows),'failed':sum(not r['pass'] for r in rows)},ensure_ascii=False,indent=2),encoding='utf-8')

call('manage_editor',{'action':'stop'})
code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return "opened";')
call('read_console',{'action':'clear'});call('manage_editor',{'action':'play'});time.sleep(3)
code('Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData{cultivation=new CampusRift.Progression.CultivationData{realm=6,tier=5}});UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.PlayerMonsterHealth>().Revive(1,600);UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;foreach(var v in UnityEngine.InputSystem.InputSystem.devices)UnityEngine.InputSystem.InputSystem.EnableDevice(v);UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return "QA isolated";')

# The fixture positions the real UI in a paused rest state; no claim of a full battle.
code('CampusRift.UI.UIStateManager.Instance.EnterScene(true);var d=CampusRift.Levels.LevelDirector.Ensure();d.Begin(CampusRift.Levels.LevelCatalog.Instance.Get(9));d.enabled=false;CampusRift.SkyBeast.FireBreathCycle.Instance.StopCycle();typeof(CampusRift.Levels.LevelDirector).GetProperty("State").SetValue(d,CampusRift.Levels.LevelDirector.Phase.Rest);typeof(CampusRift.Levels.LevelDirector).GetProperty("RestRemaining").SetValue(d,15f);return "UI rest fixture";')
for language in (0,1):
    for size in (0,1,2):
        label=f'mobile-rest-lang{language}-size{size}'
        if label+' round targets >=68' in done and label+' ComicTextAudit' in done:continue
        code('var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.Language=(CampusRift.Localization.GameLanguage)'+str(language)+';s.TextSize='+str(size)+';s.AccessibleColors=CampusRift.UI.AccessiblePalette.ColorBlind;s.ControlMode=CampusRift.Controls.ControlMode.Mobile;s.Fullscreen=false;s.ResolutionWidth=1920;s.ResolutionHeight=1080;CampusRift.UI.SettingsManager.Instance.Apply(s,false);CampusRift.UI.UIStateManager.Instance.EnterScene(true);CampusRift.UI.RestLoadoutUI.Instance.Open();CampusRift.UI.RestLoadoutUI.Instance.SelectSlot(0);return "mobile panel";')
        time.sleep(.4)
        data=code('var p=CampusRift.UI.RestLoadoutUI.Instance;var b=p.GetComponentsInChildren<UnityEngine.UI.Button>();return new {count=b.Length,valid=System.Linq.Enumerable.All(b,x=>x.GetComponent<CampusRift.UI.RiftGraphic>()!=null&&x.GetComponent<UnityEngine.RectTransform>().rect.width>=68&&x.GetComponent<UnityEngine.RectTransform>().rect.height>=68)};')
        check(label+' round targets >=68',data['valid'],data)
        check(label+' ComicTextAudit',audit(label)==0)
        if language==0 and size==2:
            page=code('var p=CampusRift.UI.RestLoadoutUI.Instance;var scroll=p.GetComponentInChildren<UnityEngine.UI.ScrollRect>();scroll.verticalNormalizedPosition=1;var b=System.Linq.Enumerable.First(p.GetComponentsInChildren<UnityEngine.UI.Button>(),x=>x.name=="Scroll next");b.onClick.Invoke();UnityEngine.ScreenCapture.CaptureScreenshot("task/p23/screens/rest-loadout-mobile-scroll-final.png");return scroll.verticalNormalizedPosition;')
            check('Real circular page-down button scrolls to later skills',page<.95,page)
        code('CampusRift.UI.RestLoadoutUI.Instance.Close();return "closed";')
code('CampusRift.Levels.LevelDirector.Instance.End();CampusRift.UI.UIStateManager.Instance.EnterScene(false);return "menu";')
for language in (0,1):
    for size in (0,1,2):
        label=f'settings-final-lang{language}-size{size}'
        if label+' ComicTextAudit' in done:continue
        code('var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.Language=(CampusRift.Localization.GameLanguage)'+str(language)+';s.TextSize='+str(size)+';s.ControlMode=CampusRift.Controls.ControlMode.PC;s.Subtitles=true;s.SlowReading=true;CampusRift.UI.SettingsManager.Instance.Apply(s,false);CampusRift.UI.UIStateManager.Instance.EnterScene(false);CampusRift.UI.UIStateManager.Instance.OpenSettings();UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.SettingsUI>().SelectTab(3);return "comfort";')
        time.sleep(.4);label=f'settings-final-lang{language}-size{size}'
        check(label+' ComicTextAudit',audit(label)==0)
        if language==0 and size==2:
            code('UnityEngine.ScreenCapture.CaptureScreenshot("task/p23/screens/settings-accessibility-large.png");return "final wording photo";')
        code('CampusRift.UI.UIStateManager.Instance.Back();return "back";')

# Comic billboards do not live under a Canvas; verify their size ratio separately.
code('CampusRift.UI.UIStateManager.Instance.EnterScene(true);CampusRift.UI.UIStateManager.Instance.Pause();var a=CampusRift.Levels.LevelCatalog.Instance.Get(1).waves[0].entries[0].archetype;var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();var e=CampusRift.Enemies.EnemyPool.Ensure().Spawn(a,p.transform.position+UnityEngine.Vector3.right*4,CampusRift.Enemies.EnemyScaling.Default,false);e.Brain.enabled=false;e.Motor.Stop();CampusRift.Combat.EnemyHealthBars.Instance.SetName(e.Vitality,"QA WORLD TEXT");return "paused billboard fixture";')
for size in (0,1,2):
    if 'World nameplate size '+str(size) in done and 'World damage number size '+str(size) in done:continue
    expected=(1,1.15,1.3)[size]
    amount=231+size
    code('var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TextSize='+str(size)+';CampusRift.UI.SettingsManager.Instance.Apply(s,false);var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();var pool=CampusRift.Combat.DamageNumberPool.Instance;typeof(CampusRift.Combat.DamageNumberPool).GetField("groupAt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(pool,-1f);pool.Show(p.transform.position+UnityEngine.Vector3.up*2,'+str(amount)+',CampusRift.Combat.Element.None,false,null);return "world size";')
    time.sleep(.3)
    data=code('var b=System.Linq.Enumerable.First(CampusRift.Combat.EnemyHealthBars.Instance.GetComponentsInChildren<TMPro.TextMeshPro>(),t=>t.text=="QA WORLD TEXT");var n=System.Linq.Enumerable.First(CampusRift.Combat.DamageNumberPool.Instance.GetComponentsInChildren<TMPro.TextMeshPro>(),t=>t.text.EndsWith("'+str(amount)+'"));var cam=UnityEngine.Camera.main;float bd=UnityEngine.Vector3.Distance(cam.transform.position,b.transform.parent.position);float nd=UnityEngine.Vector3.Distance(cam.transform.position,n.transform.position);return new {nameRatio=b.transform.parent.localScale.x/UnityEngine.Mathf.Clamp(bd*.018f,.6f,3f),numberRatio=n.transform.localScale.x/(nd*.023f*1.3f)};')
    check('World nameplate size '+str(size),abs(data['nameRatio']-expected)<.015,data)
    check('World damage number size '+str(size),abs(data['numberRatio']-expected)<.015,data)
code('CampusRift.Enemies.EnemyPool.Instance.ReleaseAll();CampusRift.UI.UIStateManager.Instance.Resume();return "world UI cleaned";')

# Reproduce the original input assertion at a clear existing point, with no geometry edits.
code('CampusRift.UI.UIStateManager.Instance.EnterScene(true);var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.ControlMode=CampusRift.Controls.ControlMode.PC;s.TextSize=0;s.Language=CampusRift.Localization.GameLanguage.Vietnamese;CampusRift.UI.SettingsManager.Instance.Apply(s,false);var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();var c=p.GetComponent<UnityEngine.CharacterController>();p.enabled=true;p.spawnPosition=new UnityEngine.Vector3(-5,.13f,2);p.spawnYaw=0;p.ReturnToSpawn();p.GetComponent<CampusRift.Skills.SkillLoadout>().Equip(0,"hu-khong-ket-gioi");UnityEngine.Cursor.lockState=UnityEngine.CursorLockMode.Locked;foreach(var b in UnityEngine.Object.FindObjectsByType<CampusRift.Monsters.MonsterBrain>(UnityEngine.FindObjectsSortMode.None))b.enabled=false;return new {point=p.transform.position,obstacles=UnityEngine.Physics.OverlapCapsule(p.transform.position+UnityEngine.Vector3.up*.6f,p.transform.position+UnityEngine.Vector3.up*1.5f,.45f,CampusRift.Combat.CombatLine.SolidMask,UnityEngine.QueryTriggerInteraction.Ignore).Length};')
time.sleep(.5)
code('UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.LeftShift));return "sprint";')
time.sleep(.25)
before=code('var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();return new {speed=p.CurrentSpeed,x=p.transform.position.x,z=p.transform.position.z,charges=p.GetComponent<CampusRift.Skills.VoidWallSkill>().Charges};')
code('UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.LeftShift,UnityEngine.InputSystem.Key.Q));return "Q down";')
time.sleep(.04)
code('UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.LeftShift));return "Q up";')
time.sleep(.1)
after=code('UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();var w=p.GetComponent<CampusRift.Skills.VoidWallSkill>();return new {speed=p.CurrentSpeed,x=p.transform.position.x,z=p.transform.position.z,charges=w.Charges,wall=w.LastDeployed!=null&&w.LastDeployed.IsSolid};')
check('Original sprint/Q conditions on clear campus point',before['speed']>3.4 and after['charges']==before['charges']-1 and after['wall'],{'before':before,'after':after})
console=call('read_console',{'action':'get','types':['error','warning'],'count':100,'format':'detailed','include_stacktrace':True})
(out/'console.json').write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')
check('No runtime exception in related probe',not any(x.get('type') in ('Error','Exception','Assert') for x in console.get('data',[]) if isinstance(x,dict)))
call('manage_editor',{'action':'stop'})
with (root/'task/p23/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n- Related closeout probe: '+str(sum(r['pass'] for r in rows))+' PASS / '+str(sum(not r['pass'] for r in rows))+' FAIL; raw task/p23/closeout-probe. Giữ nguyên hai suite FAIL thô, không chạy lại toàn bộ suite.\n')
print('RELATED PROBE',sum(r['pass'] for r in rows),sum(not r['pass'] for r in rows),flush=True)
