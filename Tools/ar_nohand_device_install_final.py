from ar_nohand import *
args=[str(ADB),'-s','WGH6S8I7GIMBGQKR']
assert (OUT/'build/DONE.txt').read_text(encoding='utf-8').strip()=='Succeeded'
assert (OUT/'build/verification.json').exists()
p=subprocess.run(args+['install','--no-streaming','-r','APK-Test/CampusRift-20261007-ar-nohand-fix.apk'],capture_output=True,text=True,encoding='utf-8',errors='replace')
(OUT/'build/adb-install-final-nonstreaming.txt').write_text(p.stdout+p.stderr,encoding='utf-8');print(p.stdout+p.stderr,flush=True)
if p.returncode:
 diag=subprocess.run(args+['shell','pm','list','packages','com.campusrift.game'],capture_output=True,text=True,encoding='utf-8',errors='replace');save('device-availability-final.json',dict(installCode=p.returncode,packages=diag.stdout,stderr=diag.stderr));exit(1)
print(subprocess.check_output(args+['shell','input','keyevent','224'],text=True,encoding='utf-8',errors='replace'))
print(subprocess.check_output(args+['shell','am','start','-n','com.campusrift.game/com.unity3d.player.UnityPlayerGameActivity'],text=True,encoding='utf-8',errors='replace'))
milestone('APK cuối cài --no-streaming -r Success (sau first app bị gỡ/stream retry fail); launch app để xác nhận ready. Không rebuild/rerun tests thêm, không quay/ghi camera.')
