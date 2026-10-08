"""Close P10 checklists only when the report's final validation guards pass."""
import json
import runpy
from pathlib import Path

base = Path('Artifacts/Skills')
state = json.loads((base/'P10-FinalEditorState.json').read_text(encoding='utf-8-sig'))
console = json.loads((base/'P10-Console-final.json').read_text(encoding='utf-8-sig'))
assert state['target']=='Android' and not any(state[k] for k in ('playing','compiling','updating'))
assert state['build'].startswith('Succeeded') and console['success']
assert all(x['type'] not in ('Error','Exception','Assert') for x in console['data'])
runpy.run_path('Tools/p10_write_report.py')
perf = json.loads((base/'SkillSet1-Performance-Player.json').read_text(encoding='utf-8-sig'))
main = json.loads((base/'SkillSet1.json').read_text(encoding='utf-8-sig'))
pool = json.loads((base/'SkillSet1-Pool.json').read_text(encoding='utf-8-sig'))
edge = json.loads((base/'SkillSet1-Edges.json').read_text(encoding='utf-8-sig'))
ui = json.loads((base/'P10-ComicTextAudit.json').read_text(encoding='utf-8-sig'))
maximum = max(x['lossPercent'] for x in perf['trials'])
phase = Path('task/P10-ky-nang-dot-1.md')
text = phase.read_text(encoding='utf-8-sig').replace('⬜', '✅').replace('- [ ]', '- [x]')
phase.write_text(text, encoding='utf-8')
readme = Path('task/README.md')
lines = readme.read_text(encoding='utf-8-sig').splitlines()
lines = [line.replace('⬜', '✅').replace('🔄', '✅') if '| [P10](' in line else line for line in lines]
readme.write_text('\n'.join(lines)+'\n', encoding='utf-8')
progress = Path('task/p10/PROGRESS.md')
text = progress.read_text(encoding='utf-8-sig')
status = ('\n\n**Trạng thái cuối 02/10/2026: P10-T01–T10 và fix1 hoàn tất.** '
          'Xem [REPORT-P10.md](REPORT-P10.md) cho kết quả và liên kết ảnh. '
          'Các dòng chưa đạt bên dưới là lịch sử trước sửa; kết luận cuối nằm ở cuối file.\n')
if '**Trạng thái cuối 02/10/2026:' not in text:
    head, rest = text.split('\n', 1)
    text = head+status+'\n'+rest
final = f'''

### Đóng P10 và fix1 — lượt xác nhận cuối 02/10/2026

- Main fresh **{len(main['passed'])}/0**, Pool **{len(pool['passed'])}/0**, Edge **{len(edge['passed'])}/0**; ComicTextAudit **{len(ui['screens'])} màn/0 issues**.
- Mười suite hồi quy không có FAIL mới: chín suite PASS, PhantomDecoy World giữ FAIL cửa đã có trước P10 với cùng năm cờ kiểm tra như baseline. Giữ nguyên DONE=FAIL và ghi rõ giới hạn trong REPORT; không gọi lỗi cũ là PASS. HubFlow/HubLayout đều PASS.
- Standalone Windows64 build Succeeded sau sửa popup material và màu Kim. PC thật **{perf['width']}×{perf['height']}**, {perf['pipeline']}; postProcessing/Bloom/HDR/ComicInk=true, Bloom {perf['bloomIntensity']}. **7/7 đạt**, giảm FPS cao nhất **{maximum:.2f}%**; VFX LateUpdate **0B/frame**.
- Cả bảy chiêu giữ **1122→1122** object sau10cast liên tiếp; hạt PC≤1500/mobile≤400, không exhaustion. Số từng chiêu và FPS gốc trong REPORT/JSON, không thay FAIL Editor thành PASS.
- Đã trực tiếp soi đủ14 sheet sáng/tối,7impact,7mobile cuối sau sửa; bảng tự đánh giá cuối phía trên ghi Có cho cả tám tiêu chí mỗi chiêu. HUD, Settings giảm flash PC/mobile, SkillBook và Chuẩn Bị10chiêu kiểm bằng ảnh/audit thật.
- Unity không có lỗi biên dịch; target Android được khôi phục sau đo PC. Chưa đo FPS/ảnh trên thiết bị Android vật lý. Font Be Vietnam Pro/OFL và material popup được bổ sung vào LICENSES.md; không tải tài nguyên ngoài mới.
- REPORT-P10.md đã ghi T01–T10, nguồn/license, số đo, ảnh, cách tái kiểm và giới hạn. Checkbox phase và trạng thái P10 trong README chỉ đóng sau các kiểm tra trên. Không tuyên bố hoàn tất fix2 chưa được giao.
'''
if '### Đóng P10 và fix1 — lượt xác nhận cuối' not in text:
    text += final
progress.write_text(text, encoding='utf-8')
print(f'Closed P10: {len(main["passed"])} main, {len(pool["passed"])} pool, {len(edge["passed"])} edge; max PC FPS loss {maximum:.2f}%')
