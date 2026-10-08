"""Produce the two requested full-regression documents only after every suite is reviewed."""
from pathlib import Path
import collections, json

root = Path.cwd()
out = root / 'Artifacts/V2/P23-regression'
inventory = json.loads((out / 'inventory.json').read_text(encoding='utf-8'))
rows = json.loads((out / 'Summary.json').read_text(encoding='utf-8'))
notes_path = out / 'classifications.json'
notes = json.loads(notes_path.read_text(encoding='utf-8')) if notes_path.exists() else {}
names = [r['name'] for r in rows]
assert len(names) == len(set(names)), 'Duplicate canonical suite rows'
assert set(names) == {r['name'] for r in inventory}, 'Full pass is still incomplete'
for r in rows:
    if r['status'] != 'PASS':
        assert r['name'] in notes, 'Unreviewed result: ' + r['name']
        assert notes[r['name']]['assessment'] in ('ACCEPTED BASELINE', 'INVALID LEGACY FIXTURE', 'FIXED GAME BUG'), 'Unresolved actual-game failure: ' + r['name']
        assert notes[r['name']]['evidence'], 'Missing classification evidence'

baseline = {}
for line in (root / 'Artifacts/V2/Regression-MVP.md').read_text(encoding='utf-8').splitlines():
    if line.startswith('| ') and not line.startswith('| Suite'):
        cols = [x.strip() for x in line.split('|')[1:-1]]
        if len(cols) >= 3:
            baseline[cols[0]] = cols[1] + ' ' + cols[2]
counter = collections.Counter(r['status'] for r in rows)
text = '# P23 · Hồi quy toàn bộ · 04/10/2026\n\n'
text += f"Đủ **{len(rows)} suite/nhóm chức năng**: raw " + ', '.join(f'{v} {k}' for k,v in counter.items()) + '. Không đổi FAIL thô thành PASS. Các ngoại lệ đã đọc source và evidence được phân loại trong cột cuối. Không còn FAIL game mới đã xác nhận chưa xử lý.\n\n'
text += 'Các suite gameplay dùng scene/domain mới, hồ sơ QA transient và input được bật; Cultivation/LevelData chạy Edit đúng guard của validator. Chỉ kiểm lại nhánh liên quan sau lỗi thực tế/fixture; attempt chưa chạy check vì lỗi orchestration hoặc đọc mesh được giữ trong `task/p23/diagnostics/invalid-context/`. Stair reader dùng Editor snapshot cùng geometry, không đổi collider/NavMesh/thuật toán/ngưỡng. Suite PASS không chạy lại. Full legacy SkillSet1/Reaction giữ assertion cũ; current P18 smoke được chạy riêng. Bỏ 50-trial thống kê, bot cân bằng, benchmark nhiều lượt và ma trận ảnh theo TEST-POLICY. Mẫu hiệu năng P23 riêng một lượt mỗi cảnh.\n\n'
text += '| Suite | P17 MVP trước | P23 raw | PASS / FAIL | Đánh giá và evidence |\n|---|---|---|---|---|\n'
for r in rows:
    name = r['name']
    p, f = r.get('passed', '—'), r.get('failed', '—')
    if p is None: p = '—'
    if f is None: f = '—'
    raw = 'P23-regression/runs/' + name + '/'
    note = notes.get(name, {})
    detail = (note.get('assessment', '') + ': ' + note.get('reason', '')).strip(': ')
    if r.get('reviewNote'): detail += ' ' + r['reviewNote']
    links = ' '.join('[' + e.get('label', 'evidence') + '](' + e['path'] + ')' for e in note.get('evidence', []))
    text += f"| {name} | {baseline.get(name, 'bổ sung/current V2')} | **{r['status']}** | {p} / {f} | [raw]({raw}) {detail} {links} |\n"
text += '\nĐối chiếu [P00 baseline](Baseline.md): Boost 18/1 →19/0, ShabanBehavior18/1 →19/0; traversal arrival/lift dựa planner hiện hành. [P17 MVP](Regression-MVP.md) đánh giá54PASS/17FAIL, không phải all-PASS. [STABILIZE](Regression-Stabilize.md) đã sửa runtime dash và các fixture cũ; P23 giữ các cập nhật đó. Shelter40điểm biên là baseline đã chấp nhận, không sửa geometry/NavMesh. Mọi ngoại lệ khác phải có phân tích riêng ở bảng trên.\n\n'
text += 'Inventory: [inventory.json](P23-regression/inventory.json); tổng kết máy đọc: [Summary.json](P23-regression/Summary.json); log Console/outputs/DONE của từng lượt nằm trong raw. Historical evidence được copy cách ly rồi phục hồi từ P23-pre, không ghi đè bằng kết quả P23. Lỗi runner trước launch hoặc collector sau DONE được giữ trong diagnostics và không tính là gameplay FAIL.\n\n'
text += 'Ngoài full inventory: T03 Accessibility68/0, Gameplay25/0 và P23ReadingVariants339/0 (337audit) kiểm cả ba cỡ chữ và chế độ mù màu. LearningFollowupPlayTest không còn source trong project; LearningRegressionRunner/V2RegressionRunner là orchestrator trùng các suite đã liệt kê. P12PlayTest là abstract base. UIPlayValidation chạy assertion chức năng một độ phân giải, bỏ capture matrix; ComicReview/ComicOutcome là photo matrices, thay bằng audit T03 và các flow/outcome functional hiện hành. ComicPerformance/P12Balance/P18 performance là bench/bot bị bỏ theo TEST-POLICY.\n'
for filename in ('Regression-Full.md', 'Final-Regression.md'):
    (root / 'Artifacts/V2' / filename).write_text(text, encoding='utf-8')
print('Reviewed full regression:', len(rows), dict(counter))
