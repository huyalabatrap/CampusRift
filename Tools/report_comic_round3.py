from pathlib import Path
import json,hashlib,re
from PIL import Image,ImageDraw,ImageFont,ImageOps
import numpy as np
root=Path('task/ui-comic');tests=root/'tests/round3';screens=root/'screens/round3'
read=lambda p:json.loads(Path(p).read_text(encoding='utf-8-sig'))
before=read('Backups/ComicUI-round2-pre-round3/manifest.json')
changed=[p for p,h in before.items() if Path(p).exists() and hashlib.sha256(Path(p).read_bytes()).hexdigest()!=h]
created=[]
for group in ['Assets/CampusRiftUI','Assets/Resources/ContentImages','Tools']:
    for p in Path(group).rglob('*'):
        if p.is_file() and p.as_posix() not in before:created.append(p.as_posix())
def scene3d(path):
    text=Path(path).read_text(encoding='utf-8-sig');entries={}
    for part in re.split(r'(?m)^--- !u!',text)[1:]:
        match=re.match(r'(\d+) &(\d+)\s*\n',part)
        if match and int(match[1]) in [4,23,33,54,65,195,196]:entries[match[0].strip()]=part
    return entries
old=scene3d('Backups/ComicUI-round2-pre-round3/Assets/Scenes/SampleScene.unity');new=scene3d('Assets/Scenes/SampleScene.unity')
geo={'unchanged':old==new,'components':len(old),'changedKeys':[k for k in set(old)|set(new) if old.get(k)!=new.get(k)],'levelsUnchanged':all(hashlib.sha256(Path(p).read_bytes()).hexdigest()==h for p,h in before.items() if p.startswith('Assets/Levels/') and Path(p).is_file())}
(tests/'Geometry.json').write_text(json.dumps(geo,indent=2),encoding='utf-8');assert geo['unchanged'] and geo['levelsUnchanged'],geo
brightness=[]
for i in range(1,11):
    p=screens/'levels'/f'level-{i:02}.png'
    if not p.exists():continue
    a=np.array(Image.open(p).convert('RGB'),dtype=float)/255;v=a[:a.shape[0]//2]@np.array([.2126,.7152,.0722]);brightness.append({'level':i,'upper_srgb':float(v.mean())})
(root/'level-brightness-round3.json').write_text(json.dumps(brightness,indent=2),encoding='utf-8')
files={'baseline':'Backups/ComicUI-round2-pre-round3/manifest.json','modified':changed,'created':created,'screens':[p.as_posix() for p in sorted(screens.rglob('*.png'))]}
(root/'FILES-round3.json').write_text(json.dumps(files,ensure_ascii=False,indent=2),encoding='utf-8')
font=ImageFont.truetype('Assets/CampusRiftUI/Fonts/BeVietnamPro-Medium.ttf',18)
names=['menu','hub','dan-cac','hub-skills','loadout','quiz','hud-combat','settings-video']
sheet=Image.new('RGB',(1280,4*384),(13,27,42));d=ImageDraw.Draw(sheet)
for i,name in enumerate(names):
    p=screens/(name+'.png')
    if not p.exists():continue
    pic=ImageOps.fit(Image.open(p).convert('RGB'),(624,351),Image.Resampling.LANCZOS);sheet.paste(pic,(i%2*640+8,i//2*384+8));d.text((i%2*640+12,i//2*384+362),name,font=font,fill=(255,243,212))
sheet.save(screens/'review-sheet.png')
print('Geometry',geo);print('Brightness',brightness);print('Files changed',len(changed),'created',len(created),'screens',len(files['screens']))
