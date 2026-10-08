"""Compose unmodified Unity frames. No repainting, recoloring or synthetic gameplay imagery."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path('task/p11/screens/fix3');font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
for mode in ('light','dark'):
    for first in sorted((root/'frames').glob('*-'+mode+'-0.png')):
        name=first.name[:-(len(mode)+7)];files=[root/'frames'/f'{name}-{mode}-{i}.png' for i in range(8)]
        if not all(p.exists() for p in files):continue
        sheet=Image.new('RGB',(1280,1568),(13,27,42));draw=ImageDraw.Draw(sheet)
        for i,p in enumerate(files):
            frame=Image.open(p).convert('RGB');frame.thumbnail((640,360));x=i%2*640;y=i//2*392
            sheet.paste(frame,(x,y+32));draw.text((x+10,y+5),f'{name} / {mode} / F{i+1}',font=font,fill=(245,179,1))
        sheet.save(root/f'{name}-sheet-{mode}.png');print(f'{name}-{mode}')
for p in root.glob('*-impact-light.png'):
    shot=Image.open(p);w,h=shot.size;shot.crop((int(w*.17),int(h*.12),int(w*.86),int(h*.77))).save(root/p.name.replace('-impact-light','-impact'))
