using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    public sealed class FireLotusRuntime:Set1SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(1,.25f,.025f);
        protected override float Range=>18;
        protected override float AimRadius=>7;
        SkillVfxPool.Node flower,zone,ribbon,trailSmoke;
        readonly SkillVfxPool.Node[] petals=new SkillVfxPool.Node[2];
        public int FlowerCount=>Mastered?3:1;
        Vector3 launch;bool thrown,exploded;float impactAt,nextTick;int danger;
        public bool HasExploded=>exploded;
        public float ExplosionTime=>impactAt;
        protected override void OnCast()
        {
            thrown=exploded=false;flower=vfx.Spawn(SkillVfxKind.Lotus,HandPoint(),Accent,7,1,vfx.config.fireCast,vfx.config.fireCast);if(flower!=null){flower.Follow=transform;flower.Offset=Vector3.up*1.3f+transform.right*.5f;flower.Progress=0;}
            if(Mastered){if(flower!=null)flower.Radius=.55f;for(int i=0;i<2;i++){petals[i]=vfx.Spawn(SkillVfxKind.Lotus,HandPoint()+transform.right*(i==0?-.8f:.8f),Accent,7,.55f);if(petals[i]!=null){petals[i].Follow=transform;petals[i].Offset=Vector3.up*1.3f+transform.right*(i==0?-.3f:1.3f);}}}
            explorer.SkillMoveMultiplier=.35f;GetComponent<SkillCastPose>()?.Play(1.4f,-8,65);
            danger=DangerZoneRegistry.Register(point,7,6);vfx.Spawn(SkillVfxKind.Ring,point,Accent,1.95f,7);
        }
        Vector3 HandPoint()=>transform.position+Vector3.up*1.3f+transform.right*.5f;
        protected override void TickCast(float dt)
        {
            if(!thrown){if(flower!=null)flower.Progress=Mathf.Clamp01(elapsed/1.2f);foreach(var p in petals)if(p!=null)p.Progress=Mathf.Clamp01(elapsed/1.2f);if(elapsed<1.2f)return;thrown=true;launch=HandPoint();ribbon=vfx.Spawn(SkillVfxKind.FireRibbon,launch,Accent,1.05f);trailSmoke=vfx.Spawn(SkillVfxKind.FireTrailSmoke,launch,Accent,1.05f);if(flower!=null){flower.Follow=null;flower.StopLoop();}foreach(var p in petals)if(p!=null){p.Follow=null;p.StopLoop();}explorer.SkillMoveMultiplier=1;GetComponent<SkillCastPose>()?.Play(.35f,12,-45);}
            if(!exploded)
            {
                float t=Mathf.Clamp01((elapsed-1.2f)/.65f);Vector3 next=Vector3.Lerp(launch,point,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*1.4f;
                for(int i=0;i<2;i++)if(petals[i]!=null)petals[i].Position=next+Vector3.Cross(Vector3.up,direction)*(i==0?-1:1)*(1+t);
                bool solidContact=false;
                if(flower!=null){Vector3 previous=flower.Position;Vector3 wall;if(CombatLine.Block(previous,next,out wall)){next=wall;point=wall;solidContact=true;}flower.Position=next;if(ribbon!=null)ribbon.Position=next;if(trailSmoke!=null)trailSmoke.Position=next;}
                bool contact=false;for(int i=0;i<MonsterVitality.Active.Count;i++){var m=MonsterVitality.Active[i];if(m!=null&&!m.Defeated&&(m.transform.position+Vector3.up-next).sqrMagnitude<.7f){contact=true;point=m.transform.position;break;}}
                if(t>=1||contact||solidContact){Explode();impactAt=elapsed;nextTick=elapsed+.5f;}
                return;
            }
            if(elapsed>=nextTick&&elapsed<impactAt+4)
            {
                nextTick+=.5f;
                for(int i=0;i<MonsterVitality.Active.Count;i++){var m=MonsterVitality.Active[i];if(InArea(m,point,7))Hit(m,.2f,DamageSource.Reaction);}
            }
            if(elapsed>=impactAt+4){if(zone!=null)zone.Fade(.65f);Finish();}
        }
        void Explode()
        {
            for(int i=0;i<2;i++){if(petals[i]!=null){vfx.Release(petals[i]);petals[i]=null;}if(Mastered)vfx.Spawn(SkillVfxKind.FireBloom,point+Vector3.Cross(Vector3.up,direction)*(i==0?-2:2)+Vector3.up*.8f,Accent,.7f,1.2f);}
            exploded=true;vfx.Release(flower);flower=null;if(ribbon!=null)ribbon.Fade(.35f);vfx.Burst(point+Vector3.up*.5f,Accent,2.6f,vfx.config.fireHit);vfx.Fragments(point+Vector3.up*.4f,Accent,SkillVfxPool.MobileQuality?4:10);
            if(trailSmoke!=null)trailSmoke.Fade(.35f);
            var bloom=vfx.Spawn(SkillVfxKind.FireBloom,point+Vector3.up*.9f,Accent,.6f,2.5f);
            vfx.Spawn(SkillVfxKind.Shockwave,point,Accent,.65f,7);
            DangerZoneRegistry.Remove(danger);danger=DangerZoneRegistry.Register(point,7,4);
            vfx.Spawn(SkillVfxKind.FireField,point,Accent,4.6f,7);vfx.Spawn(SkillVfxKind.Smoke,point,Accent,4.8f,5);
            for(int i=0;i<5;i++)vfx.Spawn(SkillVfxKind.Scorch,point+Quaternion.Euler(0,i*72,0)*Vector3.forward*3.5f,new Color(.22f,.03f,.006f),2.8f,2);
            zone=vfx.Spawn(SkillVfxKind.Scorch,point,Accent,4.65f,7,null,vfx.config.fireCast);
            var shockwave=vfx.Spawn(SkillVfxKind.Ring,point,Color.Lerp(Accent,Color.white,.3f),.7f,7);if(shockwave!=null)shockwave.Progress=0;
            bool hit=false;for(int i=0;i<MonsterVitality.Active.Count;i++)
            {var m=MonsterVitality.Active[i];bool struck=false;for(int p=0;p<FlowerCount;p++){Vector3 center=point+(p==0?Vector3.zero:Vector3.Cross(Vector3.up,direction)*(p==1?-2:2));if(InArea(m,center,7)&&Hit(m,Mastered?1.5f:4.5f))struck=true;}if(struck){hit=true;float burn=(stats!=null?stats.Attack*stats.DamageDealt:20)*.1f*power*CastDamageMultiplier*ElementChart.Multiplier(Element.Hoa,m.Element);m.GetComponent<StatusEffectHost>()?.Apply(StatusType.Burn,3,burn,gameObject);}}
            if(hit)impact.Pulse(1,.07f);
        }
        protected override void Cleanup(){if(explorer!=null)explorer.SkillMoveMultiplier=1;DangerZoneRegistry.Remove(danger);if(flower!=null&&vfx!=null){flower.Fade(.4f);flower.StopLoop();flower=null;}for(int i=0;i<2;i++)if(petals[i]!=null){petals[i].Fade(.4f);petals[i].StopLoop();petals[i]=null;}}
    }
}
