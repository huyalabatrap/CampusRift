#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Fresh Play session per affected suite; no unaffected suites are launched.
[InitializeOnLoad]
public static class StabilizeRunner
{
    const string Key="CampusRift.Stabilize.";
    static double started;
    static bool attached;
    static StabilizeRunner(){EditorApplication.update+=Tick;}
    public static string BeginQueue(string jobs)
    {
        if(EditorApplication.isPlaying||SessionState.GetBool(Key+"running",false))throw new InvalidOperationException("Already running");
        SessionState.SetString(Key+"queue",jobs);return "Queued affected suites";
    }
    public static string Begin(string name,string type,string done,float timeout=300,string report="")
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Need idle Edit Mode");
        SessionState.SetString(Key+"name",name);SessionState.SetString(Key+"type",type);SessionState.SetString(Key+"done",done);SessionState.SetFloat(Key+"timeout",timeout);
        SessionState.SetString(Key+"report",report);
        var path="Artifacts/V2/Stabilize/runs/"+name;
        Directory.CreateDirectory(path);
        SessionState.SetString(Key+"path",path);
        SessionState.SetString(Key+"stamp",File.Exists(done)?File.GetLastWriteTimeUtc(done).ToString("o"):"");
        SessionState.SetBool(Key+"running",true);attached=false;started=0;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.isPlaying=true;return "Started "+name;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!SessionState.GetBool(Key+"running",false))
        {
            string pending=SessionState.GetString(Key+"queue","");
            if(!string.IsNullOrEmpty(pending))
            {
                var lines=pending.Split(new[]{'\n'},StringSplitOptions.RemoveEmptyEntries);var job=lines[0].Split('|');
                SessionState.SetString(Key+"queue",string.Join("\n",lines.Skip(1)));
                Begin(job[0],job[1],job[2],float.Parse(job[3],System.Globalization.CultureInfo.InvariantCulture),job[4]);return;
            }
        }
        if(!SessionState.GetBool(Key+"running",false)||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode!=EditorApplication.isPlaying)return;
        string name=SessionState.GetString(Key+"name",""),path=SessionState.GetString(Key+"path",""),done=SessionState.GetString(Key+"done","");
        if(!EditorApplication.isPlaying)return;
        if(!attached)
        {
            attached=true;started=EditorApplication.timeSinceStartup;
            CampusRift.UI.TutorialDirector.Suppress=true;
            CampusRift.UI.UIStateManager.Instance.EnterScene(true);
            var settings=CampusRift.UI.SettingsManager.Instance.Current.Copy();settings.TelemetryConsentAsked=true;settings.LocalTelemetryEnabled=false;CampusRift.UI.SettingsManager.Instance.Apply(settings,false);
            Type type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(SessionState.GetString(Key+"type",""))).FirstOrDefault(t=>t!=null);
            if(type==null){Finish("ERROR: type missing");return;}
            new GameObject("Stabilize / "+name).AddComponent(type);
            File.WriteAllText(path+"/START.txt",DateTime.UtcNow.ToString("o"));return;
        }
        if(File.Exists(done)&&File.GetLastWriteTimeUtc(done).ToString("o")!=SessionState.GetString(Key+"stamp",""))
        {
            string content=File.ReadAllText(done);
            if(done.EndsWith("Chase.txt")&&!content.Contains("Twenty"))return;
            if(done.EndsWith("shaban-traversal-progress.txt")&&!content.Contains("DONE"))return;
            Finish(content);return;
        }
        if(EditorApplication.timeSinceStartup-started>SessionState.GetFloat(Key+"timeout",300))Finish("TIMEOUT");
    }
    static void Finish(string result)
    {
        string path=SessionState.GetString(Key+"path","");
        File.WriteAllText(path+"/DONE.txt",result);
        File.WriteAllText(path+"/SECONDS.txt",(EditorApplication.timeSinceStartup-started).ToString("F1"));
        string report=SessionState.GetString(Key+"report","");
        if(File.Exists(report))File.Copy(report,path+"/result"+Path.GetExtension(report),true);
        string summary=result.Replace("\r"," ").Replace("\n"," ");if(summary.Length>220)summary=summary.Substring(0,220)+"…";
        File.AppendAllText("task/stabilize/PROGRESS.md","\n- Smoke "+SessionState.GetString(Key+"name","")+": "+summary+" · `"+path+"` (raw retained).\n");
        SessionState.SetBool(Key+"running",false);EditorApplication.isPlaying=false;
    }
}
#endif
