from pathlib import Path
import re
paths=[]
for p in Path('Assets/Skills').rglob('*.cs'):
 if '/Runtime/' not in p.as_posix():continue
 s=p.read_text(encoding='utf-8-sig')
 if 'Color Accent' not in s:continue
 updated=re.sub(r'(public (?:override|virtual) Color Accent\s*=>\s*)(new Color\([^;]+\))',r'\1CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : \2',s)
 if updated!=s:p.write_text(updated,encoding='utf-8');paths.append(p.as_posix())
Path('task/p23/accent-changes.json').write_text(__import__('json').dumps(paths,indent=2),encoding='utf-8');print(len(paths),'skill accents use alternative palette')
