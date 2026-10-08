"""Inspect the four built packages, release guards, summaries and native logs."""
from pathlib import Path
import json, re, zipfile

root=Path.cwd()
release=root/'Releases/2026-10-04-v1.0'
rows=[]
for platform in ('Windows','Android'):
    for variant in ('dev','release'):
        folder=release/platform/variant
        summary=(folder/'build-summary.txt').read_text(encoding='utf-8-sig')
        checks={'succeeded':'Result: Succeeded' in summary,'zero_build_errors':'Errors: 0' in summary}
        if platform=='Windows':
            data=(folder/'CampusRift_Data/Managed/Assembly-CSharp.dll').read_bytes()
            checks['runtime_files_present']=all((folder/name).exists() for name in ('CampusRift.exe','CampusRift_Data','UnityPlayer.dll','MonoBleedingEdge'))
        else:
            with zipfile.ZipFile(folder/'CampusRift.apk') as z:
                names=z.namelist()
                data=z.read(next(n for n in names if n.endswith('/global-metadata.dat')))
                checks['arm64_il2cpp']='lib/arm64-v8a/libil2cpp.so' in names
                markers={b'tthcm-ch1':False,'Tư tưởng Hồ Chí Minh'.encode('utf-8'):False}
                for name in names:
                    if name.startswith('assets/bin/Data/') and '/Managed/' not in name and not name.endswith('/'):
                        body=z.read(name)
                        for marker in markers:markers[marker]|=marker in body
                checks['course_packaged']=all(markers.values())
        for name in ('AccessibilityDirector','LevelDirector','EndgameFactory','LocalTelemetry','SmallMonsterForLevel'):
            checks[name]=name.encode() in data
        for name in ('LevelFlowPlayTest','LearningPlayTest','V2DevTools','ShabanHunterValidation','ReleaseContentGate'):
            checks[name+'_absent']=name.encode() not in data
        if variant=='release':
            for name in ('P17PlayerSmoke','P23Performance','SkillSet1TestWorld','StartDev','ResetCooldownForValidation'):
                checks[name+'_absent']=name.encode() not in data
        if platform=='Windows' and variant=='dev':checks['explicit_perf_fixture_present']=b'P23Performance' in data
        row={'platform':platform,'variant':variant,'checks':checks,'passed':all(checks.values()),'summary':summary}
        (folder/'package-verification.json').write_text(json.dumps(row,ensure_ascii=False,indent=2),encoding='utf-8')
        rows.append(row)
        print(platform,variant,row['passed'],[k for k,v in checks.items() if not v])
logs=[]
for p in (root/'task/p23').glob('*player.log'):
    text=p.read_text(encoding='utf-8-sig',errors='replace')
    errors=[line for line in text.splitlines() if re.search(r'^(?:[\w.]*Exception|ERROR|Error:|Crash!!!)',line)]
    logs.append({'file':p.relative_to(root).as_posix(),'errors':errors,'shaderWarnings':text.count('Hidden/Universal Render Pipeline/GaussianDepthOfField')})
result={'builds':rows,'nativeLogs':logs,'passed':all(r['passed'] for r in rows) and all(not r['errors'] for r in logs),'androidDeviceTested':False}
(root/'task/p23/package-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
assert result['passed'],result
