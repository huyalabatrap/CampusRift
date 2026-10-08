"""Reuse Job 7's APK inspection with an isolated output directory."""
from pathlib import Path
import sys,subprocess,json,re

apk=Path('APK-Test/CampusRift-20261007-batch1007-dev2.apk')
out=Path('task/batch-1007/fix8/build')
sys.argv=[__file__,str(apk)]
source=Path('Tools/b1007_verify_apk.py').read_text(encoding='utf-8-sig')
source=source.replace("out=Path('task/batch-1007/build')", "out=Path('task/batch-1007/fix8/build')",1)
exec(compile(source,'Tools/b1007_verify_apk.py','exec'))

java=Path('C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe')
jar=sdk/'build-tools/36.0.0/lib/apksigner.jar'
signature=subprocess.check_output([str(java),'-jar',str(jar),'verify','--verbose','--print-certs',str(apk)],text=True,encoding='utf-8')
(out/'apksigner.txt').write_text(signature,encoding='utf-8')
assert 'Verified using v2 scheme (APK Signature Scheme v2): true' in signature
assert 'CN=Android Debug' in signature
for component in ['com.campusrift.tech.ClipConsentActivity','com.campusrift.tech.ClipRecordingService']:
    component_block=manifest[manifest.index('="'+component+'"'):].split('\n      E:',1)[0]
    assert re.search(r'android:exported[^\n]*0x0',component_block),component
r['signatureV2']=True;r['debugCertificate']=True;r['clipComponentsPrivate']=True
(out/'verification.json').write_text(json.dumps(r,indent=2),encoding='utf-8')
with Path('task/batch-1007/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n- Job8: APK dev2 kiểm đóng gói/chữ ký PASS; '+str(r['bytes'])+' byte / SHA256 '+r['sha256']+'. Realme connected='+str(r['realmeConnected'])+'. Evidence fix8/build/verification.json, apksigner.txt, AndroidManifest.txt, adb-devices.txt.\n')

if r['realmeConnected']:
    adb=sdk/'platform-tools/adb.exe'
    def device(args,name):
        p=subprocess.run([str(adb),'-s','WGH6S8I7GIMBGQKR',*args],capture_output=True,text=True,encoding='utf-8',errors='replace')
        (out/name).write_text(p.stdout+p.stderr,encoding='utf-8');p.check_returncode();return p.stdout
    device(['install','-r',str(apk)],'adb-install.txt')
    device(['shell','monkey','-p','com.campusrift.game','-c','android.intent.category.LAUNCHER','1'],'adb-launch.txt')
    print('Realme installed/launched; inspect UI before collecting logcat.',flush=True)
