from ar_nohand import *
for args in [['devices'],['-s','WGH6S8I7GIMBGQKR','shell','pm','path','com.campusrift.game'],['-s','WGH6S8I7GIMBGQKR','shell','dumpsys','power']]:
 data=subprocess.check_output([str(ADB),*args],text=True,encoding='utf-8',errors='replace');print(data if args[-1]!='power' else '\n'.join(l.strip() for l in data.splitlines() if any(t in l for t in ['mWakefulness=','mDisplayPowerRequest='])))
