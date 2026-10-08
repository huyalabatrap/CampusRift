from pathlib import Path
import shutil,hashlib,json,datetime
out=Path('task/ar/fix2');out.mkdir(exist_ok=True)
assert not (out/'manifest.json').exists(), 'Reuse existing backup'
backup=Path('Backups')/('AR-fix2-pre-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'))
paths=set()
for folder in ['Assets/ARRift','Assets/Controls/Editor','Assets/Settings','ProjectSettings']:
    paths.update(p for p in Path(folder).rglob('*') if p.is_file())
paths.update(Path('task').glob('run-*.ps1'));paths.add(Path('task/codex-accounts.json'))
rows=[]
for p in sorted(paths):
    q=backup/p;q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q)
    rows.append(dict(path=p.as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
shutil.copytree('C:/Users/Admin/AppData/LocalLow/DefaultCompany/Campus Rift',backup/'UserSave')
Path('task/ar/FIX2-BACKUP.txt').write_text(str(backup.resolve()),encoding='utf-8')
(out/'manifest.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
Path('task/ar/screens/fix2').mkdir(exist_ok=True)
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## AR fix2 — tiếp quản / backup\n- Đã đọc đủ REPORT-fix1, SPEC, PROMPT-fix2, PROGRESS và TEST-POLICY; chưa có code/evidence fix2 trên đĩa. Backup FIX2-BACKUP.txt + fix2/manifest.json + UserSave. Không sửa điều phối.\n- Unity/MCP đã đóng khi tiếp quản; đang khởi động lại Editor Android.\n- Xác minh portrait=true, pre-transform=true; Android thực tế Manual Vulkan→GLES3 (không Automatic như nghi vấn). Package ARCore6.6.2 hỗ trợ Vulkan nhưng URP cần ARCommandBufferSupportRendererFeature; AR_Renderer hiện thiếu feature đó. Sẽ chọn chỉ GLES3.\n- Chưa chạy harness/build fix2.\n')
print(backup,len(rows))
