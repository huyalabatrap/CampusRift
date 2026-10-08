from ar_fix3_local import *
import shutil, hashlib

backup=Path(Path('task/ar/FIX3-BACKUP.txt').read_text(encoding='utf-8-sig').strip())
state=code('return new {playing=UnityEditor.EditorApplication.isPlaying,compiling=UnityEditor.EditorApplication.isCompiling,building=UnityEditor.BuildPipeline.isBuildingPlayer};')
assert not any(state.values()),state
archive=out/'post-qa-save'; user=Path('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift');rows=[]
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
script+='UnityEngine.PlayerPrefs.DeleteKey("CampusRift.AR.HelpSeen");UnityEngine.PlayerPrefs.Save();'
script+='UnityEditor.EditorSettings.enterPlayModeOptions=(UnityEditor.EnterPlayModeOptions)'+str(settings['enterPlay'])+';UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(settings['enterEnabled']).lower()+';'
script+='UnityEditor.EditorUserBuildSettings.development='+str(settings['dev']).lower()+';'
script+='UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=(UnityEngine.InputSystem.InputSettings.BackgroundBehavior)'+str(settings['inputBackground'])+';'
script+='UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=(UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)'+str(settings['inputEditor'])+';UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);'
script+='var settings=UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/XR/Resources/XRSimulationRuntimeSettings.asset");var so=new UnityEditor.SerializedObject(settings);so.FindProperty("m_EnvironmentScanParams.m_RaysPerCast").intValue=10;so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.EditorUtility.ClearDirty(settings);return true;'
assert code(script)
restored=[]
for rel in ['Assets/Settings/Mobile_RPAsset.asset','Assets/ARRift/Settings/AR_RPAsset.asset','ProjectSettings/EditorSettings.asset','Assets/XR/Resources/XRSimulationRuntimeSettings.asset']:
    p=Path(rel);original=out/'simulation-settings-original.asset' if '/XR/' in rel else backup/rel
    if p.read_bytes()!=original.read_bytes():
        dest=out/'build-generated'/rel;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
        shutil.copy2(original,p);restored.append(rel)
for rel in restored:
    if rel.startswith('Assets/'):
        code('UnityEditor.AssetDatabase.ImportAsset('+json.dumps(rel)+',UnityEditor.ImportAssetOptions.ForceUpdate);return true;')
save(out/'restoration.json',dict(files=rows,restoredAssets=restored,simulationRays=10,tutorialKeyRemoved=True))
progress('Khôi phục sau QA\n- Save gốc, PlayerPrefs/settings, tùy chọn Editor/Input và simulation 10 rays đã phục hồi. Prefilter phát sinh do build được lưu evidence rồi trả về baseline. Còn kiểm trạng thái cuối và báo cáo.')
print('Restored',len(rows),'save files;',restored,flush=True)
