from pathlib import Path
import json,subprocess,re,zipfile,struct
out=Path('task/ar/build-goiA');apk=Path('APK-Test/CampusRift-AR-dev-20261006-goiA.apk')
assert (out/'DONE.txt').read_text().strip()=='Succeeded'
subprocess.run(['python','Tools/ar_verify_apk.py',str(apk),str(out/'packaging.json')],check=True,capture_output=True,text=True)
aapt=Path('C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/36.0.0/aapt.exe')
manifest=subprocess.check_output([str(aapt),'dump','xmltree',str(apk),'AndroidManifest.xml'],text=True,encoding='utf-8');badging=subprocess.check_output([str(aapt),'dump','badging',str(apk)],text=True,encoding='utf-8')
(out/'AndroidManifest.txt').write_text(manifest,encoding='utf-8');(out/'badging.txt').write_text(badging,encoding='utf-8')
orientation=[line.strip() for line in manifest.splitlines() if 'screenOrientation' in line]
assert orientation and all(int(re.search(r'\(type 0x10\)0x([0-9a-f]+)$',s)[1],16)==6 for s in orientation)
assert "native-code: 'arm64-v8a'" in badging and 'application-debuggable' in badging
def methods(data):
    u32=lambda i:struct.unpack_from('<I',data,i)[0];strings=u32(60);types=u32(68);protos=u32(76);mt=u32(92)
    def string(index):
        at=u32(strings+index*4)
        while data[at]&128:at+=1
        at+=1;return data[at:data.index(b'\0',at)].decode('utf-8','replace')
    def dtype(index):return string(u32(types+index*4))
    for i in range(u32(88)):
        owner,proto,name=struct.unpack_from('<HHI',data,mt+i*8)
        if dtype(owner) not in ('Lcom/campusrift/gesture/GestureBridge;','Lcom/campusrift/gesture/GestureBridge$GestureListener;'):continue
        p=protos+proto*12;params=u32(p+8);args='' if params==0 else ''.join(dtype(struct.unpack_from('<H',data,params+4+j*2)[0]) for j in range(u32(params)))
        yield string(name)+'('+args+')'+dtype(u32(p+4))
signatures=[]
with zipfile.ZipFile(apk) as z:
    for name in z.namelist():
        if name.endswith('.dex'):signatures.extend(methods(z.read(name)))
assert 'submit(Ljava/nio/ByteBuffer;IIIJIJ)Z' in signatures
assert 'recover(IZ)V' in signatures and 'selectDelegate(ILjava/lang/String;)V' in signatures and 'clockNanos()J' in signatures
assert any(s.startswith('onResult(IJLjava/lang/String;FLjava/lang/String;[F[F[Ljava/lang/String;[FZZJIIJJ)') for s in signatures),signatures
assert 'onState(ILjava/lang/String;ZLjava/lang/String;)V' in signatures
r=json.loads((out/'packaging.json').read_text());r.update(arm64=True,development=True,screenOrientation=orientation,bridgeMethods=signatures,device='No adb device connected')
(out/'verification.json').write_text(json.dumps(r,indent=2),encoding='utf-8');print(json.dumps(r,indent=2))
