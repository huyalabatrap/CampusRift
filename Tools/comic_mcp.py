"""File-based Unity MCP calls, avoiding shell quoting of C# and JSON."""
import json, sys
from pathlib import Path
import unity_mcp as m
m.initialize()
if sys.argv[1] == 'schema':
    names = sys.argv[2:]
    print(json.dumps({t['name']:t['inputSchema'] for t in m.rpc('tools/list')['result']['tools'] if t['name'] in names}, indent=2))
elif sys.argv[1] == 'code':
    result = m.call('execute_code', {'action': 'execute', 'code': Path(sys.argv[2]).read_text(encoding='utf-8-sig')})
    print(json.dumps(result, ensure_ascii=False))
else:
    args = json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig')) if len(sys.argv)>2 else {}
    print(json.dumps(m.call(sys.argv[1], args), ensure_ascii=False))
