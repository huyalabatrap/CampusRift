from ar_nohand import *
args=[str(ADB),'-s','WGH6S8I7GIMBGQKR']
p=subprocess.run(args+['install','-r','APK-Test/CampusRift-20261007-ar-nohand-fix.apk'],capture_output=True,text=True,encoding='utf-8',errors='replace');(OUT/'build/adb-install-after-external-removal.txt').write_text(p.stdout+p.stderr,encoding='utf-8');p.check_returncode();print(p.stdout)
print(subprocess.check_output(args+['shell','input','keyevent','224'],text=True,encoding='utf-8',errors='replace'))
print(subprocess.check_output(args+['shell','am','start','-n','com.campusrift.game/com.unity3d.player.UnityPlayerGameActivity'],text=True,encoding='utf-8',errors='replace'))
milestone('Editor đã phục hồi/handoff sạch/Console0; việc device còn tiếp tục độc lập. Sau install đầu Success, log Android ghi PACKAGE_FULLY_REMOVED và pm path trả absent trước bước mở AR. Đã cài lại cùng APK đã verify (không rebuild/rerun tests), ghi adb-install-after-external-removal.txt để hoàn thành xác nhận ready theo brief. Không lấy ảnh camera/phòng; ảnh menu thử trước AR là màn đen.')
