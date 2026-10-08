from ar_session import *
import shutil,hashlib,datetime
out=Path('task/ar/fix1');out.mkdir(exist_ok=True)
backup=Path('Backups')/('AR-fix1-pre-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'))
paths=set()
for folder in ['Assets/ARRift','Assets/Skills','Assets/Combat','Assets/Settings','ProjectSettings']:
    paths.update(p for p in Path(folder).rglob('*') if p.is_file())
paths.update(Path('task').glob('run-*.ps1'))
rows=[]
for p in sorted(paths):
    q=backup/p;q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q)
    rows.append(dict(path=p.as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
user=Path('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift')
shutil.copytree(user,backup/'UserSave')
Path('task/ar/FIX1-BACKUP.txt').write_text(str(backup.resolve()),encoding='utf-8')
save(out/'manifest.json',rows)
save(out/'settings-before.json',code('return new { settings=UnityEngine.PlayerPrefs.GetString("CampusRift.Settings.v1"), floor=UnityEngine.PlayerPrefs.GetInt("CampusRift.AR.Floor",0), safety=UnityEngine.PlayerPrefs.GetInt("CampusRift.AR.Safety",0), enterPlay=(int)UnityEditor.EditorSettings.enterPlayModeOptions, enterEnabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled, dev=UnityEditor.EditorUserBuildSettings.development,inputBackground=(int)UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior,inputEditor=(int)UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode };'))
Path('task/ar/screens/fix1').mkdir(exist_ok=True)
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n## AR fix1 — tiếp quản / backup\n- Đã đọc đủ brief và 4 file ngữ cảnh; M0–M8 đã hoàn tất, chưa có thay đổi/evidence fix1. Không chạy lại các gate cũ.\n- Unity Android/Edit/SampleScene. Backup trước sửa: FIX1-BACKUP.txt; manifest + settings/save hiện tại trong fix1/. Không sửa task/run-*.ps1.\n- Đang sửa VFX/giới hạn chiến trường, số sát thương, shrine; sẽ kiểm HUD hiện tại (M6 đã có auto-hide, ảnh M5 là trước M6), ARRiftPlayTest + ComicTextAudit mỗi1lượt rồi build.\n')
print(backup, len(rows))
