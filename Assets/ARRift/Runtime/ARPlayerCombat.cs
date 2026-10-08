using UnityEngine;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Skills;
namespace CampusRift.AR
{
    [DisallowMultipleComponent]
    public sealed class ARPlayerCombat:MonoBehaviour
    {
        public const float WarningSeconds=1.2f,DodgeMetres=.25f,PerfectWindow=.25f,ShieldCooldown=6;
        sealed class Shot
        {public GameObject visual;public AudioSource audio;public bool live,returning,meteor;public EnemyInstance enemy;public float launch,arrive,damage;public Vector3 start,target,right,up;public Quaternion rotation;}
        readonly Shot[] shots=new Shot[6];
        ARBattlefield field;ARSkillCaster caster;ARMonsterDirector director;ARBattleEvents events;ARCombatAudio sound;
        Material orbMaterial;SkillVfxPool.Node bell;float shieldAt=-100,shieldUntil,nextShield,slowUntil,meteorAt;int meteorRemaining;bool priorPaused,havePose;Vector3 previousCamera;
        public bool PendingHazards {get{if(meteorRemaining>0)return true;foreach(var s in shots)if(s!=null&&s.live)return true;return false;}}
        public bool ShieldActive=>bell!=null&&bell.Live&&field.Clock<shieldUntil;
        public float CooldownRemaining=>Mathf.Max(0,nextShield-field.Clock);
        public float WarningRemaining {get;private set;}
        public Vector3 WarningPosition {get;private set;}
        public float DodgeUntil {get;private set;} public float ImpactUntil {get;private set;}
        public string Notice {get;private set;}="";public float NoticeUntil {get;private set;}
        public float ClockRate=>Time.unscaledTime<slowUntil?.2f:1;
        public bool Enabled=>field!=null&&field.ModeSession.PlayerAttacksEnabled&&!field.CheckLoad&&!caster.Practice;
        void Awake(){field=GetComponent<ARBattlefield>();caster=GetComponent<ARSkillCaster>();director=GetComponent<ARMonsterDirector>();sound=GetComponent<ARCombatAudio>()??gameObject.AddComponent<ARCombatAudio>();}
        void Start(){events=GetComponent<ARBattleEvents>();events.MeteorRequested+=Meteors;field.Removing+=Clear;field.Built+=ResetBattle;director.BattleStarted+=ResetBattle;CreatePool();}
        void CreatePool()
        {
            orbMaterial=new Material(caster.handConfig.handMaterial);
            if(orbMaterial.HasProperty("_BaseColor"))orbMaterial.SetColor("_BaseColor",new Color(1,.12f,.25f));
            if(orbMaterial.HasProperty("_Emission"))orbMaterial.SetColor("_Emission",new Color(1,.16f,.3f));
            for(int i=0;i<shots.Length;i++){var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="AR incoming energy "+i;Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=orbMaterial;go.transform.localScale=Vector3.one*.13f;var audio=go.AddComponent<AudioSource>();ARCombatAudio.Spatial(audio);go.SetActive(false);shots[i]=new Shot{visual=go,audio=audio};}
        }
        public bool RaiseShield()
        {
            if(field.Paused||caster.Caster==null||CooldownRemaining>0)return false;
            bell?.Fade(.05f);var camera=field.placement.view;var pool=caster.Caster.GetComponent<SkillVfxPool>();
            bell=pool.Spawn(SkillVfxKind.Bell,camera.transform.position+camera.transform.forward*.75f-camera.transform.up*.22f,CampusRift.UI.ComicTheme.Gold,2,.18f/field.Scale);
            if(bell==null)return false;bell.Position=camera.transform.position+camera.transform.forward*.75f-camera.transform.up*.22f;bell.Radius=.18f;bell.Opacity=.65f;caster.Caster.GetComponent<ARCombatContext>().CameraEffect=bell.root.transform;shieldAt=field.Clock;shieldUntil=field.Clock+2;nextShield=field.Clock+ShieldCooldown;ARHaptics.Skill("Thumb_Up");return true;
        }
        public bool Launch(EnemyInstance enemy,bool meteor=false)
        {
            if(!Enabled||field.Paused||director.Finished||field.placement.view==null||!meteor&&(enemy==null||!enemy.Alive))return false;
            Shot shot=null;foreach(var item in shots)if(item!=null&&!item.live){shot=item;break;}if(shot==null)return false;
            var camera=field.placement.view.transform;shot.live=true;shot.returning=false;shot.enemy=enemy;shot.meteor=meteor;shot.launch=field.Clock;shot.arrive=field.Clock+WarningSeconds;shot.damage=meteor?25:Mathf.Max(12,enemy.Damage);
            shot.target=camera.position;shot.rotation=camera.rotation;shot.right=camera.right;shot.up=camera.up;shot.start=meteor?camera.position+camera.forward*2+Vector3.up*1.8f:enemy.transform.position+Vector3.up*1.2f*field.Scale;
            shot.visual.transform.position=shot.start;shot.visual.GetComponent<Renderer>().sharedMaterial=orbMaterial;shot.visual.SetActive(true);sound.Warn(shot.audio);return true;
        }
        void Meteors(){meteorRemaining=3;meteorAt=field.Clock;}
        bool Dodged(Shot shot)
        {
            var camera=field.placement.view.transform;var delta=camera.position-shot.target;
            // Translation from real 6DoF is primary. A physical phone lean may also move the
            // projected head centre at a conservative 40cm viewing distance; pure yaw is ignored.
            var oldHead=shot.target+shot.rotation*Vector3.up*.4f;var head=camera.position+camera.up*.4f;
            bool lateral=Mathf.Abs(Vector3.Dot(delta,shot.right))>=DodgeMetres||Mathf.Abs(Vector3.Dot(head-oldHead,shot.right))>=DodgeMetres;
            return lateral||Vector3.Dot(delta,shot.up)<=-DodgeMetres;
        }
        void Update()
        {
            WarningRemaining=0;
            if(field.Paused){CancelShots();bell?.Fade(.05f);bell=null;slowUntil=0;priorPaused=true;havePose=false;return;}
            if(ShieldActive){var camera=field.placement.view.transform;bell.Position=camera.position+camera.forward*.75f-camera.up*.22f;}
            if(field.Root==null||!Enabled||director.Finished){CancelShots();meteorRemaining=0;slowUntil=0;havePose=false;return;}
            if(priorPaused){priorPaused=false;return;}
            var position=field.placement.view.transform.position;
            if(havePose&&(position-previousCamera).sqrMagnitude>.65f*.65f){CancelShots();havePose=false;return;}
            previousCamera=position;havePose=true;
            if(meteorRemaining>0&&field.Clock>=meteorAt){if(Launch(null,true)){meteorRemaining--;meteorAt=field.Clock+.8f;}}
            foreach(var shot in shots)
            {
                if(shot==null||!shot.live)continue;
                if(shot.returning)
                {
                    if(shot.enemy==null||!shot.enemy.Alive){Release(shot);continue;}
                    shot.target=shot.enemy.transform.position+Vector3.up*field.Scale;
                    var returningPosition=Vector3.Lerp(shot.start,shot.target,Mathf.Clamp01((field.Clock-shot.launch)/.35f));
                    if(!MoveShot(shot,returningPosition))continue;
                    if(field.Clock>=shot.arrive){if(!(GetComponent<ARDepthCollision>()?.Hidden(shot.enemy.Vitality)??false)){var info=DamageInfo.Create(shot.damage*2,Element.Kim,DamageSource.Projectile,shot.target,(shot.target-shot.start).normalized,caster.Caster);info.skillId="kim-chung-trao";shot.enemy.Vitality.ApplyDamage(info);}Release(shot);}continue;
                }
                float remaining=shot.arrive-field.Clock;if(remaining>WarningRemaining){WarningRemaining=remaining;WarningPosition=shot.start;}
                var destination=Vector3.Lerp(shot.start,shot.target,Mathf.Clamp01((field.Clock-shot.launch)/WarningSeconds));
                if(!MoveShot(shot,destination))continue;
                if(remaining>0)continue;
                if(Dodged(shot)){caster.AddSeal(10);DodgeUntil=Time.unscaledTime+.8f;slowUntil=Time.unscaledTime+.3f;ARHaptics.Dodge();Release(shot);continue;}
                if(ShieldActive)
                {bool perfect=shot.arrive-shieldAt>=0&&shot.arrive-shieldAt<=PerfectWindow+1e-5f;bell.Fade(.12f);bell=null;Notice=CampusRift.UI.LevelHUD.Vietnamese?(perfect?"PHẢN ĐÒN!":"ĐỠ!"):(perfect?"REFLECT!":"BLOCK!");NoticeUntil=Time.unscaledTime+.9f;ARHaptics.Block(perfect);if(perfect&&shot.enemy!=null&&shot.enemy.Alive){shot.returning=true;shot.start=shot.visual.transform.position;shot.launch=field.Clock;shot.arrive=field.Clock+.35f;shot.audio.Stop();}else Release(shot);continue;}
                caster.Caster?.GetComponent<SpiritPower>()?.Drain(shot.damage);ImpactUntil=Time.unscaledTime+.5f;ARHaptics.Impact();Release(shot);
            }
        }
        bool MoveShot(Shot shot,Vector3 destination)
        {
            var depth=GetComponent<ARDepthCollision>();
            if(depth!=null&&depth.Sweep(shot.visual.transform.position,destination,out var contact)){caster.Caster.GetComponent<SkillVfxPool>().Burst(contact,CampusRift.UI.ComicTheme.Gold,.35f/field.Scale,null,false);Release(shot);return false;}
            shot.visual.transform.position=destination;return true;
        }
        void ResetBattle(){Clear();caster.Caster?.GetComponent<SpiritPower>()?.Refill();}
        void Release(Shot shot){shot.live=false;shot.audio.Stop();shot.visual.SetActive(false);}
        void CancelShots(){foreach(var shot in shots)if(shot!=null&&shot.live)Release(shot);WarningRemaining=0;}
        void Clear(){CancelShots();bell?.Fade(.05f);bell=null;meteorRemaining=0;havePose=false;nextShield=0;slowUntil=0;DodgeUntil=ImpactUntil=NoticeUntil=0;}
        void OnDestroy(){if(events!=null)events.MeteorRequested-=Meteors;if(director!=null)director.BattleStarted-=ResetBattle;if(field!=null){field.Removing-=Clear;field.Built-=ResetBattle;}Clear();foreach(var shot in shots)if(shot!=null)Destroy(shot.visual);if(orbMaterial!=null)Destroy(orbMaterial);}
    }
}
