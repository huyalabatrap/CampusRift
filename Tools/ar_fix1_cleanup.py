from ar_session import *
out=Path('task/ar/fix1')
console(out/'editor-stop-initial-console.json')
print(code('return new {playing=UnityEditor.EditorApplication.isPlaying,compiling=UnityEditor.EditorApplication.isCompiling,comboField=typeof(CampusRift.AR.ARBattleHUD).GetField("comboPanel",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!=null};'),flush=True)
# Existing gameplay navigation shuts XR down before unloading. Use that same order in the collector.
call('read_console',{'action':'clear'})
print('Raw Editor Stop diagnostic saved; next capture uses explicit Shutdown',flush=True)
