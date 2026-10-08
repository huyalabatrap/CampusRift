from ar_nohand import *
import re
old=ROOT/'task/ar/device-logs/nohand-realme-final.txt';dest=ROOT/'task/ar/device-logs/nohand-realme.txt';shutil.copy2(old,dest)
lines=old.read_text(encoding='utf-8').splitlines();valid=[l for l in lines if '[ARGesture]' in l and 'model=Open_Palm' in l and 'decision=dynamic-motion' in l]
summary=dict(blockedOpenPalm=valid[-20:],errors=[l for l in lines if any(t in l for t in ['Exception','onError','native-error'])][-15:])
save('device-before-fix-summary.json',summary)
log=subprocess.check_output([str(ADB),'-s','WGH6S8I7GIMBGQKR','logcat','-d','-v','time','-s','Unity'],text=True,encoding='utf-8',errors='replace')
(ROOT/'task/ar/device-logs/nohand-realme-live.txt').write_text(log,encoding='utf-8')
print('\n'.join([l for l in log.splitlines() if '[ARDiag]' in l or '[ARGesture]' in l][-5:]))
print(subprocess.check_output([str(ADB),'-s','WGH6S8I7GIMBGQKR','shell','dumpsys','activity','activities'],text=True,encoding='utf-8',errors='replace').split('mResumedActivity:')[-1][:300])
milestone('Realme được cắm trong lúc build/verify; adb install -r Success và launch mới PID18206. Đã lấy log cũ trước install vào device-logs/nohand-realme.txt: model Open_Palm .74/.83/.81 nhưng decision=dynamic-motion; chứng minh model có nhận tay, blocker ngăn D1, không phải MediaPipe mất hoàn toàn. Log cũ không có motion.reason nên chưa phân biệt mọi frame do Cancel/pinch/swipe/gap. native-comparison model+MPJNI byte-identical. Đang vào AR bản mới để xác nhận ready như brief, chưa làm gesture thật.')
