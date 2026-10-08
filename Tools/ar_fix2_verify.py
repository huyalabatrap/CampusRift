from pathlib import Path
import json,subprocess,re
out=Path('task/ar/build-fix2');apk=Path('APK-Test/CampusRift-AR-dev-20261004-fix2.apk')
assert (out/'DONE.txt').read_text().strip()=='Succeeded'
subprocess.run(['python','Tools/ar_verify_apk.py',str(apk),str(out/'packaging.json')],check=True,capture_output=True,text=True)
aapt=Path('C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/36.0.0/aapt.exe')
manifest=subprocess.check_output([str(aapt),'dump','xmltree',str(apk),'AndroidManifest.xml'],text=True,encoding='utf-8')
badging=subprocess.check_output([str(aapt),'dump','badging',str(apk)],text=True,encoding='utf-8')
(out/'AndroidManifest.txt').write_text(manifest,encoding='utf-8');(out/'badging.txt').write_text(badging,encoding='utf-8')
orientation=[line.strip() for line in manifest.splitlines() if 'screenOrientation' in line]
values=[int(re.search(r'\(type 0x10\)0x([0-9a-f]+)$',line)[1],16) for line in orientation]
assert values and all(value in [0,6] for value in values),orientation
assert "native-code: 'arm64-v8a'" in badging
assert 'com.google.ar.core' in manifest and '"optional"' in manifest
assert 'application-debuggable' in badging
assert "package: name='com.campusrift.game'" in badging
result=json.loads((out/'packaging.json').read_text());result.update(dict(arm64=True,development=True,arcoreOptional=True,package='com.campusrift.game',screenOrientation=orientation,physicalDeviceTest='PENDING'))
(out/'verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:result[k] for k in ['apk','bytes','sha256','arm64','development','arcoreOptional','package','screenOrientation']},indent=2))
