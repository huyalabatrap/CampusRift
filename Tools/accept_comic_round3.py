from pathlib import Path
from PIL import Image, ImageOps
import sys,json,shutil,re
root=Path('task/ui-comic')
ident,source=sys.argv[1:3]
if ' by default.' in source:
    source=re.search(r' as (.*?) by default\.',source,re.S).group(1)
job=next(j for j in json.loads((root/'image-jobs-round3.json').read_text(encoding='utf-8')) if j['id']==ident)
p=Path(source)
native=root/'generated-round3'/f'{ident}-native.png'
shutil.copy2(p,native)
im=Image.open(native).convert('RGBA' if job['transparent'] else 'RGB')
dest=Path(job['dest']);dest.parent.mkdir(parents=True,exist_ok=True)
if job['kind']=='skill':
    im.thumbnail((512,512),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',(512,512));canvas.alpha_composite(im,((512-im.width)//2,(512-im.height)//2));canvas.save(dest)
elif job['kind']=='sky':
    im=im.resize((4096,2048),Image.Resampling.LANCZOS)
    import numpy as np
    a=np.array(im,dtype=float)
    for x in range(192):
        t=x/191; avg=(a[:,x,:]+a[:,-1-x,:])/2
        a[:,x,:]=avg*(1-t)+a[:,x,:]*t;a[:,-1-x,:]=avg*(1-t)+a[:,-1-x,:]*t
    Image.fromarray(a.clip(0,255).astype('uint8')).save(dest)
else:
    ImageOps.fit(im,(1920,1080) if job['kind']=='background' else (1024,576),method=Image.Resampling.LANCZOS).save(dest)
record={**job,'native':native.as_posix(),'source':str(source),'nativeSize':list(Image.open(native).size),'finalSize':list(Image.open(dest).size),'hasAlpha':im.mode=='RGBA'}
(root/'generated-round3'/f'{ident}.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
print(ident+' saved')
