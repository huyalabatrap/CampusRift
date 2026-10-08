import json
from pathlib import Path
from p13_cli import call
warnings=call('read_console',dict(action='get',types=['warning'],count=50,format='detailed'))
Path('task/p14/final-warnings.json').write_text(json.dumps(warnings,indent=2),encoding='utf-8')
print(json.dumps(warnings,ensure_ascii=True))
