"""Panorama asset preparation: high quality resample, aligned horizon, feathered U wrap."""
from pathlib import Path
from PIL import Image
import numpy as np, json
source=Path('task/ui-comic/generated-round2')
names={'dusk':'877a04b1-faae-44ff-a7a3-18f21586c069','night':'b7bebe42-2649-4ca5-ae4e-c53fe2534485','blood':'89960b77-7db2-4008-a492-310f577e6a23','inferno':'9d4306e5-5e14-4d0e-a8f8-79f21ae06932','eclipse':'271c403d-47c0-4c98-8e17-df97cf938bb4'}
target=Path('Assets/CampusRiftUI/Comic/Resources/Comic');records=[]
for id,token in names.items():
    original=Image.open(source/('sky-'+id+'-native.png')).convert('RGB');w,h=original.size
    # Latitude-longitude panoramas put the horizon at v=.5. Imagegen placed it at about .82.
    horizon=round(h*.82)
    sky=original.crop((0,0,w,horizon)).resize((4096,1024),Image.Resampling.LANCZOS)
    ground=original.crop((0,horizon,w,h)).resize((4096,1024),Image.Resampling.LANCZOS)
    pano=Image.new('RGB',(4096,2048));pano.paste(sky,(0,0));pano.paste(ground,(0,1024))
    arr=np.asarray(pano).astype(np.float32).copy();band=192
    for x in range(band):
        weight=.5*(1+np.cos(np.pi*x/band))
        average=(arr[:,x].copy()+arr[:,-1-x].copy())*.5
        arr[:,x]=arr[:,x]*(1-weight)+average*weight
        arr[:,-1-x]=arr[:,-1-x]*(1-weight)+average*weight
    arr=np.clip(arr,0,255).astype('uint8');Image.fromarray(arr).save(target/('sky-'+id+'.png'))
    luma=(arr[:1024]@np.array([.2126,.7152,.0722])/255).mean()
    records.append({'id':id,'native':list(original.size),'final':[4096,2048],'upper_luma_srgb':round(float(luma),4),'edge_mean_delta':float(np.abs(arr[:,0].astype(float)-arr[:,-1].astype(float)).mean()),'source':str(source/('sky-'+id+'-native.png'))})
Path('task/ui-comic/sky-assets-round2.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print(json.dumps(records,indent=2))
