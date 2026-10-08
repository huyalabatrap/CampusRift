"""One short P18 skill probe. Runs only the requested runtime, with transient profile."""
import sys,time,json,pathlib
import unity_mcp as m
m.initialize()
skill=sys.argv[1]; count=int(sys.argv[2]); capture=float(sys.argv[3]) if len(sys.argv)>3 else 1
rank=int(sys.argv[4]) if len(sys.argv)>4 else 1
suffix='-mastery' if rank>=4 else ''
def call(name,args):
    r=m.call(name,args).get('result',{}); data=r.get('structuredContent',r)
    if r.get('isError') or data.get('success') is False: raise RuntimeError(json.dumps(data))
    return data
call('manage_editor',{'action':'stop'})
if count>0: call('execute_code',{'action':'execute','code':f'CampusRift.Skills.SkillSet2Setup.InstallUntil({count}); return "Installed";'})
artifact=pathlib.Path(f'Artifacts/Skills/Set2/{skill}{suffix}-smoke.json')
stamp=artifact.stat().st_mtime if artifact.exists() else 0
call('manage_editor',{'action':'play'});time.sleep(1)
call('read_console',{'action':'clear'})
call('execute_code',{'action':'execute','code':f'var go=new UnityEngine.GameObject("P18 skill smoke"); var s=go.AddComponent<CampusRift.Skills.P18SkillSmoke>(); s.skillId="{skill}"; s.captureAt={capture}f; s.testRank={rank}; return "Started";'})
until=time.monotonic()+55
while time.monotonic()<until:
    if artifact.exists() and artifact.stat().st_mtime>stamp: break
    time.sleep(1)
else: raise RuntimeError('P18 smoke timed out')
time.sleep(1)
errors=call('read_console',{'types':['error'],'count':30,'format':'json'})
pathlib.Path(f'Artifacts/Skills/Set2/{skill}{suffix}-console.json').write_text(json.dumps(errors,ensure_ascii=False,indent=2),encoding='utf-8')
call('manage_editor',{'action':'stop'})
print(artifact.read_text(encoding='utf-8'))
