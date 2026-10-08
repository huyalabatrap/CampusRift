from pathlib import Path
import sys,datetime
p=Path('task/ar/PROGRESS.md');s=p.read_text(encoding='utf-8-sig');message=sys.argv[1]
line='- **Gói A '+datetime.datetime.now().strftime('%d/%m %H:%M')+':** '+message+'\n'
s=s.replace('## Trạng thái hiện tại — tiếp tục từ đây\n','## Trạng thái hiện tại — tiếp tục từ đây\n'+line,1)
p.write_text(s,encoding='utf-8')
