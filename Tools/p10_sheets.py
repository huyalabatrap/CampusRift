"""Compose actual Unity timeline captures into labeled QA contact sheets."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json,sys
root=Path(sys.argv[1] if len(sys.argv)>1 else 'task/p10/screens')
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
for first in sorted((root/'frames').glob('*-pc-0.png')):
    skill=first.name[:-9]
    files=[root/'frames'/f'{skill}-pc-{i}.png' for i in range(8)]
    if not all(p.exists() for p in files):continue
    output=Image.new('RGB',(960,1200),(15,23,40));draw=ImageDraw.Draw(output)
    for i,p in enumerate(files):
        shot=Image.open(p).convert('RGB');shot.thumbnail((480,270))
        x=(i%2)*480;y=(i//2)*300
        output.paste(shot,(x+(480-shot.width)//2,y+30))
        draw.text((x+12,y+4),f'{skill} / frame {i+1}',font=font,fill=(255,204,65))
    output.save(root/f'{skill}-sheet.png')
    print(root/f'{skill}-sheet.png')
for background in ['light','dark']:
    for first in sorted((root/'frames').glob(f'*-{background}-0.png')):
        skill=first.name[:-(len(background)+7)]
        files=[root/'frames'/f'{skill}-{background}-{i}.png' for i in range(8)]
        if not all(p.exists() for p in files):continue
        output=Image.new('RGB',(1280,1568),(15,23,40));draw=ImageDraw.Draw(output)
        for i,p in enumerate(files):
            shot=Image.open(p).convert('RGB');shot.thumbnail((640,360));x=(i%2)*640;y=(i//2)*392
            output.paste(shot,(x,y+32));draw.text((x+10,y+5),f'{skill} / {background} / {i+1}',font=font,fill=(255,204,65))
        output.save(root/f'{skill}-sheet-{background}.png');print(root/f'{skill}-sheet-{background}.png')
        if background=='light':output.save(root/f'{skill}-sheet.png')
for shot in root.glob('*-impact-light.png'):
    source=Image.open(shot);w,h=source.size
    source.crop((round(w*260/1280),round(h*110/720),round(w*1140/1280),round(h*650/720))).save(root/(shot.name.replace('-impact-light','-impact')))
