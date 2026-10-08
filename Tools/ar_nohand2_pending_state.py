from ar_nohand2 import *
args=[str(ADB),'-s','WGH6S8I7GIMBGQKR','shell']
rows={}
for key,command,terms in [('window',['dumpsys','window'],['mCurrentFocus','mFocusedApp']),('policy',['dumpsys','window','policy'],['deviceLocked','mShowingLockscreen','isStatusBarKeyguard','showing=true']),('package',['pm','path','com.campusrift.game'],['package:'])]:
    p=subprocess.run(args+command,capture_output=True,text=True,encoding='utf-8',errors='replace')
    rows[key]=dict(exit=p.returncode,lines=[l.strip() for l in (p.stdout+p.stderr).splitlines() if any(t in l for t in terms)])
save('build/install-pending-state.json',dict(at=time.strftime('%Y-%m-%d %H:%M:%S'),**rows));print(json.dumps(rows))
