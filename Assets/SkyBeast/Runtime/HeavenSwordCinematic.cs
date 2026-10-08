using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using CampusRift.UI;
using CampusRift.Skills;
namespace CampusRift.SkyBeast
{
    [DefaultExecutionOrder(32000)]
    public sealed class HeavenSwordCinematic : MonoBehaviour
    {
        public static HeavenSwordCinematic Active {get;private set;}
        public bool Playing {get;private set;}
        public bool CanSkip {get;private set;}
        public float Progress=>Playing?Mathf.Clamp01(age/duration):0;
        public float Duration=>duration;
        public bool HitApplied {get;private set;}
        public GiantSwordVisual Swords {get;private set;}
        public bool HoldForCapture;
        HeavenSwordUltimate owner;Camera camera;GiantHandCameraImpulse cameraImpulse;Vector3 cameraStart,focus,center;Quaternion cameraRotation;float cameraFov,cameraFar,age,duration;
        FireBreathCycle cycle;SkyBeastScheduler scheduler;SkyBeastController target;Animator targetAnimator;float targetSpeed;
        readonly List<Canvas> hidden=new List<Canvas>();
        readonly List<Behaviour> paused=new List<Behaviour>();
        readonly List<ParticleSystem> particles=new List<ParticleSystem>();
        struct AgentHold {public NavMeshAgent agent;public bool stopped;}
        readonly List<AgentHold> agents=new List<AgentHold>();
        readonly List<Animator> animators=new List<Animator>();readonly List<float> speeds=new List<float>();
        Canvas overlay;Button skip;UnityEngine.UI.Image flash;UnityEngine.UI.RawImage lines;TMPro.TMP_Text title;
        HeavenSwordHeroVisual blade;HeavenSwordImpactVisual shock;HeavenSwordEdgeMist mist;
        Volume bloom;bool oldHdr,oldPost;Quaternion modelRotation;
        SwordChannelVisual residue;ParticleSystem fragments,embers;float residueUntil;
        int audioStep,swordCount;bool final,finishing;
        PlayableDirector timeline;
        public PlayableDirector Timeline=>timeline;
        Vector3 shotOffset;float shotWeight=.62f,shotFov=62;bool shotFall;
        public void Initialize(HeavenSwordUltimate ultimate)
        {
            owner=ultimate;cameraImpulse=GetComponent<GiantHandCameraImpulse>();var cfg=owner.Config;
            var go=new GameObject("P15 pooled sword fleet");Swords=go.AddComponent<GiantSwordVisual>();go.transform.SetParent(transform,false);Swords.Build(cfg);
            blade=new GameObject("150m faceted golden Heaven Sword").AddComponent<HeavenSwordHeroVisual>();blade.transform.SetParent(transform,false);blade.Build();
            shock=new GameObject("P15 layered spherical impact").AddComponent<HeavenSwordImpactVisual>();shock.transform.SetParent(transform,false);shock.Build();
            mist=new GameObject("P15 cinematic edge mist").AddComponent<HeavenSwordEdgeMist>();mist.transform.SetParent(transform,false);mist.gameObject.SetActive(false);
            bloom=new GameObject("P15 temporary cinematic bloom").AddComponent<Volume>();bloom.transform.SetParent(transform,false);bloom.isGlobal=true;bloom.priority=100;bloom.profile=ScriptableObject.CreateInstance<VolumeProfile>();var glow=bloom.profile.Add<Bloom>(true);glow.intensity.Override(.32f);glow.threshold.Override(1.8f);glow.scatter.Override(.55f);bloom.gameObject.SetActive(false);
            residue=new GameObject("P15 scorched sword grooves").AddComponent<SwordChannelVisual>();residue.transform.SetParent(transform,false);residue.Build(cfg.gold);residue.Tint(new Color(.08f,.05f,.025f));residue.gameObject.SetActive(false);
            fragments=Particles("Scales and ash",new Color(.11f,.055f,.015f),.85f,32,2.8f);embers=Particles("Golden follow through",new Color(1,.65f,.08f),.4f,18,2.5f);
            overlay=ComboUIFactory.Canvas("Heaven Sword cinematic overlay",transform,80);overlay.gameObject.AddComponent<GraphicRaycaster>();
            flash=ComboUIFactory.Rect("Comic impact",overlay.transform,new Vector2(1920,1080),Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();flash.color=Color.clear;flash.raycastTarget=false;
            lines=ComboUIFactory.Rect("Impact speed lines",overlay.transform,new Vector2(1920,1080),Vector2.zero).gameObject.AddComponent<UnityEngine.UI.RawImage>();lines.raycastTarget=false;lines.color=Color.clear;var speed=GetComponent<SpeedForceVFX>();if(speed!=null)lines.texture=speed.speedLinesTexture;
            var upper=ComboUIFactory.Rect("Cinematic top bar",overlay.transform,new Vector2(1920,90),new Vector2(0,495));var im=upper.gameObject.AddComponent<UnityEngine.UI.Image>();im.color=new Color(.015f,.01f,.02f,.95f);im.raycastTarget=false;
            var lower=ComboUIFactory.Rect("Cinematic lower bar",overlay.transform,new Vector2(1920,100),new Vector2(0,-490));im=lower.gameObject.AddComponent<UnityEngine.UI.Image>();im.color=new Color(.015f,.01f,.02f,.95f);im.raycastTarget=false;
            title=ComboUIFactory.Text("Vạn Kiếm Quy Tông",upper,new Vector2(1300,64),Vector2.zero,36);title.color=ComicTheme.Gold;
            var r=ComboUIFactory.Rect("Skip Heaven Sword",overlay.transform,new Vector2(310,64),new Vector2(750,-490));ComicTheme.Frame(r.gameObject,"button-red",true);skip=r.gameObject.AddComponent<Button>();skip.onClick.AddListener(Skip);ComboUIFactory.Text("Skip label",r,new Vector2(280,55),Vector2.zero,23).text="BỎ QUA [SPACE]";
            overlay.gameObject.SetActive(false);
        }
        ParticleSystem Particles(string name,Color tint,float size,float speed,float life)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var p=go.AddComponent<ParticleSystem>();var main=p.main;main.playOnAwake=false;main.loop=false;main.useUnscaledTime=true;main.startColor=tint;main.startSize=size;main.startSpeed=speed;main.startLifetime=life;main.gravityModifier=.6f;main.maxParticles=192;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var e=p.emission;e.enabled=false;var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=12;
            var fade=p.colorOverLifetime;fade.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(tint,0),new GradientColorKey(tint,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=g;
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Resources.Load<SkillSet1VfxConfig>("SkillSet1Vfx").particles;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);return p;
        }
        void Pause(Behaviour b){if(b!=null&&b.enabled&&!paused.Contains(b)){paused.Add(b);b.enabled=false;}}
        public void Play()
        {
            if(Playing||Time.timeScale<=0)return;camera=Camera.main;scheduler=SkyBeastScheduler.Instance;cycle=FireBreathCycle.Instance;
            if(camera==null||scheduler==null||scheduler.SwordTarget==null){owner.CinematicFinished(false);return;}
            Playing=true;Active=this;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;HitApplied=false;final=false;age=0;audioStep=1;finishing=false;
            var profile=Progression.ProfileService.Instance;bool seen=profile!=null&&profile.Data.heavenSwordSeen;CanSkip=seen||owner.Realm>=Progression.Realm.DoKiep;duration=CanSkip?owner.Config.repeatSeconds:owner.Config.firstSeconds;
            if(scheduler.Level==10&&scheduler.Phase==3)duration+=1.5f;
            ImportantCaptions.Show("[Vạn Kiếm hội tụ] Thiên Kiếm giáng xuống cự thú.","[Swords gather] The Heaven Sword descends upon the sky beast.",duration);
            cameraStart=camera.transform.position;cameraRotation=camera.transform.rotation;cameraFov=camera.fieldOfView;cameraFar=camera.farClipPlane;camera.farClipPlane=Mathf.Max(cameraFar,700);
            scheduler.CinematicPaused=true;cycle.CinematicPaused=true;owner.Director.CinematicPaused=true;
            target=scheduler.SwordTarget.GetComponent<SkyBeastController>();center=target.definition.center;foreach(var b in scheduler.Beasts)b.BeginSwordCinematic();
            focus=BoundsCenter(target);targetAnimator=target.GetComponentInChildren<Animator>();targetSpeed=targetAnimator!=null?targetAnimator.speed:1;if(targetAnimator!=null)modelRotation=targetAnimator.transform.localRotation;
            oldHdr=camera.allowHDR;oldPost=camera.GetUniversalAdditionalCameraData().renderPostProcessing;camera.allowHDR=true;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;bloom.gameObject.SetActive(true);
            if(mist.GetComponent<MeshFilter>()==null)mist.Build(center);mist.gameObject.SetActive(true);
            Pause(cycle.GetComponent<FireBreathVisuals>());
            foreach(var b in scheduler.Beasts){Pause(b.GetComponent<FeatherBarrage>());Pause(b.GetComponent<MeteorShower>());Pause(b.GetComponent<DragonFury>());}
            foreach(var e in Enemies.EnemyDirector.Instance.Active)
            {
                Pause(e.Brain);Pause(e.GetComponent<Enemies.EnemyAbilityRunner>());Pause(e.Motor);Pause(e.GetComponent<Monsters.MonsterCombat>());
                var agent=e.GetComponent<NavMeshAgent>();if(agent!=null&&agent.enabled&&agent.isOnNavMesh){agents.Add(new AgentHold{agent=agent,stopped=agent.isStopped});agent.isStopped=true;}
                var animator=e.GetComponentInChildren<Animator>();if(animator!=null){animators.Add(animator);speeds.Add(animator.speed);animator.speed=0;}
            }
            foreach(var p in FindObjectsByType<ParticleSystem>())if(p!=fragments&&p!=embers&&p.isPlaying){particles.Add(p);p.Pause();}
            swordCount=FireVisualQuality.Current==FireEffectQuality.Mobile?owner.Config.mobileSwords:FireVisualQuality.Current==FireEffectQuality.PcLow?owner.Config.lowSwords:owner.Config.highSwords;
            overlay.gameObject.SetActive(true);skip.gameObject.SetActive(CanSkip);title.text=LevelHUD.Vietnamese?"VẠN KIẾM QUY TÔNG":"TEN THOUSAND SWORDS RETURN";
            skip.GetComponentInChildren<TMPro.TMP_Text>().text=Controls.CampusInput.Mobile?(LevelHUD.Vietnamese?"BỎ QUA":"SKIP"):(LevelHUD.Vietnamese?"BỎ QUA [SPACE]":"SKIP [SPACE]");
            if(timeline==null){timeline=gameObject.AddComponent<PlayableDirector>();timeline.playOnAwake=false;timeline.timeUpdateMode=DirectorUpdateMode.Manual;timeline.extrapolationMode=DirectorWrapMode.Hold;}
            var asset=Resources.Load<TimelineAsset>("P21/Timelines/HeavenSword"+scheduler.Level);
            timeline.playableAsset=asset;foreach(var track in asset.GetOutputTracks())timeline.SetGenericBinding(track,this);timeline.Play();
            HideHud();HeavenSwordAudio.Instance.Step(1);EvaluateTimeline();
        }
        static Vector3 BoundsCenter(SkyBeastController beast){var bounds=new Bounds(beast.transform.position,Vector3.zero);bool found=false;foreach(var r in beast.GetComponentsInChildren<SkinnedMeshRenderer>())if(r.enabled){if(!found){bounds=r.bounds;found=true;}else bounds.Encapsulate(r.bounds);}return bounds.center;}
        void HideHud(){foreach(var c in FindObjectsByType<Canvas>())if(c!=overlay&&c.GetComponent<ImportantCaptions>()==null&&c.enabled){if(!hidden.Contains(c))hidden.Add(c);c.enabled=false;}}
        void LateUpdate()
        {
            if(residueUntil>0&&Time.unscaledTime>residueUntil){residue.Fade();residueUntil=0;}
            if(!Playing||Time.timeScale<=0)return;
            HideHud();if(CanSkip&&Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame){Skip();return;}
            if(!HoldForCapture)age+=Time.unscaledDeltaTime;
            EvaluateTimeline();if(age>=duration)Finish();
        }
        void EvaluateTimeline(){timeline.time=Mathf.Min(Progress,.999999f)*6;timeline.Evaluate();}
        public void TimelineFrame(Vector3 offset,float weight,float fov,bool followFall)
        {
            shotOffset=offset;shotWeight=weight;shotFov=fov;shotFall=followFall;float p=Progress;
            if(audioStep==1&&p>=.35f){audioStep=2;HeavenSwordAudio.Instance.Step(2);}
            if(audioStep==2&&p>=.61f){audioStep=3;HeavenSwordAudio.Instance.Step(3);}
            if(!HitApplied&&p>=.735f)Impact();
            if(audioStep==4&&p>=.80f){audioStep=5;HeavenSwordAudio.Instance.Step(5);}
            if(audioStep==5&&p>=.94f){audioStep=6;HeavenSwordAudio.Instance.Step(6);}
            Render(p);
        }
        public void SeekForCapture(float fraction){if(!Playing)return;HoldForCapture=true;age=Mathf.Clamp01(fraction)*duration;EvaluateTimeline();}
        void Render(float p)
        {
            var wide=center+shotOffset;var aim=shotFall&&target!=null&&target.gameObject.activeSelf?BoundsCenter(target):focus;
            var look=Vector3.Lerp(center+Vector3.up*28,aim,shotWeight);
            float pull=Mathf.SmoothStep(0,1,Mathf.Clamp01(p/.16f));camera.transform.position=Vector3.Lerp(cameraStart,wide,pull);camera.transform.rotation=Quaternion.Slerp(cameraRotation,Quaternion.LookRotation(look-wide),pull);camera.fieldOfView=Mathf.Lerp(cameraFov,shotFov,pull);
            mist.gameObject.SetActive(p<.90f);
            Swords.Show(p,center,focus,camera,swordCount);
            float grow=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.36f,.60f,p)),fall=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,.735f,p));
            var tip=focus+Vector3.up*(60*(1-fall)-12*fall-Mathf.Max(0,p-.735f)*250);
            // A three-quarter view reveals bevel depth; the tip penetrates beyond the beast at hit.
            var facing=Quaternion.LookRotation(Vector3.ProjectOnPlane(focus-camera.transform.position,Vector3.up).normalized)*Quaternion.Euler(0,-22,-8);
            blade.Show(tip,facing,owner.Config.swordLength/150*grow,p>=.36f&&p<.89f?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.77f,.89f,p)):0,Time.unscaledTime);
            if(targetAnimator!=null)targetAnimator.speed=HitApplied&&(p-.735f)*duration<.065f?0:p>=.70f&&p<.80f?.10f:targetSpeed;
            float impactSeconds=(p-.735f)*duration;bool pulse=HitApplied&&impactSeconds>=0&&impactSeconds<.055f;bool reduced=SettingsManager.Instance.Current.ReduceSkillFlashes;
            if(HitApplied)cameraImpulse?.ApplyCinematic(camera,impactSeconds,.95f);
            flash.color=new Color(1,.74f,.05f,pulse?(reduced?.02f:.08f):0);lines.color=new Color(1,.72f,.09f,pulse?(reduced?.10f:.48f):0);
            if(targetAnimator!=null){float recoil=HitApplied?Mathf.Sin(impactSeconds*65)*3.5f*Mathf.Exp(-Mathf.Max(0,impactSeconds)*8):0;targetAnimator.transform.localRotation=modelRotation*Quaternion.Euler(recoil,0,recoil*.7f);}
            var fragmentClock=fragments.main;fragmentClock.simulationSpeed=p>=.70f&&p<.80f?.1f:1;var emberClock=embers.main;emberClock.simulationSpeed=fragmentClock.simulationSpeed;
            if(HitApplied){shock.Show(focus,camera,impactSeconds);if(final)SettingsManager.Instance.Sky.SetDawnProgress(Mathf.Clamp01((p-.79f)/.21f));}
        }
        void Impact()
        {
            // The scheduler still decides target/gates. One call even if skipped or sought repeatedly.
            if(HitApplied)return;HitApplied=scheduler.ApplySkySwordHit();if(!HitApplied)return;
            final=scheduler.Completed;audioStep=4;HeavenSwordAudio.Instance.Step(4);
            shock.Show(focus,camera,0);fragments.transform.position=embers.transform.position=focus;fragments.Emit(FireVisualQuality.Current==FireEffectQuality.Mobile?64:160);embers.Emit(FireVisualQuality.Current==FireEffectQuality.Mobile?48:128);
            residue.transform.position=transform.position+Vector3.up*.025f;residue.gameObject.SetActive(true);residue.Show(1);residue.transform.localScale=Vector3.one*2;residueUntil=Time.unscaledTime+3;
            if(final)HeavenSwordAudio.Instance.Victory();
        }
        public void Skip(){if(!Playing||!CanSkip||Time.timeScale<=0)return;Impact();HeavenSwordAudio.Instance.Step(5);HeavenSwordAudio.Instance.Step(6);Finish();}
        void Finish()
        {
            if(finishing)return;finishing=true;bool hit=HitApplied;Restore();
            if(hit){var profile=Progression.ProfileService.Instance;profile.Data.heavenSwordSeen=true;profile.MarkDirty();profile.Flush();if(final)SettingsManager.Instance.Sky.SetDawnProgress(1);else cycle.PostSwordRest(owner.Config.restSeconds);}
            owner.CinematicFinished(hit);finishing=false;
        }
        public void Cancel(){if(Playing)Restore();}
        void Restore()
        {
            Playing=false;if(timeline!=null)timeline.Stop();if(Active==this)Active=null;HoldForCapture=false;Swords.Hide();blade.Hide();overlay.gameObject.SetActive(false);shock.Hide();mist.gameObject.SetActive(false);bloom.gameObject.SetActive(false);
            foreach(var c in hidden)if(c!=null)c.enabled=true;hidden.Clear();
            foreach(var b in paused)if(b!=null)b.enabled=true;paused.Clear();
            foreach(var a in agents)if(a.agent!=null&&a.agent.enabled&&a.agent.isOnNavMesh)a.agent.isStopped=a.stopped;agents.Clear();
            for(int i=0;i<animators.Count;i++)if(animators[i]!=null)animators[i].speed=speeds[i];animators.Clear();speeds.Clear();
            foreach(var p in particles)if(p!=null)p.Play();particles.Clear();
            if(targetAnimator!=null){targetAnimator.speed=targetSpeed;targetAnimator.transform.localRotation=modelRotation;}
            if(scheduler!=null){scheduler.CinematicPaused=false;foreach(var b in scheduler.Beasts)b.EndSwordCinematic();}
            if(cycle!=null)cycle.CinematicPaused=false;if(owner.Director!=null)owner.Director.CinematicPaused=false;
            UIStateManager.Instance?.RefreshCursor();
            if(camera!=null){camera.transform.SetPositionAndRotation(cameraStart,cameraRotation);camera.fieldOfView=cameraFov;camera.farClipPlane=cameraFar;camera.allowHDR=oldHdr;camera.GetUniversalAdditionalCameraData().renderPostProcessing=oldPost;}
        }
        void OnDestroy(){Cancel();if(bloom!=null&&bloom.profile!=null)Destroy(bloom.profile);}
    }
}
