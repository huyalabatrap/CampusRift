from pathlib import Path
import json, hashlib
out=Path('task/ar/fix3');read=lambda p:json.loads(Path(p).read_text(encoding='utf-8-sig'))
v=read('task/ar/build-fix3/verification.json');state=read(out/'final-state.json')
assert hashlib.sha256(Path(v['apk']).read_bytes()).hexdigest()==v['sha256']
assert state['target']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and not state['playing'] and not state['dirty']
assert read(out/'final-console.json')['data']==[]
assert len(read(out/'gesture-unit-tests.json')['passed'])==27
assert len(read(out/'ar-results.json')['passed'])==25
assert len(read(out/'HubFlow/HubFlow.json')['passed'])==42
assert all(x['identical'] for x in read(out/'protected.json')+read(out/'orchestrators.json'))
report=Path('task/ar/REPORT-AR-fix3.md');body=report.read_text(encoding='utf-8')
assert v['sha256'] in body and 'chưa đạt' in body
for size in ['2400x1080','1600x720']:
    for phase in ['placement','battle','menu','guide','combo']:
        assert Path('task/ar/screens/fix3/'+phase+'-'+size+'.png').exists()
p=Path('task/ar/PROGRESS.md');s=p.read_text(encoding='utf-8-sig');lines=s.splitlines()
for i,line in enumerate(lines):
    if line.startswith('- **AR fix3 đang'):
        lines[i]='- **AR fix3 HOÀN TẤT (05/10):** `REPORT-AR-fix3.md`, APK fix3 548.558.335 byte / SHA256 '+v['sha256']+'; build2 PASS, DEX ByteBuffer/model STORED/sensorLandscape. Unit27/0, AR25/0, Hub42/0; drift0m; HUD3,44%/combo5,16%; audit raw1issue đã sửa/kiểm hẹp0. Timing3,38s/1,59s chưa đạt mục tiêu, máy thật chưa có adb. Save/settings/scan10rays và prefilter phục hồi; 38protected+4điều phối nguyên hash. Unity Android/Edit/SampleScene/Console0, shader0. Xem report và DEVICE-TEST cho phần cần kiểm trên máy.'
        break
s='\n'.join(lines)+'\n'
s+='\n## AR fix3 — HOÀN TẤT / bàn giao\n- APK cuối build2 thành công 54,21s/0errors/58warnings; SHA256 '+v['sha256']+'. Verification xác nhận model STORED, sensorLandscape, ByteBuffer JNI mới. Build1 giữ attempt1 vì các sửa cuối có source đổi.\n- Unit27/0, AR25/0, Hub42/0 mỗi suite1lượt; các kiểm hẹp visual/scale/cleanup đạt. Timing controlled chưa đạt mục tiêu được báo trung thực; EditorMOCK không phải hiệu năng MediaPipe. ADB không có máy, bỏ device theo brief.\n- 7save + settings/PlayerPrefs/Editor/Input/scan10rays/prefilter phục hồi; 38protected +4điều phối nguyên hash. Finalstate Android/Edit/SampleScene/khôngdirty/loaderinactive/Console0/shader0. DEVICE-TEST cập nhật. REPORT-AR-fix3.md chỉ ghi sau khi build/verify/restore/final hoàn tất.\n'
p.write_text(s,encoding='utf-8')
print('Fix3 complete; final report and progress verified.')
