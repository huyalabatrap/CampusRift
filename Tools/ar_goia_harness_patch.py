from pathlib import Path
p=Path('Assets/ARRift/Validation/ARRiftPlayTest.cs');s=p.read_text(encoding='utf-8-sig').replace('task/ar/m5','task/ar/goiA').replace('results.json','ar-results.json').replace('DONE.txt','AR-DONE.txt').replace('task/ar/screens/m5-','task/ar/screens/goiA/combat-');p.write_text(s,encoding='utf-8')
