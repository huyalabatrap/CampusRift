import pathlib,hashlib,json,re
root=pathlib.Path('.');backup=pathlib.Path((root/'task/p17/BACKUP.txt').read_text(encoding='utf-8-sig').strip())
def digest(p):return hashlib.file_digest(p.open('rb'),'sha256').hexdigest()
protected=[]
for name in ('Assets/Scenes/SampleScene.unity','Assets/Scenes/MainMenu.unity','Assets/MonsterShaban/CampusNavMesh.asset'):
 p=root/name;b=backup/name
 protected.append(dict(path=name,current=digest(p),backup=digest(b),identical=p.read_bytes()==b.read_bytes()))
assert all(x['identical'] for x in protected),'Protected scene/NavMesh changed'
changed=[];harness=[]
for b in (backup/'Assets').rglob('*.cs'):
 rel=b.relative_to(backup);p=root/rel
 if not p.exists():continue
 if digest(b)==digest(p):continue
 row=dict(path=str(rel),before=digest(b),after=digest(p));changed.append(row)
 if any(s in p.name for s in ('PlayTest','TestWorld','Bench','Validation')):
  old=b.read_text(encoding='utf-8-sig');new=p.read_text(encoding='utf-8-sig')
  strip=lambda s:re.sub(r'^\s*#(?:if|elif|else|endif).*$', '',s,flags=re.M).replace('\r','').strip()
  row=dict(row,assertion_body_unchanged=strip(old)==strip(new));harness.append(row)
assert all(x['assertion_body_unchanged'] for x in harness),'Existing harness body changed'
report=dict(protected=protected,changed_cs=changed,harness_changes=harness)
(root/'task/p17/change-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('Protected identical:',len(protected),'changed CS:',len(changed),'guard-only harness changes:',len(harness))
