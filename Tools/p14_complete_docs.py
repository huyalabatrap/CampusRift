"""Write the completion report only after the smoke, image and Editor gates are satisfied."""
from pathlib import Path
import json
base=Path('task/p14')
state=json.loads((base/'final-state.json').read_text(encoding='utf-8-sig'))
assert state['target']=='Android' and not state['play'] and not state['compiling'] and not state['dirty'] and state['consoleErrors']==0 and state['fixtureCount']==0 and state['scene']=='Assets/Scenes/SampleScene.unity',state
verify=json.loads((base/'verification-summary.json').read_text())
assert verify['sky']['failed']==verify['fire']['failed']==0 and verify['dev']['pass'] and all(x['issues']==0 for x in verify['audits'])
assert all(x['sha256Matches'] for x in verify['audio'])
for entry in verify['preservation'].values():
 assert entry.get('sameAsBackup',True) and entry.get('sameAsP13',True) and not entry.get('changed',[]),entry
report='''# P14 — Hoàn tất theo smoke · 03/10/2026

**SkyBeast 58 PASS / 0 FAIL; FireBreath 42 PASS / 0 FAIL; DEV màn 8/9/10 PASS.** Unity cuối **Android · Edit Mode · SampleScene · không dirty · 0 lỗi Console/compile/shader · 0 fixture**. [Gate tổng hợp](verification-summary.json), [Editor](final-state.json), [PROGRESS](PROGRESS.md).

| Task | Kết quả |
|---|---|
| T01 | Tái dùng 3 model/LOD/socket/animation P12: 023 = Giao, 026 = Chu Tước, 020 = Long Vương. Giữ model người dùng và LICENSES; không tải model mới. |
| T02 | 3 definition combat riêng trong Resources/P14, tên VN/EN, đường bay, khúc và danh sách pha tham chiếu profile P13. Màn 8: 1 khúc; màn 9: 2; màn 10: mỗi con 1. Validation đạt. |
| T03 | Warning tăng độ cao 18 m và tụ lửa họng; Breath đúng pose/socket/cone; hồi phục theo clip thật. Gầm ngẫu nhiên nhường pose phun. Vitality chỉ nhận Thiên Kiếm, layer10 không va chạm Player. |
| T04 | ComicTheme, tên/khúc/pha; tối đa 2 thanh, vùng Kiếm Ý bắt đầu 122 px từ mép trên. [Màn 9](screens/level9-segments.png), [hai thanh mobile](screens/level10-two-bars-mobile.png). |
| T05 | Ngoài trời, đếm 8 s trong khoảng an toàn giữa các lần phun; 3 lông, pha2/cuồng nộ 5. Báo1,2 s, 12% máu đề nghị, vệt Dư Hỏa bán kính1 m. [Telegraph](screens/feathers-telegraph.png), [impact](screens/feathers-impact.png). |
| T06 | 6 thiên thạch mỗi15 s, báo1,5 s, 15% máu đề nghị, bán kính3 m; mặt rắn trên cùng chặn xuyên mái. [Telegraph](screens/meteors-telegraph.png), [impact](screens/meteors-impact.png). Wave3 clear gọi Long Nộ: [báo5 s](screens/long-no-warning.png), [phun12 s](screens/long-no.png), khóa kiếm cuối đến khi hết. Hỏa Linh thuộc P19. |
| T07 | Scheduler màn8/9 một con. Màn10: Giao+Chu Tước xen kẽ20 s, mỗi con40 s; nhát1 → Chu Tước28 s/5 lông; nhát2 → Long Vương25 s/báo4 s/meteor; Long Nộ → nhát3. Chỉ1 nguồn phun/pose/cone. Giữ thắng bằng HoldWin. |
| T08 | [SkyBeast smoke](SkyBeast.json) 58/58, [FireBreath](FireBreath.json) 42/42, [DEV menu/spawn thật](dev-smoke.json) đạt. [Warning rồng vọt lên](screens/warning-dragon-climb.png). |

Giữ hiệu chỉnh cao độ an toàn P12: Giao/Chu Tước base110 m, Long Vương120 m và hạ10 m ở pha3; low pass−28 m. Không hạ về90/100 m của bản thiết kế cũ để giữ khoảng hở campus. Tâm lấy bounds RoomGraph, bán kính105 m đã kiểm. Chu Tước dùng hình số8; các mẫu đường bay đạt cao độ/clearance/tầm350 m. Màn10 pha1 có Rest0 s, vì vậy feather dùng cả Afterfire và Rest; không đổi profile fire.

Pool đòn phụ6 slot trên mọi nền tảng; cosmetic mobile giảm50%, cap hạt128/96/64 cho High/Low/Mobile, không giảm số điểm gây damage. Giữ nguyên ngân sách PERF-FIRE72/48/36 meteor cosmetic và cone P13. Telegraph lõi sáng/viền tối, thân xoay/trail, impact/scorch3 s và camera0,045/0,10; dùng SkillImpact55 ms/speedlines và ReduceSkillFlashes. **9 ảnh Unity thật đã tự soi, 9 TextAudit0issue**. Capture giữ AI/wave, HP10000 và thời điểm hiệu ứng; không làm bằng chứng cân bằng.

**FPS:147,39** — mẫu duy nhất443 frame/3,0057 s, Editor targetAndroid/MobileRP1920×1080, pha3 có6 meteor+phun, VSync/cap tắt. Đo trước polish HUD/kích thước/sky cuối; chưa đo lại. [Raw đầu](capture-first.json), [metadata ảnh cuối](capture.json). Không so trực tiếp với PERF-FIRE PC target/quality khác, không coi là FPS Android thật.

## API bàn giao P15

- `SkyBeastScheduler.Instance`: `Level`, `Phase`, `Beasts`, `BreathSource`, `SwordTarget`, `SwordAllowed`, `Completed`, event `Changed`.
- `scheduler.ApplySkySwordHit()` hoặc `SwordTarget.ApplySkySwordHit()` trả bool, đúng1 khúc. `IDamageable.ApplyDamage()` chỉ nhận `DamageSource.Skill`, `skillId="thien-kiem"`, amount>0. Đòn khác bỏ qua.
- `SkyBeastVitality`: `TotalSegments`, `RemainingSegments`, `IsDead`, events `Changed`/`Defeated`.
- `scheduler.Fury`: `Request()`, `Warning`, `Active`, `Completed`, `Remaining`. Wave3 clear tự nối; sword cuối bị khóa trước/khi Long Nộ. API StartFury P13 giữ1lần/run.
- P15 sở hữu Kiếm Ý, điều kiện ngoài trời, niệm/cinematic. Dùng target/gate trên khi gây hit; P14 chưa tạo nút Thiên Kiếm gameplay. DEV trong Play: **Campus Rift/DEV/P14/Level8/9/10**, Apply Sky Sword Hit, Feather barrage, Meteor shower, Long No warning.

## Nguồn, bảo toàn và giới hạn

Vỗ cánh: AntumDeluge, [Large Wings Flap](https://opengameart.org/content/large-wings-flap); âm nhẹ/va chạm: Kenney, [Impact Sounds](https://kenney.nl/assets/impact-sounds), đều **CC0 1.0**. [LICENSES](../../Assets/SkyBeast/LICENSES.md), [URL/archive/SHA256](source/manifest.json). Feather/rock mesh, shader và UI tự viết; dùng lại particle/scorch P13. Model vẫn là tài nguyên người dùng.

Backup trước sửa tại đường dẫn trong [BACKUP](BACKUP.txt), có bổ sung shaderSky trước sửa tint Long Nộ. [Hash](preservation.json): Scene/TagManager/EditorSettings, prefab/data P12, Resources P13 và LevelsData giữ nguyên; FBX/NavMesh khớp P13. Không rebuild NavMesh hoặc sửa geometry. Warning compiler API obsolete/unused đã lưu [ở đây](final-warnings.json), đã review và clear Console; không có lỗi. Sửa thực tế biên Rest0 s có raw đầu giữ trong PROGRESS.

Theo [TEST-POLICY](../TEST-POLICY.md), không full regression, native build, benchmark nhiều lượt hoặc Android thật. **Cân bằng chưa đo thực chiến**; thời lượng màn và trải nghiệm Thiên Kiếm hoàn chỉnh chờ P15/người dùng chơi thử. Smoke core chạy trước polish visual cuối; các chỉnh visual đã kiểm bằng ảnh/compile/Console.
'''
(base/'REPORT-P14.md').write_text(report,encoding='utf-8')
spec=Path('task/P14-cu-thu-bau-troi.md');s=spec.read_text(encoding='utf-8-sig').replace('⬜','✅ smoke').replace('- [ ]','- [x]');s=s.replace('# P14 — Cự thú bầu trời','# P14 — Cự thú bầu trời\n\n> **03/10/2026: ✅ smoke T01–T08.** Xem [REPORT-P14](p14/REPORT-P14.md); kiểm chứng gọn theo TEST-POLICY, chưa đo cân bằng/Android thật.',1);spec.write_text(s,encoding='utf-8')
readme=Path('task/README.md');s=readme.read_text(encoding='utf-8-sig');s=s.replace('| [P14](P14-cu-thu-bau-troi.md) | Cự thú bầu trời | 4 | P13 | | ⬜ |','| [P14](P14-cu-thu-bau-troi.md) | Cự thú bầu trời | 4 | P13 | [Báo cáo](p14/REPORT-P14.md) · SkyBeast58/58 · FireBreath42/42 · DEV8–10 | ✅ smoke |');readme.write_text(s,encoding='utf-8')
with (base/'PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n## Mốc 7 — hoàn tất\n- T01–T08 hoàn tất theo smoke; REPORT-P14.md chỉ được ghi sau gate smoke/ảnh/hash/Editor đạt. Đã cập nhật checkbox đặc tả và README thành ✅ smoke.\n- Unity cuối Android, Edit Mode, SampleScene sạch, 0 lỗi/fixture; Console đã review warning obsolete/unused và clear, raw final-warnings.json giữ lại.\n- SkyBeast 58/0, FireBreath 42/0, DEV8/9/10 PASS, 9 audit0issue; ảnh cuối đã tự soi. FPS duy nhất147,39 trước polish, chưa đo thực chiến/native/Android thật.\n- Không còn việc P14 trong brief; P15 nhận API target/segments/sword gate/Fury trong REPORT.\n')
print('P14 report/checklist/README written after passing completion gates.')
