from pathlib import Path
import hashlib,json,struct,subprocess,zipfile,re,sys
out=Path('task/batch-1007/build');out.mkdir(exist_ok=True)
apk=Path(sys.argv[1] if len(sys.argv)>1 else 'APK-Test/CampusRift-20261007-batch1007-dev.apk')
sdk=Path('C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK')
def dex(data):
    u32=lambda i:struct.unpack_from('<I',data,i)[0];strings=u32(60);types=u32(68);protos=u32(76);mt=u32(92)
    def string(i):
        at=u32(strings+i*4)
        while data[at]&128:at+=1
        at+=1;return data[at:data.index(b'\0',at)].decode('utf-8','replace')
    def dtype(i):return string(u32(types+i*4))
    classes=[dtype(u32(u32(100)+i*32)) for i in range(u32(96))]
    methods=[]
    for i in range(u32(88)):
        owner,proto,name=struct.unpack_from('<HHI',data,mt+i*8);owner=dtype(owner)
        if not owner.startswith(('Lcom/campusrift/','Lorg/vosk/')):continue
        p=protos+proto*12;params=u32(p+8);args='' if not params else ''.join(dtype(struct.unpack_from('<H',data,params+4+j*2)[0]) for j in range(u32(params)))
        methods.append(owner+'->'+string(name)+'('+args+')'+dtype(u32(p+4)))
    return classes,methods
r={'apk':apk.as_posix(),'bytes':apk.stat().st_size,'sha256':hashlib.file_digest(apk.open('rb'),'sha256').hexdigest()}
with zipfile.ZipFile(apk) as z:
    gesture=z.getinfo('assets/gesture_recognizer.task');r['gestureModel']={'bytes':gesture.file_size,'compression':gesture.compress_type,'sha256':hashlib.sha256(z.read(gesture)).hexdigest()}
    assert gesture.compress_type==zipfile.ZIP_STORED
    assert r['gestureModel']['sha256']=='97952348cf6a6a4915c2ea1496b4b37ebabc50cbbf80571435643c455f2b0482'
    r['voskFiles']=[]
    for p in Path('Assets/StreamingAssets/vosk').rglob('*'):
        if not p.is_file() or p.suffix=='.meta':continue
        n='assets/vosk/'+p.relative_to('Assets/StreamingAssets/vosk').as_posix();info=z.getinfo(n)
        assert hashlib.sha256(z.read(n)).digest()==hashlib.sha256(p.read_bytes()).digest(),n
        # README is documentation; require STORED for every actual model file.
        if p.name!='README':assert info.compress_type==zipfile.ZIP_STORED,n
        r['voskFiles'].append({'name':n,'bytes':info.file_size,'compression':info.compress_type})
    defs={};methods=[]
    for n in z.namelist():
        if n.endswith('.dex'):
            c,m=dex(z.read(n));defs.update({s:n for s in c});methods+=m
    required=['Lcom/campusrift/gesture/GestureBridge;','Lcom/campusrift/gesture/GestureBridge$GestureListener;','Lcom/campusrift/tech/VoiceBridge;','Lcom/campusrift/tech/ClipBridge;','Lcom/campusrift/tech/ClipConsentActivity;','Lcom/campusrift/tech/ClipRecordingService;','Lorg/vosk/Model;','Lorg/vosk/Recognizer;','Lcom/sun/jna/Native;','Lcom/google/mediapipe/tasks/vision/gesturerecognizer/GestureRecognizer;']
    for s in required:assert s in defs,s
    r['requiredClasses']={s:defs[s] for s in required};r['bridgeMethods']=[s for s in methods if s.startswith('Lcom/campusrift/')]
    assert any(s.endswith('submit(Ljava/nio/ByteBuffer;IIIJIJ)Z') for s in methods)
    assert any('->onHands(' in s for s in methods)
    assert any(s.endswith('setHandCount(II)V') for s in methods)
    r['nativeLibraries']=[n for n in z.namelist() if n.startswith('lib/')]
    for lib in ['libvosk.so','libjnidispatch.so','libmediapipe_tasks_jni.so','libil2cpp.so']:assert 'lib/arm64-v8a/'+lib in r['nativeLibraries'],lib
    # MediaPipe1.0 consolidates vision JNI into the tasks library; verify that
    # the packaged Java code refers to the same native library name.
    assert any(b'mediapipe_tasks_jni\x00' in z.read(n) for n in z.namelist() if n.endswith('.dex'))
aapt=sdk/'build-tools/36.0.0/aapt.exe'
manifest=subprocess.check_output([str(aapt),'dump','xmltree',str(apk),'AndroidManifest.xml'],text=True,encoding='utf-8')
badging=subprocess.check_output([str(aapt),'dump','badging',str(apk)],text=True,encoding='utf-8')
(out/'AndroidManifest.txt').write_text(manifest,encoding='utf-8');(out/'badging.txt').write_text(badging,encoding='utf-8')
for permission in ['CAMERA','RECORD_AUDIO','FOREGROUND_SERVICE','FOREGROUND_SERVICE_MEDIA_PROJECTION']:assert 'android.permission.'+permission in manifest
for name in ['ClipConsentActivity','ClipRecordingService']:assert name in manifest,name
# aapt decodes the foreground-service flag as its integer resource value.
service_types=[l.strip() for l in manifest.splitlines() if 'foregroundServiceType' in l]
assert any('mediaProjection' in s or re.search(r'\(type 0x11\)0x20$',s) for s in service_types),service_types
orientation=[l.strip() for l in manifest.splitlines() if 'screenOrientation' in l]
assert orientation and all(int(re.search(r'\(type 0x10\)0x([0-9a-f]+)$',s)[1],16)==6 for s in orientation)
assert "native-code: 'arm64-v8a'" in badging and 'application-debuggable' in badging
gles_version=int(re.search(r'glEsVersion[^\n]*\(type 0x11\)0x([0-9a-f]+)',manifest)[1],16)
assert gles_version>>16==3,hex(gles_version)
r.update(development=True,arm64=True,gles3=True,glesManifestVersion=hex(gles_version),orientation=orientation,foregroundServiceTypes=service_types)
devices=subprocess.check_output([str(sdk/'platform-tools/adb.exe'),'devices'],text=True,encoding='utf-8');(out/'adb-devices.txt').write_text(devices,encoding='utf-8');r['devices']=devices;r['realmeConnected']='WGH6S8I7GIMBGQKR\tdevice' in devices
(out/'verification.json').write_text(json.dumps(r,indent=2),encoding='utf-8');print(json.dumps({k:r[k] for k in ['apk','bytes','sha256','development','arm64','gles3','realmeConnected']},indent=2))
