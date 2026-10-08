from pathlib import Path
import requests,re,json,hashlib
folder=Path('Assets/Audio/Resources/P17/Music');folder.mkdir(parents=True,exist_ok=True)
manifest=[]
for title,author,slug,file,extension in [('Calm Loop','wipics','calm-loop','dusk','mp3'),('Dark Place (loop)','SkyleTheFrench','dark-place-loop','night','ogg')]:
    page='https://opengameart.org/content/'+slug
    html=requests.get(page,timeout=45);html.raise_for_status()
    assert 'creativecommons.org/publicdomain/zero/1.0' in html.text
    urls=re.findall(r'href="([^"]+\.'+extension+r')"',html.text)
    url=next(u for u in urls if '/files/' in u)
    if url.startswith('/'):url='https://opengameart.org'+url
    data=requests.get(url,timeout=60);data.raise_for_status();p=folder/(file+'.'+extension);p.write_bytes(data.content)
    manifest.append(dict(title=title,author=author,page=page,download=url,license='CC0 1.0',file=str(p),sha256=hashlib.sha256(data.content).hexdigest(),bytes=len(data.content)))
Path('task/p17/source').mkdir(parents=True,exist_ok=True)
Path('task/p17/source/music-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
Path('Assets/Audio/LICENSES-P17.md').write_text('# P17 music\n\n'+''.join(f'- {x["title"]} — {x["author"]}, [source]({x["page"]}), CC0 1.0. `{x["file"]}` downloaded unchanged on 2026-10-03.\n' for x in manifest)+'\nFire-level music reuses P12 BossPhase1 (Final Stand, Centurion_of_war, CC0); see Assets/Enemies/LICENSES.md. Existing UI/element/reaction sounds retain their original licenses.\n',encoding='utf-8')
print(json.dumps(manifest,ensure_ascii=True))
