from ar_fix3_local import *
console(out/'pre-polish-console.json')
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>();if(c!=null)c.Shutdown();UnityEditor.EditorApplication.isPlaying=false;return true;')
for i in range(30):
    time.sleep(.2)
    if not code('return UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;'):break
print('Edit mode')
