"""Call local Unity MCP with C# from a UTF-8 file, or a JSON argument file."""
import json, sys
import unity_mcp as u
u.initialize()
if sys.argv[1] == 'code':
    args = {'action':'execute', 'code':open(sys.argv[2],encoding='utf-8-sig').read()}
    result = u.call('execute_code',args)
else:
    args = json.loads(open(sys.argv[2],encoding='utf-8-sig').read()) if len(sys.argv)>2 else {}
    result = u.call(sys.argv[1],args)
print(json.dumps(result,ensure_ascii=True))
