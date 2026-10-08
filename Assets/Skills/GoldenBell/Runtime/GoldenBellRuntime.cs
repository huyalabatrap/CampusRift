using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Progression;
namespace CampusRift.Skills
{
    public sealed class GoldenBellRuntime:Set1SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(1,.72f,.2f);
        protected override float Range=>1;
        public float ShieldRemaining {get;private set;}
        public float ShieldCapacity {get;private set;}
        public bool ShieldActive=>casting&&ShieldRemaining>0&&elapsed<6;
        public bool Shattered {get;private set;}
        SkillVfxPool.Node bell;
        BuffSystem buffs;
        readonly MonsterVitality[] blastTargets=new MonsterVitality[128];
        public int MasteryExplosionHits {get;private set;}
        protected override void Awake(){base.Awake();buffs=GetComponent<BuffSystem>();}
        protected override void OnCast()
        {
            ShieldCapacity=health.maxHealth*.4f*power;ShieldRemaining=ShieldCapacity;Shattered=false;MasteryExplosionHits=0;
            if(buffs!=null)buffs.SetSkillBuff("kim-chung-trao",StatType.FireResistance,.6f,6);
            bell=vfx.Spawn(SkillVfxKind.Bell,transform.position,Accent,6.5f,1,vfx.config.bell,vfx.config.voidCast);if(bell!=null){bell.Follow=transform;bell.Offset=Vector3.down*.05f;}
            var ring=vfx.Spawn(SkillVfxKind.Ring,transform.position,Accent,.7f,1.8f);if(ring!=null)ring.Progress=0;
            GetComponent<SkillCastPose>()?.Play(.45f,-5,35);
        }
        public float Absorb(float amount,DamageInfo incoming)
        {
            if(!ShieldActive||amount<=0)return amount;
            float absorbed=Mathf.Min(ShieldRemaining,amount);ShieldRemaining-=absorbed;
            impact.Pulse(.35f,.065f);
            var attacker=incoming.attacker!=null?incoming.attacker.GetComponentInParent<MonsterVitality>():null;
            if(attacker!=null)impact.HoldVictim(attacker,.065f);
            if(bell!=null)bell.Crack=1-ShieldRemaining/Mathf.Max(1,ShieldCapacity);
            var ripple=vfx.Spawn(SkillVfxKind.Ring,incoming.point,Accent,.45f,.7f,vfx.config.bell);if(ripple!=null)ripple.Progress=0;
            if(incoming.source==DamageSource.Melee&&incoming.attacker!=null)
            {
                var target=incoming.attacker.GetComponentInParent<IDamageable>();
                if(target!=null&&!target.IsDead)
                {var reflection=DamageInfo.Create(incoming.amount*.2f*CastDamageMultiplier,Element.Kim,DamageSource.Reaction,target.Anchor.position,-incoming.direction,gameObject);reflection.skillId=Id+"-reflection";target.ApplyDamage(reflection);}
            }
            if(ShieldRemaining<=.001f){Shattered=true;MasteryExplosion();vfx.Fragments(transform.position+Vector3.up,Accent,SkillVfxPool.MobileQuality?10:22,.65f,SkillVfxKind.BellShard);vfx.Burst(transform.position+Vector3.up,Accent,1.55f,vfx.config.iceHit);vfx.Spawn(SkillVfxKind.Shockwave,transform.position,Accent,.7f,2.7f);impact.Pulse(.6f,.065f);vfx.Spawn(SkillVfxKind.Scorch,transform.position,new Color(.22f,.12f,.02f),2.3f,1.7f);if(bell!=null)bell.Fade(.3f);Finish();}
            return Mathf.Max(0,amount-absorbed);
        }
        protected override void TickCast(float dt){if(elapsed>=6){if(bell!=null){bell.Fade(.5f);vfx.Emit(bell,SkillVfxPool.MobileQuality?8:20);}vfx.Spawn(SkillVfxKind.Scorch,transform.position,new Color(.22f,.12f,.02f),2,1.5f);Finish();}}
        void MasteryExplosion()
        {if(!Mastered)return;int n=0;foreach(var m in MonsterVitality.Active)if(m!=null&&!m.Defeated&&n<blastTargets.Length)blastTargets[n++]=m;for(int i=0;i<n;i++)if(InArea(blastTargets[i],transform.position,4)&&Hit(blastTargets[i],2,DamageSource.Skill,true))MasteryExplosionHits++;vfx.Spawn(SkillVfxKind.Shockwave,transform.position,Color.white,.8f,4);}
        protected override void Cleanup(){ShieldRemaining=0;if(buffs!=null)buffs.RemoveSkillBuff("kim-chung-trao");if(bell!=null){bell.StopLoop();bell.Fade(Shattered?.3f:.5f);bell=null;}}
    }
}
