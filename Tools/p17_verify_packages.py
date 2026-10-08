"""Inspect P17 APKs; keep the legacy Algorithms-course verifier unchanged."""
import pathlib,json,hashlib,zipfile,sys
for arg in sys.argv[1:]:
 p=pathlib.Path(arg)
 with zipfile.ZipFile(p) as z:
  names=z.namelist();meta=z.read(next(n for n in names if n.endswith('/global-metadata.dat')))
  checks={'arm64_il2cpp':'lib/arm64-v8a/libil2cpp.so' in names,
    'LearningEngine':b'LearningEngine\0' in meta,'TutorialDirector':b'TutorialDirector\0' in meta,
    'LocalTelemetry':b'LocalTelemetry\0' in meta,
    'editor_harnesses_absent':all(n+b'\0' not in meta for n in (b'LearningPlayTest',b'GiantHandPlayTest',b'HeavenSwordPlayTest',b'V2DevTools',b'ReleaseContentGate'))}
  content={b'tthcm-ch1':False,'Tư tưởng Hồ Chí Minh'.encode():False}
  for name in names:
   if name.startswith('assets/bin/Data/') and '/Managed/' not in name and not name.endswith('/'):
    data=z.read(name)
    for marker in content:content[marker]|=marker in data
  checks.update({'chapter_id_packaged':content[b'tthcm-ch1'],'TTHCM_content_packaged':content['Tư tưởng Hồ Chí Minh'.encode()]})
  if p.parent.name=='release':checks['release_QA_methods_absent']=all(n+b'\0' not in meta for n in (b'P17PlayerSmoke',b'StartDev',b'ResetCooldownForValidation'))
 report={'apk':str(p),'bytes':p.stat().st_size,'sha256':hashlib.file_digest(p.open('rb'),'sha256').hexdigest(),'checks':checks}
 (p.parent/'package-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps(report,ensure_ascii=True))
 if not all(checks.values()):raise SystemExit(1)
