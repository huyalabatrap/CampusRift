"""Save final Console evidence without clearing or filtering away actual errors."""
import datetime,json,pathlib
import unity_mcp as m
m.initialize()
for filename,types in (('FinalConsole.json',['error']),('FinalConsoleWithWarnings.json',['error','warning'])):
    response=m.call('read_console',{'action':'get','types':types,'count':100,'format':'detailed'})
    data=response.get('result',{}).get('structuredContent',response)
    data['capturedAt']=datetime.datetime.now(datetime.timezone.utc).isoformat()
    pathlib.Path('Artifacts/Reactions',filename).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
    print(filename,data.get('success'),len(data.get('data',[])))
