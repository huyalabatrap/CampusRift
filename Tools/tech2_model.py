from pathlib import Path
import urllib.request,zipfile,hashlib,json
out=Path('Content/AR-Tech2');out.mkdir(parents=True,exist_ok=True)
archive=out/'vosk-model-small-vn-0.4.zip'
if not archive.exists():urllib.request.urlretrieve('https://alphacephei.com/vosk/models/vosk-model-small-vn-0.4.zip',archive)
target=Path('Assets/StreamingAssets/vosk')
target.mkdir(parents=True,exist_ok=True)
with zipfile.ZipFile(archive) as z:
    for entry in z.infolist():
        rel=Path(*Path(entry.filename).parts[1:])
        if entry.is_dir() or not rel.parts:continue
        dst=target/rel;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(z.read(entry))
meta={'source':'https://alphacephei.com/vosk/models/vosk-model-small-vn-0.4.zip','license':'Apache-2.0','zipBytes':archive.stat().st_size,'unpackedBytes':sum(p.stat().st_size for p in target.rglob('*') if p.is_file()),'sha256':hashlib.sha256(archive.read_bytes()).hexdigest()}
(out/'model-source.json').write_text(json.dumps(meta,indent=2),encoding='utf-8')
urllib.request.urlretrieve('https://www.apache.org/licenses/LICENSE-2.0.txt',out/'Apache-2.0.txt')
print(json.dumps(meta))
