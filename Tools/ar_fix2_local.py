"""Editor QA helper; uses a local file request, without an MCP server."""
import json,time,os
from pathlib import Path
out=Path('task/ar/fix2')
def code(s,timeout=60):
    req=out/'local-request.json';res=out/'local-response.json'
    assert not req.exists(), 'Prior command is pending'
    if res.exists():res.unlink()
    tmp=out/'local-request.tmp'
    tmp.write_text(json.dumps(dict(action='execute',code=s)),encoding='utf-8')
    os.replace(tmp,req)
    until=time.time()+timeout
    while time.time()<until:
        if res.exists():
            r=json.loads(res.read_text(encoding='utf-8-sig'))
            if not r.get('success'):
                if 'Sharing violation' in str(r):time.sleep(.25);continue
                raise RuntimeError(str(r))
            return r['data']['result']
        time.sleep(.25)
    raise TimeoutError(s[:100])
def save(path,data):Path(path).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
def background():
    code('UnityEngine.Application.runInBackground=true;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;foreach(var d in UnityEngine.InputSystem.InputSystem.devices)UnityEngine.InputSystem.InputSystem.EnableDevice(d);UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return true;')
def console(path):
    r=code('return MCPForUnity.Editor.Tools.ReadConsole.HandleCommand(Newtonsoft.Json.Linq.JObject.Parse("{\\\"action\\\":\\\"get\\\",\\\"types\\\":[\\\"error\\\"],\\\"count\\\":100,\\\"format\\\":\\\"detailed\\\"}"));')
    save(path,r);return r
def clear():code('return MCPForUnity.Editor.Tools.ReadConsole.HandleCommand(Newtonsoft.Json.Linq.JObject.Parse("{\\\"action\\\":\\\"clear\\\"}"));')
if __name__=='__main__':
    import sys
    s=Path(sys.argv[1]).read_text(encoding='utf-8-sig')
    print(json.dumps(code(s),ensure_ascii=True))
