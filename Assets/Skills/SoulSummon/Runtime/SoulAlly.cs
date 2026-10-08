using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Enemies;
namespace CampusRift.Skills
{
    public sealed class SoulAlly:MonoBehaviour,IHealingAlly
    {
        public MonsterVitality Body {get;private set;}public MinionMotor Motor {get;private set;}
        public GameObject Summoner {get;private set;}public float Damage,AttackRange,AttackCooldown,Until;
        public Transform Anchor=>transform;public bool Alive=>gameObject.activeInHierarchy&&Body!=null&&!Body.Defeated;
        public float MaxHealth=>Body!=null?Body.maxHealth:0;
        SkillVfxPool pool;SkillSet2VisualBatch.Slot shape;
        ImmortalSwordArrayRuntime array;
        public int Hits {get;private set;}public bool SwordSoul {get;set;}
        void Awake(){Body=GetComponent<MonsterVitality>();Motor=GetComponent<MinionMotor>();}
        public bool Launch(SoulSummonRuntime.Corpse corpse,GameObject player,SkillVfxPool effects,float power)
        {
            pool=effects;Summoner=player;array=player.GetComponent<ImmortalSwordArrayRuntime>();Body.SetFaction(CombatFaction.Ally);Body.Element=Element.Am;Body.defense=corpse.defense*.6f*power;Body.SetMaxHealth(corpse.health*.6f*power,true);Body.ResetVitality();
            Damage=corpse.damage*.6f*power*(player.GetComponent<SoulSummonRuntime>()?.CastDamageMultiplier??1);AttackRange=Mathf.Max(1.3f,corpse.range);AttackCooldown=corpse.cooldown;Until=Time.time+20;Hits=0;SwordSoul=false;
            Motor.Agent.enabled=false;Motor.Agent.agentTypeID=corpse.agentType;
            gameObject.SetActive(true);if(!Motor.Place(corpse.position)){gameObject.SetActive(false);return false;}Motor.ConfigureAlly(corpse.speed*.6f*power);Body.ResetVitality();
            shape=pool.Shapes.Spawn(SkillShape.Soul,transform.position,new Color(.016f,.003f,.05f),20.5f,Vector3.one*.9f,transform.rotation);if(shape!=null){shape.Follow=transform;shape.Opacity=.48f;}
            if(!HealingAllies.Active.Contains(this))HealingAllies.Active.Add(this);
            pool.Spawn(SkillVfxKind.Shockwave,transform.position,new Color(.5f,.1f,1),.7f,1.2f,pool.config.voidCast);return true;
        }
        public void Heal(float amount){Body.Heal(amount);}
        public void Strike(MonsterVitality target)
        {
            if(!Alive||target==null||target.Defeated||target.Faction!=CombatFaction.Hostile)return;
            var info=DamageCalculator.Compute(Damage,SwordSoul?1.5f:1,Element.Am,target,0,1.5f,null,DamageSource.Melee);info.attacker=Summoner;info.point=target.transform.position+Vector3.up;info.direction=(target.transform.position-transform.position).normalized;info.skillId="am-binh-quy-hon";
            if(target.ApplyDamage(info)){Hits++;var arc=pool.Spawn(SkillVfxKind.Slash,transform.position+Vector3.up,new Color(.55f,.2f,1),.4f,.5f,pool.config.sword);if(arc!=null)arc.End=target.transform.position+Vector3.up;}
        }
        void Update(){if(!Alive||Time.time>=Until){Dissolve();return;}if(shape!=null)shape.Rotation=transform.rotation;bool inside=array!=null&&array.Contains(transform.position);if(inside&&!SwordSoul)ReactionResolver.PublishExtended(ReactionType.SwordSoul,Body,Summoner,transform.position);SwordSoul=inside;}
        public void Dissolve(){shape?.Fade(.5f);shape=null;if(pool!=null)pool.Spawn(SkillVfxKind.SwordDust,transform.position+Vector3.up,new Color(.5f,.12f,1),.8f);HealingAllies.Active.Remove(this);Motor?.Stop();gameObject.SetActive(false);}
        void OnDisable(){HealingAllies.Active.Remove(this);shape?.Fade(.5f);shape=null;}
    }
}
