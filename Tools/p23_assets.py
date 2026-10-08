from pathlib import Path
from PIL import Image,ImageDraw
import random,math,json
root=Path('Assets/CampusRiftUI/Resources/P23');(root/'Elements').mkdir(parents=True,exist_ok=True)
pal=[(235,235,235),(240,228,66),(0,158,115),(86,180,233),(230,159,0),(215,192,148),(204,121,167),(181,181,181),(129,159,245)]
names=['None','Kim','Moc','Thuy','Hoa','Tho','Loi','Am','KhongGian']
for i,name in enumerate(names):
 im=Image.new('RGBA',(128,128));d=ImageDraw.Draw(im);d.ellipse((3,3,125,125),fill=(3,8,16,255),outline=(255,243,212,255),width=4)
 def line(points,width=10):d.line(points,fill=pal[i]+(255,),width=width,joint='curve')
 if i==0:d.ellipse((38,38,90,90),outline=pal[i]+(255,),width=10)
 elif i==1:line([(64,20),(104,64),(64,108),(24,64),(64,20)])
 elif i==2:line([(64,108),(64,25)]);line([(64,72),(30,40)]);line([(64,60),(96,28)])
 elif i==3:
  for y in (38,63,88):line([(20,y),(40,y-9),(65,y+9),(89,y-9),(108,y)])
 elif i==4:line([(64,20),(106,101),(22,101),(64,20)])
 elif i==5:line([(28,28),(100,28),(100,100),(28,100),(28,28)]);line([(28,64),(100,64)],6)
 elif i==6:d.polygon([(70,16),(32,70),(59,70),(48,113),(100,54),(73,54),(84,16)],fill=pal[i]+(255,))
 elif i==7:d.ellipse((23,17,110,111),fill=pal[i]+(255,));d.ellipse((53,11,119,87),fill=(3,8,16,255))
 else:
  d.ellipse((20,20,108,108),outline=pal[i]+(255,),width=9);d.ellipse((44,44,84,84),outline=pal[i]+(255,),width=9)
 im.save(root/'Elements'/f'{name}.png')
rng=random.Random(2304);im=Image.new('RGB',(256,256));pix=im.load()
for y in range(256):
 for x in range(256):
  grain=rng.uniform(-18,18);weather=12*math.sin(x*.074)*math.sin(y*.12)+8*math.sin((x+y)*.19)
  moss=(math.sin(x*.035)+math.cos(y*.047)+math.sin((x+y)*.027))>1.45 and (y>110 or x<45)
  base=(67,83,55) if moss else (135,138,129)
  pix[x,y]=tuple(max(0,min(255,int(v+grain+weather))) for v in base)
im.save(root/'weathered-stone.png')
(root/'LICENSES.md').write_text('''# P23 · Original procedural assets

Nine geometric element emblems and a 256px weathered stone/moss texture made locally by Tools/p23_assets.py (Pillow, seeded mathematics). No downloaded artwork or third-party model. Stele mesh/cracks/inlay are original Unity code in LearningShrineVisual.cs. Existing campus, player and dragon materials retain their original attribution.
''',encoding='utf-8')
print('9 symbols + stone texture')
