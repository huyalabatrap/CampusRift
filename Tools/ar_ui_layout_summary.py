from ar_ui import *
from PIL import Image
issues=[];rows=[]
required=['training','defense','technology','technology-details','menu','diagnostics','diagnostics-collapsed','placement','mode-selection']
for name in required:
 for w,h in [(2400,1080),(1600,720)]:
  p=OUT/'views'/f'{name}-{w}.json';png=ROOT/'task/ar/screens/ui-fix'/f'{name}-{w}x{h}.png'
  if not p.exists() or not png.exists():issues.append(f'Missing {name}-{w}');continue
  data=json.loads(p.read_text(encoding='utf-8'));size=Image.open(png).size
  if size!=(w,h) or (data['width'],data['height'])!=(w,h):issues.append(f'Wrong resolution {name}-{w}: {size}')
  overflow=[t for t in data['texts'] if t['text'] and t['isTextOverflowing']]
  if overflow:issues.append({'view':name+'-'+str(w),'overflow':overflow})
  outside=[];safe=data['safe'];viewport=next((r['rect'] for r in data['rects'] if r['name']=='Technology scroll viewport'),None)
  if name=='technology':
   toggles=[r for r in data['rects'] if r['name'].startswith('Toggle technology')]
   if len(toggles)!=6 or any(r['rect'][0]<viewport[0]-.5 or r['rect'][1]<viewport[1]-.5 or r['rect'][2]>viewport[2]+.5 or r['rect'][3]>viewport[3]+.5 for r in toggles):issues.append(f'Six toggle circles not fully visible {name}-{w}')
  if name=='technology-details':
   detail=[t for t in data['texts'] if t['text'] and t['text'].startswith(('2 tay:','Depth:'))]
   if len(detail)!=2 or any(t['rect'][1]<viewport[1]-.5 or t['rect'][3]>viewport[3]+.5 for t in detail):issues.append(f'Technology notes not reachable in viewport {name}-{w}')
  if name=='diagnostics' and any(t['font']>14.01 for t in data['texts'] if t['path']=='ARDiag'):issues.append(f'Diagnostic font too large {w}')
  for t in data['texts']:
   if not t['text']:continue
   r=t['rect']
   # Scroll content is deliberately clipped by RectMask2D; long content remains reachable.
   if name.startswith('technology') and (t['path']=='Technology scroll content' or t['path'].startswith('Toggle technology')):continue
   if r[0]<safe[0]-.5 or r[1]<safe[1]-.5 or r[2]>safe[2]+.5 or r[3]>safe[3]+.5:outside.append(t)
  if outside:issues.append({'view':name+'-'+str(w),'outside':outside})
  if data['consoleVisible'] or data['consoleEnabled']:issues.append(f'Development console enabled {name}-{w}')
  if name in ['training','defense'] and data['hudPercent']>12.001:issues.append(f'HUD >12% {name}-{w}')
  rows.append({'view':name,'width':w,'height':h,'hudPercent':data['hudPercent'],'textCount':len(data['texts']),'overflowCount':len(overflow),'outsideCount':len(outside)})
for name in ['training','defense']:
 p=OUT/f'visual-console-{name}.json'
 if not p.exists() or json.loads(p.read_text(encoding='utf-8')).get('data'):issues.append('Visual console errors '+name)
save('layout-summary.json',{'issues':issues,'views':rows,'scope':'Direct screenshot/layout review only; no regression suites invoked. Rectangular occupied regions include combat/tech/space/study HUD; modal and explicit dev diagnostics are outside combat budget.'})
print(json.dumps({'issues':issues,'views':rows},ensure_ascii=True),flush=True)
if not issues:milestone('Mốc duyệt ảnh: 18 ảnh PNG đúng2400×1080/1600×720 cho Làm quen, Thủ Trận, Công nghệ đầu/cuộn cuối, ☰, ARDiag mở/thu gọn, chọn mode và đặt trận; layout-summary không overflow hoặc ra safeArea ở text hiển thị, Console0. Scroll content bị mask có chủ đích và cuộn tới được. HUD gameplay đo trực tiếp ≤12%, các panel modal/diagnostic tách khỏi combat budget. Nguồn CanvasGroup sửa null theo Unity semantics đã sạch, không chạy hồi quy. Tiếp build APK.')
assert not issues
