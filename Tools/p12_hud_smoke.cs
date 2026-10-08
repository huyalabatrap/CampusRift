var world=new CampusRift.Skills.SkillSet1TestWorld{GameplayCamera=true};
world.Begin();world.Mode(true);
foreach(var v in world.victims)if(v!=null)CampusRift.Enemies.EnemyPool.Instance.Release(v.GetComponent<CampusRift.Enemies.EnemyInstance>());
var boss=CampusRift.Enemies.EnemyPool.Instance.Spawn(CampusRift.Levels.LevelCatalog.Instance.Get(7).bosses[0],world.origin+UnityEngine.Vector3.right*6,CampusRift.Levels.LevelCatalog.Instance.Get(7).Scaling);
boss.GetComponent<CampusRift.Monsters.MonsterBrain>().enabled=false;
boss.GetComponent<CampusRift.Monsters.MonsterCombat>().enabled=false;
double start=UnityEditor.EditorApplication.timeSinceStartup;bool captured=false;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
    CampusRift.UI.UIStateManager.Instance.EnterScene(true);
    double elapsed=UnityEditor.EditorApplication.timeSinceStartup-start;
    if(!captured&&elapsed>.7){
        captured=true;CampusRift.Controls.MobileTouchZone pause=null;
        foreach(var b in UnityEngine.Object.FindObjectsByType<CampusRift.Controls.MobileTouchZone>(UnityEngine.FindObjectsSortMode.None))
            if(b.role==CampusRift.Controls.TouchRole.Pause)pause=b;
        var panel=UnityEngine.GameObject.Find("Boss panel").GetComponent<UnityEngine.RectTransform>();
        var a=new UnityEngine.Vector3[4];var p=new UnityEngine.Vector3[4];panel.GetWorldCorners(a);
        bool separated=false,clicked=false,raycast=false;
        if(pause!=null){
            ((UnityEngine.RectTransform)pause.transform).GetWorldCorners(p);
            separated=a[2].y<p[0].y;
            var eventData=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=(p[0]+p[2])*.5f};
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData,hits);
            raycast=hits.Count>0&&(hits[0].gameObject==pause.gameObject||hits[0].gameObject.transform.IsChildOf(pause.transform));
            pause.OnPointerDown(eventData);clicked=CampusRift.UI.UIStateManager.Instance.State==CampusRift.UI.UIState.Paused;
            CampusRift.UI.UIStateManager.Instance.Resume();
        }
        System.IO.File.WriteAllText("Artifacts/P12/Boss-HUD-smoke.json","{\"target\":\""+UnityEditor.EditorUserBuildSettings.activeBuildTarget+"\",\"panelBelowPause\":"+separated.ToString().ToLowerInvariant()+",\"pauseRaycast\":"+raycast.ToString().ToLowerInvariant()+",\"pauseButtonResponds\":"+clicked.ToString().ToLowerInvariant()+"}");
        UnityEngine.ScreenCapture.CaptureScreenshot("task/p12/screens/final-smoke/boss-hud.png");
    }
    if(elapsed>2){UnityEditor.EditorApplication.update-=tick;CampusRift.Enemies.EnemyPool.Instance.ReleaseAll();world.End();System.IO.File.WriteAllText("Artifacts/P12/Boss-HUD-smoke-DONE.txt","done");}
};
UnityEditor.EditorApplication.update+=tick;
return "Boss HUD smoke scheduled";
