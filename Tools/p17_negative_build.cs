UnityEditor.EditorApplication.CallbackFunction run=null;run=()=>{UnityEditor.EditorApplication.update-=run;
    var catalog=Resources.Load<CampusRift.Learning.LearningCatalog>("LearningCatalog");var chapter=catalog.courses[0];bool before=chapter.isPlaceholder;string chapterPath=UnityEditor.AssetDatabase.GetAssetPath(chapter);
    System.IO.Directory.CreateDirectory("task/p17/gate-negative");
    try{
        chapter.isPlaceholder=true;UnityEditor.EditorUtility.SetDirty(chapter);UnityEditor.AssetDatabase.SaveAssetIfDirty(chapter);
        var scenes=new System.Collections.Generic.List<string>();foreach(var s in UnityEditor.EditorBuildSettings.scenes)if(s.enabled)scenes.Add(s.path);
        var report=UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions{scenes=scenes.ToArray(),locationPathName="task/p17/gate-negative/CampusRift.apk",target=UnityEditor.BuildTarget.Android,options=UnityEditor.BuildOptions.None});
        System.IO.File.WriteAllText("task/p17/negative-build.txt","Placeholder build result="+report.summary.result+"; errors="+report.summary.totalErrors+"; expected Failed before player output.");
    }catch(System.Exception e){System.IO.File.WriteAllText("task/p17/negative-build.txt",e.ToString());}
    finally{chapter=UnityEditor.AssetDatabase.LoadAssetAtPath<CampusRift.Learning.CourseData>(chapterPath);chapter.isPlaceholder=before;UnityEditor.EditorUtility.SetDirty(chapter);UnityEditor.AssetDatabase.SaveAssetIfDirty(chapter);System.IO.File.WriteAllText("task/p17/negative-build-DONE.txt","Catalog restored; isPlaceholder="+chapter.isPlaceholder);}
};UnityEditor.EditorApplication.update+=run;return "Negative build queued; finally restores original course flag.";
