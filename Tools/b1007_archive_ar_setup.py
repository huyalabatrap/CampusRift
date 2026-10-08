from pathlib import Path
import json
root=Path('task/batch-1007/regression');out=root/'setup-errors';out.mkdir(exist_ok=True)
for folder in (root/'runs').iterdir():
    p=folder/'row.json'
    if not p.exists():continue
    r=json.loads(p.read_text(encoding='utf-8'))
    if 'Scene file not found' in r.get('error',''):
        assert not (out/folder.name).exists();folder.rename(out/folder.name)
with (root.parent/'PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n- Mốc AR setup: sửa đường dẫn scene thành Assets/ARRift/Scenes/ARRiftBattle.unity. Bảy attempt lỗi OpenScene trước khi có runtime/mock/harness được giữ ở regression/setup-errors; chưa chạy suite. ARGestureUnitTests đãPASS. ARNavigation lượtđầu FAIL2mục đầu vì fixture SampleScene không có ARHubEntry; chưa chạy cácmục tiếp theo. Sửa fixture vềMainMenu thật và chỉtiếp tục cácmục fail/chưachạy, khônglặpPASS.\n')
