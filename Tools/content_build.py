"""Builds Content/chapters.csv, lessons.csv and questions.csv from the authoring files Content/src/chN.txt.

Authoring format (UTF-8, one file per chapter):
  #chapter <n> | <chapter title>
  #lesson <lesson_id> | <lesson title>
  #page <page title>            (text follows on the next lines until the next # line)
  #note <text>                  (optional "Ghi nho" for the page just written)
  #q id | lesson_id or chN | type | difficulty | question | A | B | C | D | answer | explanation | source
     type: mot-dap-an | dung-sai | sap-xep | nhieu-dap-an | noi-cap | dien-khuyet.
     For nhieu-dap-an: answer A,B. For noi-cap: A/B are left a/b, C/D are right 1/2; answer a:1,b:2.
     For dien-khuyet: prompt contains ___ and answer is one option. For dung-sai leave A-D empty and answer dung/sai.
     For sap-xep answer lists the letters in the right order, e.g. B,D,A,C.
Usage: python Tools/content_build.py
"""
import csv, glob, io, os, re, sys

root = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
src = os.path.join(root, 'Content', 'src')
chapters, lessons, questions = [], [], []
errors = []
for path in sorted(glob.glob(os.path.join(src, 'ch*.txt'))):
    name = os.path.basename(path)
    chapter = None; lesson = None; page = None; lines = []
    def flush():
        global page, lines
        if page is not None:
            text = '\n'.join(lines).strip()
            page['text'] = text
            lessons.append(page)
        page = None; lines = []
    for n, raw in enumerate(io.open(path, encoding='utf-8').read().split('\n'), 1):
        line = raw.rstrip('\r')
        if line.startswith('#chapter '):
            flush(); a = line[9:].split(' | ', 1)
            chapter = int(a[0]); chapters.append([chapter, a[1].strip()])
        elif line.startswith('#lesson '):
            flush(); a = line[8:].split(' | ', 1); lesson = {'id': a[0].strip(), 'title': a[1].strip(), 'pages': 0}
        elif line.startswith('#page'):
            flush(); lesson['pages'] += 1
            page = {'chapter': chapter, 'id': lesson['id'], 'title': lesson['title'], 'no': lesson['pages'], 'ptitle': line[5:].strip(), 'note': ''}
        elif line.startswith('#note '):
            page['note'] = line[6:].strip()
        elif line.startswith('#q '):
            flush(); f = line[3:].split(' | ')
            if len(f) == 11 and f[2].strip() == 'sap-xep': f.insert(8, '')  # ordering with three items: empty D
            if len(f) != 12: errors.append('%s:%d: %d fields instead of 12' % (name, n, len(f))); continue
            questions.append([x.strip() for x in f])
        elif line.startswith('#'):
            errors.append('%s:%d: unknown directive' % (name, n))
        elif page is not None:
            lines.append(line)
    flush()
if errors:
    print('\n'.join(errors)); sys.exit(1)

def write(fname, header, rows):
    with io.open(os.path.join(root, 'Content', fname), 'w', encoding='utf-8', newline='') as fh:
        w = csv.writer(fh, lineterminator='\n'); w.writerow(header); w.writerows(rows)
write('chapters.csv', ['chuong', 'ten_chuong'], chapters)
write('lessons.csv', ['chuong', 'bai_id', 'bai_ten', 'trang_so', 'noi_dung_trang', 'ghi_nho', 'tieu_de_trang'],
      [[p['chapter'], p['id'], p['title'], p['no'], p['text'], p['note'], p['ptitle']] for p in lessons])
write('questions.csv', ['cau_id', 'bai_id', 'loai', 'do_kho', 'cau_hoi', 'A', 'B', 'C', 'D', 'dap_an', 'giai_thich', 'nguon'], questions)
print('chapters %d, pages %d, questions %d' % (len(chapters), len(lessons), len(questions)))
