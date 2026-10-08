from ar_nohand import *
log=(ROOT/'task/ar/device-logs/nohand-realme-live.txt').read_text(encoding='utf-8').splitlines();print('\n'.join([l for l in log if '(18206)' in l][-70:]))
for service in ['window','activity']:
 data=subprocess.check_output([str(ADB),'-s','WGH6S8I7GIMBGQKR','shell','dumpsys',service],text=True,encoding='utf-8',errors='replace');print('\n'.join(l.strip() for l in data.splitlines() if any(t in l for t in ['mCurrentFocus','mFocusedApp','ResumedActivity','com.campusrift.game'])))
