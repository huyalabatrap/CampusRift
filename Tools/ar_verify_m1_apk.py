"""Inspect built APK, then copy it for the still-pending physical-device M1 gate."""
from pathlib import Path
import hashlib,json,subprocess,zipfile,shutil
folder=Path('task/ar/build-m1')
assert (folder/'DONE.txt').read_text().strip()=='Succeeded'
apk=folder/'CampusRift-AR-M1-dev.apk'
aapt=Path('C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/36.0.0/aapt.exe')
manifest=subprocess.check_output([str(aapt),'dump','xmltree',str(apk),'AndroidManifest.xml'],text=True,encoding='utf-8')
badging=subprocess.check_output([str(aapt),'dump','badging',str(apk)],text=True,encoding='utf-8')
(folder/'AndroidManifest.txt').write_text(manifest,encoding='utf-8')
(folder/'badging.txt').write_text(badging,encoding='utf-8')
with zipfile.ZipFile(apk) as archive:
    entries=archive.namelist()
    native=[p for p in entries if p.startswith('lib/')]
    arcore=[p for p in native if 'arcore' in p.lower()]
assert arcore, 'ARCore native plugin missing'
assert "native-code: 'arm64-v8a'" in badging,badging
assert 'com.google.ar.core' in manifest and '"optional"' in manifest,manifest
assert 'application-debuggable' in badging,badging
dest=Path('APK-Test/CampusRift-AR-M1-dev-20261004.apk');shutil.copy2(apk,dest)
result={'source':apk.as_posix(),'copy':dest.as_posix(),'bytes':apk.stat().st_size,'sha256':hashlib.sha256(apk.read_bytes()).hexdigest(),'arm64':True,'development':True,'arcoreLibraries':arcore,'physicalDeviceTest':'PENDING - no adb device observed'}
(folder/'verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
