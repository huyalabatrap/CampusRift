import sys,json,time
from pathlib import Path
import unity_mcp as m
m.initialize()
def call(name,args):
    r=m.call(name,args)
    d=r.get('result',{}).get('structuredContent',r.get('result',{}))
    if d.get('success') is False: raise RuntimeError(json.dumps(d))
    return d
if __name__=='__main__':
    command=sys.argv[1]
    if command=='code': args={'action':'execute','code':Path(sys.argv[2]).read_text(encoding='utf-8-sig')};command='execute_code'
    elif command in ('play','stop'): args={'action':command};command='manage_editor'
    elif command=='refresh':args={'mode':'force','compile':'request','wait_for_ready':True};command='refresh_unity'
    elif command=='console':args={'action':'get','types':['error'],'count':30,'format':'detailed'};command='read_console'
    elif command=='clear':args={'action':'clear'};command='read_console'
    else:args=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig')) if len(sys.argv)>2 else {}
    print(json.dumps(call(command,args),ensure_ascii=True))
