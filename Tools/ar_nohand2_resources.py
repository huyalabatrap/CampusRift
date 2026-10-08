from ar_nohand2 import *
import zipfile
rows=[]
with zipfile.ZipFile(ROOT/'APK-Test/CampusRift-20261007-ar-nohand-fix2.apk') as z:
    for n in z.namelist():
        if 'globalgamemanagers' in n or n.endswith('/resources.assets'):
            b=z.read(n);rows.append(dict(name=n,resourcePath=b'armodes/featurepoints' in b.lower(),materialName=b'FeaturePoints' in b,unlitShaderName=b'Universal Render Pipeline/Unlit' in b))
assert any(r['resourcePath'] for r in rows) and any(r['unlitShaderName'] for r in rows)
save('build/resource-preservation.json',rows)
print('FeaturePoints resource and Unlit shader preserved in final APK')
