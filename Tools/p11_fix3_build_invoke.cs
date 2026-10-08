var status=System.IO.File.ReadAllText("Artifacts/Reactions/fix3/Build-status.txt");
if(status!="Queued")return status;
var callback=System.Linq.Enumerable.Single(UnityEditor.EditorApplication.delayCall.GetInvocationList(),d=>d.Method.DeclaringType.FullName.StartsWith("MCPDynamicCode"));
UnityEditor.EditorApplication.delayCall-=(UnityEditor.EditorApplication.CallbackFunction)callback;
callback.DynamicInvoke();
return System.IO.File.ReadAllText("Artifacts/Reactions/fix3/Build-status.txt");
