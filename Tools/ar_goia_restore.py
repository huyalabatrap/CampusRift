from pathlib import Path
import json,shutil,hashlib
from ar_goia_session import connect,code,save
connect();backup=Path('Backups/AR-GoiA-pre-20261006');out=Path('task/ar/goiA')
state=code('return new {playing=UnityEditor.EditorApplication.isPlaying,compiling=UnityEditor.EditorApplication.isCompiling,building=UnityEditor.BuildPipeline.isBuildingPlayer};');assert not any(state.values()),state
user=Path('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift');rows=[]
for p in (backup/'Save').rglob('*'):
    if not p.is_file():continue
    rel=p.relative_to(backup/'Save');q=user/rel
    if q.exists() and q.read_bytes()!=p.read_bytes():
        dest=out/'post-qa-save'/rel;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(q,dest)
    q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q);rows.append(dict(path=rel.as_posix(),identical=q.read_bytes()==p.read_bytes(),sha256=hashlib.sha256(q.read_bytes()).hexdigest()))
settings=json.loads((out/'settings-before.json').read_text(encoding='utf-8-sig'))['result']['structuredContent']['data']['result']
script='UnityEngine.PlayerPrefs.SetString("CampusRift.Settings.v1",'+json.dumps(settings['settings'])+');'
for field,key in [('floor','Floor'),('safety','Safety'),('help','HelpSeen'),('occlusion','Occlusion')]:
    script+=('UnityEngine.PlayerPrefs.DeleteKey("CampusRift.AR.'+key+'");' if settings[field]<0 else 'UnityEngine.PlayerPrefs.SetInt("CampusRift.AR.'+key+'",'+str(settings[field])+');')
script+='UnityEngine.PlayerPrefs.Save();UnityEditor.EditorSettings.enterPlayModeOptions=(UnityEditor.EnterPlayModeOptions)'+str(settings['enterPlay'])+';UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(settings['enterEnabled']).lower()+';'
script+='UnityEditor.EditorUserBuildSettings.development='+str(settings['dev']).lower()+';UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=(UnityEngine.InputSystem.InputSettings.BackgroundBehavior)'+str(settings['inputBackground'])+';UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)'+str(settings['inputEditor'])+';UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return true;'
assert code(script)
restored=[]
for rel in ['Assets/Settings/Mobile_RPAsset.asset','Assets/ARRift/Settings/AR_RPAsset.asset','ProjectSettings/EditorSettings.asset','Assets/XR/Resources/XRSimulationRuntimeSettings.asset']:
    p=Path(rel);original=backup/rel;assert original.exists(),str(original)
    if p.read_bytes()!=original.read_bytes():
        dest=out/'build-generated'/rel;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest);shutil.copy2(original,p);restored.append(rel)
for rel in restored:
    if rel.startswith('Assets/'):code('UnityEditor.AssetDatabase.ImportAsset('+json.dumps(rel)+',UnityEditor.ImportAssetOptions.ForceUpdate);return true;')
save('restoration.json',dict(files=rows,restoredAssets=restored,settingsRestored=True,simulationRays=10))
print('Restored',len(rows),'save files;',restored)
