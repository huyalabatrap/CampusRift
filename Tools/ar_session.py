"""Helpers for AR milestone collectors; each caller owns its fixture and evidence."""
import json,time
from pathlib import Path
import unity_mcp as m
m.initialize()
def call(name,args):
    for i in range(50):
        r=m.call(name,args).get('result',{}).get('structuredContent',{})
        if r.get('success'): return r
        if not any(s in str(r).lower() for s in ['no_unity_session','not ready','busy','compiling']):raise RuntimeError(str(r))
        time.sleep(2)
    raise RuntimeError(str(r))
def code(s):return call('execute_code',{'action':'execute','code':s})['data']['result']
def save(path,data):Path(path).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
def background():
    code('UnityEngine.Application.runInBackground=true;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;foreach(var d in UnityEngine.InputSystem.InputSystem.devices)UnityEngine.InputSystem.InputSystem.EnableDevice(d);UnityEditor.EditorUtility.ClearDirty(UnityEngine.InputSystem.InputSystem.settings);return true;')
def console(path):
    r=call('read_console',{'action':'get','types':['error'],'count':100,'format':'detailed'});save(path,r);return r
