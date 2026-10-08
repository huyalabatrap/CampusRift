"""Deterministic comic UI art; rerun with Python/Pillow. No generated backgrounds."""
from pathlib import Path
from PIL import Image, ImageDraw
import math

ROOT = Path('Assets/CampusRiftUI/Comic/Resources/Comic')
ROOT.mkdir(parents=True, exist_ok=True)
PALETTE = {'panel': '#1b2d45', 'paper':'#fff3d4', 'selected': '#f5b301', 'button-green': '#7be03a', 'button-red': '#c74332', 'button-blue': '#476797', 'rarity': '#a168d9', 'currency': '#080e18'}
for name, color in PALETTE.items():
    im = Image.new('RGBA', (128,128)); d = ImageDraw.Draw(im)
    d.polygon([(17,10),(127,10),(113,127),(1,127)],fill='#000000')
    d.polygon([(14,1),(122,1),(108,119),(0,119)],fill='#000000')
    edge='#f5b301' if name in ('currency','selected') else '#72a4be' if name=='panel' else '#fff3d4'
    d.polygon([(18,7),(115,7),(103,112),(7,112)],fill=edge)
    d.polygon([(21,10),(111,10),(100,109),(11,109)],fill=color)
    mask=Image.new('L',(128,128));md=ImageDraw.Draw(mask);md.polygon([(21,10),(111,10),(100,109),(11,109)],fill=255)
    gradient=Image.new('RGBA',(128,128));gd=ImageDraw.Draw(gradient)
    base=tuple(int(color[i:i+2],16) for i in (1,3,5))
    for y in range(128):
        lift=(1-y/128)*.08
        gd.line((0,y,127,y),fill=tuple(round(c+(255-c)*lift) for c in base)+(255,))
    im.paste(gradient,(0,0),mask)
    im.save(ROOT/(name+'.png'))
im=Image.new('RGBA',(1024,512));d=ImageDraw.Draw(im);cx,cy=470,252
points=[]
for i in range(72):
    a=i*math.pi*2/72; radius=1 if i%2==0 else .59
    points.append((cx+math.cos(a)*480*radius,cy+math.sin(a)*245*radius))
d.polygon(points,fill='#040810',outline='#040810',width=7)
inner=[(cx+(x-cx)*.93,cy+(y-cy)*.93) for x,y in points];d.polygon(inner,fill='#ff8a00')
inner=[(cx+(x-cx)*.80,cy+(y-cy)*.80) for x,y in points];d.polygon(inner,fill='#f5b301')
im.save(ROOT/'burst.png')
im=Image.new('RGBA',(1920,1080));d=ImageDraw.Draw(im)
for y in range(0,1080,14):
    for x in range(0,1920,14):
        edge=max(abs(x-960)/960,abs(y-540)/540)
        if edge>.60: d.ellipse((x,y,x+3,y+3),fill=(91,146,187,int(32*edge)))
for i in range(56):
    a=i*math.pi*2/56
    d.line([(960+math.cos(a)*820,540+math.sin(a)*465),(960+math.cos(a)*1600,540+math.sin(a)*910)],fill=(111,166,204,35),width=2)
im.save(ROOT/'halftone.png')
im=Image.new('RGBA',(256,128));d=ImageDraw.Draw(im)
for y in range(8,120,10):
    for x in range(8,248,10):
        strength=max(0,(x/256-y/128))
        d.ellipse((x,y,x+3,y+3),fill=(92,153,189,int(48*strength)))
for i in range(12):
    d.line((180+i*7,0,256,50+i*5),fill=(105,167,200,24),width=1)
im.save(ROOT/'panel-texture.png')
im=Image.new('RGBA',(96,128));d=ImageDraw.Draw(im)
d.polygon([(48,3),(89,62),(48,122),(7,62)],fill='#03060a')
d.polygon([(48,10),(82,62),(48,113),(14,62)],fill='#f5b301')
d.polygon([(48,10),(48,62),(14,62)],fill='#fff2b7')
d.polygon([(48,62),(82,62),(48,113)],fill='#8d570e')
d.line([(48,10),(48,113)],fill='#03060a',width=3)
im.save(ROOT/'crystal.png')
print('Created', len(list(ROOT.glob('*.png'))), 'comic sprites')
