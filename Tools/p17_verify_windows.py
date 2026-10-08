"""Inspect built Mono assemblies/logs; does not run the game or any harness."""
import json,pathlib,re
root=pathlib.Path('Artifacts/P17-Builds/Windows')
for mode in ('dev','release'):
    folder=root/mode;assembly=folder/'CampusRift_Data/Managed/Assembly-CSharp.dll'
    data=assembly.read_bytes()
    checks={name: name.encode() in data for name in ('TutorialDirector','LocalTelemetry','LevelMusicDirector')}
    for name in ('ShabanHunterValidation','SkillSet1PlayTest','P17Triage','P17Smoke'):
        checks[name+'Absent']=name.encode() not in data
    for name in ('P17PlayerSmoke','StartDev','ResetCooldownForValidation'):
        checks[name+('Present' if mode=='dev' else 'Absent')]=(name.encode() in data)==(mode=='dev')
    logs={}
    for filename in (('native-smoke.log','native-reopen.log') if mode=='dev' else ('native-startup.log',)):
        text=(folder/filename).read_text(encoding='utf-8-sig',errors='replace')
        logs[filename]={'exceptionLines':[s for s in text.splitlines() if re.search(r'^(?:\w*Exception|[\w.]+Exception|ERROR|Error:)',s)],'gaussianDepthShaderNotSupported':text.count('Hidden/Universal Render Pipeline/GaussianDepthOfField')}
    result={'mode':mode,'checks':checks,'allChecksPass':all(checks.values()),'logs':logs,'scope':'dev Hub/level1/QA persistence; release startup only; no Android device test'}
    (folder/'package-verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(mode,'checks:',result['allChecksPass'],'exceptions:',sum(len(l['exceptionLines']) for l in logs.values()))
    assert result['allChecksPass'] and all(not l['exceptionLines'] for l in logs.values()),result
