"""Original procedural comic flipbooks. No external artwork or image editing."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import random
root=Path('Assets/Skills/Core/Data'); root.mkdir(exist_ok=True)
for name in ['Fire','Smoke']:
    atlas=Image.new('RGBA',(512,512))
    for f in range(16):
        rng=random.Random(101+f); tile=Image.new('RGBA',(128,128)); d=ImageDraw.Draw(tile)
        if name=='Fire':
            points=[(20,114),(15,82),(30,65),(28,45),(47,57),(60,8+f%4*5),(72,54),(93,32),(90,68),(111,84),(104,114)]
            d.polygon(points,fill=(64,9,4,245));d.polygon([(x*.82+12,y*.84+17) for x,y in points],fill=(255,65,10,235))
            d.polygon([(x*.55+29,y*.72+32) for x,y in points],fill=(255,183,38,255))
            d.polygon([(45,110),(43,84),(61,51),(70,86),(84,109)],fill=(255,250,214,255))
        else:
            for j in range(14):
                x=rng.randint(28,88);y=rng.randint(22,87);r=rng.randint(16,30)
                d.ellipse((x-r,y-r,x+r,y+r),fill=(28,19,34,100))
            tile=tile.filter(ImageFilter.GaussianBlur(3));d=ImageDraw.Draw(tile)
            for y in range(12,118,8):
                for x in range(12,118,8):
                    if tile.getpixel((x,y))[3]>20:d.ellipse((x,y,x+2,y+2),fill=(65,42,44,80))
        atlas.paste(tile,((f%4)*128,(f//4)*128))
    atlas.save(root/f'P10{name}Flipbook.png')
