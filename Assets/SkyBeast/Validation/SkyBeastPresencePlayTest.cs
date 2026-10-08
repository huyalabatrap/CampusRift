#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using CampusRift.SkyBeast;
namespace CampusRift.Validation
{
    public sealed class SkyBeastPresencePlayTest : P12PlayTest
    {
        public bool PrimaryOnly;
        protected override IEnumerator Run(){Begin();
            if(PrimaryOnly){SkyBeastPresence.Begin(10);yield return null;
                var primary=SkyBeastScheduler.Instance.Beasts.FirstOrDefault();
                Check(primary!=null&&primary.definition.id=="023","level 10 correct primary dragon");yield break;
            }
            foreach(int level in new[]{8,9,10}){SkyBeastPresence.Begin(level);yield return null;
                var c=SkyBeastScheduler.Instance.Beasts.FirstOrDefault();string expected=level==8?"023":level==9?"026":"023";
                Check(c!=null&&c.definition.id==expected,"level "+level+" correct primary dragon");
                Check(c.Mouth!=null&&c.Rider!=null&&c.TailTip!=null,"dragon "+expected+" P14 sockets");
                bool periods=true,noRepeat=true;for(int i=0;i<12;i++){int prior=c.LastRoarVariant;c.Roar();float gap=c.NextRoarAt-Time.time;periods&=gap>=20&&gap<=45;noRepeat&=prior!=c.LastRoarVariant;yield return null;}
                Check(periods&&noRepeat&&c.definition.roars.Length==4&&c.definition.roars.All(x=>x!=null),"dragon "+expected+" four audio variants / 20–45s / no adjacent repeat");
                bool clearance=true;for(int i=0;i<360;i++){var point=c.Path(i*c.definition.period/360);int n=Physics.OverlapSphere(point,20,~((1<<7)|(1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore).Length;clearance&=n==0;}
                Check(clearance,"dragon "+expected+" full orbit >20m from solid geometry");yield return new WaitForSeconds(3);Check(c.Speed>1&&c.MinClearance>20,"dragon "+expected+" actual flight clearance / motion");SkyBeastPresence.StopAll();yield return null;
            }
            SkyBeastPresence.Begin(10);FireBreathCycle.BeginLevel(10);FireBreathCycle.Instance.AutoAdvance=false;yield return null; // Same initialization contract as LevelDirector.Begin (P13/P14).
            var scheduler=SkyBeastScheduler.Instance;
            Check(scheduler.Beasts.Count==2 && scheduler.Beasts[0].definition.id=="023" && scheduler.Beasts[1].definition.id=="026","P14/P16 level10 begins with 023 and 026");
            scheduler.CinematicPaused=true;Check(scheduler.ApplySkySwordHit(),"first sky sword hit accepted");yield return null;
            Check(scheduler.Phase==2 && scheduler.Beasts.Count==1 && scheduler.Beasts[0].definition.id=="026","first sword advances to 026 phase2");
            Check(scheduler.ApplySkySwordHit(),"second sky sword hit accepted");yield return null;
            Check(scheduler.Phase==3 && scheduler.Beasts.Count==1 && scheduler.Beasts[0].definition.id=="020","second sword advances to Long Vuong020 phase3");
            Check(!scheduler.SwordAllowed,"Long No gates final sword rather than a timed dragon return");
        }
    }
}
#endif
