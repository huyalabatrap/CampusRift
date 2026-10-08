from pathlib import Path
p=Path('task/ar/PROGRESS.md');s=p.read_text(encoding='utf-8-sig')
lines=s.splitlines()
for i,line in enumerate(lines):
    if line.startswith('- **AR fix3 đang làm'):
        lines[i]='- **AR fix3 đang hoàn tất (05/10):** unit 27/0, AR 25/0 và HubFlow 42/0, mỗi harness đúng 1 lượt; drift 0 cm, HUD 3,44% (combo 5,16%). Full text audit 1 issue đã sửa, kiểm hẹp 0. Timing controlled 3,38s/1,59s chưa đạt mục tiêu, fixture 256 rays; raw 10 rays giữ. Build1 PASS + DEX ByteBuffer/model STORED/sensorLandscape, giữ `build-fix3/attempt1`. Đã sửa visualizer tự bật plane và controller ở scale nhỏ; đang kiểm hẹp, ảnh cuối và build2, sau đó restore/final/report. Không chạy lại harness/full audit.'
        break
s='\n'.join(lines)+'\n'
s=s.replace('## AR fix3 — normal smoke hoàn tất\n- HubFlow chạy1lượt: 42 passed; 0 failed. Evidence fix2/HubFlow; Artifacts/UI gốc phục hồi. ShutdownXR trước Stop; Editor SampleScene. Tiếp build fix2, chưaREPORT.', '## AR fix3 — normal smoke hoàn tất\n- HubFlow chạy 1 lượt: 42 passed; 0 failed. Evidence fix3/HubFlow; Artifacts/UI gốc phục hồi. Shutdown XR trước Stop; Editor SampleScene. Tiếp build fix3, chưa REPORT.')
s+='\n## AR fix3 — scale nhỏ / sửa hẹp\n- Kiểm visual cuối phát hiện Step Offset của CharacterController legacy khi caster co theo bàn nhỏ. Đã tạo controller ở scale 1, đặt stepOffset=0 và tắt trước khi parent vào AR root. Không sửa code game thường. Evidence lỗi giữ visual-fix-console-initial.json; cần kiểm nhỏ và build2.\n'
p.write_text(s,encoding='utf-8')
