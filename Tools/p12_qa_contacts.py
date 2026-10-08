from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root=Path('task/p12/screens')
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
enemies=['tieu-yeu','doc-nhan','thiet-giap-nguu','bao-thi','liem-hon','quang-ma','hoa-trung']
sheet=Image.new('RGB',(1920,7*320+60),'#101a25')
draw=ImageDraw.Draw(sheet)
draw.text((16,12),'Actual warnings and impacts | daylight / night | gameplay camera',font=font,fill='white')
for row,name in enumerate(enemies):
    for column,(state,light) in enumerate([('warning','light'),('impact','light'),('warning','dark'),('impact','dark')]):
        source=Image.open(root/'enemy-vfx'/f'{name}-{state}-{light}.png').convert('RGB')
        tile=source.crop((360,40,1560,760)).resize((480,288),Image.Resampling.LANCZOS)
        x=column*480;y=60+row*320
        sheet.paste(tile,(x,y));draw.text((x+8,y+290),f'{name} / {state} / {light}',font=font,fill='#ffd55a')
sheet.save(root/'enemy-vfx'/'contact.png')

sheet=Image.new('RGB',(1920,5*580+60),'#101a25');draw=ImageDraw.Draw(sheet)
draw.text((16,12),'Ten level roster fixtures | actual pool and level lighting',font=font,fill='white')
for i in range(10):
    source=Image.open(root/'rosters'/f'level-{i+1:02}.png').convert('RGB').resize((960,540),Image.Resampling.LANCZOS)
    x=(i%2)*960;y=60+(i//2)*580
    sheet.paste(source,(x,y));draw.text((x+10,y+543),f'Level {i+1}',font=font,fill='#ffd55a')
sheet.save(root/'rosters'/'all-levels-contact.png')
print('VFX and ten-level roster contacts saved')
