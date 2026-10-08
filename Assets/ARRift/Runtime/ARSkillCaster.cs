using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using CampusRift.Skills;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.AR
{
    public sealed class ARSkillCaster:MonoBehaviour
    {
        public ARBattlefield field;public GestureRecognizerBridge source;public GestureSkillMapper map;public GiantHandConfig handConfig;
        public GameObject Caster {get;private set;}public string Feedback {get;private set;}="";
        public string ProgressLabel {get;private set;}public float Progress {get;private set;}public Vector3 Aim {get;private set;}
        public int Fired {get;private set;}public string LastSkill {get;private set;}
        public readonly GestureStateMachine gestures=new GestureStateMachine();readonly OneEuroAim filter=new OneEuroAim();readonly List<ARRaycastHit> hits=new List<ARRaycastHit>();
        public float Seal {get;private set;}public readonly GestureSequenceMatcher sequences=new GestureSequenceMatcher();
        public readonly ARTwoHands twoHands=new ARTwoHands();long dualIntentId;float delayedVoiceMultiplier=1;
        ARPlayerCombat playerCombat;ARCombatAudio combatAudio;ARUltimate? delayedUltimate;float lightningAt;
        public event Action<ARUltimate> UltimateFired;
        public void AddSeal(float amount){if(Caster!=null&&!field.Paused&&!field.CheckLoad&&!Practice)Seal=Mathf.Clamp(Seal+amount,0,100);}
        void Damage(CampusRift.Monsters.MonsterVitality victim,DamageInfo info){if(info.amount>0&&info.attacker==Caster&&victim.GetComponent<ARCombatContext>()?.battlefield==field&&info.source!=DamageSource.Reaction)AddSeal(2);}
        void Reaction(ReactionEvent r){if(r.attacker==Caster&&(int)r.type<5)AddSeal(15);}
        GiantHandConfig clonedHand;bool priorMobile;GroundAimIndicator indicator;Vector2 smoothed;bool aimValid;float nextLog;
        public event Action<string,bool> CastAttempted;
        public event Action<GestureIntent,CastOutcome> Outcome;public event Action<GestureIntent,double> FirstVfx;
        public bool DevPalmAim,Practice;public CastOutcome? InjectedRejection;public bool AimValid=>aimValid;GestureIntent pendingIntent;bool awaitingVfx;
        void Awake(){if(field==null)field=GetComponent<ARBattlefield>();if(source==null)source=GetComponent<GestureRecognizerBridge>();gestures.Settings=field.placement.settings;combatAudio=GetComponent<ARCombatAudio>()??gameObject.AddComponent<ARCombatAudio>();playerCombat=GetComponent<ARPlayerCombat>()??gameObject.AddComponent<ARPlayerCombat>();}
        void OnEnable(){field.Built+=Build;field.Removing+=Clear;source.Result+=Frame;source.HandsResult+=Hands;twoHands.Single+=DualSingle;twoHands.Pair+=DualPair;gestures.Intent+=Intent;sequences.Single+=Single;sequences.Ultimate+=Ultimate;sequences.Step+=combatAudio.Chime;MonsterVitality.AnyDamaged+=Damage;ReactionResolver.Feedback+=Reaction;source.Invalidated+=Invalidate;gestures.GestureProgress+=Charge;priorMobile=SkillVfxPool.ForceMobileQuality;SkillVfxPool.ForceMobileQuality=true;}
        void OnDisable(){field.Built-=Build;field.Removing-=Clear;source.Result-=Frame;source.HandsResult-=Hands;twoHands.Single-=DualSingle;twoHands.Pair-=DualPair;gestures.Intent-=Intent;sequences.Single-=Single;sequences.Ultimate-=Ultimate;sequences.Step-=combatAudio.Chime;MonsterVitality.AnyDamaged-=Damage;ReactionResolver.Feedback-=Reaction;source.Invalidated-=Invalidate;gestures.GestureProgress-=Charge;Clear();SkillVfxPool.ForceMobileQuality=priorMobile;}
        void Build()
        {
            Clear();Caster=new GameObject("ARCaster");Caster.SetActive(false);
            // Configure the unused legacy controller before inheriting a tiny AR scale.
            var controller=Caster.AddComponent<CharacterController>();controller.stepOffset=0;controller.enabled=false;
            Caster.transform.SetParent(field.Root,false);Caster.transform.position=field.Shrine.transform.position;
            var context=Caster.AddComponent<ARCombatContext>();context.battlefield=field;context.scale=field.Scale;context.allUnlocked=field.placement.settings.allUnlocked;context.FirstVfx+=Vfx;
            Caster.AddComponent<ARVisualBounds>().context=context;
            var health=Caster.AddComponent<PlayerMonsterHealth>();health.respawnOnDefeat=false;
            var stats=Caster.AddComponent<PlayerStats>();stats.baseSpirit=field.placement.settings.spirit;stats.baseSpiritRegen=field.placement.settings.spiritRegen;
            Caster.AddComponent<SpiritPower>();Caster.AddComponent<GenerationChainTracker>();Caster.AddComponent<SkillVfxPool>();Caster.AddComponent<SkillImpact>();indicator=Caster.AddComponent<GroundAimIndicator>();
            // Required legacy explorer remains disabled. Its controller/input never drive this proxy.
            var input=Caster.AddComponent<CampusRift.Controls.CampusInput>();input.enabled=false;
            var hand=Caster.AddComponent<GiantHandSkill>();Caster.GetComponent<CampusExplorer>().enabled=false;Caster.GetComponent<CharacterController>().enabled=false;
            clonedHand=Instantiate(handConfig);clonedHand.range*=field.Scale;clonedHand.radius*=field.Scale;clonedHand.verticalTolerance*=field.Scale;clonedHand.maximumHeight=Mathf.Min(clonedHand.maximumHeight,3.5f)*field.Scale;hand.config=clonedHand;
            SkillRuntime[] runtimes={Caster.AddComponent<GiantHandRuntime>(),Caster.AddComponent<BlackHoleRuntime>(),Caster.AddComponent<ChainLightningRuntime>(),Caster.AddComponent<SwordRainRuntime>(),Caster.AddComponent<IceSealRuntime>()};
            for(int i=0;i<runtimes.Length;i++)runtimes[i].definition=map.Find(GestureSkillMapper.Labels[i]);
            Caster.AddComponent<ReactionFeedback>();Caster.SetActive(true);health.Revive(1,0);Caster.GetComponent<SpiritPower>().Refill();
            indicator.Show(false,.45f,CampusRift.UI.ComicTheme.Gold);
        }
        void Clear(){twoHands.Reset();Seal=0;sequences.Cancel();delayedUltimate=null;if(Caster!=null)Destroy(Caster);Caster=null;if(clonedHand!=null)Destroy(clonedHand);clonedHand=null;filter.Reset();aimValid=false;}
        public SkillRuntime Runtime(string label){var definition=map!=null?map.Find(label):null;if(Caster==null||definition==null)return null;foreach(var s in Caster.GetComponents<SkillRuntime>())if(s.Id==definition.id)return s;return null;}
        void Charge(string label,float value){ProgressLabel=label;Progress=value;}
        void Frame(GestureFrame f)
        {
            if(source.HandCount==2)return;
            bool allowed=Caster!=null&&!field.Paused&&(!field.Mode.loseOnShrine||!field.Shrine.IsDead)&&!field.GetComponent<ARMonsterDirector>().Finished;
            if(DevPalmAim&&allowed){smoothed=filter.Filter(GestureCoordinates.Palm(f)*new Vector2(Screen.width,Screen.height),f.timestampMs*.001);aimValid=FindAim(smoothed,out var aim);Aim=aim;}
            bool dynamicBlocked=GetComponent<ARHandInteractions>()?.Observe(f,allowed)??false;
            if(dynamicBlocked){sequences.Cancel();GetComponent<ARSpaceModes>()?.CancelDynamic();}
            gestures.Process(f,allowed,dynamicBlocked,GetComponent<ARHandInteractions>()?.motion.CompletedThisFrame??false);
            GetComponent<ARSpaceModes>()?.ObserveFrame(f);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(Time.unscaledTime>=nextLog||gestures.Decision=="fire"){nextLog=Time.unscaledTime+.2f;var motion=GetComponent<ARHandInteractions>()?.motion;Debug.Log($"[ARGesture] model={f.label} score={f.score:0.00} geometry={gestures.Geometry.label} geometryReason={gestures.Geometry.reason} inFrame={gestures.Geometry.inFrame} quality={gestures.Geometry.quality} fingers={gestures.Geometry.Fingers} norm={f.landmarks?.Length??0} world={f.worldLandmarks?.Length??0} top={gestures.TopLabel}:{gestures.TopEvidence:0.00} decision={gestures.Decision} requireReleaseReason={gestures.RequireReleaseReason} heldLabel={gestures.HeldLabel} allowed={allowed} motion={motion?.State} motionReason={motion?.Reason} dynamicCompleted={motion?.CompletedThisFrame} convertMs={f.convertReadyMs-f.acquireMs:0.0} inferMs={f.resultMs-f.inferStartMs:0.0} queueMs={(f.nativeToUnityMs.HasValue?f.consumeMs-f.resultMs-f.nativeToUnityMs.Value:-1):0.0} latencyMs={source.LatencyMs:0.0} Hz={source.RecognitionFps:0.0} delegate={source.DelegateName} input={f.width}x{f.height}");}
#endif
        }
        bool DualAllowed=>Caster!=null&&!field.Paused&&!Practice&&!field.CheckLoad&&(!field.Mode.loseOnShrine||!field.Shrine.IsDead)&&!field.GetComponent<ARMonsterDirector>().Finished;
        public GestureStateMachine HandState(int id){for(int i=0;i<2;i++)if(id>0&&twoHands.Identity[i]==id)return twoHands.D1[i];return gestures;}
        void Hands(GestureHandsFrame batch){twoHands.Process(batch,field.placement.settings,DualAllowed,field.placement.view);source.PrimaryHandId=twoHands.Identity[0];foreach(var f in batch.hands)GetComponent<ARSpaceModes>()?.ObserveFrame(f,HandState(f.handId).Geometry);}
        void DualSingle(GestureIntent intent){if(!DualAllowed)return;intent.id=++dualIntentId;Intent(intent);}
        void DualPair(bool quick,GestureIntent intent)
        {
            if(!DualAllowed||!field.ModeSession.Has(ARModeFeature.Sequences)||(GetComponent<ARKnowledgeSeal>()?.Active??false)||field.Mode.winRule==ARWinRule.CompleteRhythm)return;
            sequences.Cancel();GetComponent<ARSpaceModes>()?.CancelDynamic();intent.id=++dualIntentId;
            if(quick){if(Seal>=100)Ultimate(ARUltimate.SwordConvergence,new GestureSequenceMatcher.Cast{intent=intent,aim=Aim,aimValid=aimValid});else{Feedback=CampusRift.UI.LevelHUD.Vietnamese?"Cần Linh Ấn đầy":"Full seals required";Outcome?.Invoke(intent,CastOutcome.Unavailable);}return;}
            bool ok=aimValid&&Caster.GetComponent<GiantHandSkill>().CastARTwin(Aim);Feedback=CampusRift.UI.LevelHUD.Vietnamese?(ok?"THIÊN THỦ ĐÔI":"Thiên Thủ chưa sẵn sàng · hạ hai tay"):(ok?"TWIN HANDS":"Hand skill unavailable · lower both hands");Outcome?.Invoke(intent,ok?CastOutcome.Success:CastOutcome.Unavailable);CastAttempted?.Invoke("Open_Palm",ok);if(ok){Fired++;ARHaptics.Skill("Open_Palm");}
        }
        void Invalidate(){twoHands.Reset();GetComponent<ARHandInteractions>()?.motion.Cancel();if(gestures.HasProcessed||source.Recovering)gestures.Suspend(reason:source.Recovering?"recovery":!source.SamplingActive?"pause":"epoch");aimValid=false;sequences.Cancel();}
        void ResetEnergy(){twoHands.Reset();Seal=0;sequences.Cancel();delayedUltimate=null;gestures.Suspend(reason:"!allowed",latch:false);if(Caster!=null){foreach(var runtime in Caster.GetComponents<SkillRuntime>())runtime.ReadyOnRestEquip();Caster.GetComponent<SpiritPower>().Refill();}}
        void Start(){field.GetComponent<ARMonsterDirector>().BattleStarted+=ResetEnergy;}
        void OnDestroy(){var director=field!=null?field.GetComponent<ARMonsterDirector>():null;if(director!=null)director.BattleStarted-=ResetEnergy;}
        void Update()
        {
            if(field.Root==null||field.placement.view==null){aimValid=false;return;}
            if(!DevPalmAim){aimValid=FindAim(new Vector2(Screen.width*.5f,Screen.height*.5f),out var point);Aim=point;}
            if(indicator!=null)indicator.SetExternal(Aim,aimValid);
            if(field.Paused||field.CheckLoad||Practice||field.GetComponent<ARMonsterDirector>().Finished){sequences.Cancel();if(field.Root==null||field.GetComponent<ARMonsterDirector>().Finished)delayedUltimate=null;return;}
            if(source.HandCount==2){if(DualAllowed)twoHands.Flush(GestureRecognizerBridge.Now);else twoHands.Reset();}
            sequences.Tick(Time.unscaledTime);
            if(delayedUltimate.HasValue&&field.Clock>=lightningAt){delayedUltimate=null;((ChainLightningRuntime)Runtime("Pointing_Up")).CastARUltimate(field.Root.position,3*delayedVoiceMultiplier);}
        }
        public static bool DiscAim(Ray ray,Vector3 center,Vector3 normal,float radius,out Vector3 point)
        {
            point=center;if(!new Plane(normal,center).Raycast(ray,out float distance)||distance<=0)return false;
            point=ray.GetPoint(distance);return Vector3.ProjectOnPlane(point-center,normal).sqrMagnitude<=radius*radius;
        }
        bool FindAim(Vector2 screen,out Vector3 point)=>DiscAim(field.placement.view.ScreenPointToRay(screen),field.Root.position,field.Root.up,field.placement.Radius,out point);
        void Vfx(double now){if(!awaitingVfx)return;awaitingVfx=false;FirstVfx?.Invoke(pendingIntent,now);}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Explicit render load for the final check minute; never a recognition intent or latency sample.
        public bool CheckScheduledVfx(int g)
        {
            if(!field.CheckLoad||field.Paused||!aimValid||g<0||g>=5)return false;
            var runtime=Runtime(GestureSkillMapper.Labels[g]);if(runtime==null)return false;
            awaitingVfx=false;runtime.ReadyOnRestEquip();Caster.GetComponent<SpiritPower>().Refill();
            return runtime is GiantHandRuntime?Caster.GetComponent<GiantHandSkill>().CastAt(Aim):((Set1SkillRuntime)runtime).CastAt(Aim);
        }
#endif
        void Intent(GestureIntent intent)
        {if(!Practice&&!field.CheckLoad){if(GetComponent<ARKnowledgeSeal>()?.Consume(intent)??false)return;if(GetComponent<ARSealPractice>()?.Consume(intent)??false)return;}if(GetComponent<ARSpaceModes>()?.Consume(intent)??false)return;sequences.Submit(new GestureSequenceMatcher.Cast{intent=intent,aim=Aim,aimValid=aimValid},Time.unscaledTime,Seal>=100&&field.ModeSession.Has(ARModeFeature.Sequences)&&!Practice&&!field.CheckLoad);}
        public void SpaceFeedback(bool hit,GestureIntent intent){Feedback=CampusRift.UI.LevelHUD.Vietnamese?(hit?"ĐIỂM YẾU · NỐI COMBO":"Ngắm điểm yếu · chờ nhịp chiêu"):(hit?"WEAK POINT · CHAIN COMBO":"Aim at weak point · wait for strike");Outcome?.Invoke(intent,hit?CastOutcome.Success:CastOutcome.Unavailable);CastAttempted?.Invoke(intent.label,hit);}
        void Single(GestureSequenceMatcher.Cast cast){Fire(cast.intent,cast.aim,cast.aimValid);}
        void Ultimate(ARUltimate kind,GestureSequenceMatcher.Cast cast)
        {var voice=GetComponent<ARVoiceCommands>();float voicePower=voice?.Multiplier(kind)??1;bool ok=false;if(!field.Paused&&Seal>=100&&Caster!=null){switch(kind){case ARUltimate.SwordConvergence:ok=((SwordRainRuntime)Runtime("Victory")).CastARUltimate(field.Root.position,3*voicePower);break;case ARUltimate.IceLightningPrison:var ice=(IceSealRuntime)Runtime("Thumb_Down");var lightning=(ChainLightningRuntime)Runtime("Pointing_Up");if(ice.CanARUltimate&&lightning.CanARUltimate){ok=ice.CastARUltimate(field.Root.position);if(ok){delayedUltimate=kind;delayedVoiceMultiplier=voicePower;lightningAt=field.Clock+.8f;}}break;case ARUltimate.StarAbsorption:ok=Caster.GetComponent<GiantHandSkill>().CastARAbsorption(voicePower);break;}}
            if(ok){voice?.Consume();GetComponent<ARSpaceModes>()?.Boss.AcceptUltimate(voicePower);Seal=0;field.ModeSession.RegisterUltimate();ARHaptics.Ultimate((int)kind);UltimateFired?.Invoke(kind);}else Single(cast);}
        void Fire(GestureIntent intent,Vector3 destination,bool valid)
        {
            string label=intent.label;
            if(label=="Thumb_Up"){bool raised=!field.Paused&&playerCombat.RaiseShield();Feedback=CampusRift.UI.LevelHUD.Vietnamese?(raised?"KIM CHUNG TRÁO":"Khiên đang hồi chiêu · hạ tay rồi giơ lại"):(raised?"GOLDEN BELL":"Shield cooling down · lower your hand");Outcome?.Invoke(intent,raised?CastOutcome.Success:field.Paused?CastOutcome.Paused:CastOutcome.Cooldown);CastAttempted?.Invoke(label,raised);if(!raised)HandState(intent.handId).RequireRelease();return;}
            if(label=="Pointing_Up"&&delayedUltimate.HasValue){Feedback=CampusRift.UI.LevelHUD.Vietnamese?"Sét tuyệt kỹ đang tụ":"Ultimate lightning charging";Outcome?.Invoke(intent,CastOutcome.Unavailable);return;}
            var runtime=Runtime(label);bool ok=false;CastOutcome outcome;
            pendingIntent=intent;awaitingVfx=false;
            if(Practice&&runtime!=null){runtime.ReadyOnRestEquip();Caster.GetComponent<SpiritPower>().Refill();}
            if(InjectedRejection.HasValue)outcome=InjectedRejection.Value;
            else if(field.Paused||runtime==null)outcome=CastOutcome.Paused;
            else if(!valid)outcome=CastOutcome.Aim;
            else if(runtime.CooldownRemaining>0)outcome=CastOutcome.Cooldown;
            else if(!runtime.HasSpirit)outcome=CastOutcome.Spirit;
            else
            {
                awaitingVfx=true;var hand=runtime as GiantHandRuntime;
                ok=hand!=null?Caster.GetComponent<GiantHandSkill>().CastAt(destination):((Set1SkillRuntime)runtime).CastAt(destination);
                outcome=ok?CastOutcome.Success:CastOutcome.Unavailable;if(!ok)awaitingVfx=false;
            }
            Feedback=ok?runtime.ShortName:outcome==CastOutcome.Cooldown?"Đang hồi chiêu · hạ tay rồi giơ lại":outcome==CastOutcome.Spirit?"Thiếu Linh Lực · hạ tay rồi giơ lại":outcome==CastOutcome.Aim?"Ngắm vào vòng trận · hạ tay rồi giơ lại":outcome==CastOutcome.Paused?"Đã tạm dừng · hạ tay rồi giơ lại":"Chiêu chưa sẵn sàng · hạ tay rồi giơ lại";
            if(ok){Fired++;LastSkill=runtime.Id;ARHaptics.Skill(label);}else HandState(intent.handId).RequireRelease();
            Outcome?.Invoke(intent,outcome);CastAttempted?.Invoke(label,ok);
        }
    }
    public enum CastOutcome { Success,Cooldown,Spirit,Aim,Paused,Unavailable }
}
