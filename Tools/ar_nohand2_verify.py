from ar_nohand2 import *
import zipfile,re
apk=Path('APK-Test/CampusRift-20261007-ar-nohand-fix2.apk');out=OUT/'build'
source=Path('Tools/b1007_verify_apk.py').read_text(encoding='utf-8-sig')
source=source.replace("out=Path('task/batch-1007/build');out.mkdir(exist_ok=True)","out=Path('task/ar/nohand2/build');out.mkdir(parents=True,exist_ok=True)",1)
sys.argv=[__file__,str(apk)];exec(compile(source,'Tools/b1007_verify_apk.py','exec'))
java=SDK.parent/'OpenJDK/bin/java.exe';jar=SDK/'build-tools/36.0.0/lib/apksigner.jar'
signature=subprocess.check_output([str(java),'-jar',str(jar),'verify','--verbose','--print-certs',str(apk)],text=True,encoding='utf-8')
(out/'apksigner.txt').write_text(signature,encoding='utf-8');assert 'Verified using v2 scheme (APK Signature Scheme v2): true' in signature;assert 'CN=Android Debug' in signature
before=(ROOT/'task/ar/nohand/old-apk/AndroidManifest.txt').read_text(encoding='utf-8');r['manifestUnchanged']=before==manifest;assert r['manifestUnchanged']
r['signatureV2']=True;r['debugCertificate']=True
with zipfile.ZipFile(apk) as z:
 with zipfile.ZipFile('APK-Test/CampusRift-20261007-batch1007-dev2.apk') as old:
  r['mediaPipeNativeUnchanged']=z.read('lib/arm64-v8a/libmediapipe_tasks_jni.so')==old.read('lib/arm64-v8a/libmediapipe_tasks_jni.so');assert r['mediaPipeNativeUnchanged']
(out/'verification.json').write_text(json.dumps(r,indent=2),encoding='utf-8')
connected=device_log('nohand2-realme-before-install.txt')
if connected and "--install" in sys.argv:
 p=subprocess.run([str(ADB),'-s','WGH6S8I7GIMBGQKR','install','-r',str(apk)],capture_output=True,text=True,encoding='utf-8',errors='replace');(out/'adb-install.txt').write_text(p.stdout+p.stderr,encoding='utf-8');p.check_returncode()
 p=subprocess.run([str(ADB),'-s','WGH6S8I7GIMBGQKR','shell','monkey','-p','com.campusrift.game','-c','android.intent.category.LAUNCHER','1'],capture_output=True,text=True,encoding='utf-8',errors='replace');(out/'adb-launch.txt').write_text(p.stdout+p.stderr,encoding='utf-8');p.check_returncode()
 device_log('nohand-realme-installed.txt')
milestone('APK build/verify hoàn tất: '+str(r['bytes'])+' byte / SHA256 '+r['sha256']+'. Development/ARM64/GLES3/sensorLandscape, model STORED/hash cũ, native MediaPipe không đổi, manifest byte-identical dev2, bridges/Vosk/JNA/privateclip/signatureV2 debug đạt. Realme connected='+str(connected)+'. Evidence nohand/build/verification.json; còn phục hồi và report, không chạy lại suite.')
