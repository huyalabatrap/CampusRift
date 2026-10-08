from ar_nohand import *
args=[str(ADB),'-s','WGH6S8I7GIMBGQKR']
print(subprocess.check_output(args+['shell','am','start','-n','com.campusrift.game/com.unity3d.player.UnityPlayerGameActivity'],text=True,encoding='utf-8',errors='replace'))
time.sleep(2)
# Capture only the app menu, before any camera consent/AR entry.
png=subprocess.check_output(args+['exec-out','screencap','-p']);(OUT/'device-menu.png').write_bytes(png)
