from ar_fix2_local import code, background, clear
from pathlib import Path
import json, time
out=Path('task/ar/fix3')
def save(path,data): Path(path).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
def console(path):
    r=code('return MCPForUnity.Editor.Tools.ReadConsole.HandleCommand(Newtonsoft.Json.Linq.JObject.Parse("{\\\"action\\\":\\\"get\\\",\\\"types\\\":[\\\"error\\\"],\\\"count\\\":100,\\\"format\\\":\\\"detailed\\\"}"));')
    save(path,r); return r
def progress(s):
    with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f: f.write('\n## AR fix3 — '+s+'\n')
if __name__=='__main__':
    import sys
    print(json.dumps(code(Path(sys.argv[1]).read_text(encoding='utf-8-sig')),ensure_ascii=True))
