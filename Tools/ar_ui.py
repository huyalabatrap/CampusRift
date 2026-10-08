from pathlib import Path
import json, shutil, hashlib, subprocess, difflib, time, sys
import unity_mcp as u
ROOT=Path.cwd();OUT=ROOT/'task/ar/ui-fix';BACKUP=ROOT/'Backups/AR-UI-pre-20261007';OUT.mkdir(parents=True,exist_ok=True)
SDK=Path('C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK');ADB=SDK/'platform-tools/adb.exe'
def save(name,value):
 p=OUT/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
def milestone(message):
 with (ROOT/'task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n## AR UI OVERLAP — '+time.strftime('%Y-%m-%d %H:%M:%S')+'\n'+message+'\n')
def connect():
 u.initialize()
 resource=u.rpc('resources/read',{'uri':'mcpforunity://instances'})['result']
 instances=json.loads(resource['contents'][0]['text'])['instances']
 if len(instances)==1:
  row=instances[0]
  result=u.call('set_active_instance',{'instance':row.get('id') or row.get('hash') or row.get('name')})
def call(name,args):
 for attempt in range(25):
  r=u.call(name,args)['result']
  if r.get('isError') and name=='execute_code' and 'Unknown tool' in str(r):r=u.call('execute_custom_tool',{'tool_name':name,'parameters':args})['result']
  v=r.get('structuredContent')
  if v is None:
   try:v=json.loads(r['content'][0]['text'])
   except json.JSONDecodeError:raise RuntimeError(r)
  if isinstance(v.get('result'),dict):v=v['result']
  if v.get('success') is not False:return v
  if (v.get('message') or v.get('data')) and not any(t in str(v).lower() for t in ['no_unity_session','not ready','retry','compiling','domain reload','busy']):raise RuntimeError(v)
  time.sleep(2)
 raise RuntimeError(v)
def code(s):return call('execute_code',{'action':'execute','code':s})['data']['result']
def stop():
 code('var x=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>();if(x!=null)x.Shutdown();return true;')
 call('manage_editor',{'action':'stop'})
if __name__=='__main__':
 connect()
 if sys.argv[1]=='prepare':
  assert not (OUT/'original-editor.json').exists(),'Resume existing snapshot'
  snap=code((ROOT/'Tools/tech2_context.cs').read_text(encoding='utf-8'))
  snap['devPrefs']=code('return new[]{"CampusRift.DevMode","CampusRift.DevMode.Invincible","CampusRift.DevMode.NoCooldown"}.Select(k=>new {key=k,exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}).ToArray();')
  snap['build']=code('return new {EditorUserBuildSettings.development,EditorUserBuildSettings.buildAppBundle,EditorUserBuildSettings.exportAsGoogleAndroidProject,inputBackground=(int)UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior,inputEditor=(int)UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode};')
  assert not snap['playing'] and not snap['dirty'],snap
  save('original-editor.json',snap)
  for name in ['Assets/ARRift','Assets/Plugins/Android','ProjectSettings','Packages/manifest.json','Assets/Controls/CampusInputSettings.asset','Assets/Settings/Mobile_RPAsset.asset','Assets/XR/Resources/XRSimulationRuntimeSettings.asset']:
   src=ROOT/name;dest=BACKUP/name;dest.parent.mkdir(parents=True,exist_ok=True)
   if src.is_dir():shutil.copytree(src,dest)
   elif src.exists():shutil.copy2(src,dest)
  shutil.copytree(snap['savePath'],BACKUP/'save')
  protected=list((ROOT/'task').glob('run-*.ps1'))+[ROOT/'task/codex-accounts.json']+list((ROOT/'Assets/ARRift/Validation').glob('*.cs'))
  save('protected-files.json',{p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in protected if p.exists()})
  save('console-before.json',call('read_console',{'action':'get','types':['error'],'count':100}))
  devices=subprocess.check_output([str(ADB),'devices'],text=True,encoding='utf-8');save('adb-before.json',{'devices':devices})
  milestone('Đã đọc toàn bộ brief UTF-8 và PROGRESS/REPORT; NOHAND2 đã hoàn tất, chưa có thay đổi UI vòng này. Đã xem ảnh máy thật. Snapshot Editor/save/settings và backup AR-UI-pre-20261007; bảo vệ run-*.ps1/accounts và toàn bộ suite. Không chạy hồi quy. Physics module đã có trong manifest; primitive sphere/cylinder tự tạo collider rồi Destroy là nguồn cần giữ stripping. Tiếp sửa layout/modal/menu/diagnostics, compile, ảnh và APK.')
  print(json.dumps(snap,ensure_ascii=True))
 elif sys.argv[1]=='code':print(json.dumps(code(Path(sys.argv[2]).read_text(encoding='utf-8-sig')),ensure_ascii=True))
