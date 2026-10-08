"""Restore original user bytes after AR smoke; archive modified QA files first."""
from pathlib import Path
import hashlib,json,shutil,sys
import unity_mcp as m
m.initialize()
def code(s):
    r=m.call('execute_code',{'action':'execute','code':s}).get('result',{}).get('structuredContent',{})
    if not r.get('success'):raise RuntimeError(str(r))
    return r['data']['result']
state=code('return new { playing=UnityEditor.EditorApplication.isPlaying, compiling=UnityEditor.EditorApplication.isCompiling, building=UnityEditor.BuildPipeline.isBuildingPlayer };')
assert not any(state.values()),state
original=Path(Path('task/p23/BACKUP.txt').read_text(encoding='utf-8-sig').strip())/'UserSave'
user=Path('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift')
archive=Path('task/ar/user-save-after-qa');rows=[]
for p in original.rglob('*'):
    if not p.is_file():continue
    rel=p.relative_to(original);dest=user/rel
    if dest.exists() and dest.read_bytes()!=p.read_bytes():
        q=archive/rel;q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(dest,q)
    dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
    rows.append({'path':rel.as_posix(),'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'identical':dest.read_bytes()==p.read_bytes()})
settings=json.loads(Path('task/p23/settings-before.json').read_text(encoding='utf-8-sig'))
script='UnityEngine.PlayerPrefs.SetString("CampusRift.Settings.v1",'+json.dumps(settings['settings'])+');UnityEngine.PlayerPrefs.Save();'
script+='UnityEditor.EditorSettings.enterPlayModeOptions=(UnityEditor.EnterPlayModeOptions)'+str(settings['enterPlay'])+';UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(settings['enterEnabled']).lower()+';'
script+='UnityEditor.EditorUserBuildSettings.development='+str(settings['dev']).lower()+';'
script+='UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=(UnityEngine.InputSystem.InputSettings.BackgroundBehavior)'+str(settings['inputBackground'])+';UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)'+str(settings['inputEditor'])+';UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);'
script+='return "original user settings restored";'
result=code(script)
Path('task/ar/user-restoration.json').write_text(json.dumps({'files':rows,'settings':result},indent=2))
print(result, len(rows),'files byte-identical')
