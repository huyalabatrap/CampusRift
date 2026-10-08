from pathlib import Path
import requests, json, hashlib, urllib.parse, concurrent.futures, zipfile
from bs4 import BeautifulSoup
root=Path(__file__).resolve().parents[1]/'task/models/source'
root.mkdir(parents=True,exist_ok=True)
slugs=['night-demon','rigged-textured-mage','dark-priest','rigged-textured-executioner','vampire-bat-animated','gargoyle','lava-golem']
def download(href,dst):
    size=int(requests.head(href,timeout=30).headers['Content-Length'])
    if dst.exists() and dst.stat().st_size==size:return
    chunk=128*1024
    parts=dst.parent/(dst.name+'.parts');parts.mkdir(exist_ok=True)
    def part(start):
        end=min(size-1,start+chunk-1);target=parts/str(start)
        if target.exists() and target.stat().st_size==end-start+1:return target
        for trial in range(12):
            try:
                offset=target.stat().st_size if target.exists() else 0
                host=href.replace('opengameart.org','lpc.opengameart.org') if trial%2 else href
                r=requests.get(host,headers={'Range':f'bytes={start+offset}-{end}'},timeout=(20,30),stream=True)
                r.raise_for_status()
                if r.status_code!=206:raise ValueError('Range failed')
                with target.open('ab') as f:
                    for block in r.iter_content(2048):f.write(block)
                if target.stat().st_size!=end-start+1:raise ValueError('Incomplete range')
                return target
            except Exception:
                if trial==11:raise
    with concurrent.futures.ThreadPoolExecutor(max_workers=16) as pool: chunks=list(pool.map(part,range(0,size,chunk)))
    with dst.open('wb') as out:
        for c in chunks:out.write(c.read_bytes())
    print('Downloaded',dst.name,size,flush=True)
def fetch(slug):
    folder=root/slug;folder.mkdir(exist_ok=True)
    url='https://opengameart.org/content/'+slug
    page=requests.get(url,timeout=60);page.raise_for_status()
    (folder/'page.html').write_text(page.text,encoding='utf-8')
    soup=BeautifulSoup(page.text,'html.parser')
    body=soup.select_one('div.node') or soup
    text=body.get_text('\n',strip=True)
    (folder/'page.txt').write_text(text,encoding='utf-8')
    links=[]
    for a in soup.select('a[href]'):
        href=urllib.parse.urljoin(url,a['href'])
        ext=urllib.parse.unquote(href.split('?')[0]).lower()
        if '/files/' in href and ext.endswith(('.blend','.zip','.7z','.rar')) and href not in links: links.append(href)
    # Exclude duplicate nude executioner and large unnecessary alternate formats.
    if slug=='rigged-textured-executioner':links=[x for x in links if 'nude' not in x]
    if slug=='dark-elf-game-ready-and-animated':links=links[:1]
    files=[]
    for href in links:
        name=Path(urllib.parse.unquote(urllib.parse.urlparse(href).path)).name
        dst=folder/name
        download(href,dst)
        files.append({'file':name,'download':href,'sha256':hashlib.sha256(dst.read_bytes()).hexdigest(),'bytes':dst.stat().st_size})
        if dst.suffix=='.zip':
            extracted=folder/'extracted';extracted.mkdir(exist_ok=True)
            with zipfile.ZipFile(dst) as z:
                for entry in z.infolist():
                    target=(extracted/entry.filename).resolve()
                    if not target.is_relative_to(extracted.resolve()):raise ValueError('Unsafe archive path')
                z.extractall(extracted)
    return {'slug':slug,'url':url,'date':'2026-10-03','files':files,'summary':text[:1500]}
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    data=list(pool.map(fetch,slugs))
(root/'manifest.json').write_text(json.dumps(data,indent=2,ensure_ascii=False),encoding='utf-8')
for row in data:print(json.dumps(row,ensure_ascii=False))
