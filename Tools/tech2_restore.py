"""Restore the Job6 photo session's original user data and Editor preferences."""
from pathlib import Path
import json, shutil, hashlib
import unity_mcp as m

out=Path('task/batch-1007')
original=json.loads((out/'6-original-editor.json').read_text(encoding='utf-8-sig'))['data']['result']
backup=Path('Backups/AR-Tech2-pre-20261006/save')
destination=Path(original['savePath'])
# The snapshot names this exact save directory. Never recursively delete or move it.
rows=[]
for source in backup.rglob('*'):
    if not source.is_file(): continue
    relative=source.relative_to(backup)
    target=destination/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,target)
    rows.append({'path':relative.as_posix(),'sha256':hashlib.sha256(target.read_bytes()).hexdigest()})
statements=[]
for key,value in original['prefs'].items():
    statements.append('PlayerPrefs.DeleteKey('+json.dumps(key)+');' if value is None else 'PlayerPrefs.SetString('+json.dumps(key)+','+json.dumps(value)+');')
for key,name in [('floor','Floor'),('occlusion','Occlusion')]:
    statements.append('PlayerPrefs.SetInt("CampusRift.AR.'+name+'",'+str(original[key])+');')
statements += [
    'PlayerPrefs.Save();',
    'EditorSettings.enterPlayModeOptionsEnabled='+str(original['enterPlayEnabled']).lower()+';',
    'EditorSettings.enterPlayModeOptions=(EnterPlayModeOptions)'+str(original['enterPlayOptions'])+';',
    'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");',
    'var gv=System.Type.GetType("UnityEditor.GameView,UnityEditor");var win=UnityEditor.EditorWindow.GetWindow(gv);gv.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(win,'+str(original['gameViewIndex'])+');',
    'return "Save/settings restored; SampleScene reopened from disk";'
]
script='\n'.join(statements)
Path('Tools/tech2_restore_editor.cs').write_text(script,encoding='utf-8')
m.initialize()
response=m.call('execute_code',{'action':'execute','code':script})['result']
data=response.get('structuredContent') or json.loads(response['content'][0]['text'])
if not data.get('success'): raise RuntimeError(str(data))
(out/'6-restoration.json').write_text(json.dumps({'saveFiles':rows,'editor':data},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'saveFilesRestored':len(rows),'editor':data},ensure_ascii=True))
