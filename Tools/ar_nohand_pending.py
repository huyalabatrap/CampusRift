from ar_nohand import *
serial='WGH6S8I7GIMBGQKR'
args=[str(ADB),'-s',serial]
window=subprocess.check_output(args+['shell','dumpsys','window'],text=True,encoding='utf-8',errors='replace')
focus=[l.strip() for l in window.splitlines() if 'mCurrentFocus=' in l or 'mFocusedApp=' in l]
trust=subprocess.check_output(args+['shell','dumpsys','trust'],text=True,encoding='utf-8',errors='replace')
locked='deviceLocked=1' in trust
save('pending-device.json',dict(at=time.strftime('%Y-%m-%d %H:%M:%S'),serial=serial,focus=focus,deviceLocked=locked,installSession=7194,installCommand='python Tools/ar_nohand_device_install_final.py',finalApk='APK-Test/CampusRift-20261007-ar-nohand-fix.apk',finalSha256='20a4ea1ab3c84dccfe01bc830eaac0c15fc3768cb791834be6254041465418ee',done=['source fixes','single mock path','ARGestureUnitTests once 55/0','ARRiftPlayTest once 25/0','final build2/package verification','Editor/save/settings restore','four supplemental log findings + Shader.Find audit'],remaining=['final device install Success','navigate AR and confirm native ready=True','write final REPORT-AR-NOHAND.md only after complete'],reportDraft='task/ar/nohand/report-draft.md'))
device_log('nohand-realme-pending.txt')
milestone('Bàn giao chờ thao tác máy: nguồn/APK cuối/kiểm55-0+25-0/mock1-1/audit bổ sung/phục hồi Editor đều xong; Realme vẫn deviceLocked=1 và InstallGuide, chưa Success/ready APK cuối. Không viết REPORT-AR-NOHAND.md. Bản nháp đầy đủ sáu nghi vấn/bốn điểm tại nohand/report-draft.md; nohand/pending-device.json giữ session7194. Tiếp quản chỉ poll install, sau unlock vào AR lấy ready=True native rồi viết final; không build hoặc rerun hai suite. Các mốc restoration ghi đủ điều kiện report trước đây chỉ nói phần Editor, không xóa yêu cầu device-ready.')
print(json.dumps(dict(locked=locked,focus=focus),ensure_ascii=True))
