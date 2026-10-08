from pathlib import Path
from PIL import Image
import json,shutil,hashlib
root=Path(__file__).resolve().parents[1];jobs=json.loads((root/'task/p20/image-jobs.json').read_text(encoding='utf-8'));audit=[]
for j in jobs:
    source=Path(j['source']);original=root/'task/p20/generated'/ (j['id']+'.png');original.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,original)
    image=Image.open(original).convert('RGBA');dest=root/'Assets/Resources/ContentImages/P20'/j['group']/(j['id']+'.png');dest.parent.mkdir(parents=True,exist_ok=True)
    assert not dest.exists(),str(dest)
    image.resize((512,512),Image.Resampling.LANCZOS).save(dest)
    author=root/'Content/Images/P20'/j['group']/(j['id']+'.png');author.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(dest,author)
    alpha=image.getchannel('A');hist=alpha.histogram();audit.append({'id':j['id'],'path':dest.relative_to(root).as_posix(),'sourceSize':image.size,'alphaMinMax':alpha.getextrema(),'transparentFraction':hist[0]/(image.width*image.height),'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()})
(root/'task/p20/image-alpha.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
print('Saved 8 transparent comic icons and originals')
