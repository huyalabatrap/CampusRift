from ar_ui import *
connect()
def wait_for(expr,seconds=50):
 deadline=time.time()+seconds
 while time.time()<deadline:
  if code('return '+expr+';'):return
  time.sleep(1)
 raise TimeoutError(expr)
def prepare(scene):
 stop();code('EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene('+json.dumps(scene)+');return true;')
 for row in json.loads((OUT/'original-editor.json').read_text(encoding='utf-8'))['devPrefs']:code('PlayerPrefs.SetInt('+json.dumps(row['key'])+',0);return true;')
 call('manage_editor',{'action':'play'})
 wait_for('EditorApplication.isPlaying && !EditorApplication.isCompiling && CampusRift.UI.SettingsManager.Instance!=null && CampusRift.Progression.ProfileService.Instance!=null')
 code('Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.Subtitles=false;s.TextSize=0;s.ControlMode=CampusRift.Controls.ControlMode.PC;s.Language=0;s.ResolutionWidth=2400;s.ResolutionHeight=1080;s.Fullscreen=false;CampusRift.UI.SettingsManager.Instance.Apply(s,false);CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);PlayerPrefs.SetInt("CampusRift.AR.HelpSeen",1);return true;')
def setup(mode):
 if code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();return f!=null&&f.Root!=null&&f.Mode.id=='+json.dumps(mode)+';'):
  code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.GetComponent<CampusRift.AR.ARBattleHUD>().SetHelp(false);f.GetComponent<CampusRift.AR.MockGestureSource>().enabled=false;return true;')
  code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.UserPaused=false;f.enabled=true;f.GetComponent<CampusRift.AR.ARMonsterDirector>().StartBattle();f.Shrine.SetProgressionMaxHealth(10000);f.Shrine.Revive(1,0);return true;')
  return
 import b1007_ar as old
 old.prepare=prepare
 old.code=lambda s:{'data':{'result':code(s.replace('float best=.21f;','float best=.18001f;'))}}
 original_wait=old.wait_for
 def gate(expr,seconds=40):
  if expr=='!UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>().Paused':code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.GetComponent<CampusRift.AR.ARBattleHUD>().SetHelp(false);return true;')
  return original_wait(expr,seconds)
 old.wait_for=gate
 old.setup(mode)
 code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.UserPaused=false;f.Shrine.SetProgressionMaxHealth(10000);f.Shrine.Revive(1,0);return true;')
def shot(name,w,h):
 code('UIValidation.SetResolution('+str(w)+','+str(h)+');return true;');time.sleep(.5)
 path='task/ar/screens/ui-fix/'+name+'-'+str(w)+'x'+str(h)+'.png'
 code('CampusRift.Controls.LookCapture.Image('+json.dumps(path)+');return true;')
 audit=code((ROOT/'task/ar/ui-fix/layout-audit.cs').read_text(encoding='utf-8'));save('views/'+name+'-'+str(w)+'.json',audit)
 print(path,flush=True)
if __name__=='__main__':
 screens=ROOT/'task/ar/screens/ui-fix';screens.mkdir(parents=True,exist_ok=True)
 mode=sys.argv[1] if len(sys.argv)>1 else 'training'
 setup(mode)
 for w,h in [(2400,1080),(1600,720)]:shot(mode,w,h)
 if mode=='defense':
  code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.UserPaused=false;f.GetComponent<CampusRift.AR.ARTechHUD>().Open(true);return true;')
  for w,h in [(2400,1080),(1600,720)]:shot('technology',w,h)
  code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.GetComponent<CampusRift.AR.ARTechHUD>().GetComponentInChildren<UnityEngine.UI.ScrollRect>(true).verticalNormalizedPosition=0;return true;')
  for w,h in [(2400,1080),(1600,720)]:shot('technology-details',w,h)
  code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.GetComponent<CampusRift.AR.ARTechHUD>().Open(false);f.GetComponent<CampusRift.AR.ARBattleHUD>().SetMenu(true);return true;')
  for w,h in [(2400,1080),(1600,720)]:shot('menu',w,h)
  code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.GetComponent<CampusRift.AR.ARBattleHUD>().SetMenu(false);UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARDeviceDiagnostics>().Toggle();return true;')
  for w,h in [(2400,1080),(1600,720)]:shot('diagnostics',w,h)
  code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARDeviceDiagnostics>().Collapse();return true;')
  for w,h in [(2400,1080),(1600,720)]:shot('diagnostics-collapsed',w,h)
  code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARDeviceDiagnostics>().Toggle();var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.UserPaused=false;f.GetComponent<CampusRift.AR.ARModeSelectionHUD>().Open();return true;')
  for w,h in [(2400,1080),(1600,720)]:shot('mode-selection',w,h)
  code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var selection=f.GetComponent<CampusRift.AR.ARModeSelectionHUD>();selection.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="Start selected").onClick.Invoke();f.placement.AutoPlacement=false;return true;')
  for w,h in [(2400,1080),(1600,720)]:shot('placement',w,h)
 console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save('visual-console-'+mode+'.json',console);assert not console.get('data'),console
 milestone('Mốc chụp '+mode+': ảnh thật GameView tại screens/ui-fix/ hai kích thước; XR Simulation/polygon/anchor, không phát cử chỉ hay gọi suite. Mỗi ảnh lưu layout-audit về safeArea/text/rect. Cần xem ảnh và sửa các vấn đề layout còn lại trước build; chưa report cuối.')
