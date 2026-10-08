#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using CampusRift.Levels;
using CampusRift.Enemies;
// Trailer-only portal animation staging. Never attached or enabled automatically.
public sealed class TrailerPortalCapture : MonoBehaviour
{
    Camera cam;EnemyInstance enemy;RiftPortal crack;LineRenderer violet,gold;Vector3 centre;float begun;
    IEnumerator Start()
    {
        foreach(var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))c.enabled=false;
        var player=FindAnyObjectByType<CampusRift.CampusExplorer>();player.enabled=false;
        foreach(var r in player.GetComponentsInChildren<Renderer>())r.enabled=false;
        var director=LevelDirector.Instance;if(director!=null)director.enabled=false;
        EnemyPool.Ensure().ReleaseAll();
        foreach(var p in FindObjectsByType<CampusRift.Skills.SkillVfxPool>(FindObjectsSortMode.None))p.Clear();
        CampusRift.UI.SettingsManager.Instance.Sky.SetPreset(LevelCatalog.Instance.Get(1).sky);
        cam=Camera.main;cam.fieldOfView=58;
        cam.transform.position=new Vector3(7,2,-9);cam.transform.LookAt(new Vector3(12,2,5));
        yield return null;TrailerCaptureOnly.Record("S01b2",117,false,0,true,.8f,4);
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
        centre=new Vector3(10,.13f,2);
        var data=LevelCatalog.Instance.Get(1).spawnTable.roster.Select(x=>x.archetype).First(x=>x!=null&&x.id=="tieu-yeu");
        enemy=EnemyPool.Ensure().Spawn(data,centre+Vector3.forward*.5f,EnemyScaling.Default,false);
        if(enemy==null)throw new InvalidOperationException("No campus enemy at portal staging location");
        foreach(var b in enemy.GetComponents<MonoBehaviour>())if(!(b is EnemyInstance)&&!(b is CampusRift.Monsters.MonsterVitality))b.enabled=false;
        var agent=enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent!=null)agent.enabled=false;
        crack=RiftPortal.Open(centre,Vector3.back);
        violet=Ring("Violet space rim",new Color(.63f,.26f,1),.14f);gold=Ring("Gold space rim",new Color(1,.7f,.02f),.045f);
        for(int angle=0;angle<3;angle++){
            cam.transform.position=angle==0?new Vector3(10,1.1f,-5):angle==1?new Vector3(7,1.6f,-3):new Vector3(11.3f,.7f,-3.5f);
            cam.transform.LookAt(centre+Vector3.up*1.15f);
            enemy.transform.position=centre+Vector3.forward*.5f;enemy.transform.rotation=Quaternion.LookRotation(Vector3.back);enemy.Animation.Play("Spawn",1,1);
            begun=Time.time;
            TrailerCaptureOnly.Record("S02b2-angle"+angle,156,false);
            while(FindAnyObjectByType<TrailerCaptureOnly>()!=null){
                float age=Time.time-begun;float opening=Mathf.SmoothStep(.01f,1,Mathf.Clamp01(age/.55f));
                RenderRing(violet,opening,age,1);RenderRing(gold,opening,age,1.07f);
                typeof(RiftPortal).GetField("age",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(crack,.65f);
                if(age>1){if(enemy.Animation.CurrentState!="Walk_Forward")enemy.Animation.Play("Walk_Forward",.65f,0);enemy.transform.position=centre+Vector3.forward*Mathf.Lerp(.5f,-2.8f,Mathf.Clamp01((age-1)/3.5f));}
                cam.transform.position+=cam.transform.forward*(Time.deltaTime*.08f);
                yield return null;
            }
        }
        File.WriteAllText("task/trailer/B2-PORTAL-DONE.txt","Three angles; actual Tiểu Yêu Spawn/Walk and RiftPortal, extra dev-only purple/gold rings and staged path");
        Destroy(gameObject);
    }
    LineRenderer Ring(string name,Color color,float width){var go=new GameObject("Trailer "+name);go.transform.position=centre;var line=go.AddComponent<LineRenderer>();line.sharedMaterial=Resources.Load<Material>("EnemyVfx/EnemyTrail");line.useWorldSpace=false;line.loop=true;line.positionCount=96;line.widthMultiplier=width;line.startColor=line.endColor=color;line.numCapVertices=4;return line;}
    void RenderRing(LineRenderer line,float opening,float age,float scale){for(int i=0;i<96;i++){float a=i*Mathf.PI*2/96;float wobble=1+.03f*Mathf.Sin(a*7+age*4);line.SetPosition(i,new Vector3(Mathf.Cos(a)*1.0f*opening*wobble*scale,1.5f+Mathf.Sin(a)*1.5f*opening*wobble*scale,.05f*Mathf.Sin(a*4+age*3)));}}
}
#endif
