from ar_ui import *
connect();stop();call('refresh_unity',{});save('console-before-refresh-clear.json',call('read_console',{'action':'get','types':['error'],'count':100,'include_stacktrace':True}));call('read_console',{'action':'clear'})
print('Stopped Play mode and refreshed source',flush=True)
