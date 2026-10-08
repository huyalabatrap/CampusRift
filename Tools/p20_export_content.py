"""Current instructor packet; preserve the P17 historical export."""
from pathlib import Path
import csv,html,re,json,shutil,collections
out=Path('Artifacts/Content/P20');out.mkdir(parents=True,exist_ok=True)
def read(name):return list(csv.DictReader(Path('Content',name+'.csv').open(encoding='utf-8-sig',newline='')))
def esc(s):return html.escape(s)
chapters=read('chapters');lessons=read('lessons');questions=read('questions');cards=read('flashcards')
for r in lessons:r['nguon']='; '.join(re.findall(r'\((?:Giáo trình|GT)\s*2021,\s*tr\.[^)]+\)',r['noi_dung_trang']))
for name,rows in [('chapters',chapters),('lessons',lessons),('questions',questions),('flashcards',cards)]:
    with (out/(name+'-review.csv')).open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
parts=['<!doctype html><html lang="vi"><meta charset="utf-8"><title>P20 · Duyệt nội dung Campus Rift</title><style>body{font:15px/1.6 Georgia,serif;max-width:960px;margin:36px auto;color:#151515}h1,h2,h3{font-family:Arial,sans-serif}h2{break-before:page}.q{break-inside:avoid;border-top:1px solid #bbb;padding:12px 0}pre{white-space:pre-wrap;font:inherit}.source{color:#444}@media print{body{max-width:none;margin:0}a{color:inherit}}</style><body><h1>Campus Rift — P20 · ⏳ chờ giảng viên duyệt</h1><p>04/10/2026 · 6 chương / 21 bài / 93 trang đọc / 449 câu (9 câu mới) / 93 thẻ ghi nhớ. Chưa có xác nhận học thuật của giảng viên.</p><p>Nguồn duy nhất: giáo trình Tư tưởng Hồ Chí Minh 2021 đã cung cấp trong Content. Trang bài học và thẻ giữ nguyên văn bản Ghi nhớ. Phần chiến đấu tu tiên là hư cấu.</p><p>Dạng nối cặp: A/B → a/b (cột trái); C/D → 1/2 (cột phải). Đáp án lưu a:1,b:2. Nhiều đáp án phải chọn đúng và đủ; điền khuyết chọn cụm từ, không nhập tự do.</p>']
for c in chapters:
    n=c['chuong'];parts.append('<h2>Chương '+n+' — '+esc(c['ten_chuong'])+'</h2>')
    for r in [x for x in lessons if x['chuong']==n]:
        parts.append('<h3>'+esc(r['bai_id']+' · '+r['bai_ten']+' / Trang đọc '+r['trang_so']+': '+r['tieu_de_trang'])+'</h3><pre>'+esc(r['noi_dung_trang'])+'</pre><p><b>Ghi nhớ:</b> '+esc(r['ghi_nho'])+'</p><p class="source">'+esc(r['nguon'])+'</p>')
    for q in [x for x in questions if re.match('ch'+n+r'(?:-|$)',x['bai_id'])]:
        labels={'A':'a (trái)','B':'b (trái)','C':'1 (phải)','D':'2 (phải)'} if q['loai']=='noi-cap' else {k:k for k in 'ABCD'}
        choices=''.join('<li>'+labels[k]+'. '+esc(q[k])+'</li>' for k in 'ABCD' if q[k])
        if q['loai']=='dung-sai':choices='<li>Đúng</li><li>Sai</li>'
        parts.append('<div class="q"><b>'+esc(q['cau_id']+' / '+q['loai'])+'</b><p>'+esc(q['cau_hoi'])+'</p><ul>'+choices+'</ul><p><b>Đáp án:</b> '+esc(q['dap_an'])+'</p><p>'+esc(q['giai_thich'])+'</p><p class="source">'+esc(q['nguon'])+'</p></div>')
    parts.append('<h3>Thẻ ghi nhớ</h3>')
    for r in [x for x in cards if x['bai_id'].startswith('ch'+n+'-')]:
        parts.append('<div class="q"><b>'+esc(r['id'])+' · Mặt trước: '+esc(r['mat_truoc'])+'</b><p>Mặt sau: '+esc(r['mat_sau'])+'</p><p class="source">'+esc(r['nguon'])+'</p></div>')
parts.append('</body></html>');(out/'review-print.html').write_text('\n'.join(parts),encoding='utf-8')
old=list(csv.DictReader(Path('Artifacts/Content/P17/fantasy-names.csv').open(encoding='utf-8-sig')))
known={r['path'] for r in old}
for p in Path('Assets/Progression/Data').rglob('*.asset'):
    text=p.read_text(encoding='utf-8-sig')
    if p.as_posix() in known or not re.search(r'::CampusRift.Progression.(ItemDefinition|ArtifactDefinition)',text):continue
    import yaml
    data=yaml.load('\n'.join(text.splitlines()[3:]),Loader=yaml.BaseLoader)['MonoBehaviour']
    old.append(dict(kind='artifact' if 'ArtifactDefinition' in text else 'item',id=data['id'],name_vn=data.get('nameVN',''),name_en=data.get('nameEN',''),path=p.as_posix()))
with (out/'fantasy-names.csv').open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=['kind','id','name_vn','name_en','path']);w.writeheader();w.writerows(old)
packet={'chapters':len(chapters),'lessons':len({r['bai_id'] for r in lessons}),'pages':len(lessons),'questions':len(questions),'newQuestions':9,'questionTypes':dict(collections.Counter(q['loai'] for q in questions)),'flashcards':len(cards),'missingLessonSource':[r['bai_id']+'/'+r['trang_so'] for r in lessons if not r['nguon']],'missingQuestionSource':[q['cau_id'] for q in questions if not q['nguon']],'missingCardSource':[r['id'] for r in cards if not r['nguon']],'status':'awaiting instructor review'}
(out/'export-manifest.json').write_text(json.dumps(packet,ensure_ascii=False,indent=2),encoding='utf-8')
Path('Artifacts/Content/Review-P20-2026-10-04.md').write_text('''# P20 · Biên bản duyệt nội dung — ⏳ chờ giảng viên

Người duyệt/chức vụ: __________ · Ngày: __________ · Ấn bản đối chiếu: __________

Gói hiện hành: [bản dễ in](P20/review-print.html), [93 trang](P20/lessons-review.csv), [449 câu / 6 dạng](P20/questions-review.csv), [93 thẻ](P20/flashcards-review.csv), [tên hư cấu](P20/fantasy-names.csv).

| Checklist §11.5 | Xác nhận | Góp ý/ID |
|---|---|---|
| Đúng giáo trình, nguồn trang, đáp án và giải thích | Chờ | |
| 9 câu mới: nhiều đáp án / nối cặp / điền khuyết | Chờ | |
| 93 thẻ ghi nhớ đúng Ghi nhớ và nguồn | Chờ | |
| Tách môn học và chiến đấu; tên hư cấu phù hợp | Chờ | |
| Ghi công nội dung, hình ảnh, âm thanh | Chờ | |

Góp ý: ID · trước · đề nghị · sau · người xác nhận. Sửa Content/src/chN.txt → python Tools/p20_content.py → Import Course CSV → Validate Content → python Tools/p20_export_content.py. Giữ ID ổn định.

Kết luận: Chưa duyệt / Duyệt có điều kiện / Đồng ý phát hành thử. Chữ ký/xác nhận: __________
''',encoding='utf-8')
print(json.dumps(packet,ensure_ascii=True))
