#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using CampusRift.Levels;
using CampusRift.Enemies;
using CampusRift.Combat;
using CampusRift.Progression;
namespace CampusRift.Validation
{
    // Real LevelDirector smoke run, with explicit QA damage. Balance bot is a separate measured test.
    public sealed class Level3to7PlayTest : P12PlayTest
    {
        LevelDirector director;
        protected override IEnumerator Run(){Begin();director=LevelDirector.Ensure();director.enabled=true;director.introSeconds=.1f;director.spawnInterval=.15f;director.portalLead=.1f;director.winDelay=.1f;
            for(int level=1;level<=7;level++){
                var d=LevelCatalog.Instance.Get(level);ProfileService.Instance.Cultivation.SetState((Realm)d.requiredRealm,d.requiredTier,0);
                UI.UIStateManager.Instance.EnterScene(true);director.Begin(d);float start=Time.realtimeSinceStartup;bool sawElite=false,sawBoss=false;
                while(director.State!=LevelDirector.Phase.Won&&Time.realtimeSinceStartup-start<100){
                    world.player.GetComponent<Monsters.PlayerMonsterHealth>().Revive(1,0);
                    foreach(var e in director.Alive.ToArray()){
                        if(e.archetype.id=="shaban-elite")sawElite=true;if(e.archetype.isBoss)sawBoss=true;
                        var info=DamageInfo.Create(100000,Element.None,DamageSource.Skill,e.transform.position,Vector3.down,world.player.gameObject);info.skillId="dai-thu-an";e.Vitality.ApplyDamage(info);
                    }
                    yield return null;
                }
                Check(director.State==LevelDirector.Phase.Won&&director.Kills==director.TotalPlanned&&director.WinsRaised==1,"L"+level+" real waves / one victory / counts complete");
                if(level==3)Check(sawElite,"L3 elite appears in wave3");if(level==5||level==7)Check(sawBoss,"L"+level+" boss gate before result");
                Check((director.Result.starMask&1)!=0&&(LevelProgressService.Current.Stars(level)&1)!=0,"L"+level+" star result saved");
                Measure("L"+level+" continuous DEV smoke: "+director.Elapsed.ToString("F2")+"s; "+director.Kills+" kills; QA lethal damage (not balance duration)");
                if(level==7)yield return new WaitForSecondsRealtime(5);director.End();yield return null;
            }
        }
        protected override void Cleanup(){director?.End();base.Cleanup();}
    }
}
#endif
