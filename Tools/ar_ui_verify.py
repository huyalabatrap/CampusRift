from ar_ui import *
import zipfile
apk=ROOT/'APK-Test/CampusRift-20261007-ar-ui-fix.apk';out=OUT/'build'
source=(ROOT/'Tools/b1007_verify_apk.py').read_text(encoding='utf-8-sig').replace("out=Path('task/batch-1007/build');out.mkdir(exist_ok=True)","out=Path('task/ar/ui-fix/build');out.mkdir(parents=True,exist_ok=True)",1)
sys.argv=[__file__,str(apk)];exec(compile(source,'Tools/b1007_verify_apk.py','exec'))
signature=subprocess.check_output([str(SDK.parent/'OpenJDK/bin/java.exe'),'-jar',str(SDK/'build-tools/36.0.0/lib/apksigner.jar'),'verify','--verbose','--print-certs',str(apk)],text=True,encoding='utf-8')
(out/'apksigner.txt').write_text(signature,encoding='utf-8');assert 'Verified using v2 scheme (APK Signature Scheme v2): true' in signature
before=(ROOT/'task/ar/nohand/old-apk/AndroidManifest.txt').read_text(encoding='utf-8');r['manifestUnchanged']=before==manifest;assert r['manifestUnchanged']
with zipfile.ZipFile(apk) as z,zipfile.ZipFile(ROOT/'APK-Test/CampusRift-20261007-ar-nohand-fix2.apk') as old:
 r['mediaPipeNativeUnchanged']=z.read('lib/arm64-v8a/libmediapipe_tasks_jni.so')==old.read('lib/arm64-v8a/libmediapipe_tasks_jni.so');assert r['mediaPipeNativeUnchanged']
 r['sphereNativeNamePreserved']=b'SphereCollider' in z.read('lib/arm64-v8a/libunity.so');assert r['sphereNativeNamePreserved']
physics=ROOT/'Library/Bee/artifacts/Android/ManagedStripped/UnityEngine.PhysicsModule.dll'
data=physics.read_bytes();r['strippedPhysics']={name:name.encode() in data for name in ['SphereCollider','CapsuleCollider','BoxCollider','MeshCollider']};assert all(r['strippedPhysics'].values())
r['physicsModuleEnabled']=json.loads((ROOT/'Packages/manifest.json').read_text(encoding='utf-8'))['dependencies']['com.unity.modules.physics']=='1.0.0';assert r['physicsModuleEnabled']
r['signatureV2']=True;(out/'verification.json').write_text(json.dumps(r,indent=2),encoding='utf-8')
milestone('APK build/verify hoàn tất: '+str(r['bytes'])+' byte / SHA256 '+r['sha256']+'. ModelSTORED/hash cũ; manifest byte-identical NOHAND2/dev2, ARM64/GLES3/sensorLandscape/dev/signatureV2 đạt; MediaPipe native không đổi. ManagedStripped PhysicsModule giữ 4 collider + libunity chứa SphereCollider. Tiếp adb install-r/pm package và phục hồi; không chạy hồi quy.')
