from pathlib import Path
import re,json,struct
backup=Path(Path('task/ar/FIX2-BACKUP.txt').read_text(encoding='utf-8').strip())
text=(backup/'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8-sig')
keys=['defaultScreenOrientation','allowedAutorotateToPortrait','allowedAutorotateToPortraitUpsideDown','allowedAutorotateToLandscapeLeft','allowedAutorotateToLandscapeRight','vulkanEnablePreTransform']
result={k:int(re.search(r'^  '+k+r': (\d+)',text,re.M)[1]) for k in keys}
block=re.search(r'- m_BuildTarget: AndroidPlayer\s+m_APIs: ([0-9a-f]+)i\s+m_Automatic: (\d+)',text)
raw=bytes.fromhex(block[1]);result.update(androidAutomatic=bool(int(block[2])),androidAPIs=list(struct.unpack('<'+'i'*(len(raw)//4),raw)),apiNames=['Vulkan','OpenGLES3'])
result['source']=str(backup/'ProjectSettings/ProjectSettings.asset')
Path('task/ar/fix2/configuration-before.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
