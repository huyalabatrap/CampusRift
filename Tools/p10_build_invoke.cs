var callback=System.Linq.Enumerable.Single(UnityEditor.EditorApplication.delayCall.GetInvocationList(),d=>d.Method.DeclaringType.FullName.StartsWith("MCPDynamicCode"));
UnityEditor.EditorApplication.delayCall-=(UnityEditor.EditorApplication.CallbackFunction)callback;
callback.DynamicInvoke();
return System.IO.File.ReadAllText("Artifacts/Skills/P10-Build-status.txt");
