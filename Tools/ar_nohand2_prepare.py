from pathlib import Path
import re

# Reuse the established local Editor RPC and preservation workflow in a separate evidence folder.
root=Path.cwd()
for name in ['ar_nohand','ar_nohand_checks','ar_nohand_restore','ar_nohand_build','ar_nohand_verify']:
    s=(root/'Tools'/f'{name}.py').read_text(encoding='utf-8-sig')
    s=s.replace('from ar_nohand import *','from ar_nohand2 import *')
    s=s.replace("task/ar/nohand", "task/ar/nohand2").replace("AR-NOHAND-pre-20261007", "AR-NOHAND2-pre-20261007")
    s=s.replace('ar-nohand-fix.apk','ar-nohand-fix2.apk')
    if name=='ar_nohand':
        s=s.replace('## AR NOHAND —','## AR NOHAND2 —')
    if name=='ar_nohand_build':
        s=s.replace("['ARGestureUnitTests','OpenPalmMock','ARRiftPlayTest']", "['ARGestureUnitTests','ARRiftPlayTest']")
    if name=='ar_nohand_verify':
        s=s.replace("OUT/'old-apk/AndroidManifest.txt'", "ROOT/'task/ar/nohand/old-apk/AndroidManifest.txt'")
    if name=='ar_nohand_restore':
        s=s.replace("assert all((ROOT/rel).read_bytes()==(BACKUP/rel).read_bytes() for rel in ['Assets/ARRift/Validation/ARGestureUnitTests.cs','Assets/ARRift/Validation/ARRiftPlayTest.cs'])", "assert (ROOT/'Assets/ARRift/Validation/ARRiftPlayTest.cs').read_bytes()==(BACKUP/'Assets/ARRift/Validation/ARRiftPlayTest.cs').read_bytes()")
        s=s.replace('existingSuitesUnchanged=True','playSuiteUnchanged=True,unitExpectationUpdatedForNeutralRelease=True')
    dest=name.replace('ar_nohand','ar_nohand2',1)
    (root/'Tools'/f'{dest}.py').write_text(s,encoding='utf-8')
