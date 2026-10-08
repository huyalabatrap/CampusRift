#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Levels;
// Explicit capture-only staging. No scene attachment, initializer or player-build code.
public sealed class TrailerB3Lineup : MonoBehaviour
{
    public string take="B3-lineup-d";
    public bool record=true;
    readonly List<Transform> actors=new List<Transform>();
    readonly List<Animator> animators=new List<Animator>();
    readonly List<Renderer[]> meshes=new List<Renderer[]>();
    readonly List<string> states=new List<string>();
    readonly List<Renderer> hidden=new List<Renderer>();
    Camera cam; RiftPortal portal; LineRenderer violet,gold;
    Vector3 source=new Vector3(-6.5f,.13f,4.2f);
    float begin; public float age;
    IEnumerator Start()
    {
        foreach(var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))c.enabled=false;
        var player=FindAnyObjectByType<CampusRift.CampusExplorer>();player.enabled=false;
        foreach(var r in player.GetComponentsInChildren<Renderer>()){r.enabled=false;hidden.Add(r);}
        foreach(var brain in FindObjectsByType<CampusRift.Monsters.MonsterBrain>(FindObjectsSortMode.None))
            foreach(var r in brain.GetComponentsInChildren<Renderer>()){r.enabled=false;hidden.Add(r);}
        if(LevelDirector.Instance!=null)LevelDirector.Instance.enabled=false;EnemyPool.Ensure().ReleaseAll();
        CampusRift.UI.SettingsManager.Instance.Sky.SetPreset(LevelCatalog.Instance.Get(1).sky);
        cam=Camera.main;cam.fieldOfView=80;
        // Remove two foreground occluders only in this unsaved capture session.
        foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if(r.name=="T59_Tree_Bang_Courtyard_NW"||r.name=="T59_Tree_Bang_Courtyard_NE"||r.name.StartsWith("T59_Und_CourtL_")||r.name.StartsWith("ShrubBed_Courtyard_L_")||r.name=="T59_SlatBench_02"||r.name=="T59_SlatBench_03"||r.name=="T59_StoneBench_07"||r.name=="T59_Lamp_Courtyard_02"){r.enabled=false;hidden.Add(r);}
        var roster=new List<EnemyArchetype>();
        // Use the archetypes actually referenced by current levels, unique by ID.
        foreach(var level in LevelCatalog.Instance.levels)
          if(level.spawnTable!=null)foreach(var row in level.spawnTable.roster)
            if(row.archetype!=null&&!roster.Any(x=>x.id==row.archetype.id))roster.Add(row.archetype);
        roster=roster.OrderBy(x=>x.id=="tieu-yeu"?0:1).ThenBy(x=>x.id).ToList();
        roster.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/shaban.asset"));
        roster.Add(Resources.Load<EnemyArchetype>("P12/ShabanElite"));
        foreach(var level in LevelCatalog.Instance.levels)foreach(var boss in level.bosses)
          if(boss!=null&&!roster.Any(x=>x.id==boss.id))roster.Add(boss);
        var evidence=new List<object>();
        for(int i=0;i<roster.Count;i++){
            var data=roster[i];var construction=new GameObject("Trailer inactive construction");construction.SetActive(false);
            var e=Instantiate(data.prefab,construction.transform);
            if(e==null)throw new InvalidOperationException("Missing lineup actor: "+data.id);
            foreach(var b in e.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
            foreach(var agent in e.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))agent.enabled=false;
            foreach(var c in e.GetComponentsInChildren<Collider>(true))c.enabled=false;
            var anim=e.GetComponentInChildren<Animator>();anim.enabled=true;anim.applyRootMotion=false;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var rs=e.GetComponentsInChildren<Renderer>();foreach(var r in rs)r.enabled=false;
            e.transform.SetParent(transform,true);e.SetActive(true);Destroy(construction);
            e.transform.localScale*=.7f;foreach(var r in rs)r.enabled=false;
            actors.Add(e.transform);animators.Add(anim);meshes.Add(rs);states.Add("");
            evidence.Add(new {index=i,id=data.id,name=data.LocalizedName(true),asset=UnityEditor.AssetDatabase.GetAssetPath(data),prefab=UnityEditor.AssetDatabase.GetAssetPath(data.prefab),ranged=data.ranged,boss=data.isBoss,spawnAt=2.2f+i*.5f,slotX=-3+(roster.Count-1-i)*1.9f,slotZ=-.5f,uniformScale=.7f,hasWalk=anim.HasState(0,Animator.StringToHash("Base Layer.Walk_Forward")),originalAnimation=anim.HasState(0,Animator.StringToHash("Base Layer.OriginalAnimation"))});
        }
        File.WriteAllText("task/trailer/output/b3-lineup-roster.json",Newtonsoft.Json.JsonConvert.SerializeObject(evidence,Newtonsoft.Json.Formatting.Indented));
        portal=RiftPortal.Open(source,Vector3.back);violet=Ring("violet",new Color(.63f,.26f,1),.11f);gold=Ring("gold",new Color(1,.7f,.02f),.035f);
        yield return null;begin=Time.time;
        if(record)TrailerCaptureOnly.Record(take,450,false);
        while(!record||FindAnyObjectByType<TrailerCaptureOnly>()!=null){yield return null;}
        File.WriteAllText("task/trailer/"+take+"-DONE.txt","15 archetypes, sequential .5s cadence, straight row; explicit staged paths and native animation");
        enabled=false;
    }
    void Update()
    {
        if(cam==null||actors.Count==0||portal==null)return;
        age=Time.time-begin;
        // Wide low camera: mild sideways move; final hold shows every slot and the portal.
        float pan=Mathf.SmoothStep(0,1,Mathf.Clamp01((age-10)/3));
        cam.transform.position=new Vector3(9.5f+pan*.7f,3.3f,-11);
        cam.transform.LookAt(new Vector3(9.8f,1.2f,.8f));
        typeof(RiftPortal).GetField("age",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(portal,.65f);
        RenderRing(violet,age,1);RenderRing(gold,age,1.035f);
        for(int i=0;i<actors.Count;i++){
            float t=age-(2.2f+i*.5f);if(t<0){foreach(var r in meshes[i])r.enabled=false;continue;}
            foreach(var r in meshes[i])r.enabled=true;
            float x=-3+(actors.Count-1-i)*1.9f;
            Vector3 exit=source+Vector3.back*1.2f,lane=new Vector3(x,.13f,3f),slot=new Vector3(x,.13f,-.5f);
            float a=1.2f,b=Vector3.Distance(exit,lane),c=3.5f,d=Mathf.Max(0,t-.14f)*5.2f;
            Vector3 pos,dir;
            if(d<a){pos=Vector3.Lerp(source,exit,d/a);dir=Vector3.back;}
            else if(d<a+b){pos=Vector3.Lerp(exit,lane,(d-a)/b);dir=Vector3.right;}
            else if(d<a+b+c){pos=Vector3.Lerp(lane,slot,(d-a-b)/c);dir=Vector3.back;}
            else {pos=slot;dir=Vector3.back;}
            actors[i].SetPositionAndRotation(pos,Quaternion.LookRotation(dir));
            bool stopped=d>=a+b+c;
            string state=animators[i].HasState(0,Animator.StringToHash("Base Layer.Walk_Forward"))?(t<.14f?"Spawn":stopped?"Idle_Breathe":"Walk_Forward"):"OriginalAnimation";
            if(actors[i].name.StartsWith("DucYeu")){actors[i].position+=Vector3.up*.65f;state=t<.14f?"Spawn":stopped?"Hover":"Fly";}
            if(states[i]!=state){animators[i].CrossFadeInFixedTime(state,.1f,0,0);states[i]=state;}
            animators[i].speed=stopped?.8f:1.4f;
        }
    }
    void LateUpdate(){foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;foreach(var r in hidden)if(r!=null)r.enabled=false;}
    LineRenderer Ring(string name,Color color,float width){var go=new GameObject("Trailer B3 "+name);go.transform.SetParent(transform);go.transform.position=source;var l=go.AddComponent<LineRenderer>();l.sharedMaterial=Resources.Load<Material>("EnemyVfx/EnemyTrail");l.useWorldSpace=false;l.loop=true;l.positionCount=96;l.widthMultiplier=width;l.startColor=l.endColor=color;return l;}
    void RenderRing(LineRenderer l,float t,float scale){float opening=Mathf.SmoothStep(.01f,1,Mathf.Clamp01((t-.8f)/.8f));for(int i=0;i<96;i++){float a=i*Mathf.PI*2/96,w=1+.025f*Mathf.Sin(a*7+t*4);l.SetPosition(i,new Vector3(Mathf.Cos(a)*1.35f*opening*w*scale,1.6f+Mathf.Sin(a)*1.6f*opening*w*scale,.04f*Mathf.Sin(a*4+t*3)));}}
}
#endif
