from pathlib import Path
import json, hashlib, shutil
ROOT=Path('task/ui-comic')
baseline=Path('Backups/ComicUI-round2-pre-round3')
if not (baseline/'manifest.json').exists():
    manifest={}
    for group in ['Assets/CampusRiftUI','Assets/Skills','Assets/Levels','Assets/Settings','Assets/Scenes','Assets/Resources/ContentImages','Assets/Controls','Assets/Learning','Assets/Localization','Assets/Progression','ProjectSettings/QualitySettings.asset','Tools']:
        source=Path(group)
        for p in ([source] if source.is_file() else source.rglob('*')):
            if p.is_file():
                manifest[p.as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
                dest=baseline/p;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
    (baseline/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
jobs=[]
skill_subjects={
'am-binh-quy-hon':('Am','violet','one hooded ghost face with glowing eyes'),
'bac-minh-than-cong':('Thuy','teal blue','one swirling tidal whirlpool pulling a small energy spark inward'),
'banh-truong-lanh-dia':('KhongGian','cyan','one angular cubic spatial domain with a single luminous boundary'),
'con-bang-cuc-toc':('Thuy','ice blue','one streamlined ice bird wing sweeping forward'),
'hac-dong-than-la':('KhongGian','cyan violet','one black hole with a bright cyan accretion crescent'),
'han-bang-phong-an':('Thuy','ice blue','one chunky ice crystal encasing a simple lock shape'),
'hang-long-thap-bat-chuong':('Tho','ochre gold','one dragon-shaped striking palm silhouette'),
'kim-chung-trao':('Kim','gold','one protective golden bell with a thick solid silhouette'),
'moc-linh-hoi-xuan':('Moc','green','one broad sprouting leaf with a small healing sparkle'),
'phat-no-hoa-lien':('Hoa','orange red','one lotus flower made of fire with five broad petals'),
'tam-muoi-chan-hoa':('Hoa','scarlet orange','one three-pronged blazing flame'),
'than-kiem-ngu-loi':('Loi','electric yellow blue','one upright broad sword crossed by one thick lightning bolt'),
'than-thuc-linh-nhan':('Am','violet','one luminous mystic eye with a thick black eyelid'),
'thien-kiem':('Kim','gold silver','one descending celestial broadsword'),
'thien-loi-dan':('Loi','yellow cobalt','one lightning projectile shaped like a chunky bolt'),
'tich-lich-nhat-thiem':('Loi','yellow blue','one zigzag lightning slash in a forward diagonal'),
'tru-tien-kiem-tran':('Kim','gold silver','three broad swords forming a triangular sword formation'),
'van-kiem-quyet':('Kim','silver gold','one main broadsword flanked by two small sword silhouettes'),
'vo-hon-chan-than':('Tho','amber ochre','one armored warrior bust as a solid bold silhouette'),
'VoidWall':('KhongGian','cyan','one upright rectangular energy shield wall with a chunky cyan rim'),
'PhantomDecoy':('Am','violet','one running human silhouette with one offset translucent afterimage'),
'GiantHandSeal':('Tho','ochre gold','one large open palm slamming downward with a small impact burst')}
for name,(element,color,subject) in skill_subjects.items():
    dest=f'Assets/CampusRiftUI/Art/Skills/{name}.png' if name[0].isupper() else f'Assets/Resources/ContentImages/Skills/{name}.png'
    prompt=f'Use case: stylized-concept. Asset: square game skill icon, readable at 64 pixels. Subject: {subject}. Element: {element}; dominant palette {color}. Superhero comic-book thick black ink outline, bold flat cel colors, a few subtle halftone dots INSIDE the symbol. Exactly one clear simple emblem, centered, occupies 82 percent of frame, generous transparent margin. Silhouette and large color blocks first. Actual transparent PNG alpha background. No circular purple ring, no scene, no background, no text, letters, numbers, logo, watermark or checkerboard. Minimal internal detail.'
    jobs.append(dict(id=name,dest=dest,prompt=prompt,transparent=True,kind='skill',element=element))
levels=[
('The First Rift','orange sunset','one bright vertical rift above a simple central campus courtyard'),
('Poison Corridor','emerald green','one school corridor archway filled with green poison mist'),
('The Soul Hunter','purple','one hooded soul hunter silhouette before a simple school block'),
('Blackout Night','cobalt blue','one dark unpowered school doorway lit by a single flashlight beam'),
("The Soul Hunter\'s Prey",'white grey fog','one distant hunter silhouette emerging from mist in the central campus'),
('Blood Moon','deep crimson red','one large red blood moon over a low simple school roof'),
('The Hunter Awakens','violet black with crimson','one tall campus tower under a small deep red moon'),
('Crimson Flame Descends','bright orange fire','one flame drake curled above a simple low school roof'),
("Vermilion Bird\'s Wrath",'scarlet gold','one bold firebird with spread wings above a low roof'),
('Three Beasts Above','gold eclipse','one black eclipsed sun with a gold red corona, three tiny beast silhouettes below over one school roof')]
for i,(name,color,subject) in enumerate(levels,1):
    prompt=f'Use case: illustration-story. Asset: wide 16:9 game level thumbnail for {name}. Scene: {subject}. Dominant palette: {color}, clearly distinct from other levels. Superhero comic art with thick black ink outlines, broad flat cel color shapes, sparse halftone. Very simple composition: ONE large clear central subject, minimal two-tone background, just one low campus shape. Readable at 320 by 180 pixels. No intricate city, no busy foliage, no decorative frame, no UI, no text or logos. Subject silhouette immediately recognizable.'
    jobs.append(dict(id=f'level-{i:02}',dest=f'Assets/Resources/ContentImages/Scenes/level-{i:02}.png',prompt=prompt,transparent=False,kind='level'))
jobs.append(dict(id='academy-background',dest='Assets/CampusRiftUI/Comic/Resources/Comic/academy-background.png',transparent=False,kind='background',prompt='Use case: illustration-story. Asset: widescreen 16:9 quiet background behind dense game UI. Mostly smooth deep navy gradient #0d1b2a to #1b2d45. Entire central 85 percent is almost empty low contrast navy. Extremely faint sparse comic speed lines and halftone dots at outer corners. Just a few very blurred geometric silhouettes of cultivation academy library columns at the far edges, navy on navy, tiny dim muted gold accents at far corners. Maximum readability behind panels. No central subject, no shelves full of detail, no characters, text, logo, bright lights, explosion or high contrast. Flat restrained superhero comic ink style.'))
for sky,subject,palette in [('blood','large deep crimson red moon, clearly red rather than pink','deep crimson and burgundy clouds against muted warm grey golden sky'),('eclipse','large opaque BLACK eclipsed solar disc surrounded by a bright GOLD and RED fiery corona','gold ochre warm grey clouds with deep charcoal and red accents')]:
    jobs.append(dict(id='sky-'+sky,dest='Assets/CampusRiftUI/Comic/Resources/Comic/sky-'+sky+'.png',transparent=False,kind='sky',prompt=f'Use case: illustration-story. Asset: seamless wide 2:1 equirectangular 360 sky panorama for a superhero comic game. Subject: {subject}, placed at horizontal center and upper third. Palette: {palette}. Bold black ink cel-painted clouds, flat broad shapes, sparse halftone. Bright readable midtones across at least half the image, average brightness above 40 percent, absolutely no pastel pink. Horizon at vertical center, quiet bottom half with atmospheric gradient. No buildings, people, ground detail, text or watermark. Left and right edges match. One celestial disc, unmistakable, visible above horizon.'))
(ROOT/'generated-round3').mkdir(parents=True,exist_ok=True)
(ROOT/'image-jobs-round3.json').write_text(json.dumps(jobs,ensure_ascii=False,indent=2),encoding='utf-8')
(ROOT/'IMAGE-PROMPTS-round3.md').write_text('# Round 3 — built-in imagegen\n\n'+'\n\n'.join('## '+j['id']+'\n'+j['prompt']+'\nDestination: `'+j['dest']+'`' for j in jobs),encoding='utf-8')
print('Snapshot ready; image jobs:',len(jobs))
