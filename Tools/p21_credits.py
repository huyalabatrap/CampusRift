from pathlib import Path
import json,hashlib,urllib.request
src='https://freesound.org/people/WhisperingEarth/sounds/798842/'
url='https://cdn.freesound.org/previews/798/798842_17200815-hq.mp3'
p=Path('Assets/Audio/Resources/P21/dawn-birds.mp3');p.parent.mkdir(parents=True,exist_ok=True)
if not p.exists():p.write_bytes(urllib.request.urlopen(url,timeout=40).read())
lic=Path('Assets/Audio/Resources/P21/LICENSES.md')
lic.write_text('# P21 dawn ambience\n\n- Morning Birds in a Quiet Urban Garden — **WhisperingEarth**, **CC0 1.0**, [Freesound source]('+src+'). Public HQ MP3 preview downloaded unchanged from '+url+'; preview is Freesound transcoding of the recording, not the original320kbps file. Routed SFX, ambience gain0,10, no pitch edit.\n',encoding='utf-8')
manifest=Path('task/p21/source/manifest.json');m=[x for x in json.loads(manifest.read_text(encoding='utf-8')) if x['id']!='dawn-birds'];m.append(dict(id='dawn-birds',author='WhisperingEarth',title='Morning Birds in a Quiet Urban Garden',license='CC0 1.0',source=src,download=url,file=p.as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),checked='2026-10-04',version='Public HQ MP3 preview, not original'));manifest.write_text(json.dumps(m,indent=2),encoding='utf-8')
intro='''CAMPUS RIFT

THỰC HIỆN / PRODUCTION
Nhóm phát triển Campus Rift

MODEL NGƯỜI DÙNG CUNG CẤP / USER PROVIDED MODELS
Campus · SchoolGirl · Shaban · bảy model yêu thú
Rồng020 Silver Cloud ·023 Azure Serpent ·026 Lava Wing
Model, texture và hoạt ảnh: quyền thuộc người dùng / rights retained by their owner.

MỸ THUẬT / ART
Giao diện comic, icon, mesh và hiệu ứng: Campus Rift
Minh họa vật phẩm: tạo với OpenAI imagegen

DANH HIỆU / TITLE
PHÁ RIFT · RIFT BREAKER
'''
entries=[
('Night Demon · Ảnh Yêu','Morgan Strauss (nubux)','CC-BY3.0','https://opengameart.org/content/night-demon','Modified for Campus Rift: clothing, idle animation, materials and LOD.\nhttps://creativecommons.org/licenses/by/3.0/'),
('Rigged, textured mage · Triệu Hồn Sư','thecubber · source rig: Julius','CC-BY-SA3.0','https://opengameart.org/content/rigged-textured-mage','Modified for Campus Rift: rig, animations, materials and LOD.\nhttps://creativecommons.org/licenses/by-sa/3.0/'),
('Vampire Bat · Dực Yêu','rubberduck · source textures: Yughues','CC0 1.0','https://opengameart.org/content/vampire-bat-animated','Modified for Campus Rift: animation, scale and LOD.'),
('Lava Golem · Hỏa Linh','gavlig · concept: Boris','CC0 1.0','https://opengameart.org/content/lava-golem','Clean model and glow; modified for Campus Rift.'),
('Ultimate Monsters','Quaternius','CC0 1.0','https://quaternius.com/packs/ultimatemonsters.html',''),
('Particle Pack · Impact Sounds · Sci-fi Sounds','Kenney Vleugels','CC0 1.0','https://kenney.nl/assets/particle-pack','https://kenney.nl/assets/impact-sounds\nhttps://kenney.nl/assets/sci-fi-sounds'),
('Monster Sound Effects Pack','Ogrebane','CC0 1.0','https://opengameart.org/content/monster-sound-effects-pack','Layered/processed into dragon roars for Campus Rift.'),
('Cat Roar1 /2','Joseph SARDIN · BigSoundBank','CC0 1.0','https://bigsoundbank.com/cat-roar-1-s1881.html','https://bigsoundbank.com/cat-roar-2-s1882.html\nLayered/processed into dragon roars for Campus Rift.'),
('Large Wings Flap','AntumDeluge · original chop: dave.des','CC0 1.0','https://opengameart.org/content/large-wings-flap',''),
('Alarm','EZduzziteh','CC0 1.0','https://opengameart.org/content/alarm-1',''),
('Fireplace Sound loop','PagDev','CC0 1.0','https://opengameart.org/content/fireplace-sound-loop',''),
('Final Stand','Centurion_of_war','CC0 1.0','https://opengameart.org/content/final-stand-0',''),
('Calm Loop','wipics','CC0 1.0','https://opengameart.org/content/calm-loop',''),
('Dark Place (loop)','SkyleTheFrench','CC0 1.0','https://opengameart.org/content/dark-place-loop',''),
('Victory','celestialghost8','CC0 1.0','https://opengameart.org/content/victory',''),
('The Final Battle','skrjablin','CC0 1.0','https://opengameart.org/content/the-final-battle',''),
('Boss Battle #2 [Symphonic Metal]','nene','CC0 1.0','https://opengameart.org/content/boss-battle-2-symphonic-metal','Opening/loop filenames reversed by source author; using looping part.'),
('Hope (Orchestral battle music)','MintoDog','CC0 1.0','https://opengameart.org/content/hopeorchestral-battle-music',''),
('Transformansion ending','Emma_MA','CC0 1.0','https://opengameart.org/content/transformansion-ending',''),
('Morning Birds in a Quiet Urban Garden','WhisperingEarth · Freesound','CC0 1.0',src,'Public HQ MP3 preview of the field recording.'),
('Be Vietnam Pro','The Be Vietnam Pro Project Authors (2021)','SIL Open Font License1.1','https://github.com/bettergui/BeVietnamPro',''),
('Campus surfaces and sky','Poly Haven','CC0 1.0','https://polyhaven.com/license','Textures adjusted for Campus Rift; original HDRI tone mapped.'),
]
for asset in ['plastered_wall_04','concrete_floor_worn_001','brick_pavement','rusty_painted_metal','rusty_corrugated_iron','wood_planks_grey','sparse_grass','brown_mud_03','kloppenheim_06_puresky']:
    entries.append((asset,'Poly Haven','CC0 1.0','https://polyhaven.com/a/'+asset,''))
# Attribution for source assets retained in the project archive, as listed by ReplacementP19.
entries.extend([
('Dark Priest · source archive','Danimal · Dread Knight · Katarzyna Zalecka · Ancient Beast · Alejandro CG · Yughues','CC-BY-SA3.0','https://opengameart.org/content/dark-priest','https://AncientBeast.com'),
('Executioner · source archive','thecubber · commissioned by OpenGameArt.org community','CC-BY3.0','https://opengameart.org/content/rigged-textured-executioner',''),
('Gargoyle · source archive','yd','CC0 1.0','https://opengameart.org/content/gargoyle','')])
pages=[intro]
for i in range(0,len(entries),3):
    body=[]
    for title,author,license,source,mod in entries[i:i+3]:body.append(title+'\n'+author+' · '+license+'\n'+source+ ('\n'+mod if mod else ''))
    pages.append('TÀI NGUYÊN / ASSET CREDITS\n\n'+'\n\n'.join(body))
Path('Assets/SkyBeast/Resources/P21/Credits.txt').write_text('\n---PAGE---\n'.join(pages),encoding='utf-8')
documents=[dict(path=q.as_posix(),sha256=hashlib.sha256(q.read_bytes()).hexdigest()) for q in sorted(Path('Assets').rglob('LICENSES*.md'))]
Path('task/p21/credits-index.json').write_text(json.dumps(dict(documents=documents,pages=len(pages),entries=entries,method='Reviewed every license document; deduplicated author/work/source/license/modification entries. Technical pipeline/hash text stays in report, not credits UI.'),ensure_ascii=False,indent=2),encoding='utf-8')
print(len(documents),'documents reviewed;',len(entries),'attributions;',len(pages),'pages')
