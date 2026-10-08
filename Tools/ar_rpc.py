"""Small AR checkpoint helper. C# lives in a file to avoid PowerShell quoting."""
import json, sys, time
from pathlib import Path
import unity_mcp as m

m.initialize()
name = sys.argv[1]
args = json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig')) if name != 'code' else {'action':'execute','code':Path(sys.argv[2]).read_text(encoding='utf-8-sig')}
if name == 'code': name = 'execute_code'
for attempt in range(12):
    raw = m.call(name, args)
    result = raw.get('result', {}).get('structuredContent', raw)
    if 'result' in result and isinstance(result['result'], dict): result = result['result']
    if result.get('success') is not False or not any(s in str(result).lower() for s in ['no_unity_session','not ready','domain reload','busy','compiling']): break
    time.sleep(2)
if len(sys.argv)>3: Path(sys.argv[3]).write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(result, ensure_ascii=True))
