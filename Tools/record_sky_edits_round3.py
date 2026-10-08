from pathlib import Path
import json
root=Path('task/ui-comic');md=root/'IMAGE-PROMPTS-round3.md';content=md.read_text(encoding='utf-8');suffix=''
base='Edit this 2:1 comic sky panorama for a Unity equirectangular skybox. Preserve the existing ink style, crimson/gold colors, brightness, cloud horizon at 50 percent height and seamless left/right edges. ONLY reposition and resize the '
tail='. Its center must be at 50 percent width and 42 percent height, its visible disc diameter about 11 percent of total image HEIGHT. The celestial disc is just above the horizon, so a normal gameplay camera sees it. Remove its previous high position, filling that space with the existing clouds. Exactly one disc, keep it circular. Maintain broad readable sky midtones. No text, buildings or ground. Wide 2:1 panorama.'
for sky,subject in [('blood','deep RED blood moon'),('eclipse','opaque BLACK eclipse with GOLD/RED corona')]:
    prompt=base+subject+tail
    p=root/'generated-round3'/('sky-'+sky+'.json');data=json.loads(p.read_text(encoding='utf-8'));data['editPrompt']=prompt;data['editReference']=(root/'generated-round3'/('sky-'+sky+'-original.png')).as_posix();p.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
    suffix+='\n\n## Sky edit: '+sky+'\n'+prompt
if '## Sky edit:' not in content:md.write_text(content+suffix,encoding='utf-8')
print('Sky edit prompts recorded')
