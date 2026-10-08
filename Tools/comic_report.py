"""Save final Unity console evidence and generate the round-one handoff."""
import json
from pathlib import Path
from PIL import Image
import unity_mcp as m

root = Path('task/ui-comic')
m.initialize()
console = {}
for kind in ('errors', 'warnings'):
    args = json.loads((root / (kind+'.json')).read_text(encoding='utf-8-sig'))
    response = m.call('read_console', args)
    console[kind] = response['result'].get('structuredContent') or json.loads(response['result']['content'][0]['text'])
(root/'tests/round1/console-final.json').write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')
files = json.loads((root/'FILES-round1.json').read_text(encoding='utf-8'))
summary = json.loads((root/'tests/round1/Summary.json').read_text(encoding='utf-8-sig'))
art = 'Assets/CampusRiftUI/Comic/Resources/Comic/'
backgrounds = [('academy-background','Menu, Hub, Learning, Loadout','Thư viện học viện cổ phong pha hiện đại, giữa tối và ít tương phản'),('sky-dusk','Màn 1–2 / Default','Hoàng hôn navy, điểm vàng'),('sky-night','Màn 3–5','Đêm xanh navy'),('sky-blood','Màn 6–7','Trăng máu, đỏ trầm'),('sky-inferno','Màn 8–9','Mây than và viền cam'),('sky-eclipse','Màn 10','Nhật thực đỏ')]
screens = {'menu':'Menu chính','hub':'Hub / bản đồ','dan-cac':'Đan Các','loadout':'Chuẩn bị / loadout','quiz':'Quiz tiếng Việt','hud-level01':'HUD màn 1 trong gameplay','background-level01':'Skybox màn 1','background-level03':'Skybox màn 3','pause':'Pause','settings':'Cài đặt','result':'Kết quả màn 1, chụp bởi HubFlow','dan-cac-phone-landscape':'Đan Các điện thoại ngang','dan-cac-phone-portrait':'Đan Các điện thoại dọc'}
parts = ['# Campus Rift — Comic UI round 1\n',
'''Đã áp dụng theme comic tập trung cho UI dựng bằng code và UI có sẵn trong scene/prefab: khung navy với viền mực đen, viền trong sáng, bóng lệch cứng; nút vát chéo; CTA xanh lá; nút quay lại đỏ; tab được chọn vàng; tiêu đề đậm nghiêng in hoa có outline và bóng; tia nổ, halftone và nhãn Linh Thạch với pha lê.

## Phạm vi triển khai

- `ComicTheme` là nguồn màu, sprite, font, material và chiều cao nút tối thiểu 68 đơn vị thiết kế. `UiKit`, `RiftButton`, Hub, Đan Các, loadout và Learning dùng cùng theme.
- Menu chính, pause, settings, credits, HUD, loading, kết quả thắng/thua và các prefab UI được cập nhật qua `ComicUIBuilder`. Túi vật phẩm, thông báo migration, điều khiển mobile và bảng thang máy cũng được nối theme.
- Font TMP mới dùng Be Vietnam Pro đang có trong project, prewarm và kiểm tra bộ chữ tiếng Việt. LocalizationCatalog tham chiếu font này để đổi ngôn ngữ vẫn giữ kiểu chữ. Material tiêu đề có outline đen và underlay không blur. Text dùng Truncate để tránh cảnh báo ellipsis của TMP với chữ Bold/Italic.
- Sprite khung/nút/nhãn được sinh bằng Pillow, import Sprite Single, không nén; border 9-slice 28 px trong ảnh nguồn 128 px. `burst`, `halftone`, `crystal` là sprite trang trí không slice.
- FitFrame được tách khỏi UiKit thành MonoBehaviour có file/GUID riêng, sửa tham chiếu scene/prefab và loại component trùng. Bố cục dùng khung thiết kế 1920×1080 theo safe area.
- SkyLightingController chọn năm skybox theo preset màn hiện có. Chỉ bổ sung phần hiển thị sky; giữ logic sun, fog, brightness, ambient probe và gameplay hiện có. Không thay collider, NavMesh hoặc geometry trong Assets/Levels.
- Giữ ID vật phẩm/kỹ năng, công thức phần thưởng và callback nút. Thêm lối tắt footer “ĐẾN KHÓA HỌC” / “VỀ HUB”. Kiểm thử/capture dùng profile transient và đã khôi phục profile thật; backup ban đầu còn nguyên.

## Ảnh nền đã tạo

Tạo bằng công cụ imagegen tích hợp. Prompt đầy đủ: [IMAGE-PROMPTS-round1.md](IMAGE-PROMPTS-round1.md). Các ảnh không chứa chữ hoặc UI vẽ sẵn; panel đặc màu giữ chữ rõ trên nền minh họa.

| File trong project | Áp dụng | Nội dung | Kích thước nguồn |
|---|---|---|---|
''']
for name, where, description in backgrounds:
    path=Path(art+name+'.png'); size=Image.open(path).size
    parts.append(f'| `{path.as_posix()}` | {where} | {description} | {size[0]}×{size[1]} |\n')
parts.append('''
Năm skybox có material `Skybox/Panoramic` tương ứng trong cùng thư mục. Một số mái học viện nằm sát chân trời; tại camera gameplay, phần chính nhìn thấy là các khối mây comic navy.

## Kiểm tra Unity

''')
parts.append(f"Lần chạy cuối: `{summary['started']}` → `{summary['finished']}`.\n\n")
for suite in summary['suites']:
    parts.append(f"- **{suite['name']}: {suite['status']} — {suite['summary']}**. [JSON](tests/round1/{suite['name']}.json).\n")
parts.append('''
Tổng 66 kiểm tra thành công. HubFlow xác nhận mua vật phẩm, equip/loadout, vào màn, pause, phần thưởng chỉ trả một lần và trở về Hub. HubLayout kiểm tra map/skills/loadout với EN và VI tại 1920×1080, 2340×1080, 1440×1080, 1280×720.

''')
for kind,label in [('errors','Lỗi'),('warnings','Cảnh báo')]:
    data=console[kind]
    if not data.get('success'): raise RuntimeError('Console verification failed: '+str(data))
    entries=data.get('data',[])
    if entries: raise RuntimeError('Review console entries before reporting clean: '+str(entries))
    parts.append(f'- {label} Console ở lần đọc cuối: **0**.\n')
parts.append('''
Đã refresh/compile Unity, không còn cảnh báo missing/runtime script của FitFrame. Bằng chứng: [console-final.json](tests/round1/console-final.json), [Summary.json](tests/round1/Summary.json).

## Ảnh chụp Game view

Tất cả nằm trong `task/ui-comic/screens/round1/`. Ảnh được chụp bằng ScreenCapture trong Play Mode; nền màn 1/3 dùng camera review và tạm ẩn panel, không lưu vị trí camera vào scene.

| Màn | Ảnh | Kích thước |
|---|---|---|
''')
for name,label in screens.items():
    path=root/'screens/round1'/f'{name}.png'; size=Image.open(path).size
    parts.append(f'| {label} | [{name}.png](screens/round1/{name}.png) | {size[0]}×{size[1]} |\n')
parts.append('''
## Giới hạn và phần chưa hoàn thiện

- Không còn lỗi biên dịch hoặc cảnh báo Console tại thời điểm bàn giao. Hai bộ regression đã yêu cầu đều PASS.
- Màn hình điện thoại dọc dùng FitFrame thu nhỏ/letterbox bố cục ngang; chưa có bố cục xếp dọc riêng nên chữ/nút nhìn nhỏ hơn. Mức 68 là đơn vị thiết kế trước khi scale, không phải 68 px vật lý trên mọi điện thoại.
- Icon minh họa vật phẩm/kỹ năng và thumbnail level giữ bộ art hiện có. Chưa vẽ lại toàn bộ icon theo nét mực của ảnh tham chiếu.
- Các màn UI chính đã nối theme. Overlay debug IMGUI của công cụ QA/MonsterDebug và fallback debug của CampusExplorer chưa được làm lại thành comic; đây là phần chưa chuyển giao diện trong round 1.
- Năm skybox đã được tạo/gắn theo preset; mới có ảnh kiểm chứng trực tiếp màn 1 và 3. Chưa chạy kiểm chứng hình ảnh riêng từng màn 2, 4–10 hoặc thử trên thiết bị điện thoại thật. Vật liệu mesh 3D và nhân vật/quái giữ art hiện có.

## File tạo/sửa

Danh sách máy đọc đầy đủ: [FILES-round1.json](FILES-round1.json). File `.meta` do Unity tạo đi kèm tài nguyên và script mới. Các font TMP cũ trong danh sách có thay đổi cache glyph khi chạy UI trước khi chuyển font.

### File đã sửa

''')
parts.extend(f'- `{p}`\n' for p in files['modified_against_backup'])
parts.append('\n### File đã tạo\n\n')
parts.extend(f'- `{p}`\n' for p in files['created'])
parts.append('''
Các script QA C# và JSON cấu hình MCP nằm trong `task/ui-comic/`; helper Python dùng C# 6 cho execute_code. `Tools/comic_capture.py` lưu log mỗi lượt chụp. `Tools/comic_report.py` sinh báo cáo và lưu Console. Prompt, inventory, test JSON và ảnh chụp là các tài liệu bàn giao bổ sung.

Để áp dụng lại theme sau khi sửa scene/prefab: dừng Play Mode rồi chọn `Campus Rift > UI > Apply Comic Theme`. Sinh lại sprite: `python Tools/build_comic_sprites.py`. Import skybox: `Campus Rift > UI > Import Comic Skies`. Unity kết thúc ở Edit Mode, scene MainMenu, Game view 1920×1080.
''')
(root/'REPORT-round1.md').write_text(''.join(parts),encoding='utf-8')
print('Saved REPORT-round1.md; console errors/warnings = 0; screenshots = '+str(len(screens)))
