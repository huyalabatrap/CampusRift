from pathlib import Path
import urllib.request, json, hashlib, zipfile, re, textwrap
root=Path('task/p21/source');root.mkdir(parents=True,exist_ok=True)
dest=Path('Assets/Audio/Resources/P21/Music');dest.mkdir(parents=True,exist_ok=True)
items=[('giao','skrjablin','The Final Battle','the-final-battle','https://opengameart.org/sites/default/files/the_final_battle.ogg'),
('bird','nene','Boss Battle #2 [Symphonic Metal]','boss-battle-2-symphonic-metal','https://opengameart.org/sites/default/files/boss_battle_%232_metal_pack.zip'),
('king','MintoDog','Hope (Orchestral battle music)','hopeorchestral-battle-music','https://opengameart.org/sites/default/files/hope_orchestral_battle_music_bpm165_0.ogg'),
('dawn','Emma_MA','Transformansion ending','transformansion-ending','https://opengameart.org/sites/default/files/Transformansion%20ending.wav')]
manifest=[]
for name,author,title,page,url in items:
    pageurl='https://opengameart.org/content/'+page
    html=root/(name+'-source.html')
    if not html.exists():html.write_bytes(urllib.request.urlopen(pageurl,timeout=60).read())
    path=root/(name+('.zip' if url.endswith('.zip') else '.wav' if url.endswith('.wav') else '.ogg'))
    if not path.exists():path.write_bytes(urllib.request.urlopen(url,timeout=60).read())
    member=None
    if path.suffix=='.zip':
        z=zipfile.ZipFile(path);names=z.namelist();print('ZIP',names)
        # Author explicitly says the file named opening is the looping part.
        member=next(n for n in names if 'opening' in n.lower() and not n.endswith('/'))
        audio=z.read(member);suffix=Path(member).suffix
    else:audio=path.read_bytes();suffix=path.suffix
    output=dest/(name+suffix);output.write_bytes(audio)
    manifest.append(dict(id=name,author=author,title=title,license='CC0 1.0',source=pageurl,download=url,member=member,file=output.as_posix(),sha256=hashlib.sha256(audio).hexdigest(),downloadSha256=hashlib.sha256(path.read_bytes()).hexdigest(),checked='2026-10-04'))
licenses='# P21 Music · CC0 1.0\n\n'+'\n\n'.join(f"- `{x['id']}`: **{x['title']}**, **{x['author']}**, [source]({x['source']}), **CC0 1.0**. "+(f"ZIP member `{x['member']}`: author states opening/loop filenames are reversed; using opening as the loop. " if x['member'] else '')+'Downloaded unchanged; importer streaming/Vorbis; transition/volume controlled by runtime mixer.' for x in manifest)
licenses+='\n\nShaban5: Final Stand phase1; Shaban7: Final Stand phase2 (Centurion_of_war/CC0, P12). Phase2 Shaban7 and Chu Tước switch to Hope; stage10 switches Giao→Chu Tước→Long Vương. No remaster/pitch edit.\n'
(dest/'LICENSES.md').write_text(licenses,encoding='utf-8')
(root/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
p=Path('Assets/SkyBeast/LICENSES.md');s=p.read_text(encoding='utf-8');p.write_text(s+'\n# P21 — Identity / cinematic / ending\n\n- Model020/023/026, rig/animation/LOD giữ tài nguyên do người dùng cung cấp, không gắn nhãnCC0. Chín sừng/bờm, phiến cánh/đuôi và vảy: mesh trọng số cứng tạo bằng code SkyBeastIdentity, shader IdentityGlow viết cho project; 1 mesh/renderer mỗi con, dùng chung các LOD.\n- Rift ring, Timeline shot clips, camera/subtitle/credits UI viết cho project. Không có model/texture thương mại mới.\n- Reveal dùng lại tiếng gầm P12, rumble/wind P12; dawn fanfare dùng Emma_MA/CC0 ở Audio/Resources/P21/Music/LICENSES.md. Không tải thêm âm thanh bình minh vì fanfare và wind sẵn có đủ cảnh.\n- Nhạc riêng và manifest nguồn/SHA256: Audio/Resources/P21/Music/LICENSES.md; task/p21/source/manifest.json.\n',encoding='utf-8')
# Credit every license document, including LICENSES-P12/P17/P18. Keep source text and URLs.
files=sorted(Path('Assets').rglob('LICENSES*.md'))
pages=['CAMPUS RIFT\n\nThực hiện / Production\nNhóm phát triển Campus Rift · người dùng cung cấp campus, nhân vật và model rồng.\n\nDanh hiệu / Title: PHÁ RIFT · RIFT BREAKER\n\nTài nguyên / Assets\nKenney · Poly Haven · OpenGameArt · Freesound và các tác giả ghi ở các trang sau.\n\nNguồn / Sources\nToàn bộ LICENSES*.md trong Assets, gồm giấy phép CC-BY, CC0, font và model người dùng. Các sửa đổi/dẫn xuất giữ ghi công gốc.\n\nTháp Thí Luyện / Trial Tower và Ác Mộng / Nightmare: nội dung hậu kết P22.']
index=[]
for file in files:
    raw=file.read_text(encoding='utf-8-sig')
    text=re.sub(r'\[([^\]]+)\]\(([^)]+)\)',r'\1 — \2',raw)
    text=re.sub(r'^#+\s*','',text,flags=re.M).replace('**','').replace('`','')
    lines=[]
    for line in text.splitlines():lines.extend(textwrap.wrap(line,width=105,break_long_words=True,break_on_hyphens=False) or [''])
    first=len(pages)+1
    for i in range(0,len(lines),17):pages.append(str(file).replace('\\','/')+'\n\n'+'\n'.join(lines[i:i+17]))
    index.append(dict(path=file.as_posix(),sha256=hashlib.sha256(file.read_bytes()).hexdigest(),firstPage=first,lastPage=len(pages)))
out=Path('Assets/SkyBeast/Resources/P21');out.mkdir(parents=True,exist_ok=True)
(out/'Credits.txt').write_text('\n---PAGE---\n'.join(pages),encoding='utf-8')
Path('task/p21/credits-index.json').write_text(json.dumps(dict(documents=index,pages=len(pages)),indent=2),encoding='utf-8')
print(len(files),'license documents;',len(pages),'credit pages')
