from ar_nohand2 import *
import zipfile
serial='WGH6S8I7GIMBGQKR';args=[str(ADB),'-s',serial];apk=ROOT/'APK-Test/CampusRift-20261007-ar-nohand-fix2.apk'
devices=subprocess.check_output([str(ADB),'devices'],text=True,encoding='utf-8');save('build/device-before.json',dict(devices=devices,at=time.strftime('%Y-%m-%d %H:%M:%S')))
if serial+'\tdevice' not in devices:
    save('build/device-install.json',dict(connected=False,skipped='No specified Realme connected'))
    milestone('Device conditional: Realme không cắm tại thời điểm cài; lưu adb devices, bỏ install theo brief.')
    sys.exit(0)
assert (OUT/'build/verification.json').exists()
stamp=time.time();save('build/install-invoked.json',dict(at=stamp,serial=serial,apk=apk.as_posix(),sha256=hashlib.file_digest(apk.open('rb'),'sha256').hexdigest(),command='adb install -r'))
p=subprocess.run(args+['install','-r',str(apk)],capture_output=True,text=True,encoding='utf-8',errors='replace')
(OUT/'build/adb-install.txt').write_text(p.stdout+p.stderr,encoding='utf-8')
listing=subprocess.run(args+['shell','pm','list','packages'],capture_output=True,text=True,encoding='utf-8',errors='replace')
campus='\n'.join(l for l in listing.stdout.splitlines() if 'campus' in l.lower())
(OUT/'build/pm-list-campus.txt').write_text(campus+'\n',encoding='utf-8')
path=subprocess.run(args+['shell','pm','path','com.campusrift.game'],capture_output=True,text=True,encoding='utf-8',errors='replace')
(OUT/'build/pm-path.txt').write_text(path.stdout+path.stderr,encoding='utf-8')
save('build/device-install.json',dict(connected=True,serial=serial,installExit=p.returncode,installOutput=p.stdout+p.stderr,pmCampus=campus,pmPath=path.stdout,verified=p.returncode==0 and 'Success' in p.stdout and 'package:com.campusrift.game' in campus and 'package:' in path.stdout))
assert p.returncode==0 and 'Success' in p.stdout and 'package:com.campusrift.game' in campus and 'package:' in path.stdout,(p.returncode,p.stdout,p.stderr,campus,path.stdout)
milestone('Realme '+serial+': adb install -r APK fix2 Success; đã kiểm pm list packages lọc campus có package:com.campusrift.game và pm path có base.apk. Evidence nohand2/build/device-install.json; không dùng riêng chữ Success để kết luận.')
print('Install Success; package listing and path verified',flush=True)
