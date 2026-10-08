from pathlib import Path
import json,hashlib
from PIL import Image
import unity_mcp as m
root=Path('task/ui-comic');read=lambda p:json.loads(Path(p).read_text(encoding='utf-8-sig'))
audit=read(root/'tests/round3/TextAudit.json');outcomes=read(root/'tests/round3/OutcomeAudit.json');summary=read(root/'tests/round3/Summary.json');perf=read(root/'tests/round3/Performance.json')
assert audit['finished'] and audit['issues']==0 and not audit['error']
assert len(outcomes)==9 and sum(len(s['issues']) for s in outcomes)==0
assert all(s['status']=='PASS' for s in summary['suites'])
assert all(perf[k] for k in ['withinTenPercent','toggleWorks','lowQualityOff','mobileOff'])
brightness=read(root/'level-brightness-round3.json');assert len(brightness)==10 and all(b['upper_srgb']>=.35 for b in brightness)
alphas=read(root/'skill-alpha-round3.json');assert len(alphas)==22 and all(a['alphaMin']==0 and a['alphaMax']==255 and a['transparentPixels']>.01 for a in alphas)
jobs=read(root/'image-jobs-round3.json');assert len(jobs)==35
assert all((root/'generated-round3'/(j['id']+'.json')).exists() for j in jobs)
baseline=read('Backups/ComicUI-round2-pre-round3/manifest.json');guids=[]
for j in jobs:
    p=Path(j['dest']+'.meta');old=Path('Backups/ComicUI-round2-pre-round3')/p
    if old.exists():
        before=next(x for x in old.read_text().splitlines() if x.startswith('guid:'))
        after=next(x for x in p.read_text().splitlines() if x.startswith('guid:'));assert before==after,j['id'];guids.append(j['id'])
assert len(guids)==35
m.initialize();console={}
for key in ['errors','warnings']:
    r=m.call('read_console',read(root/(key+'.json')))['result'];console[key]=r.get('structuredContent') or json.loads(r['content'][0]['text'])
assert console['errors']['success'] and not console['errors']['data'],console['errors']
(root/'tests/round3/console-final.json').write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')
(root/'tests/round3/AssetIds.json').write_text(json.dumps({'preservedGuids':guids,'count':len(guids)},indent=2),encoding='utf-8')
files=read(root/'FILES-round3.json');screens=root/'screens/round3'
required=['menu','hub','dan-cac','hub-skills','loadout','quiz','hud-combat','settings-video','skills-sheet','levels-sheet']
assert all((screens/(name+'.png')).exists() for name in required)
assert 'Verified visible HUD icons: 3' in (root/'tests/round3/IconBindings.txt').read_text(encoding='utf-8-sig')
parts=['# Campus Rift — Comic UI, vòng 3\n',
'''Đã đọc toàn bộ PROMPT-03.md (UTF-8), kiểm tra báo cáo và hiện trạng vòng 2 trước khi tiếp tục. Trên đĩa chưa có phần thực hiện vòng 3 lúc bắt đầu. Bản sao trước thay đổi: `Backups/ComicUI-round2-pre-round3/`; giữ các backup và bằng chứng vòng trước.

## 1. Thumbnail, bản đồ và nền chung

Tạo mới **10 thumbnail** bằng imagegen tích hợp: một chủ thể chính, nét mực, mảng màu và halftone; tên/nội dung đối chiếu `Assets/Levels/Editor/LevelSetup.cs`. Màu lần lượt: cam hoàng hôn, xanh độc, tím, cobalt, trắng-xám sương, đỏ huyết nguyệt, tím-đen, cam lửa, đỏ-vàng và vàng nhật thực. File vẫn là `Assets/Resources/ContentImages/Scenes/level-01.png` đến `level-10.png`. Loadout dùng cùng nguồn ảnh mới.

Đã bỏ `Map Art / level-map` phía sau lưới, thay bằng panel navy đặc có texture nhẹ. Tạo lại `Assets/CampusRiftUI/Comic/Resources/Comic/academy-background.png`: giữa khung ít chi tiết, nền navy, hình thư viện mờ ở rìa. Hub, Learning, Loadout và menu cùng dùng nguồn này. Màn khóa dùng 10 biến thể `level-XX-locked.png`, giảm bão hòa hoàn toàn và độ sáng còn 30%, kèm ổ khóa vàng; điều kiện mở khóa giữ nguyên.

Ảnh kiểm chứng: [Bản đồ/Hub](screens/round3/hub.png), [Thư viện trong Hub](screens/round3/hub-library.png), [Menu](screens/round3/menu.png), [Loadout](screens/round3/loadout.png), [10 thumbnail màu](screens/round3/levels-sheet.png).

## 2. Toàn bộ ảnh kỹ năng

Liệt kê dữ liệu `SkillCatalog`, ba `SkillDefinition`, `LoadoutUI.Suggested` và tất cả PNG trong ContentImages: **3 kỹ năng đang chơi được + 19 ảnh kỹ năng có sẵn**. Tạo riêng **22 icon** bằng imagegen tích hợp, biểu tượng và màu theo hệ. Không thêm kỹ năng gameplay hoặc đổi điều kiện mở khóa. Các ID gợi ý chưa có definition tiếp tục được logic hiện có bỏ qua.

| ID đang chơi | File ảnh giữ nguyên | Hệ trong dữ liệu |
|---|---|---|
| `hu-khong-ket-gioi` | `Assets/CampusRiftUI/Art/Skills/VoidWall.png` | Không Gian, cyan |
| `anh-phan-than` | `Assets/CampusRiftUI/Art/Skills/PhantomDecoy.png` | Âm, tím |
| `dai-thu-an` | `Assets/CampusRiftUI/Art/Skills/GiantHandSeal.png` | Thổ, vàng đất |

19 file bổ sung giữ tên trong `Assets/Resources/ContentImages/Skills/`:
''']
parts.append('\n'.join('- `'+j['id']+'.png` — '+j['element'] for j in jobs if j['kind']=='skill' and not j['id'][0].isupper()))
parts.append('''

Hệ của nhóm ảnh chưa có SkillDefinition được suy từ tên/ý tưởng ảnh, không phải dữ liệu hệ gameplay mới. Đã kiểm tra alpha 0–255, không có nền caro; mỗi icon có 41–78% pixel hoàn toàn trong suốt. [Dữ liệu alpha](skill-alpha-round3.json). [Bảng icon lớn + bản 64 px](screens/round3/skills-sheet.png).

HUD dùng ảnh mới thay glyph cũ; giữ slot và các script cooldown/charge. Loadout và Công pháp dùng icon từ definition; menu giữ tham chiếu sprite cũ nên nhận PNG mới. Kiểm chứng: [HUD](screens/round3/hud-combat.png), [Công pháp](screens/round3/hub-skills.png), [Loadout](screens/round3/loadout.png), [Menu](screens/round3/menu.png), [binding runtime](tests/round3/IconBindings.txt). Giữ nguyên GUID của cả 35 ảnh được thay: [AssetIds.json](tests/round3/AssetIds.json).

## 3. Bo góc thống nhất

Thêm `round-panel.png`, `round-selected.png`, `round-paper.png`, `round-currency.png`, `round-mask.png` vào `Assets/CampusRiftUI/Comic/Resources/Comic/`; sprite 9-slice riêng, khung 128×128, bán kính ngoài 16 px, viền mực và bóng đen lệch 6 px. Mask trắng 64×64, radius 14 px. `ComicTheme` ánh xạ panel/thẻ/ô trang bị/tiền tệ sang bộ bo góc; các nút hành động và tab giữ vát chéo.

Thumbnail có khung riêng và **Mask dùng sprite alpha bo góc**, ảnh nằm bên trong stencil. HP/năng lượng được cắt qua stencil bo góc và giữ Image fillAmount gốc. Modal/panel prefab và scene được cập nhật qua ComicUIBuilder; không chỉ đổi một màn lẻ.

## 4. Các lỗi còn sót của vòng 2

- Đan Các: nâng dòng “Mang tối đa/màn…” lên vùng trong của thẻ kem; mô tả danh mục và ký hiệu +/- cũng được căn lại sau khi audit mới phát hiện sát mép. Nhãn độ hiếm lấy preferred width của TMP cộng padding, co giãn theo chuỗi. Đã soi [Đan Các](screens/round3/dan-cac.png) và ba danh mục khác.
- ComicTextAudit so glyph bounds với rect riêng, RectMask2D/Mask và vùng fill nhìn thấy của sprite bo góc ở các cha. Có ca hồi quy chứng minh **rect TMP đủ nhưng khung cha vẫn cắt chữ**, phát hiện được cả thẻ kem lẫn RectMask2D: [ParentClip-Proof.json](tests/round3/ParentClip-Proof.json). Dòng nằm một phần ngoài viewport khi scroll được đếm riêng; khung của từng thẻ vẫn kiểm tra. Nhãn phím nằm hoàn toàn ngoài sprite slot không bị coi là sprite cắt, vì sprite không vẽ ngoài rect của nó.
- HUD: thêm plate navy tối alpha 0.91 theo vị trí/ẩn hiện của gợi ý, tiêu đề kỹ năng, phím tắt và phím slot. Nâng nhãn charge khỏi mép khung. [HUD chiến đấu](screens/round3/hud-combat.png).
- Viền mực PC tăng lên **2 px tại 1080p**, scale theo chiều cao, strength 0.88; giữ toggle, quality thấp và mobile tự tắt. Phát hiện biên từ depth, normal tái dựng bằng đạo hàm vị trí world từ depth, và chênh lệch màu; bỏ pass render normal riêng sau khi phép đo phát hiện chi phí lớn. [Tắt](screens/round3/ink-off.png), [Bật](screens/round3/ink-on.png).
- Tạo lại `sky-blood.png` và `sky-eclipse.png` bằng imagegen; chỉnh vị trí thiên thể bằng imagegen để hiện trong góc nhìn gameplay, xoay material 90°. Huyết nguyệt đỏ thẫm, nhật thực là đĩa đen viền vàng/đỏ. Tăng ánh sáng trung gian huyết nguyệt để giữ khả năng nhìn. Nguồn native **1774×887**, chuyển thành texture **4096×2048 bằng Lanczos**, không phải 4K native; feather seam U 192 px. [Màn 6](screens/round3/levels/level-06.png), [Màn 7](screens/round3/levels/level-07.png), [Màn 10](screens/round3/levels/level-10.png).
- Loadout: tăng slot từ 194 lên 242 px, icon 110 px, ba hàng kỹ năng 112 px; thêm mô tả vật phẩm, vùng xem trang bị mang vào màn và scroll kho. Dòng phản ứng Công pháp nâng khỏi đáy và đưa nội dung vào đúng panel cha để audit kiểm tra được.

## Kiểm tra cuối

''')
parts.append(f'- Unity: **0 lỗi console/biên dịch** khi bàn giao. HubFlow **42/42 PASS**, HubLayout **24/24 PASS**. Lượt suite PASS hoàn tất lúc 22:31; audit/capture và benchmark được chạy tiếp sau các chỉnh sửa HUD/shader cuối. [Summary.json](tests/round3/Summary.json).\n- Audit UI: **0 lỗi / {len(audit["screens"])} trạng thái / {audit["visibleTexts"]} lượt TMP nhìn thấy**, cộng {len(outcomes)} trạng thái HUD/kết quả có **0 lỗi**. Việt/Anh, 1920×1080 và 2340×1080; shop thêm điện thoại dọc/ngang. [TextAudit.json](tests/round3/TextAudit.json), [OutcomeAudit.json](tests/round3/OutcomeAudit.json).\n')
parts.append(f'- FPS Windows Editor 1920×1080, PC, VSync off, 4 lượt OFF/ON/OFF/ON: trung vị trung bình OFF **{perf["offFps"]:.2f}**, ON **{perf["onFps"]:.2f}**, giảm **{perf["fpsLossPercent"]:.2f}%**. Toggle/quality thấp/mobile PASS; không vượt 10% trong phép đo này. [Performance.json](tests/round3/Performance.json).\n')
parts.append('- Độ sáng trung bình nửa trên screenshot (sRGB, có cả geometry): '+', '.join(f'màn {b["level"]}: **{b["upper_srgb"]*100:.1f}%**' for b in brightness)+'. Đây là trung bình pixel sRGB, không phải độ rọi; tất cả >=35%. [Dữ liệu](level-brightness-round3.json).\n')
parts.append(f'- Geometry SampleScene: **6231 component 3D được đối chiếu không đổi** (Transform, renderer, mesh, rigidbody, collider, NavMesh). Tất cả file trong Assets/Levels của backup không đổi. [Geometry.json](tests/round3/Geometry.json).\n- Đã phục hồi build target Android, resolution 1920×1080, Edit Mode; profile/settings của fixture được phục hồi.\n- Có **{len(files["screens"])} PNG** kiểm chứng trong `screens/round3/`, bao gồm quiz, settings, pause, portrait/wide, HUD, kết quả và 10 màn chơi. [Bảng soi UI](screens/round3/review-sheet.png).\n')
parts.append('''
## File và nguồn ảnh

Danh sách đầy đủ file tạo/sửa và screenshot: [FILES-round3.json](FILES-round3.json). Các thay đổi code chính: `ComicTheme.cs`, `ComicTextPlate.cs` (mới), `UiKit.cs`, `HubUI.cs`, `HubPages.cs`, `LoadoutUI.cs`, `ComicSkillLayout.cs`, `PlayerEnergyUI.cs`, `SkyLightingController.cs`, `ComicUIBuilder.cs`, `ComicTextAudit.cs`, `ComicInkFeature.cs`, `ComicInk.shader` và ba fixture review/outcome/performance. Scene/prefab UI và import/material liên quan đã lưu. Các script tạo sprite, xử lý ảnh và kiểm chứng trong `Tools/` được giữ để tái lập, gồm `comic_round3_assets.py` tạo trước manifest backup.

35 ảnh gốc và metadata lưu trong `task/ui-comic/generated-round3/`. Prompt từng ảnh, đường dẫn đích và hai chỉnh sửa vị trí thiên thể: [IMAGE-PROMPTS-round3.md](IMAGE-PROMPTS-round3.md), [image-jobs-round3.json](image-jobs-round3.json). Dùng **imagegen tích hợp**, không dùng CLI. Script Pillow chỉ tạo các sprite hình học, chuẩn hóa kích thước/alpha, biến thể khóa và bảng so sánh; không thay ảnh AI bằng placeholder.

## Lỗi còn tồn tại / phạm vi

Không còn lỗi biên dịch hay text overflow trong các trạng thái đã kiểm tra; tất cả mục 1–4 đã thực hiện. Một lượt HubFlow trong session QA liên tục bị timeout sau bước chọn vật phẩm; chạy lại từ Play Mode mới đạt 42/42, HubLayout 24/24. Giữ bằng chứng lượt timeout tại `tests/round3/Summary-retry-timeout.json`; chưa xác định nguyên nhân của lượt không ổn định này, không coi là lỗi compile hay tự sửa logic gameplay để né test. 19 ảnh kỹ năng bổ sung được tạo lại nhưng chưa có gameplay definition trước vòng này, nên không xuất hiện như kỹ năng chơi được. Không thêm logic để ép chúng xuất hiện. Các góc QA có freeze/pose/camera riêng, không lưu vào scene. Hiệu năng đo trong Editor trên máy hiện tại; chưa benchmark trên thiết bị Android thật. Giữ cảnh báo NavMesh của fixture khi tháo scene được ghi ở vòng 2; không đổi NavMesh để xử lý cảnh báo này.
''')
(root/'REPORT-round3.md').write_text('\n'.join(parts),encoding='utf-8')
with (root/'REPORT-round3.md').open('a',encoding='utf-8') as f:
    f.write('\n\nHiệu năng: một lượt dùng normal prepass riêng giảm FPS 57,71%, lưu tại [Performance-normal-prepass.json](tests/round3/Performance-normal-prepass.json). Đã thay bằng normal tái dựng từ depth và đo lại 4 lượt với kết quả nêu trên. Editor báo Application.isFocused=false trong môi trường QA; số FPS tuyệt đối phụ thuộc hoạt động Game view và tải máy, không dùng để so trực tiếp với vòng 2 hoặc dự đoán FPS trên thiết bị khác. Giữ cả [phép đo nền trước đó](tests/round3/Performance-background.json) để thấy biến động môi trường.\n')
    f.write('\nEditor đã khởi động lại khi lượt capture cuối đang chạy; không xác định nguyên nhân từ bằng chứng hiện có. Giữ [audit bị ngắt](tests/round3/TextAudit-interrupted-editor-restart.json); sau khi MCP kết nối lại, chạy lại đầy đủ và đạt kết quả audit hoàn tất nêu trên. Cảnh báo QA được lưu riêng tại [warnings-context.json](tests/round3/warnings-context.json).\n')
print('Final report written; all required checks passed')
