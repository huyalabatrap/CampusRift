using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    public sealed class IceSealRuntime:Set1SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(.15f,.85f,1);
        protected override float Range=>10*WorldScale;
        protected override float AimRadius=>10;
        protected override bool Cone=>true;
        readonly MonsterVitality[] frozen=new MonsterVitality[64];
        readonly SkillVfxPool.Node[] shells=new SkillVfxPool.Node[64];
        int count,row,danger;bool struck,shattered;
        // A consumed Freeze shatters this existing shell on the next update. Reactions can reuse
        // that debris instead of emitting a second identical shard burst on the same target.
        public bool HasShell(MonsterVitality target)
        {for(int i=0;i<count;i++)if(frozen[i]==target&&shells[i]!=null&&shells[i].Live&&!target.resistHardControl)return true;return false;}
        public bool ShatterForReaction(MonsterVitality target)
        {for(int i=0;i<count;i++)if(frozen[i]==target&&shells[i]!=null){Shatter(i,true);return true;}return false;}
        protected override void OnCast()
        {
            count=row=0;struck=shattered=false;
            if(arUltimate){point=ARContext.battlefield.Root.position;vfx.Spawn(SkillVfxKind.Ring,point,Accent,2,ARContext.battlefield.placement.Radius/WorldScale,vfx.config.iceCast);foreach(var e in ARContext.battlefield.GetComponent<AR.ARMonsterDirector>().Actors){if(e==null||!ARTarget(e.Vitality))continue;var m=e.Vitality;e.Status?.Apply(StatusType.Freeze,2.5f,0,gameObject);if(count<shells.Length){frozen[count]=m;shells[count]=vfx.Spawn(SkillVfxKind.IceShell,m.transform.position,Accent,2.8f,1.1f);if(shells[count]!=null)shells[count].Follow=m.transform;count++;}}return;}danger=DangerZoneRegistry.Register(transform.position,10*WorldScale,.8f,DangerShape.Cone,direction,90);
            var cone=vfx.Spawn(SkillVfxKind.Cone,transform.position,Accent,.8f,10,vfx.config.iceCast);if(cone!=null)cone.End=transform.position+direction;
            GetComponent<SkillCastPose>()?.Play(.6f,-12,45);
            vfx.Spawn(SkillVfxKind.FrostMist,transform.position+direction*(5*WorldScale),Accent,2.9f,4);
        }
        protected override void TickCast(float dt)
        {
            if(arUltimate){for(int i=0;i<count;i++)if(frozen[i]!=null&&!frozen[i].GetComponent<StatusEffectHost>().Has(StatusType.Freeze))Shatter(i,true);if(elapsed>=2.8f)Finish();return;}
            while(row<5&&elapsed>=.18f+row*.07f)
            {
                float range=(2+row*1.8f)*WorldScale;int n=SkillVfxPool.MobileQuality?3:5;
                for(int p=0;p<n;p++)
                {Vector3 offset=Quaternion.AngleAxis(-38+76*p/(n-1f),Vector3.up)*direction*range;var spike=vfx.Spawn(SkillVfxKind.Ice,transform.position+offset,Accent,2.9f,Random.Range(.75f,1.3f));if(spike!=null)vfx.Emit(spike,SkillVfxPool.MobileQuality?2:5);vfx.Fragments(transform.position+offset+Vector3.up*(.6f*WorldScale),Accent,1,.16f);}
                row++;
            }
            if(!struck&&elapsed>=.18f)
            {
                struck=true;
                for(int i=0;i<MonsterVitality.Active.Count;i++)
                {
                    var m=MonsterVitality.Active[i];if(!InArea(m,transform.position,10)||Vector3.Angle(direction,Vector3.ProjectOnPlane(m.transform.position-transform.position,Vector3.up))>45)continue;
                    if(!Hit(m,1.2f))continue;var status=m.GetComponent<StatusEffectHost>();if(status!=null)status.Apply(StatusType.Freeze,2.5f,0,gameObject);
                    if(count<shells.Length)
                    {
                        frozen[count]=m;
                        var shell=vfx.Spawn(m.resistHardControl?SkillVfxKind.Scorch:SkillVfxKind.IceShell,m.transform.position,Accent,3.05f,m.resistHardControl?.7f:.6f);
                        if(shell!=null){shell.Follow=m.transform;shells[count]=shell;}count++;
                    }
                    var renderer=m.GetComponentInChildren<Renderer>();float flashSize=renderer!=null?Mathf.Max(.9f,renderer.bounds.size.y*.9f/WorldScale):1.5f;
                    vfx.Burst(m.transform.position+Vector3.up*WorldScale,Accent,flashSize,vfx.config.iceHit,false);
                    vfx.Spawn(SkillVfxKind.Scorch,m.transform.position,new Color(.02f,.1f,.24f),2.8f,.8f);
                }
                if(LastHitCount>0)impact.Pulse(.55f,.055f);
            }
            for(int i=0;i<count;i++)if(shells[i]!=null&&frozen[i]!=null)
            {
                var status=frozen[i].GetComponent<StatusEffectHost>();
                if(status!=null&&!status.Has(StatusType.Freeze)&&!status.Has(StatusType.Chill))Shatter(i);
            }
            if(elapsed>=2.72f&&!shattered){shattered=true;for(int i=0;i<count;i++)Shatter(i);}
            if(elapsed>=3.35f)Finish();
        }
        void Shatter(int i,bool reaction=false)
        {if(shells[i]==null)return;var shell=shells[i];if(Mastered)GetComponent<SkillMasteryFields>()?.ChillZone(shell.Position);shell.Fade(reaction?.08f:.4f);if(reaction)shell.Opacity=.12f;vfx.Fragments(shell.Position+Vector3.up*WorldScale,Accent,SkillVfxPool.MobileQuality?3:6,reaction?.45f:.35f,SkillVfxKind.Shard,reaction);shells[i]=null;frozen[i]=null;}
        protected override void Cleanup(){DangerZoneRegistry.Remove(danger);if(vfx!=null)for(int i=0;i<count;i++)Shatter(i);}
    }
}
