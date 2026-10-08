using UnityEngine;
using CampusRift.Monsters;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public sealed class SpiritSightRuntime:Set2SkillRuntime
    {
        public override Color Accent=>CampusRift.UI.Accessibility.ColorBlind && Definition!=null ? CampusRift.Combat.ElementChart.ColorOf(Definition.element) : new Color(.85f,.9f,1);public bool Active=>casting&&elapsed<10;
        readonly MonsterVitality[] marked=new MonsterVitality[3];SkillSet2VisualBatch.Slot eye;
        UI.EnemyRevealMarker reveal;float nextScan;
        void Start(){reveal=UI.LevelHUD.Attach()?.Reveal;}
        public int MarkedCount {get;private set;}
        public bool IsMarked(MonsterVitality m){if(!Active||!Mastered)return false;for(int i=0;i<3;i++)if(marked[i]==m)return true;return false;}
        public float DamageBonus(MonsterVitality m,bool critical)=>Active?(critical?1.25f:1)*(IsMarked(m)?1.3f:1):1;
        protected override void OnCast()
        {
            nextScan=0;MarkedCount=0;System.Array.Clear(marked,0,3);
            if(reveal==null)reveal=Object.FindAnyObjectByType<UI.LevelHUD>()?.Reveal;reveal?.RevealAll(10);
            eye=vfx.Shapes.Spawn(SkillShape.Eye,transform.position+Vector3.up*2.4f,Accent,10.5f,Vector3.one*.6f,transform.rotation);if(eye!=null){eye.Follow=transform;eye.Offset=Vector3.up*2.4f;eye.Opacity=.65f;}
            vfx.Spawn(SkillVfxKind.Shockwave,transform.position,Accent,1,15,vfx.config.voidCast);GetComponent<SkillCastPose>()?.Play(.45f,-5,45);
        }
        protected override void TickCast(float dt)
        {
            if(eye!=null)eye.Rotation=explorer.followCamera!=null?explorer.followCamera.transform.rotation:transform.rotation;
            if(elapsed>=nextScan){nextScan=elapsed+.4f;int n=Snapshot();for(int i=0;i<n;i++)targets[i].GetComponent<IEnemyConcealment>()?.RevealFor(.6f);if(Mastered){System.Array.Clear(marked,0,3);MarkedCount=0;for(int i=0;i<n;i++)for(int j=0;j<3;j++)if(marked[j]==null||targets[i].Health<marked[j].Health){for(int k=2;k>j;k--)marked[k]=marked[k-1];marked[j]=targets[i];break;}for(int i=0;i<3;i++)if(marked[i]!=null){MarkedCount++;var ring=vfx.Spawn(SkillVfxKind.Ring,marked[i].transform.position, new Color(1,.2f,.3f),.55f,.8f);}}}
            if(elapsed>=10)Finish();
        }
        protected override void Cleanup(){reveal?.EndSkillReveal();eye?.Fade(.5f);eye=null;MarkedCount=0;System.Array.Clear(marked,0,3);}
    }
}
