import re
from pathlib import Path
from stabilize_handoff import ROOT, OUT, read, sha

assert read(OUT/'build.json')['result']=='succeeded'
assert read(OUT/'final-state.json')['pass']
assert read(OUT/'final-disk-audit.json')['pass']
assert read(OUT/'shelter-baseline-comparison.json')['pass']
assert read(OUT/'polish-audits.json')['pass']
models=read(OUT/'runs/ModelsPolish/result.json')
polish=read(ROOT/'task/stabilize/polish-after.json')
assert len(models['passed'])==30 and not models['failed']
assert len(polish['passed'])==13 and not polish['failed']
build=read(OUT/'build.json')
build['exe_sha256']=sha(ROOT/build['output_path'])
(OUT/'build.json').write_text(__import__('json').dumps(build,ensure_ascii=False,indent=2),encoding='utf-8')

reg=(ROOT/'Artifacts/V2/Regression-Stabilize.md').read_text(encoding='utf-8')
table=reg[reg.index('| Suite |'):reg.index('\n\nKhông có FAIL mới')]
table=table.replace('(../../task/','(../').replace('(Stabilize/','(../../Artifacts/V2/Stabilize/').replace('(Regression-MVP.md)','(../../Artifacts/V2/Regression-MVP.md)')
items=[
 ('Thiên Kiếm / campus + impact','heaven-sword-campus-impact','Camera impact(70,64,−140) giữ campus ở đáy khung; gather không nhìn lên quá cao. Bloom0,6→0,32, flash0,22→0,08; ring/shard vàng bão hòa, giảm bạc màu.'),
 ('Kiếm Ý chờ đợt','intent-waiting','Thay0%·0/0 bằng “KIẾM Ý · CHỜ ĐỢT QUÁI” khi chưa có target, vẫn hiển thị tiến độ thật khi wave bắt đầu.'),
 ('Đổi kỹ năng mobile / cuộn','rest-loadout-scroll','Viewport đúng hai hàng; bỏ hai nhãn hàng3 bị cắt (partial-scroll2→0). Thêm cue bấm/vuốt và rail/thumb; smoke cuộn tới hàng cuối.'),
 ('Hỏa Tâm sát thân','elite-fire-aura','24particle tối đa, alpha0,28, size0,06–0,12, local simulation; bounds theo animation, hẹp sát thân, maxscreen2,5%. Outline mảnh0,012 và footring theo capsule. Smoke particle nằm trong1,2×body; miễn nhiễm không đổi.'),
 ('Bảng tên tinh anh','elite-nameplate','Hai dòng tên/phụ tố, icon0,42 và nền comic. Hiện đủ khi ≤18m hoặc lock. Chữ thực đo24,99px, không truncate; kiểm far-hide/lock-show thật.'),
 ('Dực Yêu cỡ người','bat-human-scale','Visual scale1→0,8, wingspan3,70→2,96m. Root/hitbox radius0,35, độ cao bay3–6m, warningR2/0,6s và dodge giữ nguyên.'),
 ('Ảnh Yêu có trang phục / idle','shadow-assassin-detail','Cloak rách, hood, khăn, dao, tunic và sash weighted; texture vải tự tạo128px. Idle hạ tay khỏi A pose. Giữ FBX, source UV và clip khác; conceal/reveal, teleport0,5s, hit/death/dissolve/pool đều đạt.'),
 ('Tên kỹ năng dài','long-skill-name','“Thần Kiếm Ngự Lôi Chân Quyết” wrap hai dòng, font21 cố định cùng slot khác; listfont22, không autosize nhỏ riêng tên dài.'),
]
polish_table='| Mục | Đã làm | Ảnh trước / sau |\n|---|---|---|\n'+''.join(f'|{title}|{detail}|[trước](screens/{key}-before.png) · [sau](screens/{key}-after.png)|\n' for title,key,detail in items)

text='''# REPORT-STABILIZE · hoàn tất04/10/2026

Đã xử lý17suite FAIL P17: **16suite PASS, Shelter giữ40mismatch baseline P13 và0mismatch mới**; PhantomDecoy PASS. Hoàn tất cả8mục polish. Windows development build Succeeded/0error. Unity cuối **Android · Edit Mode · SampleScene sạch · Console0error/0warning**. Không còn việc bắt buộc của brief chưa xử lý.

Đã đọc toàn bộ brief, TEST-POLICY và các báo cáoP17/P18/P19/AI-SQUADfix1/MODELS cùng polish-notes; tiếp tục theo trạng thái disk, không có reportSTABILIZE hoàn tất trước đó. Backup trước sửa ở [BACKUP](BACKUP.txt), [manifestSHA256](pre-manifest.json). [PROGRESS](PROGRESS.md) ghi các milestone và mọi lượt smoke. Giữ nguyên raw P17; đã phục hồi783tệp evidence lịch sử sau khi cách ly kết quả mới. Các lượt đầu FAIL/timeout khi hiệu chỉnh fixture vẫn giữ trong `Artifacts/V2/Stabilize/diagnostics/` và `runs/*-first`; bảng dưới lấy lượt cuối.

## Từng FAIL và PhantomDecoy

[Bảng trước/sau độc lập](../../Artifacts/V2/Regression-Stabilize.md). Lỗi game thật được sửa là đồng hồ movement dash khi frame chậm; notch MobileControls đã được P17 sửa. Các fixture được cập nhật đúng thiết kế hiện hành, giữ assertion chức năng và ngưỡng traversal. Không chứng minh Lightning xuyên tường hoặc Phantom có bug traversal: đo phiên sạch dừng tường đúng và phân thân vượt cửa/stair đúng. Không tuyên bố40điểm Shelter đã hết sai.

'''+table+'''

## Từng mục polish

'''+polish_table+'''
Tất cả8ảnh trước/sau là framebuffer1920×1080 từ prefab/runtime/luồng game thật, đã tự soi. Mỗi ảnh có `*-text-audit.json` cùng tên. [Audit sau tổng hợp](../../Artifacts/V2/Stabilize/polish-audits.json):0issue; ComicTextAudit kiểm text canvas, worldTMP bảng tên kiểm riêng ở [ModelsPolish30/0](../../Artifacts/V2/Stabilize/runs/ModelsPolish/result.json). [PolishAfter13/0](polish-after.json) đi waveL9→ThiênKiếm→Rest thật, kiểm cuộn và chờ đợt. Ảnh model dùng AI dừng để chụp, không phải kết quả cân bằng.

Ảnh Yêu thêm757triangle dùng lại cả baLOD: tổng3705/2231/1493tri; không vertex thiếu weight. Sáu phụ kiện bind lên rig có sẵn, rootBone/localBounds cùng hệ tọa độ; không đổi gameplay component/GUID. Mesh/texture mới tự tạo bằng Unity, không tải thêm: [license/tái tạo](../../Assets/Enemies/Models/Stabilize/LICENSES.md). Night Demon vẫn ghi công Morgan Strauss(nubux), CC-BY3; clip idle dẫn xuất giữ provenance. Không sửa Mage/Golem,7model người dùng hoặc pipeline Blender nguồn. Đây là polish nhỏ trên model miễn phí/thủ tục, chưa phải clothing simulation hoặc animation chỉnh tay hoàn chỉnh.

## Build, khôi phục và bàn giao

- [Windows Development](../../Builds/STABILIZE-Windows-Development/Campus%20Rift.exe): Mono, MainMenu+SampleScene,164,7s,858,67MB, **Succeeded/0error/45warning**. [Buildmetadata+SHA256](../../Artifacts/V2/Stabilize/build.json), [console đã lưu](../../Artifacts/V2/Stabilize/build-console.json). Warning APIobsolete, TMP debug pragma và1269mesh collider thiếu pre-bake là giới hạn hiện có; không sửa geometry để dọn warning. Không auto-run/native benchmark.
- [Disk audit](../../Artifacts/V2/Stabilize/final-disk-audit.json):276tệp structural/source/settings bảo vệ nguyên byte, gồm150GameReadyModels; scene/NavMesh/authoredLevelData không đổi. Hai prefab giữ nguyên23block gameplay vàGUID.783evidence lịch sử,5save user nguyên SHA256; fontTMP authored được khôi phục sau capture. PlayerPrefsSettings khôi phục nguyên văn trước lượt persist.
- [Trạng thái cuối](../../Artifacts/V2/Stabilize/final-state.json):Android/Edit/SampleScene khôngdirty, khôngcompile/update; input0/0, enterPlay options gốc, developmentfalse. Trước clear không có error; lưu warning build rồi clear và đọc lại Console0error/0warning. README có hàngSTABILIZE, polish-notes đánh dấu xong8mục.

Giới hạn theo [TEST-POLICY](../TEST-POLICY.md): không chạy lại toàn bộ71suite, không benchmark/FPS/50trial, chưa test thiết bịAndroid hoặc cân bằng thực chiến. Mốc5P17 vẫn chờ người chơi/giảng viên/Android thật. Shelter40điểm baseline và mức chi tiết model tiếp tục được nêu rõ để người điều phối/người dùng đánh giá; không che FAIL bằng cách ghi0 hoặc xóa raw.
'''
# Percent escapes are unsuitable for local filesystem links in some renderers.
text=text.replace('(../../Builds/STABILIZE-Windows-Development/Campus%20Rift.exe)','(<../../Builds/STABILIZE-Windows-Development/Campus Rift.exe>)')
report=ROOT/'task/stabilize/REPORT-STABILIZE.md'
assert not report.exists(), 'Final report already exists; review before replacing it'
report.write_text(text,encoding='utf-8')
print(f'Wrote completed report: {report}')
