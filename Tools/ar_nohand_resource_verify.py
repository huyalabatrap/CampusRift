from pathlib import Path
import zipfile,json
apk=Path('APK-Test/CampusRift-20261007-ar-nohand-fix.apk');rows=[]
with zipfile.ZipFile(apk) as z:
 for n in z.namelist():
  if 'globalgamemanagers' in n or n.endswith('/resources.assets'):
   b=z.read(n);rows.append(dict(name=n,bytes=len(b),resourcePath=b'armodes/featurepoints' in b.lower(),materialName=b'FeaturePoints' in b,unlitShaderName=b'Universal Render Pipeline/Unlit' in b))
Path('task/ar/nohand/build/resource-preservation.json').write_text(json.dumps(rows,indent=2),encoding='utf-8');print(rows)
