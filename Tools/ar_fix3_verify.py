from pathlib import Path
import json, subprocess, re, zipfile, struct
out=Path('task/ar/build-fix3');apk=Path('APK-Test/CampusRift-AR-dev-20261005-fix3.apk')
assert (out/'DONE.txt').read_text().strip()=='Succeeded'
subprocess.run(['python','Tools/ar_verify_apk.py',str(apk),str(out/'packaging.json')],check=True,capture_output=True,text=True)
aapt=Path('C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/36.0.0/aapt.exe')
manifest=subprocess.check_output([str(aapt),'dump','xmltree',str(apk),'AndroidManifest.xml'],text=True,encoding='utf-8')
badging=subprocess.check_output([str(aapt),'dump','badging',str(apk)],text=True,encoding='utf-8')
(out/'AndroidManifest.txt').write_text(manifest,encoding='utf-8');(out/'badging.txt').write_text(badging,encoding='utf-8')
orientation=[line.strip() for line in manifest.splitlines() if 'screenOrientation' in line]
assert orientation and all(int(re.search(r'\(type 0x10\)0x([0-9a-f]+)$',s)[1],16)==6 for s in orientation),orientation
assert "native-code: 'arm64-v8a'" in badging and 'application-debuggable' in badging
assert 'android.permission.VIBRATE' in manifest and '"optional"' in manifest and "package: name='com.campusrift.game'" in badging
def methods(data):
    u32=lambda i:struct.unpack_from('<I',data,i)[0]
    strings=u32(60);types=u32(68);protos=u32(76);method_table=u32(92)
    def string(index):
        at=u32(strings+index*4)
        while data[at]&128:at+=1
        at+=1
        return data[at:data.index(b'\0',at)].decode('utf-8','replace')
    def dtype(index):return string(u32(types+index*4))
    for i in range(u32(88)):
        owner,proto,name=struct.unpack_from('<HHI',data,method_table+i*8)
        if dtype(owner)!='Lcom/campusrift/gesture/GestureBridge;':continue
        p=protos+proto*12;params=u32(p+8)
        args='' if params==0 else ''.join(dtype(struct.unpack_from('<H',data,params+4+j*2)[0]) for j in range(u32(params)))
        yield string(name)+'('+args+')'+dtype(u32(p+4))
signatures=[]
with zipfile.ZipFile(apk) as z:
    for name in z.namelist():
        if name.endswith('.dex'):signatures.extend(methods(z.read(name)))
assert 'submit(Ljava/nio/ByteBuffer;IIIJ)Z' in signatures,signatures
assert '<init>(Landroid/app/Activity;Lcom/campusrift/gesture/GestureBridge$GestureListener;Ljava/lang/String;)V' in signatures,signatures
assert not any(s.startswith('submit([B') for s in signatures),signatures
result=json.loads((out/'packaging.json').read_text());result.update(arm64=True,development=True,arcoreOptional=True,screenOrientation=orientation,directBufferSignatures=signatures,physicalDevice='Not connected')
(out/'verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:result[k] for k in ['apk','bytes','sha256','screenOrientation','directBufferSignatures']},indent=2))
