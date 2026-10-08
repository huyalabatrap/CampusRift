using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Combat;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARHandInteractions : MonoBehaviour
    {
        public readonly ARHandMotion motion=new ARHandMotion();
        public EnemyInstance Held {get;private set;}public float SwipeCooldown=>Mathf.Max(0,nextSwipe-field.Clock);
        public string Notice {get;private set;}="";public float NoticeUntil {get;private set;}
        ARBattlefield field;ARSkillCaster caster;ARMonsterDirector director;float nextSwipe;
        LineRenderer slash;Material slashMaterial;float slashUntil;
        void Awake()
        {field=GetComponent<ARBattlefield>();caster=GetComponent<ARSkillCaster>();director=GetComponent<ARMonsterDirector>();motion.PinchStarted+=Lift;motion.PinchReleased+=Release;motion.Swiped+=Swipe;}
        void Start(){field.Removing+=Clear;director.BattleStarted+=Clear;}
        public bool Observe(GestureFrame frame,bool allowed)
        {
            bool actions=allowed&&!caster.Practice&&!field.CheckLoad&&field.ModeSession.Has(ARModeFeature.HandPhysics)&&!(GetComponent<ARKnowledgeSeal>()?.Active??false);
            motion.Process(frame,field.placement.view,field.placement.settings,allowed,actions);return motion.BlocksStatic;
        }
        void Say(string vi,string en){Notice=LevelHUD.Vietnamese?vi:en;NoticeUntil=Time.unscaledTime+1.4f;}
        void Lift()
        {
            if(Held!=null)return;EnemyInstance best=null;float distance=130*Screen.height/1080f;
            foreach(var actor in director.Actors)
            {
                if(actor==null||!actor.Alive||(GetComponent<ARDepthCollision>()?.Hidden(actor.Vitality)??false)||actor.Status==null||!(actor.Status.Has(StatusType.Stun)||actor.Status.Has(StatusType.Freeze))||!actor.GetComponent<ARMinionBrain>().enabled)continue;
                var p=field.placement.view.WorldToScreenPoint(actor.transform.position+Vector3.up*.8f*field.Scale);float d=Vector2.Distance(p,new Vector2(Screen.width*.5f,Screen.height*.5f));
                if(p.z<=0||d>=distance)continue;best=actor;distance=d;
            }
            if(best==null){Say("Ngắm quái Choáng / Băng rồi véo","Aim at a stunned / frozen foe and pinch");return;}
            Held=best;(best.GetComponent<ARThrownMonster>()??best.gameObject.AddComponent<ARThrownMonster>()).Lift(field,caster);caster.sequences.Cancel();
            Say("ĐÃ NHẤC · vung rồi thả véo","LIFTED · swing, then release pinch");ARHaptics.Pulse();
        }
        void Release(Vector3 velocity,bool allowed)
        {
            if(Held==null)return;Held.GetComponent<ARThrownMonster>()?.Release(velocity,allowed);Held=null;
            Say(allowed&&velocity.magnitude>=.55f?"NÉM!":"ĐÃ ĐẶT XUỐNG",allowed&&velocity.magnitude>=.55f?"THROW!":"SET DOWN");
        }
        void Swipe(int sign)
        {
            if(field.Clock<nextSwipe||!caster.AimValid)return;nextSwipe=field.Clock+4;caster.sequences.Cancel();
            var right=Vector3.ProjectOnPlane(field.placement.view.transform.right,Vector3.up).normalized*sign;
            Vector3 origin=caster.Aim;float radius=field.placement.Radius;
            foreach(var enemy in director.Actors)
            {
                if(enemy==null||!enemy.Alive||(GetComponent<ARDepthCollision>()?.Hidden(enemy.Vitality)??false))continue;
                var d=Vector3.ProjectOnPlane(enemy.transform.position-origin,Vector3.up);
                if(Mathf.Abs(Vector3.Dot(d,Vector3.Cross(Vector3.up,right)))>.65f*field.Scale||Mathf.Abs(Vector3.Dot(d,right))>radius*2)continue;
                var stats=caster.Caster.GetComponent<PlayerStats>();var hit=DamageInfo.Create(stats.Attack*stats.DamageDealt*1.5f,Element.Kim,DamageSource.Skill,enemy.transform.position,right,caster.Caster);hit.skillId="ar-sword-wave";hit.isArea=true;enemy.Vitality.ApplyDamage(hit);
            }
            if(slash==null)
            {slash=new GameObject("AR sword wave visual").AddComponent<LineRenderer>();slash.transform.SetParent(transform);slashMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));slashMaterial.color=ComicTheme.Gold;slash.sharedMaterial=slashMaterial;slash.positionCount=9;slash.numCapVertices=4;}
            slash.startWidth=.1f*field.Scale;slash.endWidth=.015f*field.Scale;slash.gameObject.SetActive(true);
            for(int i=0;i<9;i++)slash.SetPosition(i,origin+right*((i/8f-.5f)*radius*1.8f)+Vector3.up*(.7f+Mathf.Sin(i/8f*Mathf.PI)*.4f)*field.Scale);
            slashUntil=Time.unscaledTime+.3f;Say("KIẾM KHÍ · hồi 4s","SWORD WAVE · 4s cooldown");ARHaptics.Skill("Victory");
        }
        void Update()
        {
            if(slash!=null&&Time.unscaledTime>=slashUntil)slash.gameObject.SetActive(false);
            if(field.Root==null||field.Paused||director.Finished||caster.source.Recovering)motion.Cancel();
            if(Held!=null&&(!Held.Alive||!(Held.GetComponent<ARThrownMonster>()?.Active??false)))Held=null;
        }
        public void Clear(){motion.Cancel();Held=null;nextSwipe=0;if(slash!=null)slash.gameObject.SetActive(false);foreach(var e in director.Actors)if(e!=null)e.GetComponent<ARThrownMonster>()?.Restore();}
        void OnDestroy(){if(field!=null)field.Removing-=Clear;if(director!=null)director.BattleStarted-=Clear;if(slash!=null)Destroy(slash.gameObject);if(slashMaterial!=null)Destroy(slashMaterial);}
    }
}
