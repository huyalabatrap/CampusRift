"""Publish P20 documentation only after the saved final evidence is complete."""
from pathlib import Path
import csv,hashlib,json
ROOT=Path(__file__).resolve().parents[1]
QA=ROOT/'task/p20'
def readjson(path):return json.loads(path.read_text(encoding='utf-8-sig'))
state=readjson(QA/'final-state.json')['data']['result']
console=readjson(QA/'final-console.json')
disk=readjson(QA/'final-disk-audit.json')
assert state['target']=='Android' and not any(state[k] for k in ['isPlaying','isPaused','isCompiling','isUpdating','sceneDirty'])
assert state['scene']=='Assets/Scenes/SampleScene.unity' and state['savedSettingsUnchanged']
assert console['data']==[]
assert all(r['matches'] for r in disk['protected']) and all(r['matchesInitial'] for r in disk['userProfile'])
assert not disk['historicalMismatches'] and not disk['settingsMismatches']
suites={'ExtendedLearning':'smoke.json','Economy':'runs/Economy.json','BreakthroughExam':'runs/BreakthroughExam/Breakthrough.json',
        'LearningCurrentUI':'learning-flow.json','HubFlow':'runs/HubFlow/HubFlow.json','HubLayout':'runs/HubLayout/HubLayout.json','Shop':'shop-smoke.json'}
summary=[]
for name,filename in suites.items():
    r=readjson(QA/filename);assert not r['failed'],(name,r['failed'])
    summary.append({'suite':name,'passed':len(r['passed']),'failed':len(r['failed']),'evidence':filename})
audits=[readjson(p) for p in (QA/'screens').glob('*audit.json')]
assert all(not a['issues'] for a in audits)
summary={'suites':summary,'passed':sum(r['passed'] for r in summary),'failed':0,'textAudits':len(audits),
         'textIssues':0,'legacyLearning':'19 passed / 2 failed; removed HUD ring; retained separately',
         'policy':'smoke only; no full regression, native build, benchmark or real Android device'}
(QA/'verification-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
pre=ROOT/'Backups/P20-pre-20261004-021044'
changes=[]
for p in (pre/'Assets').rglob('*'):
    if p.is_file():
        rel=p.relative_to(pre);current=ROOT/rel
        if not current.exists():changes.append({'file':str(rel),'change':'missing'})
        elif hashlib.sha256(p.read_bytes()).digest()!=hashlib.sha256(current.read_bytes()).digest():changes.append({'file':str(rel),'change':'modified'})
(QA/'source-change-audit.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2),encoding='utf-8')
readcsv=lambda p:list(csv.DictReader(p.open(encoding='utf-8-sig',newline='')))
old={q['cau_id']:q for q in readcsv(pre/'Content/questions.csv')};new={q['cau_id']:q for q in readcsv(ROOT/'Content/questions.csv')}
assert all(new[k]==v for k,v in old.items()) and len(new)==449
(QA/'content-change-audit.json').write_text(json.dumps({'oldQuestions':len(old),'currentQuestions':len(new),'modifiedOldQuestions':[],
    'newIds':sorted(new.keys()-old.keys()),'sources':'Content/src/ch1.txt, supplied textbook pages 12–26','instructorReview':'pending'},ensure_ascii=False,indent=2),encoding='utf-8')
phase=ROOT/'task/P20-hoc-tap-mo-rong.md';text=phase.read_text(encoding='utf-8-sig').replace('[ ]','[x]').replace('| ⬜ |','| ✅ |')
text+='''
## Kết quả 04/10/2026

Hoàn tất T01–T08 theo TEST-POLICY: smoke hiện hành PASS, 0 lỗi nội dung và biên dịch; Android/Edit Mode/SampleScene sạch. [Báo cáo](p20/REPORT-P20.md), [tiến độ](p20/PROGRESS.md), [bằng chứng](p20/verification-summary.json).

Nhóm học hiện hành: Extended156/0 + UI16/0 + Exam48/0. LearningPlayTest legacy vẫn19/2 vì phụ thuộc vòng HUD cũ đã bị ẩn; giữ raw, không tính PASS, không sửa assertion legacy. Không chạy full LearningRegressionRunner theo chính sách smoke.

9 câu mới từ nguồn giáo trình có sẵn và93 thẻ có nguồn trang; gói duyệt giảng viên [P20](../Artifacts/Content/P20/review-print.html) ⏳ chờ duyệt. Chưa test Android thật hoặc cân bằng thực chiến.
'''
phase.write_text(text,encoding='utf-8')
readme=ROOT/'task/README.md';text=readme.read_text(encoding='utf-8-sig')
oldrow='| [P20](P20-hoc-tap-mo-rong.md) | Học tập mở rộng | 4,5 | P17 | | ⬜ |'
newrow='| [P20](P20-hoc-tap-mo-rong.md) | Học tập mở rộng | 4,5 | P17 | 6 dạng/449 câu ·93 thẻ ·sổ sai/ngày/bia10 màn ·16 vật phẩm/5 pháp bảo; smoke467/0, giữ legacyLearning19/2. [Báo cáo](p20/REPORT-P20.md) · [Tiến độ](p20/PROGRESS.md) | ✅ smoke / ⏳ giảng viên |'
assert oldrow in text;readme.write_text(text.replace(oldrow,newrow),encoding='utf-8')
print(json.dumps(summary,ensure_ascii=True))
