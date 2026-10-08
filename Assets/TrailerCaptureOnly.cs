#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
// Capture-only. Never attached by default and never saved into a scene.
public sealed class TrailerCaptureOnly : MonoBehaviour
{
    public string take;
    public int frames=117;
    public bool realtime;
    public float seconds=3.9f;
    public bool ownCamera;
    public float travel=.4f, yawTravel;
    public string action;
    bool aimed, ice, lightning, dashed;
    Vector3 target;
    Camera cam; Vector3 start; Quaternion rotation; int oldCapture,oldFps,oldVsync;
    double begun; int index; bool restored;
    public static string Record(string name,int count,bool real=false,float duration=0,bool camera=false,float move=0,float pan=0)
    {
        if(FindAnyObjectByType<TrailerCaptureOnly>()!=null)throw new InvalidOperationException("Capture already running");
        var go=new GameObject("Trailer capture only");
        var r=go.AddComponent<TrailerCaptureOnly>();r.take=name;r.frames=count;r.realtime=real;r.seconds=duration;r.ownCamera=camera;r.travel=move;r.yawTravel=pan;
        return "Recording "+name;
    }
    void LateUpdate()
    {
        if(!ownCamera||cam==null)return;
        float t=realtime?(float)(Time.realtimeSinceStartupAsDouble-begun)/seconds:index/(float)Mathf.Max(1,frames-1);
        t=Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
        cam.transform.SetPositionAndRotation(start+rotation*Vector3.forward*(travel*t),rotation*Quaternion.Euler(0,yawTravel*t,0));
    }
    void Update()
    {
        // Editor capture may lose OS focus to the orchestration client. Resume only this explicit take.
        if(CampusRift.UI.UIStateManager.Instance!=null&&CampusRift.UI.UIStateManager.Instance.State==CampusRift.UI.UIState.Paused)CampusRift.UI.UIStateManager.Instance.Resume();
        if(action=="sky"&&cam!=null){var b=CampusRift.SkyBeast.SkyBeastScheduler.Instance?.SwordTarget;if(b!=null){cam.transform.position=new Vector3(3,3,-2);cam.transform.LookAt(b.transform.position);}}
        if(action=="boss"&&cam!=null){var bosses=FindObjectsByType<CampusRift.Enemies.EnemyInstance>(FindObjectsSortMode.None);foreach(var b in bosses)if(b.Alive&&b.archetype.isBoss){var renderers=b.GetComponentsInChildren<SkinnedMeshRenderer>();if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);cam.transform.position=bounds.center+new Vector3(0,.3f,-1).normalized*Mathf.Max(9,bounds.size.magnitude*1.25f);cam.transform.LookAt(bounds.center);}break;}}
        if(action=="portal"&&!aimed){var portal=FindAnyObjectByType<CampusRift.Levels.RiftPortal>();if(portal!=null&&portal.gameObject.activeInHierarchy){target=portal.transform.position;cam.transform.position=target+portal.transform.forward*7+Vector3.up*2.2f;cam.transform.LookAt(target+Vector3.up*1.1f);aimed=true;ownCamera=false;}}
        var player=FindAnyObjectByType<CampusRift.CampusExplorer>();
        if(action=="combo"&&player!=null){var loadout=player.GetComponent<CampusRift.Skills.SkillLoadout>();if(!ice&&index>=8){ice=true;var e=CampusRift.Levels.LevelDirector.Instance.Alive;foreach(var enemy in e)if(enemy!=null&&enemy.Alive){target=enemy.transform.position;break;}bool ok=((CampusRift.Skills.IceSealRuntime)loadout.Get(1)).CastAt(target);File.AppendAllText("task/trailer/raw/"+take+"/actions.txt","Ice cast="+ok+"\n");}if(!lightning&&index>=25){lightning=true;bool ok=((CampusRift.Skills.ChainLightningRuntime)loadout.Get(2)).CastAt(target);File.AppendAllText("task/trailer/raw/"+take+"/actions.txt","Lightning cast="+ok+"\n");}}
        if(action=="mobile"&&player!=null){var input=player.GetComponent<CampusRift.Controls.CampusInput>();input.TouchMove=index<25?new Vector2(.35f,.35f):Vector2.zero;if(!dashed&&index>25){dashed=true;input.Press(CampusRift.Controls.CampusAction.Dash);input.Press(CampusRift.Controls.CampusAction.Attack);}}
    }
    IEnumerator Start()
    {
        string dir="task/trailer/raw/"+take;
        if(Directory.Exists(dir)){File.WriteAllText("task/trailer/capture-error.txt","Take exists: "+take);Destroy(gameObject);yield break;}
        Directory.CreateDirectory(dir);
        cam=Camera.main; start=cam.transform.position;rotation=cam.transform.rotation;
        oldCapture=Time.captureFramerate;oldFps=Application.targetFrameRate;oldVsync=QualitySettings.vSyncCount;
        Time.captureFramerate=realtime?0:30;Application.targetFrameRate=30;QualitySettings.vSyncCount=0;
        begun=Time.realtimeSinceStartupAsDouble;
        File.WriteAllText(dir+"/take.json",JsonUtility.ToJson(new Take{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,frames=frames,realtime=realtime,seconds=seconds,level=CampusRift.Levels.LevelDirector.Instance!=null?CampusRift.Levels.LevelDirector.Instance.Level.index:0,mode=CampusRift.Controls.CampusInput.Mobile?"Mobile":"PC",cameraPosition=start,cameraEuler=rotation.eulerAngles,staging="Disposable profile; real runtime; capture only"},true));
        while(realtime?Time.realtimeSinceStartupAsDouble-begun<seconds:index<frames)
        {
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            if(texture!=null){File.WriteAllBytes(dir+"/frame-"+index.ToString("D6")+".png",texture.EncodeToPNG());Destroy(texture);}
            var b2=FindAnyObjectByType<TrailerB2Capture>();if(b2!=null)b2.LogFrame(dir,index);
            File.AppendAllText(dir+"/timestamps.csv",index+","+(Time.realtimeSinceStartupAsDouble-begun).ToString("F6",System.Globalization.CultureInfo.InvariantCulture)+"\n");index++;
        }
        Restore();File.WriteAllText(dir+"/DONE.txt",index+" frames");Destroy(gameObject);
    }
    void Restore(){if(restored)return;restored=true;Time.captureFramerate=oldCapture;Application.targetFrameRate=oldFps;QualitySettings.vSyncCount=oldVsync;}
    void OnDestroy(){Restore();}
    [Serializable]class Take{public string scene,mode,staging;public int frames,level;public bool realtime;public float seconds;public Vector3 cameraPosition,cameraEuler;}
}
#endif
