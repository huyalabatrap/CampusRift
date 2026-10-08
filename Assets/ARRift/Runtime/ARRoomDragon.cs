using UnityEngine;
using CampusRift.SkyBeast;
using CampusRift.UI;
namespace CampusRift.AR
{
    // Uses all three P12 dragons, sockets, flight clips and roar cadence with an AR clock.
    public sealed class ARRoomDragon : MonoBehaviour
    {
        public Transform Dragon {get;private set;} public Transform WeakPoint {get;private set;}
        public bool Alive=>Dragon!=null; public int Armor {get;private set;} public float Health {get;private set;}
        public bool Exposed=>Armor<=0&&Health<=0;
        public bool Targeted {get;private set;} public bool Breathing {get;private set;}
        public float FireRemaining=>Breathing?Mathf.Max(0,fireUntil-field.Clock):0;
        public SkyBeastDefinition Definition {get;private set;}
        ARBattlefield field; System.Random random; Animator animator; Transform mouth,rigRoot;Vector3 rootRest;ARAdaptiveQuality quality;LODGroup[] lods;
        AudioSource voice; LineRenderer flame; ParticleSystem fire; Material weakMaterial;
        Vector3 centre;float born,nextRoar,nextFire,fireUntil,warningAt,lastHit=-100;int lastVariant=-1;
        string primed;float primeUntil; bool fireHit;
        readonly float[] skillReady=new float[5];float nextArmorAt;
        public bool WeakPointOpen=>Armor<=0||field.Clock>=nextArmorAt;
        public float ArmorOpensIn=>Mathf.Max(0,nextArmorAt-field.Clock);
        void Awake(){field=GetComponent<ARBattlefield>();quality=FindAnyObjectByType<ARAdaptiveQuality>();}
        public void Spawn(int seed,bool duel)
        {
            Clear();random=new System.Random(seed^1212);string[] ids={"020","023","026"};Definition=Resources.Load<SkyBeastDefinition>("P12/Dragon"+ids[random.Next(3)]);
            if(Definition==null||Definition.prefab==null)return;
            Dragon=Instantiate(Definition.prefab).transform;Dragon.name="AR P12 room dragon "+Definition.id;
            foreach(var controller in Dragon.GetComponentsInChildren<SkyBeastController>())controller.enabled=false;
            foreach(var c in Dragon.GetComponentsInChildren<Collider>())c.enabled=false;
            float scale=Definition.scale*field.Scale*.065f;Dragon.localScale=Vector3.one*scale;
            animator=Dragon.GetComponentInChildren<Animator>();if(animator!=null){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;Play("Fly_Cruise");}
            foreach(var t in Dragon.GetComponentsInChildren<Transform>()){if(t.name=="Socket_Mouth")mouth=t;if(t.name=="Root"){rigRoot=t;rootRest=t.localPosition;}}
            lods=Dragon.GetComponentsInChildren<LODGroup>();foreach(var lod in lods)lod.ForceLOD(Mathf.Min(lod.lodCount-1,quality!=null&&quality.Reduced?2:1));
            centre=field.placement.view.transform.position;born=field.Clock;nextRoar=born+Interval();nextFire=born+12;
            Armor=duel?3:0;Health=duel?6:0;nextArmorAt=born+30;System.Array.Clear(skillReady,0,skillReady.Length);
            voice=Dragon.gameObject.AddComponent<AudioSource>();ARCombatAudio.Spatial(voice,field.Scale);
            var point=GameObject.CreatePrimitive(PrimitiveType.Sphere);point.name="Glowing dragon weak point";Destroy(point.GetComponent<Collider>());point.transform.SetParent(mouth!=null?mouth:Dragon,false);point.transform.localPosition=Vector3.zero;point.transform.localScale=Vector3.one*(.2f/scale);WeakPoint=point.transform;
            weakMaterial=new Material(GetComponent<ARSkillCaster>().handConfig.handMaterial);point.GetComponent<Renderer>().sharedMaterial=weakMaterial;
            flame=FireVisualFactory.Line(Dragon,"AR breath down to Shrine",.12f*field.Scale,new Color(3,1,.1f),true);flame.enabled=false;
            fire=FireVisualFactory.RollingFire(Dragon,"AR pooled throat flame",.18f*field.Scale,.35f);var main=fire.main;main.maxParticles=24;main.startSpeed=0;
            Position();
        }
        float Interval()=>Mathf.Lerp(Definition.roarMin,Definition.roarMax,(float)random.NextDouble());
        void Play(string state){if(animator!=null&&animator.HasState(0,Animator.StringToHash("Base Layer."+state)))animator.CrossFadeInFixedTime(state,.18f);}
        void Position()
        {
            float a=(field.Clock-born)/Mathf.Max(30,Definition.period)*Mathf.PI*2;
            float radius=Mathf.Clamp(field.placement.Radius*1.2f,.75f,1.8f);float height=Mathf.Clamp(2*field.Scale,.8f,1.4f);
            var want=centre+new Vector3(Mathf.Sin(a)*radius,height+.12f*Mathf.Sin(a*2),Mathf.Cos(a)*radius);
            // Keep the flight above the camera, including when the player ducks or raises it.
            want.y=Mathf.Max(want.y,field.placement.view.transform.position.y+.65f);Dragon.position=want;Dragon.rotation=Quaternion.LookRotation(new Vector3(Mathf.Cos(a),.05f,-Mathf.Sin(a)));
        }
        public bool AimAtWeakPoint()
        {
            if(WeakPoint==null)return false;var camera=field.placement.view;var delta=WeakPoint.position-camera.transform.position;
            return delta.sqrMagnitude>.01f&&Vector3.Angle(camera.transform.forward,delta)<=8&&!(GetComponent<ARDepthCollision>()?.OccludedAt(WeakPoint.position)??false);
        }
        public bool AcceptSingle(string label)
        {
            if(!Alive||!AimAtWeakPoint()||!WeakPointOpen||field.Clock-lastHit<.8f||label=="Thumb_Up")return false;
            var caster=GetComponent<ARSkillCaster>();int index=System.Array.IndexOf(GestureSkillMapper.Labels,label);var runtime=caster.Runtime(label);
            if(index<0||index>=skillReady.Length||runtime==null||!runtime.IsReady||field.Clock<skillReady[index])return false;
            var spirit=caster.Caster.GetComponent<CampusRift.Combat.SpiritPower>();if(spirit!=null&&!spirit.TrySpend(runtime.SpiritCost))return false;
            skillReady[index]=field.Clock+runtime.CooldownDuration;lastHit=field.Clock;
            if(Armor>0)
            {
                bool combo=field.Clock<=primeUntil&&(primed=="Open_Palm"&&label=="Victory"||primed=="Thumb_Down"&&label=="Pointing_Up"||primed=="Closed_Fist"&&label=="Victory");
                if(combo){Armor--;nextArmorAt=field.Clock+60;primed=null;caster.AddSeal(15);GetComponent<ARMonsterDirector>().RegisterSpaceReaction();}
                else {primed=label;primeUntil=field.Clock+3;}
            }
            else {Health=Mathf.Max(0,Health-1);caster.AddSeal(2);}
            GetComponent<ARCombatAudio>().Chime();return true;
        }
        public void AcceptUltimate(float power=1){if(!Alive||!AimAtWeakPoint()||!WeakPointOpen)return;if(Armor>0){Armor--;nextArmorAt=field.Clock+60;primed=null;GetComponent<ARMonsterDirector>().RegisterSpaceReaction();}else Health=Mathf.Max(0,Health-2*power);}
        void Update()
        {
            if(!Alive)return;Targeted=AimAtWeakPoint();bool pause=field.Paused||GetComponent<ARMonsterDirector>().Finished;
            if(animator!=null)animator.speed=pause?0:field.GetComponent<ARPlayerCombat>()?.ClockRate??1;
            if(pause){if(voice.isPlaying)voice.Pause();flame.enabled=false;fire.Pause();return;}
            Position();voice.volume=ARCombatAudio.Volume*(SettingsManager.Instance?.Current.MonsterVolume??1);
            int budget=quality!=null?quality.RoomVfxBudget:24;var main=fire.main;main.maxParticles=budget;foreach(var lod in lods)lod.ForceLOD(Mathf.Min(lod.lodCount-1,quality!=null&&quality.Reduced?2:1));
            if(field.Clock>=nextRoar){int index=random.Next(Mathf.Max(1,Definition.roars.Length));if(index==lastVariant)index=(index+1)%Mathf.Max(1,Definition.roars.Length);lastVariant=index;if(Definition.roars.Length>0){voice.PlayOneShot(Definition.roars[index]);Play(Definition.roarState);}nextRoar=field.Clock+Interval();}
            if(!Breathing&&field.Clock>=nextFire){Breathing=true;fireHit=false;warningAt=field.Clock+1.2f;fireUntil=warningAt+2;Play("Spell_Start");}
            if(Breathing)
            {
                Vector3 from=mouth!=null?mouth.position:Dragon.position;flame.SetPosition(0,from);flame.SetPosition(1,field.Shrine.transform.position+Vector3.up*.4f*field.Scale);flame.enabled=field.Clock>=warningAt;fire.transform.position=from;if(!fire.isPlaying)fire.Play();
                if(!fireHit&&field.Clock>=warningAt){fireHit=true;field.Shrine.TakeDamage(18);Play("Spell_Loop");}
                if(field.Clock>=fireUntil){Breathing=false;nextFire=field.Clock+16;flame.enabled=false;fire.Stop(true,ParticleSystemStopBehavior.StopEmitting);Play("Fly_Cruise");}
            }
            Color color=!WeakPointOpen?ComicTheme.Muted:Targeted?ComicTheme.Gold:ComicTheme.Purple;if(weakMaterial.HasProperty("_BaseColor"))weakMaterial.SetColor("_BaseColor",color);if(weakMaterial.HasProperty("_Emission"))weakMaterial.SetColor("_Emission",color*1.5f);
        }
        public void Clear(){if(Dragon!=null)Destroy(Dragon.gameObject);if(weakMaterial!=null)Destroy(weakMaterial);Dragon=WeakPoint=null;mouth=null;Breathing=Targeted=false;primed=null;}
        void LateUpdate(){if(rigRoot!=null)rigRoot.localPosition=rootRest;}
        void OnDestroy(){Clear();}
    }
}
