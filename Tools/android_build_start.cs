System.IO.File.WriteAllText("Artifacts/Android/editor-build-status.txt","Queued " + System.DateTime.Now.ToString("O"));
UnityEditor.EditorApplication.delayCall += () => {
    System.IO.File.WriteAllText("Artifacts/Android/editor-build-status.txt","Building " + System.DateTime.Now.ToString("O"));
    try {
        CampusRift.BuildTools.AndroidApkBuild.Build();
        System.IO.File.WriteAllText("Artifacts/Android/editor-build-status.txt","Succeeded " + System.DateTime.Now.ToString("O"));
    } catch(System.Exception ex) {
        System.IO.File.WriteAllText("Artifacts/Android/editor-build-status.txt","Failed " + System.DateTime.Now.ToString("O") + "\n" + ex);
        UnityEngine.Debug.LogException(ex);
    }
};
return "Android APK build queued. Poll Artifacts/Android/editor-build-status.txt.";
