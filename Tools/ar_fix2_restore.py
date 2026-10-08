from ar_fix2_local import *
import shutil,hashlib
backup=Path(Path('task/ar/FIX2-BACKUP.txt').read_text(encoding='utf-8-sig').strip())
state=code('return new {playing=UnityEditor.EditorApplication.isPlaying,compiling=UnityEditor.EditorApplication.isCompiling,building=UnityEditor.BuildPipeline.isBuildingPlayer};')
assert not any(state.values()),state
archive=out/'post-qa-save';user=Path('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift');rows=[]
for p in (backup/'UserSave').rglob('*'):
    if not p.is_file():continue
    rel=p.relative_to(backup/'UserSave');q=user/rel
    if q.exists() and q.read_bytes()!=p.read_bytes():
        dest=archive/rel;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(q,dest)
    q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q)
    rows.append(dict(path=rel.as_posix(),sha256=hashlib.sha256(q.read_bytes()).hexdigest(),identical=q.read_bytes()==p.read_bytes()))
settings=json.loads((out/'settings-before.json').read_text(encoding='utf-8-sig'))
script='UnityEngine.PlayerPrefs.SetString("CampusRift.Settings.v1",'+json.dumps(settings['settings'])+');'
for key in ['floor','safety']:
    script+='UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.'+key.capitalize()+'",'+str(settings[key])+');'
script+='UnityEngine.PlayerPrefs.Save();'
script+='UnityEditor.EditorSettings.enterPlayModeOptions=(UnityEditor.EnterPlayModeOptions)'+str(settings['enterPlay'])+';UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(settings['enterEnabled']).lower()+';'
script+='UnityEditor.EditorUserBuildSettings.development='+str(settings['dev']).lower()+';'
script+='UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=(UnityEngine.InputSystem.InputSettings.BackgroundBehavior)'+str(settings['inputBackground'])+';'
script+='UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)'+str(settings['inputEditor'])+';UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return true;'
assert code(script)
restored=[]
for rel in ['Assets/Settings/Mobile_RPAsset.asset','Assets/ARRift/Settings/AR_RPAsset.asset','ProjectSettings/EditorSettings.asset']:
    p=Path(rel);original=backup/rel
    if p.read_bytes()!=original.read_bytes():
        dest=out/'build-generated'/rel;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
        shutil.copy2(original,p);restored.append(rel)
for rel in restored:
    if rel.startswith('Assets/'):
        code('UnityEditor.AssetDatabase.ImportAsset('+json.dumps(rel)+',UnityEditor.ImportAssetOptions.ForceUpdate);return true;')
save(out/'restoration.json',dict(files=rows,restoredAssets=restored))
print('Restored',len(rows),'save files;',restored,flush=True)
