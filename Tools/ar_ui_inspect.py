from ar_ui import *
connect()
print(json.dumps(call('read_console',{'action':'get','types':['error'],'count':15,'include_stacktrace':True}),ensure_ascii=True),flush=True)
print(json.dumps(code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();return new {EditorApplication.isPlaying,EditorApplication.isCompiling,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,field=f!=null,root=f!=null&&f.Root!=null,mode=f!=null?f.Mode.id:"none",ready=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>()?.Ready};'),ensure_ascii=True),flush=True)
