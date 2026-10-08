import urllib.request,urllib.parse,re,json,hashlib,shutil,zipfile,io
from pathlib import Path
from PIL import Image,ImageDraw
root=Path.cwd();dest=root/'Assets/SkyBeast/Resources/P13';dest.mkdir(parents=True,exist_ok=True)
source=root/'task/p13/source';source.mkdir(parents=True,exist_ok=True)
manifest=[]
for name,page,author in [('alarm','https://opengameart.org/content/alarm-1','EZduzziteh'),('fire','https://opengameart.org/content/fireplace-sound-loop','PagDev')]:
    html=urllib.request.urlopen(page,timeout=30).read().decode();(source/(name+'.html')).write_text(html,encoding='utf-8')
    links=list(dict.fromkeys(urllib.parse.urljoin(page,l) for l in re.findall(r'href=["\']([^"\']+\.(?:ogg|wav))["\']',html)))
    links=[l for l in links if '/sites/default/files/' in l and Path(urllib.parse.urlparse(l).path).suffix in ('.wav','.ogg')]
    link=links[0];data=urllib.request.urlopen(link,timeout=30).read();file=dest/(name+Path(urllib.parse.urlparse(link).path).suffix);file.write_bytes(data)
    manifest.append(dict(page=page,url=link,author=author,license='CC0-1.0',file=str(file.relative_to(root)),sha256=hashlib.sha256(data).hexdigest()))
page='https://kenney.nl/assets/particle-pack';html=urllib.request.urlopen(page,timeout=30).read().decode();(source/'kenney.html').write_text(html,encoding='utf-8')
links=re.findall(r'href=["\']([^"\']+\.zip)["\']',html);link=urllib.parse.urljoin(page,links[0]);data=urllib.request.urlopen(link,timeout=30).read();(source/'kenney-particle-pack.zip').write_bytes(data)
with zipfile.ZipFile(io.BytesIO(data)) as z:
    for part,target in [('fire_01.png','flame.png'),('smoke_01.png','smoke.png'),('spark_01.png','spark.png')]:
        matches=[p for p in z.namelist() if p.endswith(part)]
        if not matches:raise RuntimeError((part,z.namelist()[:40]))
        payload=z.read(matches[0]);(dest/target).write_bytes(payload)
        manifest.append(dict(page=page,url=link,author='Kenney',license='CC0-1.0',archiveMember=matches[0],file=str((dest/target).relative_to(root)),sha256=hashlib.sha256(payload).hexdigest()))
# Original comic symbols, rasterized from polygons to match the existing icon system.
ink='#02050a';gold='#f5b301';paper='#fff3d4';navy='#1b2d45'
for name in ['indoor','partial','outdoor','arrow','scorch']:
    im=Image.new('RGBA',(256,256));d=ImageDraw.Draw(im)
    if name=='indoor':
        d.polygon([(28,112),(128,26),(228,112),(207,132),(193,119),(193,220),(63,220),(63,119),(49,132)],fill=paper,outline=ink,width=12)
        d.rectangle((96,148,150,218),fill=navy);d.line((79,132,177,132),fill=gold,width=10)
    elif name=='partial':
        d.polygon([(128,25),(234,220),(22,220)],fill=gold,outline=ink,width=14);d.rounded_rectangle((115,92,141,155),10,fill=ink);d.ellipse((115,176,141,202),fill=ink)
    elif name=='outdoor':
        d.polygon([(126,18),(160,74),(151,106),(197,73),(224,158),(206,215),(153,239),(89,232),(42,192),(39,148),(80,95),(86,148),(110,112)],fill='#ff620d',outline=ink,width=13)
        d.polygon([(131,100),(160,159),(168,191),(132,222),(95,199),(101,163)],fill='#fff4ab')
    elif name=='arrow':d.polygon([(128,20),(230,116),(169,116),(169,233),(87,233),(87,116),(26,116)],fill=gold,outline=ink,width=14)
    else:
        import random,math
        rng=random.Random(13);p=[]
        for i in range(64):
            a=i*math.tau/64;r=100+rng.uniform(-18,12);p.append((128+math.cos(a)*r,128+math.sin(a)*r))
        d.polygon(p,fill=(23,10,6,200));d.ellipse((47,62,209,208),fill=(33,17,11,160));d.ellipse((83,82,177,176),fill=(57,23,9,140))
    im.save(dest/(name+'.png'))
(source/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps(manifest,indent=2))
