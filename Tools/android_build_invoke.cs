var callback=System.Linq.Enumerable.Single(UnityEditor.EditorApplication.delayCall.GetInvocationList(),d=>d.Method.DeclaringType.FullName=="MCPDynamicCode" && d.Method.Name=="<Execute>m__0");
UnityEditor.EditorApplication.delayCall-=(UnityEditor.EditorApplication.CallbackFunction)callback;
callback.DynamicInvoke();
return System.IO.File.ReadAllText("Artifacts/Android/editor-build-status.txt");
