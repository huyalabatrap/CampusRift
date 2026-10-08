from pathlib import Path
import py7zr,subprocess
base=Path(__file__).resolve().parents[1]/'task/models/source'
for file in base.rglob('*.7z'):
    out=file.parent/'extracted';out.mkdir(exist_ok=True)
    with py7zr.SevenZipFile(file) as z:
        for name in z.getnames():
            if not (out/name).resolve().is_relative_to(out.resolve()):raise ValueError('Unsafe archive path')
        z.extractall(out)
    print('Extracted',file.name)
