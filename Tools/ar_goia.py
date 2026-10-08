"""Goi A local editor calls and evidence, never orchestration scripts."""
import json,sys,pathlib
import unity_mcp as u
u.initialize()
if sys.argv[1]=='refresh': result=u.call('refresh_unity',{'mode':'force','scope':'all','compile':'request','wait_for_ready':False})
elif sys.argv[1]=='errors': result=u.call('read_console',{'action':'get','types':['error'],'count':50,'include_stacktrace':True,'format':'detailed'})
elif sys.argv[1]=='clear': result=u.call('read_console',{'action':'clear'})
elif sys.argv[1] in ('play','stop'): result=u.call('manage_editor',{'action':sys.argv[1]})
elif sys.argv[1]=='code':
    result=u.call('execute_code',{'action':'execute','code':pathlib.Path(sys.argv[2]).read_text(encoding='utf-8-sig'),'safety_checks':False})
elif sys.argv[1]=='read': result=u.rpc('resources/read',{'uri':sys.argv[2]})
else: result=u.call(sys.argv[1],json.loads(sys.argv[2]) if len(sys.argv)>2 else {})
if len(sys.argv)>3:
    pathlib.Path(sys.argv[3]).write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps(result,ensure_ascii=True))
