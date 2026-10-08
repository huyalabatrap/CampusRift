from PIL import Image,ImageDraw
from pathlib import Path
p=Path('Assets/Enemies/Data/Affixes/Icons');p.mkdir(parents=True,exist_ok=True)
for i,name in enumerate(['Berserk','MetalBody','Split','Vampiric','Explosion','Invisible','Guardian','FireHeart']):
 im=Image.new('RGBA',(64,64));d=ImageDraw.Draw(im)
 d.rounded_rectangle((2,2,61,61),radius=12,fill=(8,15,26,255),outline=(255,245,215,255),width=3)
 if i==0:d.polygon([(37,8),(17,35),(29,35),(24,56),(49,27),(35,27)],fill='white')
 if i==1:d.regular_polygon((32,32,20),6,fill='white');d.regular_polygon((32,32,12),6,fill=(8,15,26,255))
 if i==2:
  d.polygon([(30,10),(18,24),(12,40),(26,55),(28,37),(21,31)],fill='white');d.polygon([(34,10),(46,24),(52,40),(38,55),(36,37),(43,31)],fill='white')
 if i==3:d.polygon([(32,9),(16,34),(15,42),(22,51),(42,51),(49,42),(48,34)],fill='white')
 if i==4:d.ellipse((15,25,47,55),fill='white');d.line((34,28,38,14,49,10),fill='white',width=5)
 if i==5:d.ellipse((10,22,54,44),fill='white');d.ellipse((24,24,40,42),fill=(8,15,26,255));d.line((13,53,52,12),fill='white',width=4)
 if i==6:d.polygon([(13,15),(32,8),(51,15),(47,39),(32,55),(17,39)],fill='white');d.polygon([(21,21),(32,17),(43,21),(39,36),(32,45),(25,36)],fill=(8,15,26,255))
 if i==7:d.polygon([(31,8),(38,27),(48,22),(53,40),(46,51),(32,57),(18,51),(12,40),(23,23),(24,39),(29,30)],fill='white');d.polygon([(31,33),(39,48),(32,53),(25,48)],fill=(8,15,26,255))
 im.save(p/(name+'.png'))
