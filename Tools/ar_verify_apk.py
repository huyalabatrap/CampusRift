"""Inspect actual packaged model and DEX class definitions, without installing the APK."""
import hashlib,json,struct,sys,zipfile
from pathlib import Path

def classes(data):
    u32=lambda off:struct.unpack_from('<I',data,off)[0]
    strings=u32(60);types=u32(68)
    def string(index):
        at=u32(strings+index*4)
        while data[at]&128:at+=1
        at+=1
        return data[at:data.index(b'\0',at)].decode('utf-8','replace')
    for n in range(u32(96)):
        type_index=u32(u32(100)+n*32)
        yield string(u32(types+type_index*4))

apk=Path(sys.argv[1]);report={'apk':str(apk),'bytes':apk.stat().st_size,'sha256':hashlib.file_digest(apk.open('rb'),'sha256').hexdigest()}
with zipfile.ZipFile(apk) as z:
    model=z.getinfo('assets/gesture_recognizer.task')
    report['model']={'size':model.file_size,'compression':model.compress_type,'sha256':hashlib.sha256(z.read(model)).hexdigest()}
    found={}
    for name in z.namelist():
        if name.endswith('.dex'):
            for descriptor in classes(z.read(name)):
                if descriptor.startswith('Lcom/campusrift/gesture/') or descriptor=='Lcom/google/mediapipe/tasks/vision/gesturerecognizer/GestureRecognizer;':found[descriptor]=name
    report['classes']=found
    report['arm64_libraries']=[n for n in z.namelist() if n.startswith('lib/arm64-v8a/')]
assert report['model']['compression']==0,report['model']
assert report['model']['sha256']=='97952348cf6a6a4915c2ea1496b4b37ebabc50cbbf80571435643c455f2b0482'
assert 'Lcom/campusrift/gesture/GestureBridge;' in found
assert 'Lcom/campusrift/gesture/GestureBridge$GestureListener;' in found
assert 'Lcom/google/mediapipe/tasks/vision/gesturerecognizer/GestureRecognizer;' in found
Path(sys.argv[2]).write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
