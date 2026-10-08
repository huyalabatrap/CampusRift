using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Enemies;
namespace CampusRift.Skills
{
    public sealed class ChainLightningRuntime:Set1SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(1,.82f,.12f);
        protected override float Range=>25*WorldScale;
        readonly MonsterVitality[] visited=new MonsterVitality[9];
        MonsterVitality next;Vector3 from;int bounce;float nextAt;
        public int BounceCount=>bounce;
        public readonly float[] BounceDamage=new float[9];
        protected override void OnCast()
        {
            bounce=0;nextAt=.2f;from=transform.position+Vector3.up*(1.5f*WorldScale);System.Array.Clear(visited,0,visited.Length);System.Array.Clear(BounceDamage,0,BounceDamage.Length);
            next=targetLock!=null&&targetLock.IsValid(targetLock.Current)?targetLock.Current:FirstTarget();
            var sword=vfx.Spawn(SkillVfxKind.Sword,from,Accent,.9f,.85f,vfx.config.lightningCast);if(sword!=null){sword.Follow=transform;sword.Offset=(Vector3.up*1.7f+transform.right*.5f)*WorldScale;}
            GetComponent<SkillCastPose>()?.Play(.4f,-5,50);
        }
        MonsterVitality FirstTarget()
        {
            MonsterVitality best=null;float score=float.PositiveInfinity;
            Camera camera=explorer!=null&&explorer.followCamera!=null?explorer.followCamera:Camera.main;
            for(int i=0;i<MonsterVitality.Active.Count;i++)
            {
                var m=MonsterVitality.Active[i];if(m==null||m.Defeated||!m.isActiveAndEnabled||(m.transform.position-transform.position).sqrMagnitude>Range*Range||!CombatLine.Clear(from,m.transform.position+Vector3.up*WorldScale,transform))continue;
                Vector3 viewport=camera!=null?camera.WorldToViewportPoint(m.transform.position+Vector3.up*WorldScale):Vector3.one*.5f;
                if(ARContext==null&&viewport.z<=0)continue;float s=(new Vector2(viewport.x,viewport.y)-new Vector2(.5f,.5f)).sqrMagnitude;
                if(ARContext!=null)s=(m.transform.position-point).sqrMagnitude;
                var e=m.GetComponent<EnemyInstance>();if(ARContext==null&&e!=null&&e.archetype!=null&&e.archetype.isFlying)s-=10;
                if(s<score){score=s;best=m;}
            }
            return best;
        }
        protected override void TickCast(float dt)
        {
            if(arUltimate){if(elapsed>=.2f&&bounce==0){bounce=1;foreach(var e in ARContext.battlefield.GetComponent<AR.ARMonsterDirector>().Actors){if(e==null||!ARTarget(e.Vitality)||!e.Status.Has(StatusType.Freeze))continue;Vector3 chest=e.transform.position+Vector3.up*WorldScale;var bolt=vfx.Spawn(SkillVfxKind.ChainBolt,chest+Vector3.up*3*WorldScale,Accent,.4f,2,vfx.config.lightningHit);if(bolt!=null)bolt.End=chest;Hit(e.Vitality,2);}}if(elapsed>=.8f)Finish();return;}
            while(elapsed>=nextAt&&bounce<(Mastered?9:6)&&next!=null)
            {
                var victim=next;visited[bounce]=victim;Vector3 chest=victim.transform.position+Vector3.up*(1.6f*WorldScale);
                float percent=2*Mathf.Pow(.9f,bounce);float before=LastDamage;
                if(Hit(victim,percent))
                {
                    BounceDamage[bounce]=LastDamage-before;
                    var bolt=vfx.Spawn(SkillVfxKind.ChainBolt,from,Accent,.3f,Mathf.Pow(.9f,bounce),vfx.config.lightningHit);if(bolt!=null)bolt.End=chest;
                    var renderer=victim.GetComponentInChildren<Renderer>();vfx.Burst(chest,Accent,renderer!=null?Mathf.Max(.8f,renderer.bounds.size.y*.75f/WorldScale):1.3f,null,false);
                    for(int fork=0;fork<2;fork++){Vector3 mid=Vector3.Lerp(from,chest,.35f+fork*.3f);var branch=vfx.Spawn(SkillVfxKind.Bolt,mid,new Color(.65f,.04f,1),.16f,.55f);if(branch!=null)branch.End=mid+(Vector3.up*(fork==0?.85f:-.65f)+(fork==0?Vector3.left:Vector3.right)*.7f)*WorldScale;}
                    for(int branch=0;branch<2;branch++){var spark=vfx.Spawn(SkillVfxKind.Bolt,chest,new Color(.55f,.08f,1),.2f,.5f);if(spark!=null)spark.End=chest+(Vector3.up*.6f+(branch==0?Vector3.left:Vector3.right)*.65f)*WorldScale;}
                    vfx.Spawn(SkillVfxKind.Scorch,victim.transform.position,new Color(.16f,.025f,.22f),2.3f,.55f);
                    if(bounce==0)impact.Pulse(.45f,.065f);
                }
                bounce++;from=chest;next=NextTarget(victim);nextAt+=.08f;
            }
            if(elapsed>=1.35f)Finish();
        }
        MonsterVitality NextTarget(MonsterVitality previous)
        {
            MonsterVitality best=null;float nearest=64*WorldScale*WorldScale;
            for(int i=0;i<MonsterVitality.Active.Count;i++)
            {
                var m=MonsterVitality.Active[i];if(m==null||m.Defeated||!m.isActiveAndEnabled)continue;bool seen=false;for(int j=0;j<bounce;j++)if(visited[j]==m){seen=true;break;}if(seen)continue;
                float d=(m.transform.position-previous.transform.position).sqrMagnitude;if(d>nearest||!CombatLine.Clear(from,m.transform.position+Vector3.up*WorldScale,transform))continue;nearest=d;best=m;
            }
            return best;
        }
    }
}
