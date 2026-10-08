"""Export every authored fantasy definition, including legacy assets for instructor review."""
import csv,json,pathlib,re,collections,unicodedata,yaml
out=pathlib.Path('Artifacts/Content/P17');rows=[]
types={'SkillDefinition':'skill','SkillRewardData':'skill_unlock','ItemDefinition':'item','ArtifactDefinition':'artifact','EnemyArchetype':'enemy','SkyBeastDefinition':'dragon','HeavenSwordProfile':'skill_ultimate'}
for p in pathlib.Path('Assets').rglob('*.asset'):
    text=p.read_text(encoding='utf-8-sig',errors='replace')
    typ=next((k for k in types if re.search(r'::[\w.]*\b'+k+r'\b',text)),None)
    if not typ:continue
    data=yaml.load('\n'.join(text.splitlines()[3:]),Loader=yaml.BaseLoader)['MonoBehaviour']
    rows.append(dict(kind=types[typ],id=str(data.get('id',data.get('m_Name',''))),name_vn=str(data.get('nameVi',data.get('displayNameVN',data.get('nameVN',''))) or ''),name_en=str(data.get('nameEn',data.get('displayName',data.get('nameEN',''))) or ''),path=p.as_posix()))
plan=pathlib.Path('KE_HOACH_V2_HOC_DE_THANG.md').read_text(encoding='utf-8-sig')
section=plan.split('### 7.3 Danh sách kỹ năng')[1].split('### 7.4')[0]
for number,name in re.findall(r'^\| (\d+|★) \| \*\*([^*]+)\*\*',section,re.M):
    rows.append(dict(kind='planned_skill',id='design-'+number,name_vn=name,name_en='',path='KE_HOACH_V2_HOC_DE_THANG.md §7.3 (design only; 10 skill assets implemented)'))
rows.sort(key=lambda r:(r['kind'],r['id'],r['path']))
with (out/'fantasy-names.csv').open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=['kind','id','name_vn','name_en','path']);w.writeheader();w.writerows(rows)
def normalize(s):return ''.join(c for c in unicodedata.normalize('NFD',s.lower().replace('đ','d')) if unicodedata.category(c)!='Mn')
terms=['ho chi minh','marx','lenin','mac-lenin','tu tuong','chu nghia','dang cong san','cach mang','giai cap','dan chu','dao duc','doan ket dan toc','socialism','communism','ideology']
hits=[dict(row=r,terms=[t for t in terms if t in normalize(r['id']+' '+r['name_vn']+' '+r['name_en'])]) for r in rows if any(t in normalize(r['id']+' '+r['name_vn']+' '+r['name_en']) for t in terms)]
(out/'fantasy-name-audit.json').write_text(json.dumps(dict(counts=dict(collections.Counter(r['kind'] for r in rows)),total=len(rows),high_signal_terms=terms,hits=hits,limitation='Name screening and manual review only; not instructor approval or proof of pedagogical accuracy.'),ensure_ascii=False,indent=2),encoding='utf-8')
print(collections.Counter(r['kind'] for r in rows));print('High-signal matches:',len(hits))
