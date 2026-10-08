from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
r=Path('task/p12/screens')
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
labels=['Idle','Walk','Run','Attack windup','Attack impact','Hit reaction','Stun','Freeze (real skill)','Death','Spawn + rift','Death dissolve']
names={'005':'Bao Thi / Zombie','008':'Liem Hon / Grim Reaper','010':'Tieu Yeu / Pumpkin Mummy','012':'Quang Ma / Halo Demon','013':'Doc Nhan / Flower Alien','025':'Hoa Trung / Flower Crawler','027':'Thiet Giap / Twin Head Demon'}
for id,name in names.items():
    sheet=Image.new('RGB',(2100,1110),'#101a25');draw=ImageDraw.Draw(sheet)
    draw.text((20,12),f'{id} - {name} | Unity gameplay camera, 3.4m / FOV60 | actual rig + LOD',font=font,fill='white')
    for i,label in enumerate(labels):
        src=Image.open(r/'models'/'frames'/f'{id}-{i}.png').convert('RGB')
        tile=src.crop((560,120,1400,760)).resize((420,320),Image.Resampling.LANCZOS)
        x=(i%5)*420;y=60+(i//5)*350;sheet.paste(tile,(x,y));draw.text((x+12,y+323),label,font=small,fill='#ffd55a')
    sheet.save(r/'models'/f'{id}-anim-sheet.png')
for id in ['020','023','026']:
    sheet=Image.new('RGB',(1920,1770),'#101a25');draw=ImageDraw.Draw(sheet)
    draw.text((20,12),f'{id} - Sky presence | actual flight / roar, daylight and night',font=font,fill='white')
    for i,(state,lighting) in enumerate([('fly','light'),('fly','dark'),('roar','light'),('roar','dark'),('lowpass','light'),('lowpass','dark')]):
        src=Image.open(r/'dragons'/'frames'/f'{id}-{state}-{lighting}.png').convert('RGB').resize((960,540),Image.Resampling.LANCZOS)
        x=(i%2)*960;y=60+(i//2)*570;sheet.paste(src,(x,y));draw.text((x+12,y+542),state+' / '+lighting,font=small,fill='#ffd55a')
    sheet.save(r/'dragons'/f'{id}-presence-sheet.png')
print('7 animation sheets, 3 dragon presence sheets composed from real captures')
