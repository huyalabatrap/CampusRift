using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public sealed class KunpengSpeedRuntime:Set2SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(.12f,1,.5f);public bool Active=>casting&&elapsed<6;
        SkillSet2VisualBatch.Slot wings;SkillVfxPool.Node ring;CharacterAfterimageTrail trail;
        readonly Vector3[] ghosts=new Vector3[12],ghostStart=new Vector3[12];readonly float[] ghostUntil=new float[12];readonly CampusRift.Monsters.MonsterVitality[,] touched=new CampusRift.Monsters.MonsterVitality[12,64];readonly int[] touchCount=new int[12];int nextGhost;float nextTrail;Vector3 lastGhost;
        protected override void Awake(){base.Awake();trail=GetComponent<CharacterAfterimageTrail>();}
        protected override void OnCast()
        {
            System.Array.Clear(ghostUntil,0,ghostUntil.Length);nextGhost=0;nextTrail=0;lastGhost=transform.position;
            stats.SetModifier(StatSource.Buff,Id,StatType.MoveSpeed,0,.4f*power);
            wings=vfx.Shapes.Spawn(SkillShape.Wings,transform.position,Accent,6.5f,Vector3.one,transform.rotation);if(wings!=null)wings.Follow=transform;
            ring=vfx.Spawn(SkillVfxKind.Ring,transform.position,Accent,6.5f,.9f,vfx.config.iceCast,vfx.config.voidCast);if(ring!=null)ring.Follow=transform;
            GetComponent<SkillCastPose>()?.Play(.35f,-7,50);trail?.TriggerDash(5);
        }
        protected override void TickCast(float dt)
        {
            trail?.TriggerDash(.2f);if(wings!=null)wings.Rotation=transform.rotation;
            if(elapsed>=nextTrail&&(transform.position-lastGhost).sqrMagnitude>.04f){nextTrail=elapsed+.22f;ghostStart[nextGhost]=lastGhost;lastGhost=transform.position;ghosts[nextGhost]=lastGhost;ghostUntil[nextGhost]=Time.time+.65f;touchCount[nextGhost]=0;nextGhost=(nextGhost+1)%12;if(Mastered){var g=vfx.Spawn(SkillVfxKind.Ghost,lastGhost,Accent,.65f);if(g!=null)g.Opacity=.45f;}}
            int n=Snapshot();for(int g=0;g<12;g++)if(ghostUntil[g]>Time.time)for(int i=0;i<n;i++)
            {var m=targets[i];Vector3 segment=ghosts[g]-ghostStart[g];float t=segment.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(m.transform.position-ghostStart[g],segment)/segment.sqrMagnitude):0;if(!InArea(m,ghostStart[g]+segment*t,.85f))continue;bool seen=false;for(int j=0;j<touchCount[g];j++)if(touched[g,j]==m){seen=true;break;}if(seen||touchCount[g]>=64)continue;touched[g,touchCount[g]++]=m;m.GetComponent<ReactionResolver>()?.SpreadBurn(m,gameObject);if(Mastered)Hit(m,.6f);}
            if(elapsed>=6)Finish();
        }
        protected override void Cleanup(){stats?.RemoveSource(StatSource.Buff,Id);wings?.Fade(.5f);wings=null;ring?.StopLoop();ring?.Fade(.5f);ring=null;}
    }
}
