"""Freeze fresh fix2 evidence and write the review response after verification."""
import json, shutil
from pathlib import Path
from datetime import datetime, timezone

root=Path('Artifacts/Skills'); dst=root/'fix2'
def read(p): return json.loads(Path(p).read_text(encoding='utf-8-sig'))
main=read(root/'SkillSet1.json'); pool=read(root/'SkillSet1-Pool.json'); edge=read(root/'SkillSet1-Edges.json')
ui=read(root/'P10-ComicTextAudit.json'); perf=read(root/'SkillSet1-Performance-Player.json')
reg=read(root/'P10-Regressions.json'); final=read(root/'P10-FinalTests.json'); editor=read(root/'P10-FinalEditorState.json')
cutoff=datetime.fromisoformat('2026-10-02T05:24:00+07:00').timestamp()
for f in ('SkillSet1.json','SkillSet1-Pool.json','SkillSet1-Edges.json','P10-ComicTextAudit.json','SkillSet1-Performance-Player.json','P10-Regressions.json','P10-FinalTests.json','P10-FinalEditorState.json'):
    assert (root/f).stat().st_mtime>cutoff, 'Stale evidence: '+f
assert not main['failed'] and not pool['failed'] and not edge['failed']
assert not pool.get('error') and not ui.get('error') and ui['issues']==0
assert len(final)==4 and all(x['completed'] and 'TIMEOUT' not in x['result'] and not x.get('failed') for x in final)
assert len(reg)==10 and all(x['completed'] and 'TIMEOUT' not in x['result'] and not x.get('failed') for x in reg)
for x in reg:
    assert Path(x['report']).stat().st_mtime>cutoff, 'Stale regression: '+x['test']
    if x['result'].strip()=='FAIL':
        assert x['test']=='PhantomDecoy' and x.get('baselineKnownFail')
        baseline=read(root/'P10-baseline/PhantomDecoy-WorldValidation.json'); latest=read(x['report'])
        assert all(baseline[k]==latest[k] for k in ('doorCast','crossedEntrance','openedEntrance','stairCast','descendedStair'))
    else: assert 'PASS' in x['result'] or '0 failed' in x['result']
assert len(perf['trials'])==7 and all(x['withinTenPercent'] for x in perf['trials'])
assert all(perf[k] for k in ('postProcessing','bloom','hdr','comicInk'))
assert editor['target']=='Android' and not any(editor[k] for k in ('playing','compiling','updating')) and editor['build'].startswith('Succeeded')
console=read(root/'P10-Console-final.json'); assert console.get('success') and all(x['type']!='Error' for x in (console.get('data') or []))
assert not read(dst/'GameplayDataAudit.json')['changed']
names={'tich-lich-nhat-thiem':'Tích Lịch Nhất Thiểm','phat-no-hoa-lien':'Phật Nộ Hỏa Liên','han-bang-phong-an':'Hàn Băng Phong Ấn','than-kiem-ngu-loi':'Thần Kiếm Ngự Lôi','kim-chung-trao':'Kim Chung Tráo','hac-dong-than-la':'Hắc Động Thần La','van-kiem-quyet':'Vạn Kiếm Quyết'}
metrics={x['skill']:x for x in pool['metrics']}; measurements={x['skill']:x for x in main['measurements']}
visuals={key:read(dst/(key+'-visual.json')) for key in names}
for key,v in visuals.items():
    assert v['revision']=='fix2-final' and v['flashCaptured'] and v['passiveSwordsHidden'] and v['poolObjectsBefore']==v['poolObjectsAfter']
    for suffix in ('sheet-light','sheet-dark','impact','impact-light','impact-dark','mobile'):
        p=Path(f'task/p10/screens/fix2/{key}-{suffix}.png'); assert p.is_file() and p.stat().st_mtime>cutoff
    for mode in ('light','dark'):
        for i in range(8): assert Path(f'task/p10/screens/fix2/frames/{key}-{mode}-{i}.png').stat().st_mtime>cutoff
for p in root.iterdir():
    if p.is_file() and (p.name.startswith(('SkillSet1','P10-Final','P10-Regress','P10-ComicText','P10-Build','P10-Console','P10-Player'))):
        if p.stat().st_mtime>cutoff: shutil.copy2(p,dst/p.name)
(dst/'regressions').mkdir(exist_ok=True)
for x in reg: shutil.copy2(x['report'],dst/'regressions'/(x['test']+'.json'))
shutil.copy2(root/'P10-baseline/PhantomDecoy-WorldValidation.json',dst/'regressions/PhantomDecoy-baseline.json')
frozen_reg=[dict(x,report=str(dst/'regressions'/(x['test']+'.json')).replace('\\','/')) for x in reg]
frozen_final=[dict(x,report=str(dst/Path(x['report']).name).replace('\\','/')) for x in final]
(dst/'P10-Regressions.json').write_text(json.dumps(frozen_reg,ensure_ascii=False,indent=2),encoding='utf-8')
(dst/'P10-FinalTests.json').write_text(json.dumps(frozen_final,ensure_ascii=False,indent=2),encoding='utf-8')

lines=['# REPORT-P10-fix2 — Kết quả sửa theo review', '',
'Hoàn tất vòng fix2 ngày 02/10/2026 sau khi đọc đầy đủ brief fix2, review-notes cập nhật04:49, chuẩn fix1 và tiến độ/kết quả hiện có. Tiếp tục từ fix1; sao lưu trước sửa tại `Backups/P10-fix2-pre-20261002-0450/`. Ảnh cũ giữ nguyên trong `screens/`; toàn bộ ảnh mới ở `screens/fix2/`. Không tạo lại icon, không tải asset bên ngoài.', '',
'Các mục dưới đây đều đã sửa hoặc đã được sửa ở fix1 và kiểm lại bằng capture fix2 mới. F1–F8 là số frame ghi trên sheet; frame gốc đánh số0–7. Đã trực tiếp soi đủ14sheet,7impact và7mobile trước chốt. Frame tĩnh chứng minh hình; jitter, tốc độ hút/hất và khôi phục trạng thái được đối chiếu thêm bằng runtime/harness.', '',
'## Từng mục review', '',
'| Mục | Trạng thái / thay đổi | Bằng chứng mới |', '|---|---|---|',
'| Tích Lịch: nhân vật thành khối vàng F2 | Đã sửa: ghost có viền điện/tím, alpha thấp; giữ model mặt/quần áo. Không phủ emission lên toàn thân. | Sheet light/dark F2, F4–6 |',
'| Tích Lịch: camera xuyên quái F3 | Đã sửa và kiểm camera người chơi: tránh vật cản scene; khi camera nằm trong bounds quái, chỉ ẩn render quái đó và khôi phục khi ra ngoài. F3 còn quái gần cạnh dưới nhưng không che kín màn. | Sheet light/dark F3–4; Edge kiểm trong lúc dash với CampusExplorer.followCamera thật, orbit−10° trong góc xoay gameplay |',
'| Tích Lịch: impact ống trắng đục | Đã sửa: nét slash trắng-vàng sắc với mép tím, silhouette tối, speed lines và flash55ms dưới HUD; burst có tâm trong suốt. | impact-light/dark, sheet F6–7 |',
'| Hỏa Liên: sen cam phẳng | Đã sửa: cánh viền đỏ sẫm/mực, lõi HDR vàng-trắng, mở theo charge; tàn lửa có vận tốc vào tâm và xoáy. | Sheet light/dark F1–3 |',
'| Hỏa Liên: đường bay chồng đĩa | Đã sửa: TrailRenderer liên tục, shader scroll UV/16frame atlas lửa12fps, taper; khói đen mỏng theo sen, không sinh chuỗi đĩa. | Sheet light/dark F4 |',
'| Hỏa Liên: thiếu hai frame nổ | Đã sửa: FireBloom0,5s, flash, quả cầu lửa/flipbook, vòng lan R7 và shard; capture theo thời điểm va chạm thật+70/+280ms. | Sheet light/dark F5–6; impact-light/dark giữ flash tức thời |',
'| Hỏa Liên: vùng cháy đã tốt | Giữ và kiểm lại: vùng lửa4s, dấu cháy xoáy mực/đỏ tối, khói và tàn fade. | Sheet light/dark F7–8 |',
'| Hàn Băng: hơi lạnh dưới gai | Đã bổ sung: sương trắng-cyan dùng alpha atlas khói, giảm opacity để đọc quái. | Sheet light/dark F3–6; mobile |',
'| Hàn Băng: mảnh băng khi mọc | Đã bổ sung: mảnh tinh thể bật theo từng gai, ngân sách PC/mobile khác nhau. | Sheet F2–4 |',
'| Hàn Băng: vỡ khi hết Freeze | Đã sửa: sáu/ba shard mỗi vỏ, kích thước0,35m; frame8 chụp3,0s bắt vỏ vỡ thay vì chỉ biến mất. | Sheet light/dark F7–8; Edge Freeze2,5s |',
'| Ngự Lôi: impact làm trắng HUD | Đã sửa ở fix1 và kiểm lại: overlay tối/speed lines canvas order−10; HUD order cao hơn, flash55ms có cờ giảm nhấp nháy. | impact-light/dark; Pool kiểm reduce-flash |',
'| Ngự Lôi: dải giấy vàng / thiếu tím | Đã làm lại: filament trắng mảnh, glow vàng mềm bán trong, aura tím đậm, nhánh tím giữa cung và tại mục tiêu; jitter mỗi2frame, width nhân0,9 mỗi nảy. Chuẩn lõi mảnh của review fix2 thay thế lõi trắng dày ở fix1. | Sheet light/dark F2–6; shader LayeredStroke _Lightning=1, runtime Bolt |',
'| Ngự Lôi: cầu trắng quá to/đục | Đã sửa: rim rỗng tâm, đường kính≈1,5× chiều cao quái, alpha cao ngắn, tia điện tỏa; model còn thấy được. | Sheet light/dark F2/F6; impact |',
'| Kim Chung: vàng phẳng/che nhân vật | Đã sửa: fresnel, specular kim loại vàng, tâm opacity thấp, rune chạy/vết nứt; model nhìn rõ qua thân. | Sheet light/dark F1–5 |',
'| Kim Chung: hạ chuông tràn khung | Đã sửa và chụp bằng camera mặc định: toàn thân chuông nằm trong F1–3, không thay FOV/zoom để giả hình. | Sheet light/dark F1–3 |',
'| Kim Chung: vỡ ít/mảnh nhỏ | Đã sửa:22PC/10mobile mảnh panel chuông cong lớn, flash vàng và shockwave; natural expiry vẫn chỉ fade. | Sheet light/dark F6; impact; kim-chung-trao-expiry.png |',
'| Hắc Động: cầu nhỏ/sai điểm đặt | Đã sửa: Ø2,7m đặt trên điểm ngắm mặt đất trong15m, tâm nâng1,8m (xấp xỉ1,5m yêu cầu), không đặt trước mặt người chơi. | Sheet light/dark F2–5, aim10m/z+3; Main/Edge range |',
'| Hắc Động: đĩa chỉ là vòng mảnh | Đã làm lại: annulus có bề rộng1,77m, bán kính ngoài3,1m, nghiêng, texture shader xoáy tím-hồng chuyển động; sphere đen còn rõ. | Sheet light/dark F2–5; P10Accretion.shader |',
'| Hắc Động: thiếu hút xoắn / bụi đất | Đã làm lại: particle mesh mảnh đá, vận tốc tiếp tuyến+hướng tâm+lift trong R6–9, tracer đoạn chạy xoắn, ground swirl mực tối; distortion chỉPC. | Sheet light/dark F3–5; mobile giảm hạt/tắt refraction |',
'| Hắc Động: quái mất khỏi khung / không nâng-xoay | Đã sửa: aim lệch bên; visual rig nâng và orbit trong khi root/collider giữ trên NavMesh, khôi phục local pose khi hất/cancel. Không đổi khoảng gom/hất gameplay. | Sheet F3–6; ba kiểm Edge mới lift/pose/disable cleanup |',
'| Hắc Động: cầu hồng mờ / hất yếu | Đã sửa: core co/fade0,45s, đĩa tan0,4s, flash tím-trắng, vòng Shockwave lan đúngR9,60PC/24mobile particle bắn ra; hất4m/0,35s giữ nguyên. | Sheet light/dark F6–7; impact; Edge4m/NavMesh |',
'| Vạn Kiếm: kiếm trắng/cột cờ | Đã sửa: mesh kiếm dài1,8125m, body vàng kim, chuôi/chắn vàng lớn, chỉ tip/lưỡi vàng-trắng có mực; trail0,045s có taper. | Sheet light/dark F4–7; mobile |',
'| Vạn Kiếm: cổng nhỏ/mưa thưa | Đã sửa hình/capture: năm cổng vàng R1,1 trên cao3,7m, blade giữ kích thước lúc fade; nhịp30launch/3s dồn dần và chụp peak2,45–3,15s. Mật độ bị giới hạn bởi đúng30kiếm và phạm viR8; không tăng số hit để làm dày ảnh. | Sheet light/dark F1–2/F5–7; Main đếm30; mobile2,8s |',
'| Vạn Kiếm: tia lửa/bụi/vết đất sai màu | Đã sửa: flash vàng, sparks/bụi, dấu nứt nâu-đồng; shader giữ màu Kim thay vì đổi tím ở trail/ground. | Sheet light/dark F5–8; impact |',
'| Vạn Kiếm: kiếm cắm và tan bụi | Đã sửa: embed0,5s rồi fade0,5s, không co mesh; khi tan bật6PC/3mobile hạt bụi vàng0,6s. Flight/hit0,24s không đổi. | Sheet light/dark F6–8; Pool30return/idle |',
'| Chung: impact dưới HUD / không trắng màn | Đã sửa và kiểm cả bảy: canvasorder−10,55ms tối hóa/speed lines, giảm-flash tùy chọn; local Animator/Nav hold65ms, không đổi timeScale. | Bảy impact-light/dark; Main/Pool/UI |',
'| Chung: player che tâm / camera giả | Đã sửa fixture: camera gameplay distance3,4/FOV60/pitch14; orbit như người chơi, point Hole10m/z+3 và Rain10m/z−4; bảy EnemyPool tieu-yeu thật. |14sheet; <id>-visual.json ghi camera/enemy |',
'| Chung: ba kiếm xanh che chiêu | Đã sửa capture: tạm ẩn đúng ba kiếm thụ động rồi khôi phục sau fixture; gameplay vẫn có chúng. |14sheet,7mobile; passiveSwordsHidden=true |', '',
'Không còn mục review chưa sửa. Mức độ đọc hình là tự đánh giá qua ảnh đính kèm; không coi ảnh tĩnh là phép đo FPS hay bằng chứng thay đổi damage.', '',
'## Kiểm chứng sau sửa cuối', '',
f'- [SkillSet1PlayTest](../../Artifacts/Skills/fix2/SkillSet1.json): **{len(main["passed"])} passed /0failed**, chạy fresh PC/mobile/tầng1–5, sát thương/range/CD/spirit/status/finite/return đúng.',
f'- [Pool](../../Artifacts/Skills/fix2/SkillSet1-Pool.json): **{len(pool["passed"])} passed /0failed**,10PCcast liên tục mỗi chiêu, mobile, không exhaustion; [Edge](../../Artifacts/Skills/fix2/SkillSet1-Edges.json): **{len(edge["passed"])} passed /0failed**, gồm lift/orbit/khôi phục pose và cancel mới.',
f'- [ComicTextAudit](../../Artifacts/Skills/fix2/P10-ComicTextAudit.json): **{len(ui["screens"])} màn /0issues**. [Bốn lượt fresh](../../Artifacts/Skills/fix2/P10-FinalTests.json).',
'- [Đối chiếu dữ liệu trước/sau](../../Artifacts/Skills/fix2/GameplayDataAudit.json):16file definition/config/catalog/rank/base-runtime giống SHA256 bản sao trước sửa. Các phép thử thực tế phía trên kiểm thêm timing/damage/range/control.',
'- [Mười suite hồi quy fresh](../../Artifacts/Skills/fix2/P10-Regressions.json), HubFlow/HubLayout đềuPASS; kết quả riêng lưu trong fix2/regressions/.', '',
'| Hồi quy | Kết quả |', '|---|---|']
for x in reg:
    note='; lỗi cũ giống baseline, không gọi PASS' if x.get('baselineKnownFail') else ''
    lines.append(f'| {x["test"]} | {x["result"].strip().replace(chr(10),"; ")}{note} |')
lines += ['', 'PhantomDecoyWorld có lỗi traversal EntranceE từ trướcP10: crossedEntrance/openedEntrance=false, doorCast/stairCast/descendedStair=true. Lượt mới trùng cả năm cờ [baseline](../../Artifacts/Skills/fix2/regressions/PhantomDecoy-baseline.json). Giữ raw DONE=FAIL, không có FAIL mới; sửa traversal ngoài phạm vi reviewVFX này.', '',
'Lượt Edge đầu28/1 thiếu coverage camera: pitch14 nằm cao hơn bounds quái. Giữ [JSON thất bại](../../Artifacts/Skills/fix2/Edges-camera-coverage-before-low-orbit.json), chỉ đổi fixture sang orbit thấp−10° để tạo giao cắt thực, không đổi runtime hoặc nới assertion. Lượt Edge mới phải xác nhận có giao cắt, model chứa camera không được render, và khôi phục đúng rendering khi ra ngoài.', '',
f'Unity0lỗi biên dịch; Windows64 build `{editor["build"]}`. [Trạng thái Editor cuối](../../Artifacts/Skills/fix2/P10-FinalEditorState.json): Android, SampleScene, Stop, không compiling/updating. [Console](../../Artifacts/Skills/fix2/P10-Console-final.json) và [Player log](../../Artifacts/Skills/fix2/P10-Player.log) giữ cảnh báo thật; không tuyên bố toàn bộ warning bằng0.', '',
'## Hiệu năng / pool', '',
f'Bản PC Mono release **{perf["width"]}×{perf["height"]}**, {perf["quality"]}/{perf["pipeline"]}, GPU {perf["gpu"]}, CPU {perf["cpu"]}. PostProcessing/Bloom/HDR/ComicInk đều true, Bloom{perf["bloomIntensity"]:.2f}. Camera người chơi/bảy quái thật, không capture/MCP trong sampling. Median240idle và3cast/chiêu; Flash idle lấy cả view đầu/cuối theo số frame tương ứng. [JSON native mới](../../Artifacts/Skills/fix2/SkillSet1-Performance-Player.json), DONE=PASS. [Ảnh render PC sau đo](../../Artifacts/Skills/fix2/P10-Player-render.png).', '',
'Peak là max của Main/Pool/Visual/Player, không chọn riêng lượt nhẹ nhất. GC chỉ vòng VFX LateUpdate, không phải cả game.', '',
'| Chiêu | IdleFPS | CastFPS | GiảmFPS | Peak PC/mobile | Objects10cast | GC/frame |', '|---|---:|---:|---:|---:|---|---:|']
for x in perf['trials']:
    key=x['skill']; m=metrics[key]; a=measurements[key]; v=visuals[key]
    pc=max(m['peakPC'],a['peakPC'],v['peakPC'],x['peakParticles']); mobile=max(m['peakMobile'],a['peakMobile'],v['peakMobile'])
    assert pc<=1500 and mobile<=400 and m['before']==m['after'] and m['maxFrameGCBytes']==0 and x['maxVfxFrameGCBytes']==0 and m['exhausted']==0
    lines.append(f'| {names[key]} | {x["baselineFps"]:.1f} | {x["castFps"]:.1f} | {x["lossPercent"]:.2f}% | {pc}/{mobile} | {m["before"]}→{m["after"]} |0B|')
lines += ['', f'Giảm FPS lớn nhất **{max(x["lossPercent"] for x in perf["trials"]):.2f}%**, nằm trong10%. Pool prewarm112nodes+30FlyingSword; không Instantiate/Destroy sau warmup. Mobile giảm emission/gai/shard/trail và tắt refraction, giữ gameplay. Chưa đo FPS trên máy Android vật lý; ảnh mobile là cast thật dùng mức VFX mobile trong Unity.', '',
'## Ảnh và tự đánh giá', '',
'| Chiêu | Sáng8frame | Tối8frame | Impact gần | Mobile |', '|---|---|---|---|---|']
for key,name in names.items(): lines.append(f'| {name} | [sheet](screens/fix2/{key}-sheet-light.png) | [sheet](screens/fix2/{key}-sheet-dark.png) | [impact](screens/fix2/{key}-impact.png) | [ảnh](screens/fix2/{key}-mobile.png) |')
lines += ['', 'Bản impact-light/dark giữ đầy đủ HUD, crop impact chỉ cắt khung không sửa màu;112frame gốc ở screens/fix2/frames/. [Kim Chung hết6s tự nhiên](screens/fix2/kim-chung-trao-expiry.png). Checklist có/không theo từng chiêu và frame nằm trong [PROGRESS-fix2.md](PROGRESS-fix2.md).', '',
'Local hold65ms và impulse: Tích Lịch0,75; Hỏa1,0; Băng0,55; Lôi0,45; Chuông0,6; Hắc Động0,9; Vạn Kiếm0,5. Ground residue1,8–2,8s rồi fade; field lửa4s là gameplay riêng. Các timing/damage/range/CD/spirit/30kiếm/tầng giữ nguyên.', '',
'## Tài nguyên / tái kiểm', '',
'Các file chính: `SkillVfxPool.cs`/`SkillVfxMeshes.cs`/`SkillSet1VfxConfig.cs` và installer tạo mesh/material mới trong pool; `P10Accretion.shader`, `P10FireRibbon.shader`, `P10LayeredStroke.shader`, `P10Flipbook.shader`, `P10Surface.shader` xử lý đĩa xoáy, flame scroll, aura, sương và màu kiếm. `BlackHoleRuntime`, `SwordRainRuntime`/`FlyingSword`, `FireLotusRuntime`, `IceSealRuntime`, `ChainLightningRuntime`, `GoldenBellRuntime` đổi phần visual. `SkillVisualCapture` và `SkillSet1EdgePlayTest` đổi fixture/timeline và kiểm khôi phục state/camera. Prefab/config/scene được cài bằng installer; danh sách build game chính vẫn MainMenu+SampleScene.', '',
'[LICENSES.md](../../Assets/Skills/Core/LICENSES.md) ghi nguồn/giấy phép. Đĩa accretion, panel chuông, shader scroll lửa, aura điện và sương là code/mesh procedural trong project; tái dùng atlas lửa/khói P10, âm thanh/texture KenneyCC0, FlyingSwordP03, afterimage/cameraImpulse/comicInk hiện có. Bảy icon512alpha/GUID giữ nguyên; không AssetStore/tài nguyên trả phí/login.', '',
'Chạy lại: `python Tools/p10_finish_tests.py`; `python Tools/p10_regressions.py`. Capture: Play → `python Tools/ui_command.py code Tools/p10_batch_start.cs` → chờ DONE mới → `python Tools/p10_sheets.py task/p10/screens/fix2`. PC: Stop/Windows64 → p10_build_bench.cs, p10_build_invoke.cs nếu callback chưa chạy → executable1920×1080 → DONE mới; sau đó restoreAndroid. Report writer có guard freshness/kết quả/quality và chỉ chạy sau khi tự soi hình.']
Path('task/p10/REPORT-P10-fix2.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Wrote REPORT-P10-fix2.md; fresh evidence frozen in Artifacts/Skills/fix2.')
