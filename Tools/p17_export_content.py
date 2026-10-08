"""Print-ready reviewer packet from the authoring CSVs; does not change course content."""
from pathlib import Path
import csv,html,re,json,shutil
out=Path('Artifacts/Content/P17');out.mkdir(parents=True,exist_ok=True)
def read(name):return list(csv.DictReader(Path('Content',name+'.csv').open(encoding='utf-8-sig',newline='')))
lessons=read('lessons');questions=read('questions');chapters=read('chapters');missing=[]
for r in lessons:
    sources=re.findall(r'\((?:Giáo trình|GT)\s*2021,\s*tr\.[^)]+\)',r['noi_dung_trang'])
    r['nguon']='; '.join(sources)
    if not sources:missing.append(r['bai_id']+'/'+r['trang_so'])
with (out/'lessons-review.csv').open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=list(lessons[0]));w.writeheader();w.writerows(lessons)
shutil.copy2('Content/questions.csv',out/'questions-review.csv');shutil.copy2('Content/chapters.csv',out/'chapters-review.csv')
parts=['<!doctype html><html lang="vi"><meta charset="utf-8"><title>Duyệt nội dung Campus Rift</title><style>body{font:14px/1.6 Georgia,serif;max-width:960px;margin:36px auto;color:#151515}h1,h2,h3{font-family:Arial,sans-serif}h2{break-before:page}.q{break-inside:avoid;border-top:1px solid #bbb;padding:12px 0}pre{white-space:pre-wrap;font:inherit}.source{color:#444} @media print{body{max-width:none;margin:0}h2{page-break-before:always}a{color:inherit}}</style><body><h1>Campus Rift — bản gửi giảng viên duyệt</h1><p>03/10/2026 · 6 chương / 21 bài / 93 trang đọc / 440 câu. Chờ giảng viên xác nhận; chưa được xem là nội dung đã duyệt.</p><p>Nội dung môn học chỉ ở Thư Viện; phần chiến đấu tu tiên là hư cấu. Nguồn trang dưới mỗi bài/câu phục vụ đối chiếu giáo trình Tư tưởng Hồ Chí Minh 2021.</p>']
for c in chapters:
    n=c['chuong'];parts.append('<h2>Chương '+n+' — '+html.escape(c['ten_chuong'])+'</h2>')
    for r in [x for x in lessons if x['chuong']==n]:
        parts.append('<h3>'+html.escape(r['bai_id']+' — '+r['bai_ten']+' / Trang đọc '+r['trang_so']+': '+r['tieu_de_trang'])+'</h3><pre>'+html.escape(r['noi_dung_trang'])+'</pre><p><b>Ghi nhớ:</b> '+html.escape(r['ghi_nho'])+'</p><p class="source">Nguồn: '+html.escape(r['nguon'])+'</p>')
    for q in [x for x in questions if x['bai_id'].startswith('ch'+n)]:
        choices=''.join('<li>'+k+'. '+html.escape(q[k])+'</li>' for k in 'ABCD' if q[k])
        parts.append('<div class="q"><b>'+html.escape(q['cau_id']+' / '+q['loai'])+'</b><p>'+html.escape(q['cau_hoi'])+'</p><ul>'+choices+'</ul><p><b>Đáp án:</b> '+html.escape(q['dap_an'])+'</p><p>'+html.escape(q['giai_thich'])+'</p><p class="source">'+html.escape(q['nguon'])+'</p></div>')
parts.append('</body></html>');(out/'review-print.html').write_text('\n'.join(parts),encoding='utf-8')
packet={'chapters':len(chapters),'lessons':len(set(r['bai_id'] for r in lessons)),'pages':len(lessons),'questions':len(questions),'missingLessonSource':missing,'missingQuestionSource':[q['cau_id'] for q in questions if not q['nguon'].strip()], 'status':'awaiting instructor review'}
(out/'export-manifest.json').write_text(json.dumps(packet,ensure_ascii=False,indent=2),encoding='utf-8')
Path('Artifacts/Content/Review-2026-10-03.md').write_text('''# Biên bản duyệt nội dung — ⏳ chờ giảng viên

Người duyệt/chức vụ: __________  Ngày duyệt: __________  Giáo trình/ấn bản đối chiếu: __________

Gói duyệt: [bản dễ in](P17/review-print.html), [93 trang bài học](P17/lessons-review.csv), [440 câu](P17/questions-review.csv), [tên hư cấu](P17/fantasy-names.csv).

| Checklist §11.5 | Giảng viên xác nhận | Góp ý/ID bài hoặc câu |
|---|---|---|
| Đúng giáo trình chính thức, nguồn trang đúng | Chờ | |
| Tách lớp học thật và chiến đấu hư cấu, không hình ảnh lãnh tụ trong combat | Chờ | |
| Tên kỹ năng/vật phẩm/quái/cảnh giới không dùng tên/trích dẫn/khái niệm môn học | Chờ | |
| Câu chữ trang trọng, đáp án và giải thích rõ | Chờ | |
| Ghi công/giấy phép nội dung hình/âm thanh được cung cấp | Chờ | |

Danh sách sửa: ID · nội dung trước · đề nghị · nội dung sau · người xác nhận. Sau góp ý, sửa Content/src/chN.txt → python Tools/content_build.py → Import Course CSV → Validate Content; xuất lại gói duyệt. Không đổi ID câu/bài đang dùng.

Kết luận giảng viên: Chưa duyệt / Duyệt có điều kiện / Đồng ý phát hành thử (chọn một).
Chữ ký/xác nhận bằng văn bản: __________
''',encoding='utf-8')
print(json.dumps(packet,ensure_ascii=True))
