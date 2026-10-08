from pathlib import Path
import zipfile,hashlib,json
rows=[]
for name in ['CampusRift-AR-dev-20261006-goiA.apk','CampusRift-20261007-batch1007-dev2.apk']:
 with zipfile.ZipFile(Path('APK-Test')/name) as z:
  names=z.namelist();n='lib/arm64-v8a/libmediapipe_tasks_jni.so';model=z.getinfo('assets/gesture_recognizer.task')
  rows.append(dict(apk=name,modelStored=model.compress_type==0,modelHash=hashlib.sha256(z.read(model)).hexdigest(),mediapipeHash=hashlib.sha256(z.read(n)).hexdigest(),duplicateLibraryNames=len([s for s in names if s.startswith('lib/')])!=len(set(s for s in names if s.startswith('lib/')))))
Path('task/ar/nohand/native-comparison.json').write_text(json.dumps(rows,indent=2),encoding='utf-8');print(json.dumps(rows))
