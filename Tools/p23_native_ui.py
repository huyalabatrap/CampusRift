"""Drive the actual Windows release UI and retain direct client-area screenshots.

Usage: launch; shot NAME; click X Y NAME; key KEY NAME; close.
Coordinates are fractions of the current client rectangle. No player-side QA code.
"""
from pathlib import Path
import ctypes, json, subprocess, sys, time
import pyautogui as ui
import win32gui, win32process, win32con
from PIL import ImageGrab

ctypes.windll.user32.SetProcessDPIAware()
root=Path.cwd()
state=root/'task/p23/native-window.json'
shots=root/'task/p23/screens'
shots.mkdir(parents=True,exist_ok=True)
exe=(root/'Releases/2026-10-04-v1.0/Windows/release/CampusRift.exe').resolve()
log=root/'task/p23/windows-release-player.log'

def window():
    data=json.loads(state.read_text())
    windows=[]
    win32gui.EnumWindows(lambda h,_: windows.append(h) if win32process.GetWindowThreadProcessId(h)[1]==data['pid'] and win32gui.GetClientRect(h)[2]>100 else None,None)
    assert windows,'No release player window'
    h=windows[0]
    win32gui.ShowWindow(h,win32con.SW_RESTORE)
    try:win32gui.SetForegroundWindow(h)
    except Exception:
        ui.press('alt');win32gui.SetForegroundWindow(h)
    l,t=win32gui.ClientToScreen(h,(0,0)); _,_,w,v=win32gui.GetClientRect(h)
    return h,(l,t,w,v)

def record(action,name):
    time.sleep(.5)
    h,(l,t,w,v)=window()
    path=shots/(name+'.png')
    ImageGrab.grab(bbox=(l,t,l+w,t+v)).save(path)
    with (root/'task/p23/native-actions.jsonl').open('a',encoding='utf-8') as f:
        f.write(json.dumps({'time':time.strftime('%Y-%m-%dT%H:%M:%S'),'action':action,'image':path.relative_to(root).as_posix(),'client':[l,t,w,v]},ensure_ascii=False)+'\n')
    print(path, w,v,flush=True)

action=sys.argv[1]
if action=='launch':
    assert exe.is_file()
    startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=win32con.SW_HIDE
    p=subprocess.Popen([str(exe),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',str(log)],cwd=exe.parent,startupinfo=startup)
    state.write_text(json.dumps({'pid':p.pid,'exe':str(exe),'started':time.strftime('%Y-%m-%dT%H:%M:%S')}),encoding='utf-8')
    time.sleep(10);record('launch actual release','windows-release-menu')
elif action=='shot': record('observe',sys.argv[2])
elif action=='click':
    _,(l,t,w,v)=window();ui.click(l+float(sys.argv[2])*w,t+float(sys.argv[3])*v)
    time.sleep(float(sys.argv[5]) if len(sys.argv)>5 else 1)
    record('click '+sys.argv[2]+' '+sys.argv[3],sys.argv[4])
elif action=='key':
    window();ui.press(sys.argv[2]);time.sleep(1);record('key '+sys.argv[2],sys.argv[3])
elif action=='hold':
    window();keys=sys.argv[2].split('+')
    for key in keys:ui.keyDown(key)
    try:time.sleep(float(sys.argv[3]))
    finally:
        for key in reversed(keys):ui.keyUp(key)
    record('hold '+sys.argv[2]+' '+sys.argv[3],sys.argv[4])
elif action=='close':
    h,_=window();win32gui.PostMessage(h,win32con.WM_CLOSE,0,0);print('Release close requested')
else:raise ValueError(action)
