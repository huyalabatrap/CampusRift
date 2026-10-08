import sys,json,pathlib
import unity_mcp as m
m.initialize()
command=sys.argv[1]
if command=='code': result=m.call('execute_code',{'action':'execute','code':pathlib.Path(sys.argv[2]).read_text(encoding='utf-8')})
elif command=='menu': result=m.call('execute_menu_item',{'menu_path':sys.argv[2]})
elif command in ('play','stop','pause'): result=m.call('manage_editor',{'action':command})
elif command=='console': result=m.call('read_console',{'action':'get','types':['error','warning'],'count':30,'format':'detailed'})
elif command=='clear': result=m.call('read_console',{'action':'clear'})
elif command=='refresh': result=m.call('refresh_unity',{'mode':'force','compile':'request','wait_for_ready':True})
elif command=='shot': result=m.call('manage_camera',{'action':'screenshot','screenshot_file_name':sys.argv[2],'output_folder':'Artifacts/UI','include_image':False})
else: result=m.call(command,json.loads(pathlib.Path(sys.argv[2]).read_text()))
print(json.dumps(result.get('result',{}).get('structuredContent',result),ensure_ascii=True))
