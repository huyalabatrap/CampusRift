from pathlib import Path
import json,shutil,hashlib,subprocess,difflib,time,sys
import unity_mcp as u
ROOT=Path.cwd();OUT=ROOT/'task/ar/nohand2';BACKUP=ROOT/'Backups/AR-NOHAND2-pre-20261007';OUT.mkdir(parents=True,exist_ok=True)
SDK=Path('C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK');ADB=SDK/'platform-tools/adb.exe'
def save(name,value):
 p=OUT/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
def milestone(message):
 with (ROOT/'task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n## AR NOHAND2 — '+time.strftime('%Y-%m-%d %H:%M:%S')+'\n'+message+'\n')
def connect():u.initialize()
def call(name,args):
 for attempt in range(25):
  r=u.call(name,args)['result'];v=r.get('structuredContent')
  if v is None:v=json.loads(r['content'][0]['text'])
  if isinstance(v.get('result'),dict):v=v['result']
  if v.get('success') is not False:return v
  if v.get('data') is not None or v.get('message') and not any(t in str(v).lower() for t in ['no_unity_session','not ready','retry','compiling','domain reload','busy']):raise RuntimeError(v)
  time.sleep(2)
 raise RuntimeError(v)
def code(s):return call('execute_code',{'action':'execute','code':s})['data']['result']
def stop():
 code('var x=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>();if(x!=null)x.Shutdown();return true;')
 call('manage_editor',{'action':'stop'})
def device_log(name='nohand2-realme.txt'):
 devices=subprocess.check_output([str(ADB),'devices'],text=True,encoding='utf-8');save('adb-devices.json',{'devices':devices})
 connected='WGH6S8I7GIMBGQKR\tdevice' in devices
 dest=ROOT/'task/ar/device-logs'/name;dest.parent.mkdir(exist_ok=True)
 if connected:
  data=subprocess.check_output([str(ADB),'-s','WGH6S8I7GIMBGQKR','logcat','-d','-v','time','-s','Unity'],text=True,encoding='utf-8',errors='replace');dest.write_text(data,encoding='utf-8')
  save('device-log-matches.json',[l for l in data.splitlines() if any(t in l for t in ['[ARDiag]','[ARGesture]','Error','Exception','onError'])])
 else:dest.write_text('No adb device WGH6S8I7GIMBGQKR connected. '+time.strftime('%Y-%m-%d %H:%M:%S')+'\n'+devices,encoding='utf-8')
 return connected
if __name__=='__main__':
 connect()
 if sys.argv[1]=='prepare':
  assert not (OUT/'original-editor.json').exists(),'Snapshot exists; resume without replacement'
  snap=code((ROOT/'Tools/tech2_context.cs').read_text(encoding='utf-8')+'')
  snap['devPrefs']=code('return new[]{"CampusRift.DevMode","CampusRift.DevMode.Invincible","CampusRift.DevMode.NoCooldown"}.Select(k=>new {key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray();')
  snap['build']=code('return new {EditorUserBuildSettings.development,EditorUserBuildSettings.buildAppBundle,EditorUserBuildSettings.exportAsGoogleAndroidProject,inputBackground=(int)UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior,inputEditor=(int)UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode};')
  save('original-editor.json',snap);assert not snap['playing'] and not snap['dirty'],snap
  for name in ['Assets/ARRift','Assets/Plugins/Android','ProjectSettings','Assets/Controls/CampusInputSettings.asset','Assets/Settings/Mobile_RPAsset.asset','Assets/XR/Resources/XRSimulationRuntimeSettings.asset']:
   src=ROOT/name;dest=BACKUP/name;dest.parent.mkdir(parents=True,exist_ok=True)
   if src.is_dir():shutil.copytree(src,dest)
   elif src.exists():shutil.copy2(src,dest)
  shutil.copytree(snap['savePath'],BACKUP/'save')
  protected=list((ROOT/'task').glob('run-*.ps1'))+[ROOT/'task/codex-accounts.json']
  save('protected-files.json',{p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in protected if p.exists()})
  console=call('read_console',{'action':'get','types':['error'],'count':100});save('console-before.json',console)
  print('Realme connected:',device_log())
  milestone('Tiếp quản: đã đọc đủ 3 report bối cảnh và brief UTF-8; chưa có nguồn/evidence/report NOHAND. Snapshot nohand/original-editor.json giữ typed DevMode, save, settings, Input, GameView, EnterPlay; backup Backups/AR-NOHAND2-pre-20261007. Không sửa điều phối/accounts. Chỉ một mock Open_Palm và mỗi suite ARGestureUnitTests/ARRiftPlayTest một lượt sau sửa. ADB và log: nohand/adb-devices.json, device-logs/nohand-realme.txt.')
 elif sys.argv[1]=='code':print(json.dumps(code(Path(sys.argv[2]).read_text(encoding='utf-8-sig')),ensure_ascii=True))
