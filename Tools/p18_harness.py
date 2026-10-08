"""Run one requested P18-related smoke harness, preserving its JSON evidence."""
import json,pathlib,sys,time
import unity_mcp as m
m.initialize()
kind=sys.argv[1]
base={'SkillSet2PlayTest':'Artifacts/Skills/Set2/SkillSet2','SkillSet1PlayTest':'Artifacts/Skills/SkillSet1','ReactionPlayTest':'Artifacts/Reactions/Validation'}[kind]
photo=len(sys.argv)>2 and sys.argv[2]=='photo'
if photo: base='Artifacts/Reactions/P18Capture'
def call(name,args):
    r=m.call(name,args).get('result',{}); data=r.get('structuredContent',r)
    if r.get('isError') or data.get('success') is False: raise RuntimeError(json.dumps(data))
    return data
call('manage_editor',{'action':'stop'})
if kind=='SkillSet2PlayTest':
    call('execute_code',{'action':'execute','code':'CampusRift.Skills.SkillSet2Setup.InstallUntil(11); return "P18 installed";'})
done=pathlib.Path('Artifacts/Reactions/DONE.txt' if kind=='ReactionPlayTest' and not photo else base+'-DONE.txt');stamp=done.stat().st_mtime if done.exists() else 0
call('read_console',{'action':'clear'})
call('manage_editor',{'action':'play'});time.sleep(1)
ns='CampusRift.Combat' if kind=='ReactionPlayTest' else 'CampusRift.Skills'
retry=' s.failedOnly=true;' if len(sys.argv)>2 and sys.argv[2]=='failed' else ''
if photo: retry=' s.photoOnly=true;'
if retry and kind=='SkillSet1PlayTest': retry+=' s.resume=true;'
call('execute_code',{'action':'execute','code':f'var s=new UnityEngine.GameObject("{kind}").AddComponent<{ns}.{kind}>();{retry} return "Smoke started";'})
deadline=time.monotonic()+220
while time.monotonic()<deadline:
    if done.exists() and done.stat().st_mtime>stamp: break
    time.sleep(1)
else: raise RuntimeError(f'{kind} timed out; inspect partial JSON before rerunning')
errors=call('read_console',{'types':['error'],'count':30,'format':'json'})
pathlib.Path(base+'-console.json').write_text(json.dumps(errors,indent=2),encoding='utf-8')
call('manage_editor',{'action':'stop'})
print(done.read_text(encoding='utf-8'))
print(pathlib.Path(base+'.json').read_text(encoding='utf-8'))
