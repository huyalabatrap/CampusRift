from pathlib import Path
from PIL import Image,ImageDraw
art=Path('Assets/CampusRiftUI/Comic/Resources/Comic')
for name,color in [('panel',(27,45,69)),('selected',(245,179,1)),('paper',(255,243,212)),('currency',(9,18,29))]:
    im=Image.new('RGBA',(128,128));d=ImageDraw.Draw(im)
    d.rounded_rectangle((6,6,127,127),radius=16,fill=(2,5,10))
    d.rounded_rectangle((0,0,121,121),radius=16,fill=(2,5,10))
    d.rounded_rectangle((6,6,115,115),radius=10,fill=(170,195,209) if name=='panel' else (245,179,1) if name=='currency' else (2,5,10))
    d.rounded_rectangle((8,8,113,113),radius=8,fill=color)
    im.save(art/f'round-{name}.png')
im=Image.new('RGBA',(64,64));d=ImageDraw.Draw(im);d.rounded_rectangle((0,0,63,63),radius=14,fill='white');im.save(art/'round-mask.png')
im=Image.new('RGBA',(128,128));d=ImageDraw.Draw(im)
d.arc((31,13,97,93),180,360,fill=(2,5,10),width=20);d.arc((31,13,97,93),180,360,fill=(245,179,1),width=10)
d.rounded_rectangle((23,53,105,112),radius=10,fill=(2,5,10));d.rounded_rectangle((29,59,99,106),radius=6,fill=(245,179,1));d.ellipse((56,70,72,86),fill=(2,5,10));d.rectangle((61,82,67,95),fill=(2,5,10));im.save(art/'lock.png')
print('Rounded sprites and lock saved')
