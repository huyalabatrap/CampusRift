from pathlib import Path
import numpy as np
from PIL import Image,ImageFilter
import json
root=Path(__file__).resolve().parents[1]
base=root/'Assets/Enemies/Models/ReplacementP19'
rows=[]
for model in ['NightDemon','DarkMage','VampireBat','LavaGolem']:
    tex=base/model/'Textures';tex.mkdir(exist_ok=True)
    if model=='LavaGolem':
        size=1024;rng=np.random.default_rng(1903)
        fine=rng.random((size,size))
        broad=np.asarray(Image.fromarray((rng.random((64,64))*255).astype('uint8')).resize((size,size),Image.Resampling.BICUBIC).filter(ImageFilter.GaussianBlur(5)),dtype=float)/255
        height=.7*broad+.3*fine;rgb=np.stack([18+25*height,17+23*height,16+20*height],axis=-1).astype('uint8')
        Image.fromarray(rgb).save(tex/(model+'_BaseColor.png'))
        smooth=.08+.14*(1-height);metal=np.zeros((size,size))
        grad_y,grad_x=np.gradient(np.asarray(Image.fromarray((height*255).astype('uint8')).filter(ImageFilter.GaussianBlur(1)),dtype=float)/255)
        normal=np.stack([-grad_x*3,-grad_y*3,np.ones_like(grad_x)],axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
        Image.fromarray(np.clip((normal*.5+.5)*255,0,255).astype('uint8')).save(tex/(model+'_Normal.png'))
        rows.append({'model':model,'maps':'Original procedural charcoal albedo/height normal/roughness, seed1903. Author-supplied glow only; no cgtextures diffuse/spec/normal used.'})
    else:
        source=tex/(model+'_BaseColor.png');img=Image.open(source).convert('RGB');size=img.width
        color=np.array(img,dtype=float)/255
        # Preserve source details while reducing candy saturation and shine.
        lum=color.mean(axis=-1,keepdims=True);muted=color*.75+lum*.25
        Image.fromarray(np.clip(muted*255,0,255).astype('uint8')).save(source)
        spec=tex/(model+'_Specular.png')
        if spec.exists():
            values=np.asarray(Image.open(spec).convert('L').resize((size,size)),dtype=float)/255;smooth=np.clip(.12+values*.42,.12,.54)
        else:smooth=np.full((size,size),.22)
        metal=np.full((size,size),.025 if model=='NightDemon' else 0)
        rows.append({'model':model,'maps':'Source albedo/normal retained with 25% desaturation; dielectric metal0–0.025, smoothness derived from source specular (heuristic, not scanned PBR). Bat smoothness0.22.'})
    rgba=np.zeros((size,size,4),dtype=np.uint8);rgba[:,:,0]=(metal*255).astype('uint8');rgba[:,:,3]=(smooth*255).astype('uint8')
    Image.fromarray(rgba).save(tex/(model+'_MetallicSmoothness.png'))
    emission=tex/(model+'_Emission.png')
    if not emission.exists():Image.new('RGB',(4,4),(0,0,0)).save(emission)
(root/'task/models/material-audit.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
print('PBR maps prepared for',len(rows),'models')
