using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Enemies;
namespace CampusRift.Skills
{
    public sealed class SoulSummonRuntime:Set2SkillRuntime
    {
        public struct Corpse {public Vector3 position;public float time,health,damage,speed,defense,range,cooldown;public int agentType;public bool used;}
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(.55f,.12f,1);
        readonly Corpse[] corpses=new Corpse[24];int next;readonly SoulAlly[] allies=new SoulAlly[5];GameObject root;
        public int Raised {get;private set;}public int LiveAllies {get{int n=0;foreach(var a in allies)if(a!=null&&a.Alive)n++;return n;}}
        public int AlliedHits {get{int n=0;foreach(var a in allies)if(a!=null)n+=a.Hits;return n;}}
        public int AvailableCorpses {get{int n=0;foreach(var c in corpses)if(c.health>0&&!c.used&&Time.time-c.time<=8)n++;return n;}}
        public override bool IsReady=>base.IsReady&&AvailableCorpses>0;
#if UNITY_EDITOR
        public void ClearCorpsesForValidation(){System.Array.Clear(corpses,0,corpses.Length);next=0;}
#endif
        protected override void Awake()
        {
            base.Awake();root=new GameObject("P18 pooled allied spirits");SceneManager.MoveGameObjectToScene(root,gameObject.scene);
            for(int i=0;i<5;i++)
            {var go=new GameObject("Pooled soul ally "+i);go.transform.SetParent(root.transform);go.transform.position=transform.position;var agent=go.AddComponent<NavMeshAgent>();agent.enabled=false;agent.radius=.28f;agent.height=1.8f;agent.stoppingDistance=.8f;var body=go.AddComponent<MonsterVitality>();body.SetFaction(CombatFaction.Ally);go.AddComponent<StatusEffectHost>();go.AddComponent<MinionMotor>();var collider=go.AddComponent<CapsuleCollider>();collider.radius=.3f;collider.height=1.8f;collider.center=Vector3.up*.9f;allies[i]=go.AddComponent<SoulAlly>();go.AddComponent<MinionBrain>();go.SetActive(false);}
        }
        void OnEnable(){MonsterVitality.AnyDamaged+=Remember;}
        void Remember(MonsterVitality body,DamageInfo damage)
        {
            if(body==null||!body.Defeated||body.Faction!=CombatFaction.Hostile||body.resistHardControl)return;
            var e=body.GetComponent<EnemyInstance>();if(e==null||e.archetype==null||e.archetype.isBoss||e.archetype.swordIntentWeight>=4)return;
            corpses[next]=new Corpse{position=body.transform.position,time=Time.time,health=body.maxHealth,damage=e.Damage,speed=e.Speed,defense=body.defense,range=e.archetype.attackRange,cooldown=e.archetype.attackCooldown,agentType=body.GetComponent<NavMeshAgent>().agentTypeID};next=(next+1)%corpses.Length;
        }
        protected override void OnCast()
        {
            Raised=0;int max=Mastered?5:3;
            for(int k=0;k<corpses.Length&&Raised<max;k++){int index=(next-1-k+corpses.Length)%corpses.Length;var c=corpses[index];if(c.health<=0||c.used||Time.time-c.time>8)continue;foreach(var ally in allies)if(!ally.Alive){if(ally.Launch(c,gameObject,vfx,power)){c.used=true;corpses[index]=c;Raised++;}break;}}
            vfx.Spawn(SkillVfxKind.Ring,transform.position,Accent,1,3,vfx.config.voidCast);GetComponent<SkillCastPose>()?.Play(.7f,-12,80);
        }
        protected override void TickCast(float dt){if(elapsed>=20.1f||elapsed>.6f&&LiveAllies==0)Finish();}
        protected override void Cleanup(){foreach(var a in allies)if(a!=null&&a.Alive)a.Dissolve();}
        protected override void OnDisable(){MonsterVitality.AnyDamaged-=Remember;base.OnDisable();System.Array.Clear(corpses,0,corpses.Length);}
        void OnDestroy(){if(root!=null)Destroy(root);}
    }
}
