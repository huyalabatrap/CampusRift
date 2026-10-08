#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Local, file-based Editor commands for fix2 QA when the MCP server is unavailable.
[InitializeOnLoad]
public static class ARFix2LocalRunner
{
    const string Request = "task/ar/fix2/local-request.json";
    const string Response = "task/ar/fix2/local-response.json";
    static ARFix2LocalRunner() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            UnityEditor.BuildPipeline.isBuildingPlayer || !File.Exists(Request)) return;
        if ((DateTime.UtcNow-File.GetLastWriteTimeUtc(Request)).TotalMilliseconds < 150) return;
        try
        {
            var request = JObject.Parse(File.ReadAllText(Request));
            File.Copy(Request, "task/ar/fix2/local-consumed.json", true);
            File.Delete(Request);
            var result = MCPForUnity.Editor.Tools.ExecuteCode.HandleCommand(request);
            File.WriteAllText(Response, JsonConvert.SerializeObject(result, Formatting.Indented));
        }
        catch (IOException) { /* File watcher may briefly own a newly renamed request. Retry next update. */ }
        catch (Exception e) { File.WriteAllText(Response, JsonConvert.SerializeObject(new {success=false,error=e.ToString()})); }
    }
}
#endif
