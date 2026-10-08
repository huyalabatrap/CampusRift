from ar_ui import *
connect()
print([t for t in u.rpc('tools/list')['result']['tools'] if t['name']=='set_active_instance'])
print(json.dumps(u.rpc('resources/read',{'uri':'mcpforunity://instances'}),ensure_ascii=True)[:1500])
print(json.dumps(u.call('execute_custom_tool',{'tool_name':'execute_code','parameters':{'action':'execute','code':'return true;'}}),ensure_ascii=True)[:1500])
