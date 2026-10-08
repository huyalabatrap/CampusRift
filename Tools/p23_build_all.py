"""Build all four local P23 packages sequentially, with fresh completion and logs."""
from pathlib import Path
import json, subprocess, sys, time
import unity_mcp as m

root=Path.cwd();m.initialize()
def call(name,args):
    for _ in range(40):
        raw=m.call(name,args);v=raw.get('result',{}).get('structuredContent',raw.get('result',raw))
        if v.get('success') is not False:return v
        time.sleep(2)
    raise RuntimeError(v)

rows=json.loads((root/'Artifacts/V2/P23-regression/Summary.json').read_text())
inventory=json.loads((root/'Artifacts/V2/P23-regression/inventory.json').read_text())
assert {r['name'] for r in rows}=={r['name'] for r in inventory},'Regression inventory incomplete'
assert (root/'Artifacts/V2/Regression-Full.md').is_file(),'Regression review missing'
for platform,variant in [('Android','dev'),('Android','release'),('Windows','dev'),('Windows','release')]:
    folder=root/'Releases/2026-10-04-v1.0'/platform/variant
    if (folder/'DONE.txt').exists() and (folder/'DONE.txt').read_text()=='Succeeded':
        print('SKIP completed',platform,variant,flush=True);continue
    call('read_console',{'action':'clear'})
    stamp=time.time();subprocess.run([sys.executable,'Tools/p23_builds.py',platform,variant],check=True)
    deadline=time.time()+2400
    while time.time()<deadline:
        done=folder/'DONE.txt'
        if done.exists() and done.stat().st_mtime>=stamp:break
        time.sleep(2)
    else:raise TimeoutError(str(folder))
    result=done.read_text(encoding='utf-8-sig')
    console=call('read_console',{'action':'get','types':['error','warning'],'count':100,'format':'detailed','include_stacktrace':True})
    evidence=root/'task/p23/build-logs';evidence.mkdir(parents=True,exist_ok=True)
    (evidence/(platform+'-'+variant+'-console.json')).write_text(json.dumps(console,ensure_ascii=False,indent=2),encoding='utf-8')
    with (root/'task/p23/PROGRESS.md').open('a',encoding='utf-8') as f:
        f.write('\n- T06 '+platform+'/'+variant+': '+result.splitlines()[0]+'; '+str(round(time.time()-stamp,1))+'s; build-summary và log được lưu.\n')
    print('BUILD',platform,variant,result.splitlines()[0],flush=True)
    assert result=='Succeeded',result
print('ALL FOUR BUILDS SUCCEEDED',flush=True)
