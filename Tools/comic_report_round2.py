"""Record round-two assets, checks and a UTF-8 Vietnamese handoff."""
from pathlib import Path
import json, hashlib
from PIL import Image
import unity_mcp as m

root=Path('task/ui-comic')
read=lambda p: json.loads(Path(p).read_text(encoding='utf-8-sig'))
audit=read(root/'tests/round2/TextAudit.json')
outcomes=read(root/'tests/round2/OutcomeAudit.json')
perf=read(root/'tests/round2/Performance.json')
summary=read(root/'tests/round2/Summary.json')
assert audit['issues']==0 and not audit['error'] and audit['finished']
assert sum(len(a['issues']) for a in outcomes)==0
assert all(s['status']=='PASS' for s in summary['suites'])
assert all(perf[k] for k in ['withinTenPercent','toggleWorks','lowQualityOff','mobileOff'])
m.initialize();console={}
for key in ('errors','warnings'):
 result=m.call('read_console',read(root/(key+'.json')))['result']
 console[key]=result.get('structuredContent') or json.loads(result['content'][0]['text'])
 assert console[key]['success']
assert not console['errors']['data'],console['errors']
(root/'tests/round2/console-final.json').write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')

manifest=read('Backups/ComicUI-round1-pre-round2/manifest.json')
modified=[];created=[]
for file,digest in manifest.items():
 p=Path(file)
 if p.exists() and hashlib.sha256(p.read_bytes()).hexdigest()!=digest:modified.append(file)
for group in ['Assets/CampusRiftUI','Assets/Settings','Assets/Resources/ContentImages']:
 for p in Path(group).rglob('*'):
  if p.is_file() and p.as_posix() not in manifest:created.append(p.as_posix())
modified+=['Assets/Skills/GiantHandSeal/Runtime/GiantHandHUD.cs','Assets/Skills/PhantomDecoy/Runtime/PhantomDecoyHUD.cs','ProjectSettings/QualitySettings.asset','Tools/strip_checker.py']
created+=['Tools/comic_snapshot_round2.py','Tools/prepare_comic_skies_round2.py','Tools/edit_comic_round2.py','Tools/comic_report_round2.py']
screens=[{'file':p.as_posix(),'size':list(Image.open(p).size)} for p in sorted((root/'screens/round2').rglob('*.png'))]
record={'baseline':'Backups/ComicUI-round1-pre-round2/manifest.json','modified':sorted(set(modified)),'created':sorted(set(created)),'screens':screens}
(root/'FILES-round2.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
brightness=read(root/'level-brightness-round2.json')
parts=['# Campus Rift — Comic UI, vòng 2\n',
'''Đã đọc toàn bộ PROMPT-02.md và đối chiếu PROMPT-01.md cùng ảnh Đan Các. Vòng 2 thay lại năm nền trời, thêm hậu kỳ viền mực, sửa theme và bố cục, làm sạch icon và chụp lại các màn. Bản sao trước khi sửa: `Backups/ComicUI-round1-pre-round2/`; backup ban đầu vẫn còn nguyên.

## A. Nền màn chơi và hậu kỳ

- Tạo mới năm ảnh trời bằng imagegen tích hợp. Ảnh gốc **1774×887**, được upscale Lanczos lên **4096×2048**, căn đường chân trời về v=0.5 và feather seam U 192 px. Không gọi ảnh này là 4K native. Nguồn gốc được lưu trong `generated-round2/`, thông số trong [sky-assets-round2.json](sky-assets-round2.json), prompt trong [IMAGE-PROMPTS-round2.md](IMAGE-PROMPTS-round2.md).
- Import không nén/không Crunch, max size 4096, mipmaps, Trilinear, U Repeat/V Clamp, anisotropic 2. Chênh lệch màu trung bình giữa hai mép U là 0 ở cả năm texture. Đã soi ảnh panorama và ảnh render; không thấy đường ghép cứng trong góc chụp.
- `SkyLightingController` giữ bộ preset có sẵn: hoàng hôn ấm cho 1–2, đêm cobalt/tím cho 3–5, huyết nguyệt cho 6–7, hoả ngục cho 8–9, nhật thực đỏ cho 10. Tăng ambient và ánh sáng trung gian, dùng sun/fog/tint/exposure riêng cho từng preset; runtime sửa bản sao material. Chiều tà không còn bầu trời xanh đen.
- `ComicInkFeature` dùng depth + normal để vẽ viền mực khoảng 1.2 px, saturation 1.12 và contrast 1.035. Dùng full-screen pass của URP hỗ trợ RenderGraph và compatibility; tham khảo [tài liệu Unity URP](https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/customizing-urp), đối chiếu implementation URP trong PackageCache. Không thêm halftone hậu kỳ 3D vì đây là phần tuỳ chọn và lá cây đã có nhiều cạnh nhỏ; halftone UI vẫn có.
- Công tắc **VIỀN MỰC COMIC (PC)** ở Settings/Hình ảnh, preview tức thời, Áp dụng lưu và Huỷ phục hồi. Tự tắt ở nền tảng mobile, chế độ cảm ứng hoặc chất lượng Mobile. Nhận diện chất lượng theo tên PC để không phụ thuộc chỉ số bị lọc theo build target. Cho desktop chọn chất lượng Mobile như mức thấp; Android vẫn chỉ có chất lượng Mobile.
- Đã chụp riêng từng level. Ảnh `levels/` dùng một góc sân thoáng để so sánh preset, nhân vật hiện đầy đủ; freeze/camera/pose chỉ thuộc fixture QA, không lưu vào scene. Các màn dùng cùng preset sẽ có nền giống nhau. [hud-combat.png](screens/round2/hud-combat.png) bổ sung nhân vật và quái trong một đợt chơi thực, với pose QA để quan sát rõ.
- [Geometry.json](tests/round2/Geometry.json) đối chiếu SampleScene trước/sau: 1972 Transform, 1107 MeshRenderer, 1005 MeshFilter, 2134 BoxCollider, 12 Rigidbody và NavMeshSettings không đổi. 53 file Levels trong backup ban đầu giống byte hiện tại. Không sửa geometry, collider, NavMesh, level ID hoặc logic combat.

| Nền trong Assets/CampusRiftUI/Comic/Resources/Comic | Màn | Ảnh kiểm chứng | Độ sáng nửa trên ảnh |
|---|---|---|---|
''']
for i,row in enumerate(brightness,1):
 sky='dusk' if i<=2 else 'night' if i<=5 else 'blood' if i<=7 else 'inferno' if i<=9 else 'eclipse'
 parts.append(f'| `sky-{sky}.png` + `.mat` | {i} | [level-{i:02}.png](screens/round2/levels/level-{i:02}.png) | {row["upper_srgb"]*100:.1f}% |\n')
parts.append('''
Độ sáng dùng trung bình `0.2126R + 0.7152G + 0.0722B` trên nửa trên screenshot sRGB, bao gồm cả toà nhà/cây, không chỉ sky. Đây là tỷ lệ giá trị pixel sRGB, không phải phép đo độ rọi hay luminance tuyến tính. Tất cả ảnh kiểm chứng đều vượt mức 25–30%. [Dữ liệu](level-brightness-round2.json), [bảng 10 ảnh](levels-contact-round2.png).

### Kiểm tra FPS

''')
parts.append(f'Windows Editor, 1920×1080, PC renderer, VSync off; GPU `{perf["gpu"]}`, CPU `{perf["cpu"]}`. Cùng thế giới đang tạm dừng với {perf["enemies"]} quái và HUD, bốn lượt OFF/ON/OFF/ON, 45 frame warm-up và 240 mẫu/lượt. FPS trung vị trung bình: **OFF {perf["offFps"]:.2f} / ON {perf["onFps"]:.2f}**; thay đổi tính theo giảm FPS **{perf["fpsLossPercent"]:.2f}%**. Số âm nằm trong nhiễu đo, không kết luận hiệu ứng làm game nhanh hơn. Không quan sát mức giảm vượt 10% ở phép đo này.\n\n')
parts.append('''Công tắc, quality thấp và chế độ mobile đều PASS. [Performance.json](tests/round2/Performance.json), [OFF](screens/round2/ink-off.png), [ON](screens/round2/ink-on.png). Đã kiểm tra hai ảnh có khác biệt thực, tránh trường hợp đo khi feature chưa chạy. Editor ban đầu ở Android nên chuyển tạm sang Windows để đo; đã phục hồi Android khi bàn giao.

## B. Khung truyện tranh và Đan Các

- Sinh lại sprite 128×128: viền mực đen khoảng 6 px, viền trong xanh nhạt/vàng, bóng đen đặc lệch 9 px, góc vát; import Sprite Single, uncompressed, border 9-slice 28 px. Thêm `paper.png` và `panel-texture.png`; gradient nhẹ và halftone/hatch ở góc, speed lines nền giữ nhẹ.
- `ComicTheme`/`UiKit` áp dụng chung cho UI dựng bằng code; `ComicUIBuilder` áp dụng scene và prefab. Font TMP Be Vietnam Pro tiếp tục hỗ trợ dấu Việt; tăng outline heading 0.28 và hard underlay offset 1/-1.
- Chi tiết Đan Các có tên trên dải vàng lớn, rarity tím, thẻ chỉ số màu giấy sáng, giá đen viền vàng/pha lê và CTA xanh chữ đen. Thumbnail vật phẩm nằm trong ô riêng, không bị panel cắt. Các thông số hồi máu/giá/giới hạn lấy nguyên từ dữ liệu game, không sao chép con số mẫu.
- Tab cao 80, footer 78, CTA cửa hàng 82 đơn vị; nút còn lại ít nhất 68 đơn vị thiết kế. FitFrame tiếp tục co khung cho màn rộng và điện thoại.

Ảnh: [Đan Các](screens/round2/dan-cac.png), [mục tăng sức mạnh](screens/round2/dan-cac-1.png), [pháp bảo](screens/round2/dan-cac-2.png), [vật phẩm cấp cao](screens/round2/dan-cac-3.png), [menu](screens/round2/menu.png), [quiz](screens/round2/quiz.png).

## C. Chữ, padding và kiểm tra tự động

- Header Hub: tách vùng Linh Thạch và bài đến hạn ôn, tăng khoảng lề phải; phụ đề Đan Các tách khỏi tiêu đề. Thẻ map thêm padding, phân vùng best time/status.
- Loadout: tên kỹ năng xuống dòng với inset rộng, key badge không đè tên, bộ lọc Vai trò rộng hơn, tiêu đề panel có lề; hàng/ô chọn vàng dùng chữ tối. HUD đưa Q/E/R/F về hàng riêng cỡ 26, charge count phía trên key, bỏ READY khỏi nhãn E/F.
- Quiz: câu hỏi và đáp án dùng normal casing/normal font, wrap và tăng chiều cao theo nội dung, inset mũi tên. Tiêu đề/nút thao tác giữ comic uppercase. ESC ở footer là gợi ý ngắn.
- Sau lần quét đầu còn sáu trường hợp, đã rút gọn nhãn PC/CẢM ỨNG và tăng bảng kết quả để đủ dòng thưởng. Soi ảnh phát hiện thêm toggle đè note và góc chụp bị che; đã sửa rồi chụp lại. Bảng kết quả hiện đủ mục tiêu/thời gian/star/thưởng.
- `ComicTextAudit` quét mọi TMP đang hiển thị trong trạng thái được mở: `isTextTruncated` và `textBounds` vượt rect (tolerance 0.75), bỏ chữ hoàn toàn ngoài viewport/mask hoặc alpha=0. Không thay nội dung hay chữa lỗi tự động trong audit.

''')
parts.append(f'**{len(audit["screens"])} trạng thái trong TextAudit + {len(outcomes)} trạng thái combat/kết quả bổ sung: 0 lỗi.** Tổng {audit["visibleTexts"]+sum(a["visibleTexts"] for a in outcomes)} lượt TMP hiển thị được kiểm tra. Các màn ngoài gameplay chạy VI/EN ở 1920×1080 và 2340×1080; gồm 10 loadout, 10 câu quiz và giải thích, Hub năm tab, bốn mục cửa hàng, học/thi, menu/credits/settings. HUD/pause và outcome kiểm tra ở cả hai độ rộng; outcome bổ sung cả EN/VI. Điện thoại dọc có ảnh và audit bổ sung. [TextAudit.json](tests/round2/TextAudit.json), [OutcomeAudit.json](tests/round2/OutcomeAudit.json).\n\n')
parts.append('''Ảnh: [Hub/map](screens/round2/hub.png), [loadout](screens/round2/loadout.png), [HUD](screens/round2/hud-level01.png), [HUD combat](screens/round2/hud-combat.png), [kết quả](screens/round2/result.png), [Settings](screens/round2/settings-video.png), [điện thoại ngang](screens/round2/dan-cac-phone-landscape.png), [điện thoại dọc](screens/round2/dan-cac-phone-portrait.png).

## D. Icon alpha

Chạy `Tools/strip_checker.py` trên icon nguồn opaque, mở rộng các nhóm item/skill/artifact/realm/element/badge. Làm sạch 17 icon; một số hào quang vẫn giữ caro nên tạo lại **8 icon** với alpha thật: `hoi-khi-dan`, `hoi-xuan-dan`, `kiem-tam-dan`, `cuong-luc-dan`, `kim-cuong-phu`, `ti-hoa-chau`, `tu-linh-dan`, `bang-tam-phu`. Assets tại `Assets/Resources/ContentImages/Items/<id>.png`, resize Lanczos 512×512, giữ GUID và ID. Helper bỏ qua tám icon đã tạo mới để chạy lại không ghi đè bằng nguồn caro cũ.

Đã soi bảng **61 icon** trên navy và ảnh cửa hàng mới; không thấy nền caro bám trong các icon kiểm tra. [Bảng icon](icons-alpha-contact-round2.png), [năm icon bổ sung](extra-icons-review-round2.png), [prompt bổ sung](IMAGE-PROMPTS-extra-icons-round2.md). Icon còn lại giữ phong cách gốc sau xử lý alpha; không tuyên bố toàn bộ đều được vẽ mới.

## Kiểm tra Unity và các giới hạn còn lại

''')
for suite in summary['suites']:parts.append(f'- **{suite["name"]}: {suite["status"]} — {suite["summary"]}.** [JSON](tests/round2/{suite["name"]}.json).\n')
parts.append('''- Unity đã refresh/compile cả cấu hình Windows và Android: **0 lỗi Console ở lần đọc cuối**. [console-final.json](tests/round2/console-final.json). Hai suite tổng 66 PASS; không sửa kỳ vọng test để đạt kết quả.
- Console có cảnh báo lúc QA: `_AdditionalShadowParams` 256/32 sau khi đổi Android ↔ Windows (URP yêu cầu restart Editor để tái tạo array); `Failed to create agent` từ `LevelDirector.RestoreShaban()` khi OnDestroy/dỡ scene; đôi lúc atlas additional shadow tự giảm độ phân giải. Không sửa NavMesh/gameplay để giấu cảnh báo. Log chẩn đoán ở [warnings-context.txt](tests/round2/warnings-context.txt). Lần đổi target cũng có cảnh báo kết nối MCP tạm thời. Hai cảnh báo compiler cũ của harness về biến/local function không dùng vẫn có thể hiện sau full rebuild.
- FPS là phép đo Editor với thế giới tạm dừng, hai quái; chưa đo player build, trận đông nhất hoặc điện thoại thật. Feature tự tắt trên mobile để giới hạn chi phí. Không dùng số đo này làm bảo đảm cho mọi thiết bị.
- Ảnh trời là upscale chất lượng cao, không thêm chi tiết 4K native. Bầu trời mỗi nhóm preset dùng chung ảnh; chưa tạo mười sky riêng vì mô tả/thumbnail có năm nhóm bối cảnh. 10 level đều có ảnh kiểm chứng, nhưng góc `levels/` là góc QA chung, không phải ảnh từ mọi vị trí spawn hay góc nhìn trong level.
- Mô hình/vật liệu gốc vẫn giữ art hiện tại; comic 3D đến từ sky/ánh sáng/hậu kỳ. Không thay asset nhân vật/quái hay thêm geometry để khớp thumbnail.
- Điện thoại dọc tiếp tục FitFrame letterbox khung ngang, chưa có layout dọc riêng: chữ/nút nhỏ hơn theo scale. 68 là đơn vị thiết kế, không phải 68 px vật lý ở mọi tỷ lệ.
- Audit kiểm tra các trạng thái đã mở và chữ đang hiển thị; không chứng minh mọi chuỗi dữ liệu hoặc tooltip/dropdown/overlay phát sinh trong mọi trận đều không tràn. IMGUI debug nội bộ giữ nguyên; đây không phải màn sản phẩm được chụp.

## Ảnh và file bàn giao

''')
parts.append(f'{len(screens)} PNG trong `screens/round2/`, bao gồm 10 level, đủ các ảnh yêu cầu vòng 1 và biến thể rộng. `background-level01/03.png` là bản sao ảnh tương ứng trong `levels/`; `dan-cac-phone-landscape.png` là bản sao ảnh landscape rộng. [Danh sách file + kích thước ảnh](FILES-round2.json).\n\n')
parts.append('### File sửa so với snapshot trước vòng 2\n\n')
parts.extend(f'- `{p}`\n' for p in record['modified'] if not p.endswith('.meta'))
parts.append('\n### File tạo mới\n\n')
parts.extend(f'- `{p}`\n' for p in record['created'] if not p.endswith('.meta'))
parts.append('''
File `.meta` đi kèm được liệt kê trong FILES-round2.json. Prompt, ảnh gốc imagegen, ảnh soi và dữ liệu QA nằm trong `task/ui-comic/`.

Tái tạo sprite: `python Tools/build_comic_sprites.py`. Chuẩn bị sky từ ảnh gốc trong workspace: `python Tools/prepare_comic_skies_round2.py`, sau đó menu `Campus Rift/UI/Import Comic Skies`. Áp dụng theme scene/prefab ở Edit Mode: `Campus Rift/UI/Apply Comic Theme`.

Kết thúc ở Edit Mode, scene MainMenu, Game view 1920×1080; khôi phục target Android, profile thật và settings sau fixture QA. Chưa triển khai/publish build.
''')
(root/'REPORT-round2.md').write_text(''.join(parts),encoding='utf-8')
print('REPORT-round2.md saved; 0 errors; '+str(len(screens))+' screenshots; 212 audits, 0 issues')
