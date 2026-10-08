#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CampusRift.AR;
using CampusRift.Combat;
using CampusRift.Skills;
// Explicitly created for trailer capture only. No scene attachment or automatic initializer.
[DefaultExecutionOrder(-800)]
public sealed class TrailerB2Capture : MonoBehaviour
{
    public string label="None", job="ar";
    ARBattlefield field; ARSkillCaster caster; MockGestureSource mock; Camera cam;
    float next; bool staged; public bool followBoss; Vector3 cameraPosition; Quaternion cameraRotation;
    const BindingFlags BF=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    public static void Set(object o,string key,object value){o.GetType().GetField("<"+key+">k__BackingField",BF).SetValue(o,value);}
    void Update()
    {
        if(!staged||field==null||field.Root==null)return;
        bool wasPaused=field.Paused;Set(field,"Paused",false);Set(field,"AnchorLost",false);
        if(wasPaused)Set(field,"Clock",field.Clock+field.CombatDelta);
        field.UserPaused=field.MenuPaused=false;
        caster.source.SetSamplingActive(true);
        if(Time.unscaledTime>=next){next=Time.unscaledTime+.05f;Emit(label);}
    }
    void LateUpdate(){if(staged&&cam!=null){var boss=field.GetComponent<ARSpaceModes>().Boss;if(followBoss&&boss.WeakPoint!=null)cameraRotation=Quaternion.LookRotation(boss.WeakPoint.position-cameraPosition);cam.transform.SetPositionAndRotation(cameraPosition,cameraRotation);}}
    void Emit(string value)
    {
        var hand=value=="None"?null:GestureSyntheticLandmarks.Create(value,new Vector2(.5f,.5f),.36f);
        if(value=="Thumb_Up"){
            hand=GestureSyntheticLandmarks.Create("Thumb_Down",new Vector2(.5f,.5f),.36f);
            hand[9]=hand[6];hand[10]=hand[1]-.12f;hand[12]=hand[6];hand[13]=hand[1]-.24f;
        }
        var f=new GestureFrame{label=value,score=1,handed="Right",landmarks=hand,timestampMs=(long)(Time.realtimeSinceStartupAsDouble*1000),width=Screen.width,height=Screen.height,screenCoordinates=true};
        if(value=="Pointing_Up"){
            var points=(Vector3[])typeof(ARGestureUnitTests).GetField("Open",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            points=(Vector3[])points.Clone();
            for(int digit=1;digit<4;digit++){int m=5+digit*4;float x=points[m].x;points[m+1]=new Vector3(x,.025f,0);points[m+2]=new Vector3(x,.02f,.025f);points[m+3]=new Vector3(x,-.01f,.018f);}
            points[3]=new Vector3(-.045f,-.005f,.01f);points[4]=new Vector3(-.01f,-.01f,.025f);
            f.worldLandmarks=new float[63];for(int i=0;i<21;i++){f.worldLandmarks[3*i]=points[i].x;f.worldLandmarks[3*i+1]=points[i].y;f.worldLandmarks[3*i+2]=points[i].z;}
        }
        // The same bridge entry used by MockGestureSource; D1/caster/gameplay remain unchanged.
        typeof(GestureRecognizerBridge).GetMethod("ReceiveMock",BF).Invoke(caster.source,new object[]{f});
    }
    public void LogFrame(string dir,int index){File.AppendAllText(dir+"/hands.csv",index+","+label+"\n");}
    IEnumerator Hold(string value,float duration){label=value;yield return new WaitForSecondsRealtime(duration);}
    void Ready(){if(caster.Caster==null)return;foreach(var r in caster.Caster.GetComponents<SkillRuntime>())r.ReadyOnRestEquip();caster.Caster.GetComponent<SpiritPower>().Refill();}
    IEnumerator Record(string take,float seconds)
    {
        TrailerCaptureOnly.Record(take,0,true,seconds);
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
    }
    IEnumerator Mode(string id,bool daily=false)
    {
        staged=false;field.placement.Reposition();yield return null;
        field.ModeSession.Choose(ARModeCatalog.All.First(m=>m.id==id),0,daily);
        var selection=field.GetComponent<ARModeSelectionHUD>();selection.GetType().GetField("opened",BF).SetValue(selection,false);
        field.placement.enabled=false;field.placement.settings.Floor=false;
        field.placement.GetType().GetField("radius",BF).SetValue(field.placement,.5f);
        var root=new GameObject("Trailer fixed simulated table battlefield").transform;
        root.position=new Vector3(0,.78f,0);root.localScale=Vector3.one*field.placement.ActualScale;
        Set(field.placement,"Root",root);Set(field.placement,"Adjusting",false);
        ARRune.Create(root,field.placement.Radius/field.Scale);
        typeof(ARBattlefield).GetMethod("Build",BF).Invoke(field,new object[]{root,null});
        field.ModeSession.KnowledgeEnabled=false;field.Shrine.SetProgressionMaxHealth(10000);field.Shrine.Revive(1,0);
        cameraPosition=new Vector3(.10f,1.64f,1.25f);cameraRotation=Quaternion.LookRotation(new Vector3(0,.8f,0)-cameraPosition);
        staged=true;label="None";yield return new WaitForSecondsRealtime(1.5f);
        field.GetComponent<ARBattleHUD>().SetHelp(false);field.GetComponent<ARBattleHUD>().SetMenu(false);
        caster.CastAttempted+=(name,ok)=>File.AppendAllText("task/trailer/b2-actions.log",DateTime.UtcNow.ToString("O")+" "+field.Mode.id+" "+name+" "+ok+"\n");
        caster.UltimateFired+=u=>File.AppendAllText("task/trailer/b2-actions.log","ULTIMATE "+u+"\n");
    }
    void Room()
    {
        // Hide provider environment meshes from the fixed virtual tabletop camera.
        foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))r.enabled=false;
        Material Mat(Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;return m;}
        void Box(string name,Vector3 p,Vector3 s,Color color){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name="Trailer simulation "+name;o.transform.position=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=Mat(color);}
        Box("table",new Vector3(0,.70f,0),new Vector3(2.4f,.15f,1.8f),new Color(.34f,.20f,.10f));
        Box("floor",new Vector3(0,-.08f,0),new Vector3(10,.15f,10),new Color(.19f,.23f,.29f));
        Box("wall",new Vector3(0,1.8f,-2.4f),new Vector3(8,3.6f,.12f),new Color(.45f,.49f,.55f));
        for(int i=0;i<4;i++)Box("table leg",new Vector3(i%2==0?-1:1,.34f,i<2?-.65f:.65f),new Vector3(.12f,.70f,.12f),new Color(.18f,.10f,.06f));
        var l=new GameObject("Trailer room light").AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.4f;l.transform.rotation=Quaternion.Euler(45,-30,0);
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.20f,.26f,.34f);cam.fieldOfView=58;
        foreach(var b in cam.GetComponents<Behaviour>())if(b!=cam&&!(b is AudioListener))b.enabled=false;
        foreach(var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))if(c!=cam)c.enabled=false;
    }
    IEnumerator Start()
    {
        field=FindAnyObjectByType<ARBattlefield>();caster=field.GetComponent<ARSkillCaster>();cam=field.placement.view;
        var consent=FindAnyObjectByType<ARSessionBootstrap>();consent.Continue();consent.Continue();yield return null;
        mock=field.GetComponent<MockGestureSource>();mock.enabled=false;Room();
        CampusRift.Combat.ReactionResolver.ReactionTriggered+=(type,victim)=>File.AppendAllText("task/trailer/b2-actions.log","REACTION "+type+"\n");
        if(job=="extras"){yield return Extras();yield break;}
        if(job=="sword"){yield return SwordTake();yield break;}
        if(job=="combo-proof"){yield return ComboProof();yield break;}
        yield return Mode("training");
        // Placement animation uses the real built root on a fixed synthetic table, explicitly staged.
        var root=field.Root;var scale=root.localScale;field.GetComponent<ARMonsterDirector>().enabled=false;
        TrailerCaptureOnly.Record("S11b2",0,true,4f);
        float begun=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-begun<4){root.localScale=scale*Mathf.SmoothStep(.03f,1,Mathf.Clamp01((Time.realtimeSinceStartup-begun)/1.3f));yield return null;}
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;root.localScale=scale;field.GetComponent<ARMonsterDirector>().enabled=true;
        yield return new WaitForSecondsRealtime(1);
        TrailerCaptureOnly.Record("S12b2",0,true,5f);Ready();yield return Hold("None",.4f);yield return Hold("Open_Palm",1.2f);yield return Hold("None",.5f);yield return Hold("Closed_Fist",1);yield return Hold("None",2);
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
        yield return Record("S13b2",3);
        // Record a genuine mock-driven elemental reaction and seal sequence as extra source footage.
        foreach(var e in field.GetComponent<ARMonsterDirector>().Actors)if(e!=null&&e.Alive){e.Vitality.SetMaxHealth(3000,true);e.transform.position=field.Root.position;}
        Ready();TrailerCaptureOnly.Record("AR-combo-b2",0,true,6f);yield return Hold("None",.4f);yield return Hold("Thumb_Down",.55f);yield return Hold("None",.3f);yield return Hold("Pointing_Up",.65f);yield return Hold("None",4.2f);
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
        Ready();caster.AddSeal(100);TrailerCaptureOnly.Record("AR-seal-b2",0,true,5f);yield return Hold("None",.4f);yield return Hold("Open_Palm",.55f);yield return Hold("None",.25f);yield return Hold("Closed_Fist",.65f);yield return Hold("None",3.3f);
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
        yield return Mode("defense");TrailerCaptureOnly.Record("S14b2",0,true,3.5f);Ready();yield return Hold("Victory",.7f);yield return Hold("None",.3f);yield return Hold("Thumb_Up",.6f);yield return Hold("None",2);
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
        yield return Mode("rift-hunt");yield return new WaitForSecondsRealtime(1);yield return Hold("Pointing_Up",.6f);yield return Record("S15b2",3);
        yield return Mode("dragon-duel");yield return new WaitForSecondsRealtime(1);cameraRotation=Quaternion.LookRotation(field.GetComponent<ARSpaceModes>().Boss.WeakPoint.position-cameraPosition);yield return Hold("Pointing_Up",.6f);yield return Record("S16b2",3);
        yield return Mode("seal-practice");yield return new WaitForSecondsRealtime(1);TrailerCaptureOnly.Record("S17b2",0,true,4f);yield return Hold("None",.5f);yield return Hold("Open_Palm",.65f);yield return Hold("None",.5f);yield return Hold("Closed_Fist",.65f);yield return Hold("None",2);
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
        staged=false;field.placement.Reposition();yield return null;field.GetComponent<ARModeSelectionHUD>().Open();label="None";yield return Record("S18b2",3);
        File.WriteAllText("task/trailer/B2-AR-DONE.txt","AR capture complete; fixed table/root and real mock bridge/caster, transient profile");
    }
    void FixedHuntPortal()
    {
        var rifts=field.GetComponent<ARSpaceModes>().Rifts;rifts.enabled=false;
        var go=new GameObject("Trailer fixed secondary portal anchor");go.transform.position=field.Root.position+new Vector3(0,.26f,-.28f);
        go.transform.rotation=Quaternion.Euler(90,0,0);
        var anchor=go.AddComponent<UnityEngine.XR.ARFoundation.ARAnchor>();var plane=new GameObject("Trailer synthetic wall reference").AddComponent<UnityEngine.XR.ARFoundation.ARPlane>();
        var rune=ARRune.Create(go.transform,.23f);rune.SetValid(true);
        rifts.Portals.Add(new ARSecondaryRifts.Portal{anchor=anchor,plane=plane,visual=rune.transform,radius=.23f,wall=true,expires=field.Clock+20,sequence=0});
    }
    IEnumerator ComboProof()
    {
        yield return Mode("training");
        var director=field.GetComponent<ARMonsterDirector>();
        director.GetType().GetField("remaining",BF).SetValue(director,0);
        var victim=director.Actors.FirstOrDefault(e=>e!=null&&e.Alive&&!e.Vitality.resistHardControl);
        if(victim==null)victim=director.SpawnActor(caster.Aim);
        if(victim==null)throw new InvalidOperationException("No AR combo target");
        foreach(var e in director.Actors.ToArray())if(e!=null&&e.Alive&&e!=victim){var hit=DamageInfo.Create(100000,Element.None,DamageSource.Melee,e.transform.position,Vector3.down,caster.Caster);hit.ignoreInvulnerability=true;e.Vitality.ApplyDamage(hit);}
        victim.GetComponent<ARMinionBrain>().enabled=false;
        var agent=victim.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent!=null)agent.enabled=false;
        victim.transform.position=caster.Aim;victim.Vitality.SetMaxHealth(3000,true);victim.Status.Clear();
        bool reacted=false;
        Action<ReactionEvent> evidence=r=>{if(r.attacker==caster.Caster&&r.target==victim.Vitality&&r.type==ReactionType.IceLightning){reacted=true;File.AppendAllText("task/trailer/AR-combo-b3-events.log","IceLightning runtime feedback; same AR target; affected="+r.affected+"\n");}};
        ReactionResolver.Feedback+=evidence;
        Ready();yield return Hold("None",.3f);
        TrailerCaptureOnly.Record("AR-combo-b3",0,true,5f);
        yield return Hold("None",.2f);yield return Hold("Thumb_Down",.65f);yield return Hold("None",.25f);yield return Hold("Pointing_Up",.65f);yield return Hold("None",3.5f);
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
        ReactionResolver.Feedback-=evidence;
        File.WriteAllText("task/trailer/AR-combo-b3-success.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {sameTargetIceLightning=reacted,director.Reactions,target=victim.archetype.id,iceHits=((IceSealRuntime)caster.Runtime("Thumb_Down")).LastHitCount,lightningHits=((ChainLightningRuntime)caster.Runtime("Pointing_Up")).LastHitCount}));
    }
    IEnumerator SwordTake()
    {
        yield return Mode("dragon-duel");yield return new WaitForSecondsRealtime(2);
        var director=field.GetComponent<ARMonsterDirector>();director.GetType().GetField("remaining",BF).SetValue(director,0);
        foreach(var e in director.Actors.ToArray())if(e!=null&&e.Alive){var hit=DamageInfo.Create(100000,Element.None,DamageSource.Melee,e.transform.position,Vector3.down,caster.Caster);hit.ignoreInvulnerability=true;e.Vitality.ApplyDamage(hit);}
        var boss=field.GetComponent<ARSpaceModes>().Boss;Set(boss,"Armor",0);Set(boss,"Health",0);boss.WeakPoint.localScale*=.18f;
        cameraPosition=boss.WeakPoint.position+new Vector3(0,-.65f,-.75f);followBoss=true;
        yield return Hold("None",.4f);TrailerCaptureOnly.Record("AR-sword-b4",0,true,5f);yield return Hold("Pointing_Up",3.3f);yield return Hold("None",2);
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
        File.WriteAllText("task/trailer/AR-sword-b4-success.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {visual=field.GetComponent<ARSpaceModes>().SwordVisual!=null,field.ModeSession.Ultimates,director.Won,director.GroundCleared}));
    }
    IEnumerator Extras()
    {
        yield return Mode("training");
        TrailerCaptureOnly.Record("AR-waves-b2",0,true,22f);
        float until=Time.realtimeSinceStartup+22;int g=0;
        while(Time.realtimeSinceStartup<until&&!field.GetComponent<ARMonsterDirector>().Finished){Ready();yield return Hold("None",.3f);yield return Hold(GestureSkillMapper.Labels[g++%5],.65f);}
        label="None";while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
        File.AppendAllText("task/trailer/b2-actions.log","WAVES played="+field.GetComponent<ARMonsterDirector>().Wave+" kills="+field.GetComponent<ARMonsterDirector>().Killed+" reactions="+field.GetComponent<ARMonsterDirector>().Reactions+"\n");
        yield return Mode("rift-hunt");FixedHuntPortal();
        cameraRotation=Quaternion.LookRotation(field.GetComponent<ARSpaceModes>().Rifts.Portals[0].Position-cameraPosition);
        TrailerCaptureOnly.Record("S15b3",0,true,4f);yield return Hold("None",.3f);yield return Hold("Victory",.5f);yield return Hold("None",.25f);yield return Hold("Pointing_Up",.5f);yield return Hold("None",.25f);yield return Hold("Open_Palm",.6f);yield return Hold("None",2);
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
        yield return Mode("dragon-duel");yield return new WaitForSecondsRealtime(1);
        var boss=field.GetComponent<ARSpaceModes>().Boss;
        boss.WeakPoint.localScale*=.18f;
        cameraRotation=Quaternion.LookRotation(boss.WeakPoint.position-cameraPosition);
        yield return Record("S16b3",3.5f);
        // Ground and dragon staging is explicit; the hold, D1 and sword strike are runtime code.
        var director=field.GetComponent<ARMonsterDirector>();
        director.GetType().GetField("remaining",BF).SetValue(director,0);
        foreach(var e in director.Actors.ToArray())if(e!=null&&e.Alive){e.Vitality.SetMaxHealth(1,true);Ready();yield return Hold("None",.3f);yield return Hold("Open_Palm",.5f);}
        // Finish any ground stragglers using real caster attacks.
        float finishBy=Time.realtimeSinceStartup+10;while(director.AliveCount>0&&Time.realtimeSinceStartup<finishBy){cameraRotation=Quaternion.LookRotation(field.Root.position-cameraPosition);Ready();yield return Hold("None",.3f);yield return Hold("Victory",.65f);}
        Set(boss,"Armor",0);Set(boss,"Health",0);
        cameraPosition=boss.WeakPoint.position+new Vector3(0,-.65f,-.50f);cameraRotation=Quaternion.LookRotation(boss.WeakPoint.position-cameraPosition);
        Ready();yield return Hold("None",.4f);
        TrailerCaptureOnly.Record("AR-sword-b2",0,true,5f);yield return Hold("Pointing_Up",3.5f);yield return Hold("None",1.7f);
        while(FindAnyObjectByType<TrailerCaptureOnly>()!=null)yield return null;
        File.AppendAllText("task/trailer/b2-actions.log","AR SWORD visual="+(field.GetComponent<ARSpaceModes>().SwordVisual!=null)+" ultimates="+field.ModeSession.Ultimates+" ground="+director.GroundCleared+"\n");
        File.WriteAllText("task/trailer/B2-EXTRAS-DONE.txt","Extra captures finished");
    }
}
#endif
