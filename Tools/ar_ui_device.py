from ar_ui import *
args=[str(ADB),'-s','WGH6S8I7GIMBGQKR'];apk=ROOT/'APK-Test/CampusRift-20261007-ar-ui-fix.apk'
devices=subprocess.check_output([str(ADB),'devices'],text=True,encoding='utf-8');connected='WGH6S8I7GIMBGQKR\tdevice' in devices
result={'serial':'WGH6S8I7GIMBGQKR','devices':devices,'connected':connected,'apk':apk.name,'at':time.strftime('%Y-%m-%d %H:%M:%S')}
save('build/device-install-started.json',result)
if connected:
 try:
  p=subprocess.run(args+['install','-r',str(apk)],capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=60);result.update(installReturncode=p.returncode,installOutput=p.stdout+p.stderr,installSuccess=p.returncode==0 and 'Success' in p.stdout)
 except subprocess.TimeoutExpired as e:result.update(installSuccess=False,installTimeout=True,installOutput=str(e.stdout or '')+str(e.stderr or ''))
 p=subprocess.run(args+['shell','pm','list','packages'],capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=15)
 result.update(pmReturncode=p.returncode,pmCampus='\n'.join(l for l in p.stdout.splitlines() if 'campus' in l.lower()),pmError=p.stderr)
 result['packageListed']='package:com.campusrift.game' in result['pmCampus']
 (OUT/'build/adb-install.txt').write_text(result['installOutput'],encoding='utf-8');(OUT/'build/pm-campus.txt').write_text(result['pmCampus']+result['pmError'],encoding='utf-8')
else:result.update(skipped=True,reason='Target device absent in adb devices')
save('build/device-install.json',result)
milestone('Mốc máy: '+('adb install-r hoàn tất='+str(result.get('installSuccess'))+'; pm list packages lọc campus='+repr(result.get('pmCampus'))+'. Không chờ mở khóa, không hỏi người dùng hay cài lặp.' if connected else 'Không thấy WGH6S8I7GIMBGQKR trong adb devices; bỏ qua cài đặt theo brief.'))
print(json.dumps(result,ensure_ascii=True),flush=True)
