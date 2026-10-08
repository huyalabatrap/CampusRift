import json,re,urllib.request,urllib.parse,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]
dest=root/'Assets/SkyBeast/Audio/Source';dest.mkdir(parents=True,exist_ok=True)
pages=[
('cat1','https://bigsoundbank.com/cat-roar-1-s1881.html','Joseph SARDIN'),
('cat2','https://bigsoundbank.com/cat-roar-2-s1882.html','Joseph SARDIN'),
('monster','https://opengameart.org/content/monster-sound-effects-pack','Ogrebane'),
('boss','https://opengameart.org/content/final-stand-0','Centurion_of_war')]
sources=[]
for name,page,author in pages:
    html=urllib.request.urlopen(page).read().decode()
    (dest/(name+'-source.html')).write_text(html,encoding='utf-8')
    links=re.findall(r'(?:href|src)=[\"\']([^\"\']+\.(?:wav|ogg|zip|mp3)(?:\?[^\"\']*)?)[\"\']',html)
    links=[urllib.parse.urljoin(page,l.replace('&amp;','&')) for l in links]
    links=list(dict.fromkeys(links))
    if name=='boss':links=[l for l in links if 'phase_1.3' in l or 'phase_2.2.heavy' in l]
    if name.startswith('cat'):links=[l for l in links if '/UPLOAD/mp3/' in l][:1]
    print(name,links,flush=True)
    for link in links:
        file=dest/(name+'-'+urllib.parse.unquote(urllib.parse.urlparse(link).path.rsplit('/',1)[1]))
        if not file.exists():file.write_bytes(urllib.request.urlopen(link).read())
        sources.append(dict(author=author,page=page,url=link,file=str(file.relative_to(root)),license='CC0-1.0',sha256=hashlib.sha256(file.read_bytes()).hexdigest()))
(dest/'sources.json').write_text(json.dumps(sources,indent=2),encoding='utf-8')
