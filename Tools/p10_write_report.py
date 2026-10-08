"""Write the P10 report only after all final validation artifacts pass."""
import json
from pathlib import Path

def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))

base=Path('Artifacts/Skills')
main=read(base/'SkillSet1.json');pool=read(base/'SkillSet1-Pool.json');edge=read(base/'SkillSet1-Edges.json')
ui=read(base/'P10-ComicTextAudit.json');perf=read(base/'SkillSet1-Performance-Player.json')
reg=read(base/'P10-Regressions.json');final=read(base/'P10-FinalTests.json')
assert not main['failed'] and not pool['failed'] and not edge['failed']
assert not pool.get('error') and not ui.get('error') and ui['issues']==0
assert len(final)==4 and all(x['completed'] and 'TIMEOUT' not in x['result'] and not x.get('failed') for x in final)
assert len(reg)==10 and all(x['completed'] and 'TIMEOUT' not in x['result'] and not x.get('failed') for x in reg)
for x in reg:
    if x['result'].strip()=='FAIL':
        assert x['test']=='PhantomDecoy' and x.get('baselineKnownFail')
        old=read(base/'P10-baseline/PhantomDecoy-WorldValidation.json');new=read(x['report'])
        assert all(old[k]==new[k] for k in ('doorCast','crossedEntrance','openedEntrance','stairCast','descendedStair'))
    else:
        assert 'PASS' in x['result'] or ('0 failed' in x['result'] and 'FAIL' not in x['result'])
assert len(perf['trials'])==7 and all(x['withinTenPercent'] for x in perf['trials'])
assert all(perf[x] for x in ('postProcessing','bloom','hdr','comicInk'))
names={'tich-lich-nhat-thiem':'Tích Lịch Nhất Thiểm','phat-no-hoa-lien':'Phật Nộ Hỏa Liên','han-bang-phong-an':'Hàn Băng Phong Ấn','than-kiem-ngu-loi':'Thần Kiếm Ngự Lôi Chân Quyết','kim-chung-trao':'Kim Chung Tráo','hac-dong-than-la':'Hắc Động Thần La','van-kiem-quyet':'Vạn Kiếm Quyết'}
metrics={x['skill']:x for x in pool['metrics']};mainmetrics={x['skill']:x for x in main['measurements']}
lines=['# REPORT-P10 — Kỹ năng đợt 1 và chuẩn hình ảnh fix1', '',
'Hoàn tất P10-T01–T10 và `PROMPT-P10-fix1.md`, ngày 02/10/2026. Tiếp tục từ code và tiến độ trên đĩa; không làm lại asset/icon đã có. Các kết quả dưới đây lấy từ những lượt kiểm tra cuối sau sửa hình. Bản sao trước sửa nằm trong `Backups/P10-pre-20261001-233732/`, `Backups/P10-fix1-pre-20261002-001210/` và `Backups/P10-visuals-before-final-20261002/`.', '',
'## Tiêu chí hoàn thành', '',
'| Task | Trạng thái và bằng chứng |', '|---|---|',
'| T01 | ✅ GroundAimIndicator ngắm PC theo tâm, mobile theo SkillDrag; drag/cancel/confirm được kiểm bằng input thật. DangerZoneRegistry có Circle/Cone/Capsule và expiry, trả vùng đang sống đúng hình học. Pool chung 112 slot + 30 FlyingSword prewarm. Main và Edge PASS. |',
'| T02 | ✅ Dash 10m qua 7 quái, dừng tường, miễn thương 0,2s, 180% Công/Lôi và Shock0,5s. Slash chỉ gây hit khi grow đi qua mục tiêu, xuất hiện sau dừng0,2s. Main, LightningFlash và sheet sáng/tối PASS. |',
'| T03 | ✅ Tụ1,2s vẫn đi35%; ném cong≤18m, nổR7/450% Công/Hỏa, Kim×1,5, Burn3s và fieldReaction4s. Edge xác nhận6,8m trong/7,2m ngoài và tâm registry theo điểm va chạm thật. |',
'| T04 | ✅ Nón90°/10m,120% Công/Thủy, Freeze2,5s; boss chỉ Chill50%/2,5s. Edge kiểm góc/range và thời điểm hết control; vỏ băng/shatter thể hiện trong sheet. |',
'| T05 | ✅ Sáu mục tiêu khác nhau, mỗi bước≤8m/LOS,200% Công giảm0,9 mỗi nảy; Lôi khắc Âm×1,5. Lock đầu tiên và vật cản được kiểm riêng; ưu tiên quái bay trong lựa chọn đầu. |',
'| T06 | ✅ Khiên40% maxHP/6s trước HP, phản20% melee bằng Reaction, FireResistance0,6/cap0,8; cạn chỉ truyền damage dư tới HP. Natural expiry xóa buff và tan, khác shatter khi cạn. Main/Edge và ảnh expiry PASS. |',
'| T07 | ✅ HútR9/3s, Pulled; boss đứng yên/Chill50%. Hất250% Công/4m và khôi phục đúng enabled/updatePosition/updateRotation/isStopped của agent/motor/brain; quái còn trên NavMesh. Main/Edge PASS. |',
'| T08 | ✅ R8,30 kiếm thật rơi trong3s với nhịp dồn, mỗi hit40% Công/Kim. Dùng FlyingSword pool, random landing và raycast mặt đất; đủ30 launch/return, không tăng object sau10 lần liên tiếp. |',
'| T09 | ✅ Cả10 chiêu có5 tầng; hệ số1+0,12×(tầng−1), CD1−0,05×(tầng−1); giá150/400/900/1600, realm+1 mỗi tầng/capĐộKiếp. Lưu skills.ranks qua JSON và JsonProfileStore thật; điều kiện/wallet/locked/max guard đúng. Phantom tầng3 tan gây150% Công hệÂm/R3; ba chiêu cũ được kiểm actual rank5. HUD tầngI–V góc ô, SkillBook nâng cấp thật. |',
'| T10 | ✅ Harness mới kiểm bảy chiêu PC/mobile, damage/CD/spirit/element/status/finite/pool và cả5 tầng. Nhóm hồi quy không có FAIL mới; HubFlow/HubLayout PASS. |', '',
'Catalog có đủ10 kỹ năng, màn Chuẩn Bị DEV chọn được từng chiêu và vào màn sử dụng được. Definition/config/catalog được cài vào prefab CampusExplorer và SampleScene bằng `Campus Rift/V2/Install Skill Set 1`. Chiêu Aimed ngắm mobile bằng drag; các chiêu Instant dùng input xác nhận bình thường.', '',
'## Kiểm chứng cuối', '',
f'- [Main](../../Artifacts/Skills/SkillSet1.json): **{len(main["passed"])} passed / 0 failed**, lượt fresh không resume sau sửa runtime/camera. Sửa cuối chỉ là keyword vật liệu popup và ngưỡng màu shader; Pool/Edge/UI/hồi quy/capture/benchmark chạy sau hai sửa này. Số314 ở log cũ là check tích lũy từ resume, không dùng làm số lượt fresh.',
f'- [Pool](../../Artifacts/Skills/SkillSet1-Pool.json): **{len(pool["passed"])} passed / 0 failed**;10 PC cast liên tiếp mỗi chiêu, không Clear giữa các lần; sau đuôi hiệu ứng tự idle, finite, registry hết hạn; mobile cast thật. Kiểm popup merge100ms và giảm flash trên alpha ảnh thật.',
f'- [Edge](../../Artifacts/Skills/SkillSet1-Edges.json): **{len(edge["passed"])} passed / 0 failed**; boundary, LOS, control boss, exact NavMesh flags, shield residual/cap, profile disk và ba chiêu cũ.',
f'- [ComicTextAudit](../../Artifacts/Skills/P10-ComicTextAudit.json): **{len(ui["screens"])} màn / 0 issues**; VI/EN,1280×720/1920×1080, HUD trống/tầngV, mobile, sách kỹ năng tên dài, Chuẩn Bị10chiêu và Settings giảm flash PC/mobile.',
'- [Tổng hợp fresh](../../Artifacts/Skills/P10-FinalTests.json) và [10 suite hồi quy](../../Artifacts/Skills/P10-Regressions.json). Baseline trước P10 được giữ riêng trong Artifacts/Skills/P10-baseline/.', '',
'| Hồi quy | Kết quả |', '|---|---|']
for x in reg:
    note=' — lỗi có sẵn trước P10, năm cờ kiểm tra giống baseline' if x.get('baselineKnownFail') else ''
    lines.append(f'| {x["test"]} | {x["result"].strip().replace(chr(10),"; ")}{note} |')
lines += ['', 'PhantomDecoyWorldPlayTest đã FAIL trước P10: phân thân không qua/mở Entrance E, nhưng cast và xuống cầu thang đúng. [Baseline](../../Artifacts/Skills/P10-baseline/PhantomDecoy-WorldValidation.json) và lượt cuối có cùng năm cờ kiểm tra. Giữ nguyên DONE=FAIL, không gọi đây là PASS; không có hồi quy mới. Main/Edge kiểm riêng sát thương nổ tầng3, thời gian tồn tại/CD tầng5 của Phân Thân đều PASS. Lỗi traversal cửa có sẵn này chưa được sửa trong P10.']
lines += ['', 'Unity biên dịch 0 lỗi; standalone Windows64 build thành công. Hai warning unused của code cũ và cảnh báo shader DoF/Panini bị strip (các pass không dùng), shadow atlas có trong log; Bloom/HDR/ComicInk đang hoạt động trong lượt đo. Player log còn thông báo agent khởi tạo trước NavMesh của scene; các quái fixture được spawn sau NavMesh và kiểm NavMesh/pull PASS. Profile kiểm tra dùng transient hoặc thư mục test, không tiêu tiền/nâng tầng hồ sơ người chơi.', '',
'## Hiệu năng và pool', '',
f'PC standalone Mono release, **{perf["width"]}×{perf["height"]}**, {perf["quality"]}/{perf["pipeline"]}, VSync off, camera gameplay mặc định và bảy quái thật. GPU **{perf["gpu"]}**, CPU **{perf["cpu"]}**. PostProcessing/Bloom/HDR/ComicInk đều true, Bloom intensity {perf["bloomIntensity"]:.2f}. Không chụp/call MCP trong sampling. Median frame240idle,3cast/chiêu; riêng dash đo idle cả đầu và cuối với cùng orbit, giữ hai số gốc và cân theo số frame hoạt động từng view. Sai số âm nhỏ là biến thiên sampling, không coi là tăng tốc do chiêu.', '',
'[JSON PC đầy đủ](../../Artifacts/Skills/SkillSet1-Performance-Player.json); DONE=PASS. Peak dưới đây lấy **max** giữa Main, Pool, Visual và PC performance, không chọn riêng lượt ít hạt nhất. Object đếm toàn bộ Transform subtree pool gồm các đối tượng được warm, trước/sau10PCcast.', '',
'[Ảnh render từ bản PC sau khi đo](../../Artifacts/Skills/P10-Player-render.png), cho thấy scene gameplay và viền comic; chụp sau sampling, không dùng ảnh này thay cho các sheet kỹ năng.', '',
'| Chiêu | Idle FPS | Cast FPS | Giảm FPS | Peak PC/mobile | Object10cast | GC/frame |', '|---|---:|---:|---:|---:|---|---:|']
for x in perf['trials']:
    key=x['skill'];m=metrics[key];a=mainmetrics[key];v=read(base/(key+'-visual.json'))
    pc=max(m['peakPC'],a['peakPC'],v['peakPC'],x['peakParticles']);mobile=max(m['peakMobile'],a['peakMobile'],v['peakMobile'])
    assert pc<=1500 and mobile<=400 and m['before']==m['after'] and m['maxFrameGCBytes']==0 and x['maxVfxFrameGCBytes']==0
    lines.append(f'| {names[key]} | {x["baselineFps"]:.1f} | {x["castFps"]:.1f} | {x["lossPercent"]:.2f}% | {pc}/{mobile} | {m["before"]}→{m["after"]} | 0B |')
lines += ['', 'Không Instantiate/Destroy khi cast sau warm-up, không mở rộng buffer mỗi frame. Pool reset không tạo lại object;10cast liên tiếp không exhaustion. VFX LateUpdate đo GetAllocatedBytesForCurrentThread=0B; đây là phạm vi vòng cập nhật VFX, không phải tuyên bố cả game không cấp phát. Mobile giảm emission, số gai/shard và trail, tắt distortion; giữ damage/range/control.', '',
'Các phép đo Editor trước tối ưu có lượt vượt10%; giữ nguyên các JSON before-optimization/layered-stroke/combined-ghost và kết quả Editor cuối. Không đổi FAIL thành PASS. Bản native thử đầu thiếu ComicInk vì scene tên P10BenchmarkScene: giữ `SkillSet1-Performance-Player-before-final-visuals.json` làm lịch sử, **loại khỏi bằng chứng đạt**. Lượt hợp lệ dùng bản sao tên SampleScene và guard kiểm đồ họa trong JSON/DONE.', '',
'## Chuẩn fix1 và ảnh', '',
'Mỗi vật thể/trail chính có lõi HDR/màu hệ và bóng alpha tối theo hệ; mesh có inverted hull. Shader LayeredStroke vẽ lõi trắng/viền vàng/mép tím hoặc đồng trong một lượt vẽ. Slash và ChainBolt đặt lõi3,8% chiều cao viewport (ngoài phần taper hai đầu); total width×1,85 bù phần viền. Afterimage là hai mesh bake rig thật có outline, gộp chín bộ phận thành một mesh/ghost để giảm drawcalls.', '',
'Flash comic55ms dưới HUD, SpeedLines hướng tâm, local hold Animator/NavMesh65ms, không freeze toàn game. Victim rim khoảng1,8× chiều cao quái; opacity cao chỉ≈65ms. ReduceSkillFlashes giảm alpha overlay xuống≤0,1/SpeedLines0,15. Popup bold/outline đen, màu hệ, crit×1,4, jitterhai trục, stagger40ms, gộp hit cùng mục tiêu<100ms; tránh chồng theo khoảng cách màn hình. Ground residue sống1,8–2,8s rồi fade; vùng lửa4s là thời gian gameplay riêng, khói/tàn duy trì theo field.', '',
'| Chiêu | Local stop | Impulse | Dư âm chính |', '|---|---:|---:|---|',
'| Tích Lịch |65ms|0,75|Slash sau dừng0,2s, grow80ms/hold150ms, alpha giảm trước0,3s; dấu đất2,7s, tia điện/ghost tan|',
'| Hỏa Liên |65ms|1,0|Sen24cánh, ribbon liên tục; FireBloom/fire-smoke flipbook, dấu blast2,8s, field4s|',
'| Hàn Băng |65ms|0,55|Gai nối5hàng, vỏ alpha0,38, shatter khi Freeze hết; đất2,8s|',
'| Ngự Lôi |65ms|0,45|Nảy80ms, core giảm theo0,9; fork/tia lửa, dấu đất2,3s|',
'| Kim Chung |65ms|0,6|Chuông bán trong có metallic highlight/rune/cracks; shard khi cạn, fade riêng khi6s; đất2,3s|',
'| Hắc Động |65ms|0,9|Cầu đenØ2,7m/đĩa nghiêng, particles xoắn/bụi; sóng9m và dấu2,8s|',
'| Vạn Kiếm |65ms|0,5|Cổng ởcao3,7m, mesh kiếm vàng có tip trắng/ink; cắm/fade1s, đất1,8s|', '',
'Camera người chơi distance3,4m/FOV60/pitch14, orbit bình thường; light=màn1/brightness1, dark=màn3/brightness0,2. Quái EnemyPool tieu-yeu đúng mesh/scale prefab, không dummy. Chỉ ba kiếm ngọc thụ động được ẩn trong fixture chụp để tách hình chiêu mới, khôi phục sau chụp; gameplay không bị tắt chúng. Sheet8frame/nền, crop impact từ ảnh gốc không sửa màu; mobile là lần cast với emission/trail/distortion mobile. Tự soi lại đủ bảy chiêu sau thay đổi cuối; bảng có/không và frame nằm ở PROGRESS.md.', '',
'| Chiêu | Sheet sáng | Sheet tối | Impact cận | Mobile |', '|---|---|---|---|---|']
for key,name in names.items():
    for suffix in ('sheet-light','sheet-dark','impact','mobile'): assert Path(f'task/p10/screens/{key}-{suffix}.png').exists()
    v=read(base/(key+'-visual.json'));assert v['flashCaptured'] and v['passiveSwordsHidden'] and v['poolObjectsBefore']==v['poolObjectsAfter']
    lines.append(f'| {name} | [8frame](screens/{key}-sheet-light.png) | [8frame](screens/{key}-sheet-dark.png) | [ảnh](screens/{key}-impact.png) | [ảnh](screens/{key}-mobile.png) |')
lines += ['', 'Ảnh đầy đủ impact-light/dark và112frame gốc nằm trong screens/frames. `screens/<id>-sheet.png` trùng sheet-light. [Kim Chung hết tự nhiên](screens/kim-chung-trao-expiry.png). JSON `<id>-visual.json` ghi camera/quái, peak, pool, flashCaptured và impulse thật.', '',
'HUD đã co plate gợi ý theo chữ+32px, plate kỹ năng theo4ô, ô trống có frame mờ/+; nhãn tầngI–V ở góc và nằm trên icon/charge. Đã soi ảnh HUD PC/mobile và sách kỹ năng dài của audit cuối. Không thay đổi bố cục Hub vượt yêu cầu; HubFlow/HubLayout vẫn qua.', '',
'## Tài nguyên và giấy phép', '',
'[LICENSES.md](../../Assets/Skills/Core/LICENSES.md) ghi từng tài nguyên/tác giả/URL/license và dẫn tới license gốc. Không có bản tải ngoài mới, không AssetStore/login/paid asset.', '',
'| Tài nguyên | Nguồn/giấy phép |', '|---|---|',
'| BoltLine/ForkSheet/CrackleSheet/Flare/Streak/SpeedLines và vật liệu SpeedForce | Kenney Particle Pack, Kenney Vleugels, CC0; license gốc trong Assets/VFX/SpeedForce |',
'| laserSmall/thrusterFire/forceField/lowFrequency_explosion | Kenney Sci-fi Sounds, CC0; Audio directory gốc giữ license |',
'| explosionCrunch/impactBell/impactGlass | Kenney Impact Sounds, CC0; license gốc GiantHandSeal/VoidWall Audio |',
'| Bảy icon comic512px alpha thật | Artwork Campus Rift đã có ở UI round3, giữ nguyên PNG/GUID/ContentImages mapping |',
'| Sen24cánh, tinh thể, chuông tiện xoay, sphere/disc, shader Energy/Ink/LayeredStroke/GroundMark | Mesh/shader nguyên gốc sinh bằng code P10; không model bên thứ ba |',
'| Fire/Smoke flipbook512RGBA4×4 | Procedural nguyên gốc Tools/p10_textures.py; không ảnh bên thứ ba |',
'| FlyingSword/Afterimage/Impulse/ComicInk/Bloom | Tái dùng code/asset có sẵn P03 và các chiêu cũ |',
'| ComicVietnamese SDF / P10DamageNumbers material | Font Be Vietnam Pro Medium, The Be Vietnam Pro Project Authors, SIL OFL1.1; font và OFL có sẵn trong Assets/CampusRiftUI/Fonts. Chỉ tạo vật liệu viền, không sửa font |', '',
'SFX cast/loop/hit/tail chọn nhiều clip Kenney, prewarm hai AudioSource mỗi slot, route vào SFX mixer; sen có loop fire, chuông bell, hút bass/forceField, băng glass và kiếm impact khác lightning. Pose procedural nhẹ trên rig sau Animator, trả pose không phá locomotion.', '',
'## Tái kiểm và giới hạn', '',
'- Main/Pool/Edge/UI: `python Tools/p10_finish_tests.py`; hồi quy: `python Tools/p10_regressions.py`. Harness đều chạy trong SampleScene thật. Các script chỉ kiểm QA, không tự sửa kết quả FAIL.',
'- Visual: Play SampleScene → `python Tools/ui_command.py code Tools/p10_batch_start.cs` → chờ DONE mới → `python Tools/p10_sheets.py`.',
'- PC benchmark: Stop → `python Tools/ui_command.py code Tools/p10_build_bench.cs` → nếu callback chưa chạy, `python Tools/ui_command.py code Tools/p10_build_invoke.cs` → chờ P10-Build-status Succeeded → chạy Builds/P10Benchmark/CampusRift.exe ở1920×1080, chờ DONE mới. Benchmark scene riêng, không thêm vào danh sách build game chính.',
'- Mobile input/quality/particle budget đã đo trong Unity Editor bằng đường input mobile thật. **Chưa đo FPS hay hình trên thiết bị Android vật lý**; không suy diễn số PC thành số Android. Project vẫn có target Android và code/runtime không phụ thuộc UnityEditor ngoài harness có guard.',
'- `PROMPT-P10-fix2.md`/review-notes tồn tại trên đĩa; phiên này hoàn tất yêu cầu gốc và fix1 đã được người dùng gọi. Không tuyên bố hoàn thành brief fix2 chưa được giao.',
'- Các lỗi fixture đầu (camera, đội hình chain, rain landing, Prep instance, agent snapshot trước settle) và lượt FAIL được giữ trong PROGRESS/bản JSON lịch sử. Kết luận PASS dựa trên fresh run cuối, không xóa check để né lỗi.', '']
lines += ['Kiểm tra Đại Thủ Ấn cũ yêu cầu nhãn phím chứa READY dù HUD comic đã dùng phím F từ trước P10. Đã cập nhật assertion để kiểm cooldown=0, không casting, overlay<0,02, timer trống, nhãn đúng phím, glyph trắng và không locked; chờ thêm một Update của HUD. Giữ lượt74pass/1fail đầu ở `Artifacts/Skills/GiantHandSeal-before-hud-assertion-fix.json`. Sửa này chỉ nằm trong harness UNITY_EDITOR, không đổi gameplay hay số đo standalone.', '']
lines += ['Đã khôi phục active target Android, Editor ở SampleScene và dừng Play. [Trạng thái Editor cuối](../../Artifacts/Skills/P10-FinalEditorState.json) xác nhận không compiling/updating, build Succeeded; [console sau đổi target](../../Artifacts/Skills/P10-Console-final.json) có 0 Error/Exception. Còn bốn warning: hai unused của harness cũ, import win.mp3 và meta font LiberationSans cũ. Không sửa các tài nguyên ngoài phạm vi P10 này.', '']
Path('task/p10/REPORT-P10.md').write_text('\n'.join(lines),encoding='utf-8')
print('REPORT-P10.md written from passing final artifacts')
