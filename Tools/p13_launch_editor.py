import subprocess
from pathlib import Path
editor=Path('C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe')
p=subprocess.Popen([str(editor),'-projectPath',str(Path.cwd()),'-logFile',str(Path('Logs/P13-Editor.log').resolve())],creationflags=subprocess.CREATE_NO_WINDOW)
print('Unity launched',p.pid)
