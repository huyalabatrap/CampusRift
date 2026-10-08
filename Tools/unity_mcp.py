"""Small client for the project's already running local Unity MCP server."""
import json, sys, urllib.request
URL = 'http://127.0.0.1:8080/mcp'
session = None
def rpc(method, params=None, ident=1):
    global session
    body = dict(jsonrpc='2.0', method=method, params=params or {})
    if ident is not None: body['id'] = ident
    headers = {'Content-Type':'application/json','Accept':'application/json, text/event-stream'}
    if session: headers['Mcp-Session-Id'] = session
    req = urllib.request.Request(URL, json.dumps(body).encode(), headers)
    with urllib.request.urlopen(req, timeout=180) as r:
        session = r.headers.get('Mcp-Session-Id', session)
        raw = r.read().decode()
    if not raw: return {}
    if any(line.startswith('data:') for line in raw.splitlines()):
        lines = [line[5:].strip() for line in raw.splitlines() if line.startswith('data:')]
        return json.loads(lines[-1])
    return json.loads(raw)
def initialize():
    rpc('initialize', {'protocolVersion':'2024-11-05','capabilities':{},'clientInfo':{'name':'CampusRiftUI','version':'1.0'}})
    rpc('notifications/initialized', ident=None)
def call(name,args=None): return rpc('tools/call',{'name':name,'arguments':args or {}})
if __name__ == '__main__':
    initialize()
    if len(sys.argv)<2: result=rpc('tools/list')
    elif sys.argv[1]=='resources': result=rpc('resources/list')
    elif sys.argv[1]=='read': result=rpc('resources/read',{'uri':sys.argv[2]})
    else: result=call(sys.argv[1],json.loads(sys.argv[2]) if len(sys.argv)>2 else {})
    print(json.dumps(result,ensure_ascii=True))
