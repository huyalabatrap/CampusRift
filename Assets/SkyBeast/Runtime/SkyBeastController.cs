using System;
using System.Collections;
using UnityEngine;
using CampusRift.UI;
using CampusRift.Enemies;
using CampusRift.Skills;
namespace CampusRift.SkyBeast
{
    [DefaultExecutionOrder(150)]
    public sealed class SkyBeastController : MonoBehaviour
    {
        public SkyBeastDefinition definition;
        public event Action<SkyBeastController> Roared,BreathRequested;
        public Transform Mouth {get;private set;}
        public Transform Rider {get;private set;}
        public Transform TailTip {get;private set;}
        public int LastRoarVariant {get;private set;}=-1;
        public int RoarCount {get;private set;}
        public float NextRoarAt {get;private set;}
        public float LastRoarAt {get;private set;}
        public float Speed {get;private set;}
        public string AnimationState {get;private set;}
        public float MinClearance {get;private set;}=float.MaxValue;
        Animator animator;Transform rigRoot;Vector3 rootRest,lastPosition;float born,phaseOffset,lockUntil;
        AudioSource voice,rumble,wind;Vector3 priorForward;readonly RaycastHit[] clearance=new RaycastHit[24];bool cinematic;Coroutine pose;
        public bool PoseActive=>pose!=null;
        public bool Charging{get;private set;}
        public float ChargeProgress{get;private set;}
        float chargeBorn,chargeDuration,climb;
        ParticleSystem throat;
        public void BeginWarning(float seconds)
        {
            Charging=true;chargeBorn=Time.time;chargeDuration=Mathf.Max(.1f,seconds);ChargeProgress=0;
            Roar();Play("Spell_Start",seconds);
            if(throat==null&&Mouth!=null){throat=FireVisualFactory.RollingFire(transform,"Charging throat flame",3,.4f);var m=throat.main;m.scalingMode=ParticleSystemScalingMode.Local;m.maxParticles=FireVisualQuality.Choose(32,24,16);}
            throat?.Play();
        }
        public void EndWarning(){Charging=false;ChargeProgress=0;throat?.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
        public void ReturnToOrbit(){EndWarning();if(pose!=null)StopCoroutine(pose);pose=null;lockUntil=Time.time;Play("Spell_End",PoseSeconds("Spell_End"));}
        float swordFlightSeconds;bool swordHeld;
        public void BeginSwordCinematic(){swordFlightSeconds=Time.time-born;swordHeld=true;cinematic=true;}
        public void EndSwordCinematic(){if(!swordHeld)return;swordHeld=false;cinematic=false;born=Time.time-swordFlightSeconds;lastPosition=transform.position;ReturnToOrbit();}
        public void BeginSwordDeath(){if(pose!=null)StopCoroutine(pose);pose=null;cinematic=true;EndWarning();StartCoroutine(SwordDeath());}
        IEnumerator SwordDeath(){var start=transform.position;var rotation=transform.rotation;var renders=GetComponentsInChildren<Renderer>();var block=new MaterialPropertyBlock();float t=0;
            while(t<2.2f){t+=Time.unscaledDeltaTime;float u=Mathf.Clamp01(t/2.2f);transform.position=start+Vector3.down*(55*u*u);transform.rotation=rotation*Quaternion.Euler(u*38,0,u*55);
                foreach(var r in renders)if(r!=null){r.GetPropertyBlock(block);block.SetFloat("_Dissolve",Mathf.Clamp01((u-.25f)/.75f));r.SetPropertyBlock(block);}yield return null;}gameObject.SetActive(false);}
        public void HoldCinematic(Vector3 position,Quaternion rotation){cinematic=true;transform.SetPositionAndRotation(position,rotation);Play("Fly_Cruise",10);}
#if UNITY_EDITOR
        public void SeekFlightForValidation(float seconds){born=Time.time-seconds;transform.position=Path(seconds);lastPosition=transform.position;}
#endif
        public void Initialize(SkyBeastDefinition data,float offset=0){definition=data;transform.localScale=Vector3.one*data.scale;phaseOffset=offset;born=Time.time;RoarCount=0;LastRoarVariant=-1;
            animator=GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
            foreach(var t in GetComponentsInChildren<Transform>()){
                if(t.name=="Socket_Mouth")Mouth=t;if(t.name=="Socket_Rider")Rider=t;if(t.name=="Socket_TailTip")TailTip=t;
                if(t.name=="Root")rigRoot=t;}
            if(rigRoot!=null)rootRest=rigRoot.localPosition;
            voice=Source("Dragon roar 3D",true,.95f);rumble=Source("Dragon rumble 2D",false,.35f);wind=Source("Dragon low pass wind",true,.35f);
            NextRoarAt=Time.time+UnityEngine.Random.Range(data.roarMin,data.roarMax);transform.position=Path(0);lastPosition=transform.position;priorForward=transform.forward;
            Play("Fly_Cruise");
            (GetComponent<SkyBeastIdentity>()??gameObject.AddComponent<SkyBeastIdentity>()).Build(this);}
        AudioSource Source(string name,bool spatial,float volume){var go=new GameObject(name);go.transform.SetParent(transform,false);var a=go.AddComponent<AudioSource>();a.playOnAwake=false;a.spatialBlend=spatial?1:0;a.volume=volume;a.dopplerLevel=0;
            a.minDistance=35;a.maxDistance=650;a.rolloffMode=AudioRolloffMode.Linear;
            var mixer=SettingsManager.Instance?.Mixer;if(mixer!=null){var groups=mixer.FindMatchingGroups("Monster");if(groups.Length>0)a.outputAudioMixerGroup=groups[0];}return a;}
        public Vector3 Path(float seconds)
        {
            float angle=(seconds/definition.period+phaseOffset)*Mathf.PI*2;
            // Periodic C2 orbit with gently varying altitude; no corners or root-motion translation.
            float low=Mathf.Pow(Mathf.Max(0,Mathf.Cos(angle)),8);
            if(definition.pathType==SkyPath.FigureEight)return definition.center+new Vector3(Mathf.Sin(angle)*definition.orbitRadius,definition.altitude-low*28+Mathf.Sin(angle*2)*5,Mathf.Sin(angle*2)*definition.orbitRadius*.45f);
            return definition.center+new Vector3(Mathf.Sin(angle)*definition.orbitRadius,definition.altitude-low*28+Mathf.Sin(angle*2)*5,Mathf.Cos(angle)*definition.orbitRadius*(.8f-.6f*low));
        }
        void Update()
        {
            if(definition==null||animator==null||cinematic)return;
            ChargeProgress=Charging?Mathf.Clamp01((Time.time-chargeBorn)/chargeDuration):0;
            climb=Mathf.MoveTowards(climb,Charging?18:0,Time.deltaTime*6);
            Vector3 want=Path(Time.time-born)+Vector3.up*climb;transform.position=want;Vector3 velocity=(want-lastPosition)/Mathf.Max(.001f,Time.deltaTime);Speed=velocity.magnitude;lastPosition=want;
            if(throat!=null&&Mouth!=null){throat.transform.position=Mouth.position;var e=throat.emission;e.rateOverTime=Charging?FireVisualQuality.Choose(20,14,10)*ChargeProgress:0;}
            Vector3 forward=velocity.normalized;if(forward.sqrMagnitude>.01f){float curve=Vector3.SignedAngle(priorForward,forward,Vector3.up)/Mathf.Max(.001f,Time.deltaTime);
                Quaternion rotation=Quaternion.LookRotation(forward)*Quaternion.Euler(0,0,Mathf.Clamp(-curve*1.8f,-23,23));transform.rotation=Quaternion.Slerp(transform.rotation,rotation,1-Mathf.Exp(-Time.deltaTime*2));priorForward=forward;
                if(Time.time>=lockUntil)Play(velocity.y< -2?"Glide":Mathf.Abs(curve)>7?(curve<0?"Bank_Left":"Bank_Right"):Speed>definition.cruiseSpeed*1.4f?"Fly_Fast":Speed<1?"Hover_Idle":"Fly_Cruise");}
            animator.SetFloat("Speed",Speed>definition.cruiseSpeed*1.4f?1:Speed<1?0:.5f);
            if(Time.time>=NextRoarAt&&!Charging&&!PoseActive)Roar();
            var p=EnemyDirector.Instance?.PlayerTransform;if(p!=null&&Vector3.Distance(p.position,transform.position)<100&&want.y<definition.altitude-20&&!wind.isPlaying&&definition.wind!=null){wind.clip=definition.wind;wind.Play();p.GetComponent<GiantHandCameraImpulse>()?.Pulse(.08f);}
            int n=Physics.RaycastNonAlloc(transform.position,Vector3.down,clearance,200,~((1<<7)|(1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++)if(!clearance[i].collider.transform.IsChildOf(transform))MinClearance=Mathf.Min(MinClearance,clearance[i].distance);
        }
        void LateUpdate(){if(rigRoot!=null)rigRoot.localPosition=rootRest;if(throat!=null&&Mouth!=null)throat.transform.position=Mouth.position;}
        public bool Play(string state,float hold=0){if(animator==null||!animator.HasState(0,Animator.StringToHash("Base Layer."+state)))return false;
            if(AnimationState!=state){AnimationState=state;animator.CrossFadeInFixedTime(state,.18f,0,0);}lockUntil=Mathf.Max(lockUntil,Time.time+hold);return true;}
        public void Roar()
        {
            if(definition==null||definition.roars.Length==0)return;
            int next=UnityEngine.Random.Range(0,definition.roars.Length);if(next==LastRoarVariant)next=(next+1)%definition.roars.Length;
            UI.ImportantCaptions.Show(definition.nameVi+" [gầm vang]",definition.nameEn+" [roars]",3);
            LastRoarVariant=next;RoarCount++;LastRoarAt=Time.time;var clip=definition.roars[next];voice.PlayOneShot(clip);
            if(definition.rumble!=null)rumble.PlayOneShot(definition.rumble);Play(definition.roarState,Mathf.Min(clip.length,4));
            NextRoarAt=Time.time+UnityEngine.Random.Range(definition.roarMin,definition.roarMax);SkyBeastAudioDuck.Ensure().Duck(clip.length);
            var p=EnemyDirector.Instance?.PlayerTransform;if(p!=null&&Vector3.Distance(p.position,transform.position)<120)p.GetComponent<GiantHandCameraImpulse>()?.Pulse(.07f);
            Roared?.Invoke(this);
        }
        float PoseSeconds(string state){foreach(var clip in animator.runtimeAnimatorController.animationClips)if(clip.name==state||clip.name.EndsWith("_"+state))return clip.length;return .8f;}
        IEnumerator PoseSequence(string start,string loop,string recover){foreach(var state in new[]{start,loop,recover}){float seconds=PoseSeconds(state);Play(state,seconds);yield return new WaitForSeconds(seconds);}pose=null;lockUntil=Time.time;Play("Fly_Cruise");}
        void BeginPose(string start,string loop,string recover){if(pose!=null)StopCoroutine(pose);lockUntil=Time.time;AnimationState=null;pose=StartCoroutine(PoseSequence(start,loop,recover));}
        public void RequestBreath(){BeginPose("Spell_Start","Spell_Loop","Spell_End");BreathRequested?.Invoke(this);}
        public void RequestBreath(float seconds)
        {
            bool charged=Charging;EndWarning();
            if(pose!=null)StopCoroutine(pose);lockUntil=Time.time;AnimationState=null;
            pose=StartCoroutine(TimedBreath(seconds,charged));BreathRequested?.Invoke(this);
        }
        IEnumerator TimedBreath(float seconds,bool charged=false)
        {
            float start=charged?0:Mathf.Min(PoseSeconds("Spell_Start"),seconds*.25f);
            if(start>0){Play("Spell_Start",start);yield return new WaitForSeconds(start);}
            Play("Spell_Loop",seconds-start);yield return new WaitForSeconds(Mathf.Max(.1f,seconds-start));
            float end=PoseSeconds("Spell_End");Play("Spell_End",end);yield return new WaitForSeconds(end);
            pose=null;lockUntil=Time.time;Play("Fly_Cruise");
        }
        public void RequestDive(){BeginPose("Dive_Start","Dive_Loop","Dive_Recover");}
        void OnDisable(){EndWarning();if(pose!=null)StopCoroutine(pose);pose=null;lockUntil=0;}
    }
}
