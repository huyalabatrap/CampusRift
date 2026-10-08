using System;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Levels;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    [DefaultExecutionOrder(70)]
    public sealed class SkyBeastScheduler:MonoBehaviour
    {
        public static SkyBeastScheduler Instance{get;private set;}
        public int Level{get;private set;}
        public int Phase{get;private set;}=1;
        public bool CinematicPaused {get;set;}
        public bool Completed{get;private set;}
        public SkyBeastController BreathSource{get;private set;}
        public SkyBeastVitality SwordTarget=>beasts.Count==0?null:beasts[0].GetComponent<SkyBeastVitality>();
        public IReadOnlyList<SkyBeastController> Beasts=>beasts;
        public DragonFury Fury{get;private set;}
        public bool SwordAllowed=>!Completed&&SwordTarget!=null&&(!cycle.IsBreathing||CinematicPaused)&&!(Level==10&&Phase==3&&(Fury==null||!Fury.Completed));
        public event Action Changed;
        readonly List<SkyBeastController> beasts=new List<SkyBeastController>(2);
        FireBreathCycle cycle;int alternate;bool wave3Cleared,stopped;
        const string WinHold="p14-sky-beasts";
        public static SkyBeastScheduler Begin(int level)
        {
            StopAll();if(level<8||level>10)return null;
            var owner=new GameObject("Sky Beast Combat Scheduler").AddComponent<SkyBeastScheduler>();owner.Initialize(level);return owner;
        }
        public static void StopAll(){if(Instance!=null){Instance.StopScheduler();Destroy(Instance.gameObject);Instance=null;}}
        void Initialize(int level)
        {
            Instance=this;Level=level;Phase=1;cycle=FireBreathCycle.Ensure();cycle.StopCycle();
            cycle.WarningStarted+=Warning;cycle.BreathStarted+=Breath;cycle.BreathEnded+=Ended;
            LevelEvents.WaveCleared+=WaveCleared;LevelEvents.LevelLost+=Lost;
            LevelDirector.Instance?.HoldWin(WinHold);
            if(level==8)Spawn("xich-hoa-giao",0);
            else if(level==9)Spawn("chu-tuoc",0);
            else{Spawn("xich-hoa-giao",0);Spawn("chu-tuoc",.5f);}
            SkyBeastBarUI.Attach(this);Changed?.Invoke();
        }
        SkyBeastController Spawn(string id,float offset)
        {
            var source=SkyBeastDefinition.LoadCombat(id);if(source==null){Debug.LogError("Missing P14 definition "+id);return null;}
            var data=Instantiate(source); // Per-run placement must never modify authored data.
            if(Level==10&&Phase==3)data.altitude-=10; // Lower approach, retaining P12's safe roof clearance.
            var graph=ShelterGraphReference.Graph;
            if(graph!=null&&graph.Nodes.Length>0){var bounds=new Bounds(graph.Nodes[0].WorldPosition,Vector3.zero);foreach(var n in graph.Nodes)bounds.Encapsulate(n.WorldPosition);data.center=new Vector3(bounds.center.x,0,bounds.center.z);}
            var go=Instantiate(data.prefab,transform);go.name=source.nameVi;
            int layer=LayerMask.NameToLayer("SkyBeast");foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;
            foreach(var collider in go.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            Physics.IgnoreLayerCollision(layer,LayerMask.NameToLayer("Player"),true);
            var controller=go.GetComponent<SkyBeastController>()??go.AddComponent<SkyBeastController>();controller.Initialize(data,offset);beasts.Add(controller);
            var vitality=go.AddComponent<SkyBeastVitality>();vitality.Initialize(Level==10?data.level10Segments:data.segments);
            vitality.CanReceiveSword=()=>SwordAllowed&&SwordTarget==vitality;
            vitality.Changed+=SegmentLost;
            if(id=="chu-tuoc")go.AddComponent<FeatherBarrage>().Initialize(this,controller);
            if(id=="hoa-long-vuong"){go.AddComponent<MeteorShower>().Initialize(this,controller);Fury=go.AddComponent<DragonFury>();Fury.Initialize(this,cycle);go.AddComponent<FireSpiritDrops>().Initialize(this);}
            controller.Roar();return controller;
        }
        void SegmentLost(SkyBeastVitality vitality)
        {
            if(!vitality.IsDead){Phase=2;cycle.SetPhase(2);foreach(var b in beasts)b.Roar();Changed?.Invoke();return;}
            var dead=vitality.GetComponent<SkyBeastController>();beasts.Remove(dead);if(CinematicPaused){dead.BeginSwordDeath();Destroy(dead.definition,4);Destroy(dead.gameObject,4);}else{dead.gameObject.SetActive(false);Destroy(dead.definition);Destroy(dead.gameObject,1);}
            if(Level==10&&Phase<3)
            {
                Phase++;alternate=0;BreathSource=null;
                if(Phase==3)Spawn("hoa-long-vuong",0);
                cycle.SetPhase(Phase);
                // Start the new profile's warning without resetting Long Nộ's once-per-run guard.
                cycle.RestartWarning();foreach(var b in beasts)b.Roar();
                if(Phase==3&&wave3Cleared)Fury.Request();
            }
            else{Completed=true;cycle.StopCycle();LevelDirector.Instance?.ReleaseWin(WinHold);}
            Changed?.Invoke();
        }
        void Warning()
        {
            if(stopped||beasts.Count==0)return;
            BreathSource=Level==10&&Phase==1?beasts[alternate++%beasts.Count]:beasts[0];
            BreathSource.BeginWarning(cycle.PhaseDuration);Changed?.Invoke();
        }
        void Breath()
        {
            if(stopped||beasts.Count==0)return;
            if(cycle.IsFury)BreathSource=beasts[0];
            if(BreathSource==null)BreathSource=beasts[0];
            BreathSource.RequestBreath(cycle.PhaseDuration);Changed?.Invoke();
        }
        void Ended(){BreathSource?.ReturnToOrbit();Changed?.Invoke();}
        void WaveCleared(int wave,int count){if(Level==10&&wave==3){wave3Cleared=true;if(Phase==3)Fury?.Request();}}
        void Lost(LevelResult result){cycle.StopCycle();foreach(var b in beasts)b.ReturnToOrbit();Fury?.Cancel();}
        void StopScheduler()
        {
            if(stopped)return;stopped=true;cycle.WarningStarted-=Warning;cycle.BreathStarted-=Breath;cycle.BreathEnded-=Ended;
            LevelEvents.WaveCleared-=WaveCleared;LevelEvents.LevelLost-=Lost;Fury?.Cancel();
            LevelDirector.Instance?.ReleaseWin(WinHold);cycle.StopCycle();
            foreach(var b in beasts)if(b!=null){Destroy(b.definition);b.gameObject.SetActive(false);}beasts.Clear();
        }
        void OnDestroy(){StopScheduler();if(Instance==this)Instance=null;}
        public bool ApplySkySwordHit()=>SwordTarget!=null&&SwordTarget.ApplySkySwordHit();
    }
}
