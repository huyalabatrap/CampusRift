from ar_nohand import *
import re,collections,statistics
src=ROOT/'task/ar/device-logs/nohand-fix2.txt'
g=[];d=[];errors=[];unity=[]
with src.open(encoding='utf-8',errors='replace') as f:
 for n,l in enumerate(f,1):
  if 'Unity' not in l or '(21238)' not in l:continue
  unity.append(l)
  if '[ARGesture]' in l:g.append((n,l.strip()))
  if '[ARDiag]' in l:d.append((n,l.strip()))
  if 'ArgumentNullException' in l:errors.append((n,l.strip()))
def counts(token,rows):
 return dict(collections.Counter(m.group(1) for _,l in rows if (m:=re.search(token+r'=(\S+)',l))))
ready=[dict(line=n,text=l) for n,l in d if 'recognizer ready=True hands=1 delegate=CPU' in l]
active=[(n,l) for n,l in d if 'sampling=True paused=False input-blocked=False' in l]
result=dict(source=src.relative_to(ROOT).as_posix(),pid=21238,gestureCount=len(g),decisions=counts('decision',g),labels=counts('model',g),geometry=counts('geometry',g),nativeReady=bool(ready),firstReady=ready[0] if ready else None,activeDiagnostics=[dict(line=n,text=l) for n,l in active[:2]+active[-2:]],shaderExceptions=len(errors),shaderExceptionFirst=errors[:1],logEnd=unity[-1].strip() if unity else None,buildCorrelation='Log timestamps 20:50-20:53 precede final build2 completed 20:54/verified20:55. No build SHA in log: recognition evidence belongs to round1 diagnostics code before final point material patch; do not claim final shader patch validated by this log.')
save('device-readiness.json',result)
dest=ROOT/'task/ar/device-logs/nohand-realme-ready.txt';dest.write_text(''.join(unity),encoding='utf-8')
devices=subprocess.run([str(ADB),'devices','-l'],capture_output=True,text=True,encoding='utf-8',errors='replace')
package=subprocess.run([str(ADB),'-s','WGH6S8I7GIMBGQKR','shell','pm','path','com.campusrift.game'],capture_output=True,text=True,encoding='utf-8',errors='replace')
save('device-package-at-close.json',dict(at=time.strftime('%Y-%m-%d %H:%M:%S'),devices=devices.stdout,returncode=package.returncode,path=package.stdout,stderr=package.stderr,coordinatorNote='PROGRESS21:16 says installed with adb and pm list packages verified. Current pm path is only present-time state, not evidence of the old tested binary hash. No reinstall per coordinator handoff.'))
connect();state=code((ROOT/'Tools/tech2_handoff.cs').read_text(encoding='utf-8'));save('handoff-at-close.json',state)
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save('console-at-close.json',console)
assert state['platform']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and not any(state[k] for k in ['playing','dirty','compiling','updating']) and state['xrLoader'] is None,state
assert not console.get('data'),console
for p,h in json.loads((OUT/'protected-files.json').read_text(encoding='utf-8')).items():assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h,p
for suite in ['ARGestureUnitTests','ARRiftPlayTest']:
 assert (ROOT/f'Assets/ARRift/Validation/{suite}.cs').read_bytes()==(BACKUP/f'Assets/ARRift/Validation/{suite}.cs').read_bytes()
apk=ROOT/'APK-Test/CampusRift-20261007-ar-nohand-fix.apk';assert hashlib.sha256(apk.read_bytes()).hexdigest()=='20a4ea1ab3c84dccfe01bc830eaac0c15fc3768cb791834be6254041465418ee'
snap=json.loads((OUT/'original-editor.json').read_text(encoding='utf-8'))
for s in (BACKUP/'save').rglob('*'):
 if s.is_file():assert s.read_bytes()==(Path(snap['savePath'])/s.relative_to(BACKUP/'save')).read_bytes(),s
save('close-verification.json',dict(apkHashMatches=True,protectedFilesUnchanged=True,suitesNotRerun=True,savesByteIdentical=True,editorClean=True))
milestone('Tiếp quản lại đọc đủ brief và mốc điều phối21:16: không chờ unlock/reinstall. Phân tích đầy đủ log nohand-fix2 PID21238, xác nhận native ready CPU/1tay và '+str(len(g))+' ARGesture; thống kê tại nohand/device-readiness.json. Log có timestamp trước final build2: không gán hash cuối hoặc kết quả shader mới cho log cũ. Đã recheck hash APK, save/điều phối/suite nguyên, Android/Edit/SampleScene sạch/Console0 bằng read-only; không chạy lại test. Sẽ đóng báo cáo vòng1 trung thực: release-required vẫn ngăn fire, vòng2 có brief riêng.')
print(json.dumps({k:result[k] for k in ['gestureCount','decisions','labels','geometry','nativeReady','shaderExceptions','logEnd']},ensure_ascii=True))
print('current package',package.returncode,repr(package.stdout),'Editor clean')
