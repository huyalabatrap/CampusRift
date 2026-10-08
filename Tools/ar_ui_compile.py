from ar_ui import *
connect()
state=code((ROOT/'Tools/tech2_handoff.cs').read_text(encoding='utf-8'));save('compile-state.json',state)
console=call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True});save('compile-console.json',console)
print(json.dumps({'state':state,'console':console},ensure_ascii=True))
assert not state['compiling'] and not console.get('data')
rows=[]
for p in (ROOT/'Assets/ARRift').rglob('*'):
 if not p.is_file() or p.suffix not in ['.cs','.xml']:continue
 rel=p.relative_to(ROOT);before=BACKUP/rel
 if not before.exists() or p.read_bytes()!=before.read_bytes():
  rows.append({'path':rel.as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
  dest=OUT/'source-diffs'/rel.with_suffix(rel.suffix+'.diff');dest.parent.mkdir(parents=True,exist_ok=True)
  dest.write_text(''.join(difflib.unified_diff(before.read_text(encoding='utf-8-sig').splitlines(True) if before.exists() else [],p.read_text(encoding='utf-8-sig').splitlines(True),fromfile='before/'+rel.as_posix(),tofile='after/'+rel.as_posix())),encoding='utf-8')
save('sources.json',rows)
milestone('Mốc nguồn và compile: panel Công nghệ 1040×880, scroll 6 hàng + chú thích, backdrop và ẩn HUD; Công nghệ/clip vào ☰, chỉ hiện chấm đỏ+dừng khi quay. ARDiag mặc định ẩn kể cả native error, menu/giữ2s, 560×420 bán trong suốt font≤14, thu gọn/đóng và vùng nội dung không chặn chạm; log giữ đủ. Tắt development console; link.xml giữ Sphere/Capsule/Box/MeshCollider, Physics package đã bật. HUD phụ dùng ModalOpen, Thu mb_Up dịch dưới rail, chữ autosize, hướng dẫn tay chuyển sang trái. Compile sạch Console0, không chạy suite. Tiếp ảnh hai độ phân giải, kiểm layout trực tiếp và build.')
