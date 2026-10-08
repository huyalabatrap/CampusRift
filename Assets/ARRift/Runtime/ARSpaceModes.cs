using UnityEngine;
using CampusRift.SkyBeast;
namespace CampusRift.AR
{
    public sealed class ARSpaceModes : MonoBehaviour
    {
        ARBattlefield field;ARMonsterDirector director;ARSkillCaster caster;
        public ARSecondaryRifts Rifts {get;private set;}public ARRoomDragon Boss {get;private set;}
        public readonly GestureSequenceMatcher huntMatcher=new GestureSequenceMatcher();
        public ARSecondaryRifts.Portal HuntTarget {get;private set;}
        public int Closed {get;private set;}public int Missed {get;private set;}public int Points {get;private set;}
        public bool SwordReady=>Boss!=null&&Boss.Alive&&Boss.Exposed&&director.GroundCleared;
        public float SwordProgress {get;private set;}public float Pitch=>Mathf.Asin(Mathf.Clamp(field.placement.view.transform.forward.y,-1,1))*Mathf.Rad2Deg;
        public float Remaining=>Mathf.Max(0,field.Mode.timeLimit-(field.Clock-started));
        public bool DragonBlocksWave=>field.ModeSession.Has(ARModeFeature.Dragon)&&director.Wave>=director.WaveCount&&!defeated;
        public bool Charging=>holding;
        float started,nextRift,lastFrame,holdStarted,strikeAt=-1;bool holding,defeated;int epoch=-1,holdHandId;long holdFrameId=-1;double holdStamp=-1;
        HeavenSwordHeroVisual sword;
        public HeavenSwordHeroVisual SwordVisual=>sword;
        Material swordMaterial;
        void Awake()
        {
            field=GetComponent<ARBattlefield>();director=GetComponent<ARMonsterDirector>();caster=GetComponent<ARSkillCaster>();
            Rifts=GetComponent<ARSecondaryRifts>()??gameObject.AddComponent<ARSecondaryRifts>();Boss=GetComponent<ARRoomDragon>()??gameObject.AddComponent<ARRoomDragon>();
            huntMatcher.AssignedComplete+=CloseTarget;huntMatcher.Step+=()=>GetComponent<ARCombatAudio>().Chime();
        }
        void Start(){field.Removing+=Clear;director.BattleStarted+=Begin;caster.source.Invalidated+=Invalidate;}
        void Begin(){Clear();started=field.Clock;nextRift=started+1;Closed=Missed=Points=0;defeated=false;Rifts.Begin(field.ModeSession.Seed);}
        public bool SecondarySpawn(int index,out Vector3 origin,out bool wall)
        {origin=Vector3.zero;wall=false;return !Boss.Alive&&director.Wave>=2&&field.ModeSession.Has(ARModeFeature.SecondaryRifts)&&Rifts.SpawnOrigin(index,out origin,out wall);}
        void Update()
        {
            if(field.Root==null||field.Paused||director.Finished||field.CheckLoad||caster.Practice){Invalidate();return;}
            if(strikeAt>=0)
            {
                float t=Mathf.Clamp01((field.Clock-strikeAt)/.6f);
                if(sword!=null&&Boss.Alive)sword.Show(Boss.WeakPoint.position+Vector3.up*Mathf.Lerp(1.5f,0,t),Quaternion.identity,Mathf.Clamp(field.Scale*.018f,.006f,.018f),1,field.Clock);
                if(t>=1){strikeAt=-1;defeated=true;Boss.Clear();sword?.Hide();director.Finish(true);}return;
            }
            if(holding&&Time.unscaledTime-lastFrame>.25f)CancelHold();
            if(field.Mode.winRule==ARWinRule.CloseRifts)
            {
                Rifts.SetScanning(true);
                for(int i=Rifts.Portals.Count-1;i>=0;i--)if(field.Clock>=Rifts.Portals[i].expires){var p=Rifts.Portals[i];if(p==HuntTarget){HuntTarget=null;huntMatcher.Cancel();}Rifts.Remove(p);Missed++;}
                if(field.Clock>=nextRift&&Rifts.Portals.Count<Rifts.Limit){int before=Rifts.Portals.Count;Rifts.Request(true);nextRift=field.Clock+8;}
                UpdateTarget();huntMatcher.TickAssigned(Time.unscaledTime);
                if(Remaining<=0){director.Finish(Closed>0);Rifts.Clear();}return;
            }
            if(field.ModeSession.Has(ARModeFeature.Dragon)&&director.Wave>=director.WaveCount&&!defeated)
            {
                Rifts.Clear();if(!Boss.Alive)Boss.Spawn(field.ModeSession.Seed,field.Mode.winRule==ARWinRule.DefeatBoss);
            }
            else if(field.ModeSession.Has(ARModeFeature.SecondaryRifts)&&director.Wave>=2){Rifts.SetScanning(true);Rifts.Request(false);}
        }
        void UpdateTarget()
        {
            ARSecondaryRifts.Portal target=null;float best=10;
            foreach(var p in Rifts.Portals){if(p.visual==null||!p.visual.gameObject.activeInHierarchy)continue;float angle=Vector3.Angle(field.placement.view.transform.forward,p.Position-field.placement.view.transform.position);if(angle<best){best=angle;target=p;}}
            if(target!=HuntTarget){HuntTarget=target;huntMatcher.Cancel();}
        }
        void CloseTarget()
        {
            if(HuntTarget==null||field.Paused||field.Clock>=HuntTarget.expires)return;
            Closed++;Points+=500+Mathf.RoundToInt(Mathf.Clamp(HuntTarget.expires-field.Clock,0,20)*25);field.ModeSession.RegisterUltimate();Rifts.Remove(HuntTarget);HuntTarget=null;nextRift=Mathf.Min(nextRift,field.Clock+1);ARHaptics.Ultimate(0);
        }
        // Called before the normal caster. D1 remains the sole producer of discrete intents.
        public bool Consume(GestureIntent intent)
        {
            if(field.Paused||field.CheckLoad||caster.Practice||director.Finished)return false;
            if(field.Mode.winRule==ARWinRule.CloseRifts)
            {
                if(intent.label=="Thumb_Up")return false;
                UpdateTarget();if(HuntTarget!=null)huntMatcher.SubmitAssigned(new GestureSequenceMatcher.Cast{intent=intent},Time.unscaledTime,HuntTarget.sequence);return true;
            }
            if(SwordReady&&intent.label=="Pointing_Up")
            {if(Pitch>35){holding=true;holdHandId=intent.handId;holdStarted=Time.unscaledTime;lastFrame=Time.unscaledTime;epoch=intent.epoch;holdFrameId=-1;holdStamp=-1;}return true;}
            if(Boss.Alive&&Boss.AimAtWeakPoint()&&intent.label!="Thumb_Up")
            {if(caster.Seal>=100&&field.ModeSession.Has(ARModeFeature.Sequences))return false;bool hit=Boss.AcceptSingle(intent.label);caster.SpaceFeedback(hit,intent);return true;}
            return false;
        }
        public void CancelDynamic(){CancelHold();huntMatcher.Cancel();}
        public void ObserveFrame(GestureFrame frame,GeometryResult handGeometry=null)
        {
            if(!holding||frame.handId!=holdHandId)return;var geometry=handGeometry??caster.gestures.Geometry;
            long id=frame.frameId>0?frame.frameId:frame.timestampMs;double stamp=frame.sensorTimestamp>0?frame.sensorTimestamp:frame.timestampMs*.001;
            if(id<=holdFrameId||stamp<=holdStamp)return;holdFrameId=id;holdStamp=stamp;
            bool valid=!field.Paused&&!director.Finished&&SwordReady&&frame.epoch==epoch&&frame.label=="Pointing_Up"&&frame.score>=field.placement.settings.modelThreshold&&geometry.quality&&geometry.inFrame&&!geometry.Contradicts("Pointing_Up")&&Pitch>35;
            if(frame.acquireMs>0&&GestureRecognizerBridge.Now-frame.acquireMs>250)valid=false;
            if(!valid){CancelHold();return;}
            lastFrame=Time.unscaledTime;SwordProgress=Mathf.Clamp01((Time.unscaledTime-holdStarted)/1.5f);
            if(SwordProgress>=1)
            {
                holding=false;strikeAt=field.Clock;field.ModeSession.RegisterUltimate();ARHaptics.Ultimate(0);GetComponent<ARCombatAudio>().Chime();
                if(sword==null){sword=new GameObject("AR Heaven Sword P15").AddComponent<HeavenSwordHeroVisual>();sword.Build();var material=Resources.Load<Material>("ARModes/HeavenReflection");if(material!=null){swordMaterial=new Material(material);foreach(var r in sword.GetComponentsInChildren<Renderer>())if(r.name=="Bevels and raised ridge")r.sharedMaterial=swordMaterial;}}
                caster.HandState(holdHandId).RequireRelease();
            }
        }
        void CancelHold(){holding=false;SwordProgress=0;holdFrameId=-1;holdStamp=-1;}
        void Invalidate(){huntMatcher.Cancel();CancelHold();}
        void Clear(){Invalidate();Rifts?.Clear();Boss?.Clear();HuntTarget=null;strikeAt=-1;defeated=false;sword?.Hide();}
        void OnDestroy(){if(field!=null)field.Removing-=Clear;if(director!=null)director.BattleStarted-=Begin;if(caster!=null&&caster.source!=null)caster.source.Invalidated-=Invalidate;Clear();if(sword!=null)Destroy(sword.gameObject);if(swordMaterial!=null)Destroy(swordMaterial);}
    }
}
