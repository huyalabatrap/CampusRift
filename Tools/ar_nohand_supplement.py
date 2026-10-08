from ar_nohand import *
import re,collections,statistics,zipfile
src=ROOT/'task/ar/device-logs/nohand-live.txt'
gesture=[];diag=[];native=[];shaderErrors=0
with src.open(encoding='utf-8',errors='replace') as f:
 for line in f:
  if '[ARGesture]' in line:gesture.append(line.strip())
  if '[ARDiag]' in line:diag.append(line.strip())
  if 'ArgumentNullException' in line:shaderErrors+=1
  if '(10495)' in line and any(s in line for s in ['gesture_recognizer_graph','scheduler.cc','JNIEnv','Created TensorFlow','TfLite','delegate','Delegate']):native.append(line.strip())
def values(pattern,rows):
 return [float(m.group(1).replace(',','.')) for l in rows if (m:=re.search(pattern,l))]
def stats(vals):return dict(n=len(vals),min=min(vals),median=statistics.median(vals),max=max(vals)) if vals else None
res=dict(source=str(src.relative_to(ROOT)),gestureCount=len(gesture),diagnosticCount=len(diag),decisions=dict(collections.Counter(re.search(r'decision=(\S+)',l).group(1) for l in gesture if 'decision=' in l)),geometries=dict(collections.Counter(re.search(r'geometry=(\S+)',l).group(1) for l in gesture if 'geometry=' in l)),hz=stats(values(r'gestureHz=([\d.,]+)',diag)),latency=stats(values(r'latencyMs=([\d.,]+)',diag)),shaderExceptions=shaderErrors,nativeRelevant=native,gestureExamples=gesture[:3]+gesture[-3:],diagnosticExamples=diag[:3]+diag[-3:])
save('supplement-log-analysis.json',res)
shaderRefs=[]
for p in (ROOT/'Assets').rglob('*.cs'):
 if 'Editor' in p.parts or 'Validation' in p.parts:continue
 for i,line in enumerate(p.read_text(encoding='utf-8-sig',errors='replace').splitlines()):
  if 'Shader.Find(' in line:shaderRefs.append(dict(file=p.relative_to(ROOT).as_posix(),line=i+1,source=line.strip()))
with zipfile.ZipFile(ROOT/'APK-Test/CampusRift-20261007-ar-nohand-fix.apk') as z:
 data=z.read('assets/bin/Data/globalgamemanagers')
 names=['Universal Render Pipeline/Unlit','Universal Render Pipeline/Lit','Campus Rift/AR/Runic Circle','Campus Rift/Heaven Sword Hero','Campus Rift/Heaven Sword Aura']
 preserved={name:name.encode() in data for name in names}
save('shader-find-audit.json',dict(runtimeReferences=shaderRefs,finalApkRegisteredShaders=preserved,editorReferences='ARRiftSetup:52 and ARRiftMilestones:24,66 execute only in Editor'))
print(json.dumps({k:v for k,v in res.items() if k not in ['nativeRelevant','gestureExamples','diagnosticExamples']},ensure_ascii=True))
print('Registered shaders:',preserved)
print('\n'.join(native[:12]+native[-6:]))
milestone('Đã đọc bổ sung NOHAND-DEVICE-FINDINGS và phân tích toàn bộ nohand-live bằng streaming: nohand/supplement-log-analysis.json; geometry=None chưa đủ chứng minh world thiếu/hỏng, diagnostics mới giữ norm/world + motion.reason để phân biệt. Hz/latency và lifecycle CPU/GPU được lưu; không nới capture gap/.6/3frame/100ms. Audit mọi Shader.Find runtime AR và kiểm tên shader đăng ký APK: nohand/shader-find-audit.json. Realme đang khóa, InstallGuide chờ; đã yêu cầu unlock, không install lặp hay bypass lock.')
