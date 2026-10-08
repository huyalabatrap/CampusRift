from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageOps,ImageEnhance
import json,math,numpy as np
root=Path('task/ui-comic');out=root/'screens/round3';out.mkdir(parents=True,exist_ok=True)
jobs=json.loads((root/'image-jobs-round3.json').read_text(encoding='utf-8'))
font=ImageFont.truetype('Assets/CampusRiftUI/Fonts/BeVietnamPro-Medium.ttf',16)
audit=[]
for kind,cols,cell in [('skill',6,(250,212)),('level',5,(320,216))]:
    selected=[j for j in jobs if j['kind']==kind]
    sheet=Image.new('RGB',(cols*cell[0],math.ceil(len(selected)/cols)*cell[1]),(13,27,42));d=ImageDraw.Draw(sheet)
    for i,j in enumerate(selected):
        im=Image.open(j['dest']);x=i%cols*cell[0];y=i//cols*cell[1]
        if kind=='skill':
            small=ImageOps.contain(im.convert('RGBA'),(136,136),Image.Resampling.LANCZOS);sheet.paste(small,(x+16,y+10),small)
            tiny=ImageOps.contain(im.convert('RGBA'),(64,64),Image.Resampling.LANCZOS);sheet.paste(tiny,(x+170,y+52),tiny)
            a=np.array(im.getchannel('A'));audit.append({'id':j['id'],'transparentPixels':float(np.mean(a==0)),'opaquePixels':float(np.mean(a==255)),'alphaMin':int(a.min()),'alphaMax':int(a.max())})
            label=j['id'].replace('-',' ');d.text((x+10,y+154),label if len(label)<26 else label[:25]+'\n'+label[25:],font=font,fill=(255,243,212))
            d.text((x+174,y+126),'64 px',font=font,fill=(177,201,213))
        else:
            pic=im.resize((304,171),Image.Resampling.LANCZOS);sheet.paste(pic,(x+8,y+8));d.text((x+12,y+184),j['id'],font=font,fill=(255,243,212))
            locked=ImageEnhance.Brightness(ImageOps.grayscale(im).convert('RGB')).enhance(.30);locked.save(Path(j['dest']).with_name(j['id']+'-locked.png'))
    sheet.save(out/('skills-sheet.png' if kind=='skill' else 'levels-sheet.png'))
(root/'skill-alpha-round3.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
print('Alpha:',[(a['id'],round(a['transparentPixels'],2)) for a in audit]);print('Contact sheets saved')
