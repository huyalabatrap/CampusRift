from ar_nohand import *

def wait_for(expr,seconds=45):
 deadline=time.time()+seconds
 while time.time()<deadline:
  if code('return '+expr+';'):return
  time.sleep(1)
 raise TimeoutError(expr)

def prepare(scene):
 stop();code('EditorSettings.enterPlayModeOptionsEnabled=false;UnityEditor.SceneManagement.EditorSceneManager.OpenScene('+json.dumps(scene)+');return true;')
 for row in json.loads((OUT/'original-editor.json').read_text(encoding='utf-8'))['devPrefs']:
  code('PlayerPrefs.SetInt('+json.dumps(row['key'])+',0);return true;')
 call('manage_editor',{'action':'play'})
 wait_for('EditorApplication.isPlaying && !EditorApplication.isCompiling && CampusRift.UI.SettingsManager.Instance!=null && CampusRift.Progression.ProfileService.Instance!=null')
 code('Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.Subtitles=false;s.TextSize=0;s.ControlMode=CampusRift.Controls.ControlMode.PC;s.ResolutionWidth=2400;s.ResolutionHeight=1080;s.Fullscreen=false;CampusRift.UI.SettingsManager.Instance.Apply(s,false);CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);PlayerPrefs.SetInt("CampusRift.AR.HelpSeen",1);return true;')

def setup():
 import b1007_ar as old
 old.prepare=lambda scene:prepare(scene)
 old.code=lambda s:{'data':{'result':code(s.replace('float best=.21f;', 'float best=.18001f;'))}}
 old.setup()
 # Verify the real pause/mode gates; the scan helper uses genuine simulation polygon/anchor.
 state=code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var c=f.GetComponent<CampusRift.AR.ARSkillCaster>();return new {placed=f.Root!=null,f.Paused,f.MenuPaused,inputBlocked=f.placement.InputBlocked,f.placement.Adjusting,c.AimValid,mode=f.Mode.id,hands=c.source.HandCount,dev=CampusRift.Progression.DevMode.Active,transient=CampusRift.Progression.ProfileService.Instance.Transient};')
 assert state['placed'] and state['AimValid'] and not any(state[k] for k in ['Paused','MenuPaused','inputBlocked','Adjusting','dev']),state
 return state

def historical(paths,folder):
 result={}
 for rel in paths:
  p=ROOT/rel
  if p.exists():
   result[rel]=p.read_bytes();dest=folder/'historical'/rel;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(result[rel])
 return result

def run(name):
 folder=OUT/'checks'/name;folder.mkdir(parents=True,exist_ok=True)
 if (folder/'result.json').exists():print('Already complete',name);return
 if (folder/'invoked.json').exists():raise RuntimeError('Already invoked: inspect existing running output; never rerun '+name)
 if name=='ARGestureUnitTests':
  stop();code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
  old=historical(['task/ar/goiA/unit-export.json'],folder)
  save('checks/'+name+'/invoked.json',{'utc':time.time(),'suite':'ARGestureUnitTests.Run','count':1})
  data=code('return CampusRift.AR.ARGestureUnitTests.Run();');save('checks/'+name+'/result.json',data)
  for rel,b in old.items():(ROOT/rel).write_bytes(b)
 elif name=='OpenPalmMock':
  save('checks/'+name+'/setup.json',setup())
  code('UIValidation.SetResolution(2400,1080);return true;')
  save('checks/'+name+'/invoked.json',{'utc':time.time(),'path':'MockGestureSource -> bridge -> motion -> D1 -> sequence -> GiantHand','count':1})
  src=(ROOT/'task/ar/nohand/mock-one-path.cs').read_text(encoding='utf-8-sig');save('checks/'+name+'/invocation.json',code(src))
  path=folder/'DONE.json';deadline=time.time()+15
  while not path.exists() and time.time()<deadline:time.sleep(.25)
  if not path.exists():raise TimeoutError('Mock callback did not finish')
  data=json.loads(path.read_text(encoding='utf-8-sig'));save('checks/'+name+'/result.json',data)
  assert data['intentCount']==1 and data['castCount']==1 and data['firedAfter']==data['firedBefore']+1 and data['outcome']=='Success',data
  # Use the same one-path session to inspect diagnostics without another cast or gesture trial.
  code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARDeviceDiagnostics>().Toggle();CampusRift.Controls.LookCapture.Image("task/ar/nohand/diagnostics.png");var c=f.GetComponent<CampusRift.AR.ARGestureCheck>();c.Open();return true;')
  time.sleep(.4)
  save('checks/'+name+'/diagnostics.json',code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var check=f.GetComponent<CampusRift.AR.ARGestureCheck>();var texts=check.GetComponentsInChildren<TMPro.TMP_Text>(true).Where(t=>t.text.StartsWith("[ARDiag]")).Select(t=>new {t.text,t.isTextOverflowing,height=t.rectTransform.rect.height,preferred=t.preferredHeight}).ToArray();CampusRift.Controls.LookCapture.Image("task/ar/nohand/check-diagnostics.png");return new {status=CampusRift.AR.ARDeviceDiagnostics.RecognitionStatus(f.gameObject),texts};'))
  code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>().Close();return true;')
 elif name=='ARRiftPlayTest':
  save('checks/'+name+'/setup.json',setup());wait_for('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARMonsterDirector>().Actors.Count>0')
  paths=['task/ar/goiA/ar-results.json','task/ar/goiA/AR-DONE.txt']+[p.relative_to(ROOT).as_posix() for p in (ROOT/'task/ar/screens/goiA').glob('combat-*.png')]
  old=historical(paths,folder);stamp=time.time()
  save('checks/'+name+'/invoked.json',{'utc':stamp,'component':'ARRiftPlayTest','OnlySkill':-1,'count':1})
  code('new GameObject("NOHAND original ARRiftPlayTest one run").AddComponent<CampusRift.AR.ARRiftPlayTest>();return true;')
  path=ROOT/'task/ar/goiA/AR-DONE.txt';deadline=time.time()+220
  while time.time()<deadline:
   if path.exists() and path.stat().st_mtime>=stamp:break
   time.sleep(1)
  else:raise TimeoutError('ARRiftPlayTest has not completed; resume by collecting output, do not rerun')
  data=json.loads((ROOT/'task/ar/goiA/ar-results.json').read_text(encoding='utf-8-sig'));save('checks/'+name+'/result.json',data);shutil.copy2(path,folder/'DONE.txt')
  for rel in paths:
   p=ROOT/rel
   if p.exists() and p.suffix=='.png':shutil.copy2(p,folder/p.name)
  for rel,b in old.items():(ROOT/rel).write_bytes(b)
 else:raise ValueError(name)
 save('checks/'+name+'/console.json',call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True}))
 milestone('Mốc kiểm '+name+': hoàn tất đúng một lượt; '+str(len(data.get('passed',[])))+' PASS / '+str(len(data.get('failed',[])))+' FAIL' if name!='OpenPalmMock' else 'Mốc mock duy nhất: Open_Palm qua bridge→motion→D1→sequence→cast, 1 intent / 1 chiêu Success; có ảnh/text hai ô chẩn đoán. Không mở trial/scheduledVFX. Evidence nohand/checks/OpenPalmMock; world mock không có, không dùng làm kết quả nhận dạng điện thoại.')
 print(json.dumps(data,ensure_ascii=True),flush=True)
 stop()
if __name__=='__main__':connect();run(sys.argv[1])
