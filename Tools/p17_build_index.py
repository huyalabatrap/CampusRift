import pathlib,hashlib,json
root=pathlib.Path('Artifacts/P17-Builds');rows=[]
for target in ('Android','Windows'):
 for mode in ('dev','release'):
  folder=root/target/mode;p=folder/('CampusRift.apk' if target=='Android' else 'CampusRift.exe')
  summary=(folder/'build-summary.txt').read_text(encoding='utf-8-sig') if (folder/'build-summary.txt').exists() else 'No successful summary'
  files=[x for x in folder.rglob('*') if x.is_file()]
  exclude=lambda x:any('BackUpThisFolder_ButDontShipItWithYourGame' in s or s=='P17-Smoke' for s in x.relative_to(folder).parts) or x.parent==folder and x.suffix in ('.txt','.json','.log')
  rows.append(dict(target=target,mode=mode,file=str(p),exists=p.exists(),fileBytes=p.stat().st_size if p.exists() else 0,folderBytes=sum(x.stat().st_size for x in files),distributionBytes=sum(x.stat().st_size for x in files if not exclude(x)),sha256=hashlib.file_digest(p.open('rb'),'sha256').hexdigest() if p.exists() else None,summary=summary))
(root/'build-index.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
lines=['# P17 · Build thử MVP — 2026-10-03','','| Nền tảng | Bản | File | File bytes | Gói phân phối bytes | Kết quả |','|---|---|---|---:|---:|---|']
for r in rows:
 rel=pathlib.Path(r['file']).relative_to(root).as_posix();result='Succeeded' if 'Result: Succeeded' in r['summary'] else 'Chưa đạt — xem summary'
 lines.append(f'| {r["target"]} | {r["mode"]} | [{pathlib.Path(r["file"]).name}]({rel}) | {r["fileBytes"]:,} | {r["distributionBytes"]:,} | {result} |')
lines+=['','Android: IL2CPP ARM64, API26 tối thiểu, ký debug nội bộ; dev cho profiler, release không DEV/harness. SDK/NDK/JDK có; `adb devices` không có thiết bị, chưa cài/chạy APK trên điện thoại. Mục tiêu ≥30 FPS vẫn ⏳. Package verification và SHA256 trong thư mục từng APK. Tools/verify_android_apk.py cũ kiểm khóa học Algorithms; giữ nguyên và dùng Tools/p17_verify_packages.py kiểm gói TTHCM hiện tại.','',
'Windows: phân phối EXE cùng Data, MonoBleedingEdge, UnityPlayer.dll và các thư mục runtime đi kèm. Loại BackUpThisFolder_ButDontShipItWithYourGame, P17-Smoke, log và báo cáo QA khỏi gói gửi người chơi. Cột gói phân phối tính các file runtime; folderBytes trong JSON gồm cả QA/backup. Smoke dev native PASS: Library/quiz/mua Recovery Pill theo luật runtime, Sảnh + màn1; QA save riêng trong P17-Smoke/player-profile.json. Mở lại persistence PASS. Ảnh màn1 chụp khoảng3s, trước intro2,5s + delay0,25s + portal0,35s nên Spawned=0; không tuyên bố kiểm combat native. Đây là tự động, không thay novice test. Release chạy6s/đóng bằng smoke, không QA helper. Package flags và log không có exception; URP shader GaussianDepthOfField not supported là cảnh báo còn lại.','',
'Không phát hành lên store/server. Ghi chú và giới hạn: [Release-Notes-MVP](../V2/Release-Notes-MVP.md). Cần người chơi thử, điện thoại và giảng viên để xác nhận Mốc5.','']
for r in rows:
 lines+=['## '+r['target']+' '+r['mode'],'','```text',r['summary'].strip(),'```','']
(root/'BUILD-INDEX.md').write_text('\n'.join(lines),encoding='utf-8')
print(json.dumps([{k:r[k] for k in ('target','mode','fileBytes','folderBytes','exists')} for r in rows]))
