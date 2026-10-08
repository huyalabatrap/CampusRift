"""Download CC0 originals once; derive Unity 1K PBR sets and a 2K real sky."""
import requests, json, hashlib, io
from pathlib import Path
from PIL import Image, ImageOps
from concurrent.futures import ThreadPoolExecutor

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'task/look/source'
TARGET = ROOT / 'Assets/CampusLook/Textures'
SOURCE.mkdir(parents=True, exist_ok=True)
TARGET.mkdir(parents=True, exist_ok=True)
HEADERS = {'User-Agent': 'CampusRiftLook/1.0 (Unity game material authoring)'}
SETS = ['plastered_wall_04','concrete_floor_worn_001','brick_pavement','rusty_painted_metal','rusty_corrugated_iron','wood_planks_grey','sparse_grass','brown_mud_03']

def get(url):
    r = requests.get(url, headers=HEADERS, timeout=90)
    r.raise_for_status()
    return r

def download(asset):
    api = get('https://api.polyhaven.com/files/' + asset).json()
    (SOURCE / (asset + '-api.json')).write_text(json.dumps(api, indent=2), encoding='utf-8')
    rows = []
    for channel, options in [('base', ['Diffuse','diff']), ('normal',['nor_gl']), ('arm',['arm'])]:
        key = next(k for k in options if k in api)
        spec = api[key]['1k'].get('jpg') or api[key]['1k']['png']
        dest = SOURCE / (asset + '_' + channel + Path(spec['url']).suffix)
        if not dest.exists(): dest.write_bytes(get(spec['url']).content)
        data = dest.read_bytes()
        if hashlib.md5(data).hexdigest() != spec['md5']: raise ValueError('MD5 mismatch: ' + str(dest))
        img = Image.open(io.BytesIO(data)).convert('RGB')
        img.thumbnail((1024,1024), Image.Resampling.LANCZOS)
        if channel == 'base' and asset == 'plastered_wall_04':
            # Preserve photographed cracks/peeling, warm the neutral plaster to faded ivory.
            img = ImageOps.colorize(ImageOps.grayscale(img), '#605a49', '#eee2be')
        if channel == 'arm':
            # URP compatible AO red, roughness green; custom world shader consumes the original packing.
            pass
        img.save(TARGET / (asset + '_' + channel + '.png'))
        rows.append({'asset':asset,'channel':channel,'url':spec['url'],'md5':spec['md5'],'sha256':hashlib.sha256(data).hexdigest(),'file':str(dest.relative_to(ROOT))})
    print('Downloaded ' + asset, flush=True)
    return rows

def sky():
    asset = 'kloppenheim_06_puresky'
    api = get('https://api.polyhaven.com/files/' + asset).json()
    spec = api['tonemapped']
    p = SOURCE / (asset + '.jpg')
    if not p.exists(): p.write_bytes(get(spec['url']).content)
    data=p.read_bytes()
    if hashlib.md5(data).hexdigest()!=spec['md5']: raise ValueError('sky MD5 mismatch')
    img=Image.open(p).convert('RGB').resize((2048,1024), Image.Resampling.LANCZOS)
    img.save(TARGET / 'campus-real-sky.jpg', quality=94)
    return [{'asset':asset,'url':spec['url'],'md5':spec['md5'],'sha256':hashlib.sha256(data).hexdigest(),'file':str(p.relative_to(ROOT))}]

if __name__ == '__main__':
    with ThreadPoolExecutor(max_workers=4) as pool:
        rows = [r for group in pool.map(download, SETS) for r in group]
    rows += sky()
    (SOURCE / 'manifest.json').write_text(json.dumps(rows, indent=2), encoding='utf-8')
    print('DONE: ' + str(len(rows)) + ' verified source files')
