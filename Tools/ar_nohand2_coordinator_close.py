from ar_nohand2 import *
progress=(ROOT/'task/ar/PROGRESS.md').read_text(encoding='utf-8')
line=next(l for l in progress.splitlines() if l.startswith('## ĐIỀU PHỐI VIÊN 21:44:'))
assert 'tự cài APK fix2 lên Realme bằng adb và kiểm gói' in line and 'viết ngay REPORT-AR-NOHAND2.md' in line
args=[str(ADB),'-s','WGH6S8I7GIMBGQKR']
devices=subprocess.check_output([str(ADB),'devices'],text=True,encoding='utf-8')
listing=subprocess.run(args+['shell','pm','list','packages'],capture_output=True,text=True,encoding='utf-8',errors='replace')
campus='\n'.join(l for l in listing.stdout.splitlines() if 'campus' in l.lower())
path=subprocess.run(args+['shell','pm','path','com.campusrift.game'],capture_output=True,text=True,encoding='utf-8',errors='replace')
(OUT/'build/pm-list-campus.txt').write_text(campus+'\n',encoding='utf-8')
(OUT/'build/pm-path.txt').write_text(path.stdout+path.stderr,encoding='utf-8')
record=dict(connected='WGH6S8I7GIMBGQKR\tdevice' in devices,serial='WGH6S8I7GIMBGQKR',coordinatorHandoff='ĐIỀU PHỐI VIÊN 21:44',coordinatorText=line,installationConfirmedByCoordinator=True,packageCheckConfirmedByCoordinator=True,currentPackagePresent='package:com.campusrift.game' in campus,pmCampus=campus,pmListExit=listing.returncode,pmPath=path.stdout,pmPathExit=path.returncode,observedAt=time.strftime('%Y-%m-%d %H:%M:%S'),verified=False,installedApkHashVerified=False)
save('build/device-install.json',record);save('build/device-handoff.json',record)
# Validate every saved final source rather than rerunning the suites or rebuilding.
sources=json.loads((OUT/'sources.json').read_text(encoding='utf-8'))
assert all(hashlib.sha256((ROOT/r['path']).read_bytes()).hexdigest()==r['afterSha256'] for r in sources)
assert json.loads((OUT/'state-traces.json').read_text(encoding='utf-8'))['assertions']=='PASS'
assert json.loads((OUT/'startup-deadline.json').read_text(encoding='utf-8'))['assertions']=='PASS'
milestone('Tiếp quản lại đã đọc toàn bộ brief/findings/report và PROGRESS: mốc điều phối21:44 xác nhận cài fix2/kiểm gói, yêu cầu đóng ngay, không chờ unlock. Lượt session83536 không còn truy cập được; không lặp install. Recheck nguồn9file khớp sourcehash, trace/deadline PASS lưu sẵn, không test/build lại. ADB hiện campus/path rỗng lưu provenance riêng trong device-handoff.json; không tạo Success hoặc hash installed giả. Tiếp đối soát Editor/save/settings/hash APK và viết REPORT-AR-NOHAND2 theo handoff.')
print('Coordinator handoff recorded; source/evidence unchanged')
