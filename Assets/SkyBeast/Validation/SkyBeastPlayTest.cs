#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Levels;
namespace CampusRift.SkyBeast
{
    public sealed class SkyBeastPlayTest:MonoBehaviour
    {
        [Serializable]sealed class Check{public string name;public bool pass;public float expected,actual;}
        [Serializable]sealed class Report{public int passed,failed;public List<Check> checks=new List<Check>();public string method="One smoke using production scheduler, damage, pooled strikes and fury; no balance bot or regression group.";}
        Report report=new Report();SkillSet1TestWorld world;FireBreathCycle cycle;SkyBeastScheduler scheduler;PlayerMonsterHealth player;Vector3 outdoor=new Vector3(-5,.13f,2),indoor;
        void CheckResult(bool pass,string name,float expected=0,float actual=0){report.checks.Add(new Check{name=name,pass=pass,expected=expected,actual=actual});if(pass)report.passed++;else report.failed++;}
        IEnumerator Start()
        {
            world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();world.player.enabled=false;yield return null;
            player=world.player.GetComponent<PlayerMonsterHealth>();player.SetProgressionMaxHealth(10000);player.respawnOnDefeat=false;
            foreach(var v in world.victims)v.gameObject.SetActive(false);
            foreach(var n in ShelterGraphReference.Graph.Nodes)if(n.WorldPosition.y<1&&ShelterDetector.AtFeet(n.WorldPosition)==Shelter.Indoor){indoor=n.WorldPosition;break;}
            cycle=FireBreathCycle.Ensure();cycle.AutoAdvance=false;cycle.Ground.enabled=false;
            foreach(var id in new[]{"xich-hoa-giao","chu-tuoc","hoa-long-vuong"})CheckResult(SkyBeastDefinition.LoadCombat(id).Valid,id+" combat data validated");
            foreach(int level in new[]{8,9,10})
            {
                SkyBeastPresence.Begin(level);scheduler=SkyBeastScheduler.Instance;FireBreathCycle.BeginLevel(level);yield return null;
                CheckResult(scheduler.Beasts.Count==(level==10?2:1),"Level "+level+" initial presence",level==10?2:1,scheduler.Beasts.Count);
                CheckResult(scheduler.SwordTarget.TotalSegments==(level==9?2:1),"Level "+level+" segments",level==9?2:1,scheduler.SwordTarget.TotalSegments);
                foreach(var beast in scheduler.Beasts)
                {
                    bool safe=true,range=true,layer=true;
                    for(int i=0;i<24;i++){var p=beast.Path(i*beast.definition.period/24);safe&=p.y>=beast.definition.altitude-33&&p.y<=beast.definition.altitude+6&&Physics.OverlapSphere(p,20,ShelterDetector.EnvironmentMask,QueryTriggerInteraction.Ignore).Length==0;range&=Vector3.Distance(outdoor,p)<350;}
                    foreach(var c in beast.GetComponentsInChildren<Collider>())layer&=!c.enabled;
                    CheckResult(safe&&range,"Level "+level+" "+beast.definition.id+" orbit clearance / altitude / far clip");CheckResult(layer&&beast.gameObject.layer==10,"Non-colliding SkyBeast layer");
                    var hp=beast.GetComponent<SkyBeastVitality>();bool ignored=true;foreach(DamageSource src in Enum.GetValues(typeof(DamageSource))){var hit=DamageInfo.Create(99999,Element.Hoa,src,beast.transform.position,Vector3.down);hit.skillId="regular";ignored&=!hp.ApplyDamage(hit);}
                    CheckResult(ignored,"All non-sword damage sources ignored");
                }
                CheckResult(cycle.Profile.cycleSeconds==(level==8?45:level==9?40:20),"Level "+level+" cycle profile");
                var first=scheduler.BreathSource;cycle.Advance(cycle.Profile.warningSeconds);
                int posing=0;foreach(var b in scheduler.Beasts)if(b.PoseActive)posing++;
                CheckResult(posing==1&&scheduler.BreathSource==first,"Exactly one breathing pose/source level "+level,1,posing);
                CheckResult(!scheduler.ApplySkySwordHit(),"Sword gated while breathing");
                cycle.Advance(4);yield return null;CheckResult(!first.Charging,"Warning ends after breath");
                if(level==10)
                {cycle.Advance(10);var second=scheduler.BreathSource;CheckResult(first!=second,"Level10 sources alternate every20s");cycle.Advance(10);cycle.Advance(10);CheckResult(scheduler.BreathSource==first,"Each source repeats every40s");}
                SkyBeastPresence.StopAll();yield return null;
            }
            SkyBeastPresence.Begin(9);scheduler=SkyBeastScheduler.Instance;FireBreathCycle.BeginLevel(9);cycle.Advance(10);cycle.Ground.Clear();world.PlacePlayer(outdoor);
            var feather=FindAnyObjectByType<FeatherBarrage>();feather.enabled=false;var pool=scheduler.GetComponent<SkyStrikePool>();pool.enabled=false;
            CheckResult(feather.TryDrop()&&feather.LastCount==3,"Phase1 feathers exactly3 Outdoor");
            CheckResult(pool.CreatedCount==6,"Fixed six-slot attack pool");float hpBefore=player.CurrentHealth;pool.Advance(1.19f);CheckResult(pool.Impacts==0&&player.CurrentHealth==hpBefore,"Feather telegraph lasts1.2s");pool.Advance(.02f);
            CheckResult(pool.Impacts==3&&Mathf.Abs(hpBefore-player.CurrentHealth-78)<.5f,"Feather impact12% of650",78,hpBefore-player.CurrentHealth);pool.Clear();cycle.Ground.Clear();
            world.PlacePlayer(indoor);CheckResult(!feather.TryDrop(),"No feathers when player indoors");world.PlacePlayer(outdoor);
            CheckResult(scheduler.ApplySkySwordHit()&&scheduler.Phase==2&&cycle.Profile.cycleSeconds==32,"Level9 first sword -> phase2 /32s");
            CheckResult(feather.TryDrop()&&feather.LastCount==5,"Phase2 feathers exactly5");pool.Clear();cycle.Ground.Clear();CheckResult(scheduler.ApplySkySwordHit()&&scheduler.Completed,"Level9 second sword completes beast");SkyBeastPresence.StopAll();yield return null;
            SkyBeastPresence.Begin(10);scheduler=SkyBeastScheduler.Instance;FireBreathCycle.BeginLevel(10);
            CheckResult(scheduler.ApplySkySwordHit()&&scheduler.Phase==2&&scheduler.Beasts.Count==1&&cycle.Profile.cycleSeconds==28,"Level10 sword1 removes Giao / phase2 28s");
            CheckResult(scheduler.ApplySkySwordHit()&&scheduler.Phase==3&&scheduler.Beasts[0].definition.id=="020"&&cycle.Profile.cycleSeconds==25&&cycle.Profile.warningSeconds==4,"Level10 sword2 -> Long Vuong /25s warning4s");yield return null;
            var meteor=FindAnyObjectByType<MeteorShower>();meteor.enabled=false;pool=scheduler.GetComponent<SkyStrikePool>();pool.enabled=false;cycle.Ground.Clear();world.PlacePlayer(outdoor);
            CheckResult(meteor.TryDrop()&&meteor.LastCount==6,"Meteor shower six points");hpBefore=player.CurrentHealth;pool.Advance(1.49f);CheckResult(player.CurrentHealth==hpBefore,"Meteor warning1.5s prevents early damage");pool.Advance(.02f);CheckResult(Mathf.Abs(hpBefore-player.CurrentHealth-123)<.5f,"Meteor impact15% of820 /3m",123,hpBefore-player.CurrentHealth);pool.Clear();
            CheckResult(SkyStrikePool.ResolveSurface(indoor,SkyStrikeKind.Meteor,out var roof)&&roof.y>indoor.y+2,"Meteor catches roof above real indoor room");CheckResult(!SkyStrikePool.CanHitPlayer(indoor,roof,3),"Roof strike cannot damage indoor player");
            CheckResult(!scheduler.ApplySkySwordHit(),"Final sword blocked before fury");LevelEvents.RaiseWaveCleared(3,3);
            CheckResult(scheduler.Fury.Warning&&scheduler.Fury.Remaining==5,"Wave3 clear starts5s Fury warning");scheduler.Fury.enabled=false;scheduler.Fury.Advance(4.99f);CheckResult(!cycle.IsFury,"No Fury damage before full warning");scheduler.Fury.Advance(.01f);CheckResult(cycle.IsFury&&!scheduler.SwordAllowed,"Fury active and sword locked");
            world.PlacePlayer(indoor);hpBefore=player.CurrentHealth;cycle.Advance(12);CheckResult(Mathf.Abs(hpBefore-player.CurrentHealth-98.4f)<1,"Fury Indoor1%/s for12s",98.4f,hpBefore-player.CurrentHealth);
            CheckResult(scheduler.Fury.Completed&&scheduler.SwordAllowed&&!cycle.StartFury(),"Fury completes / final sword unlocks / once per run");CheckResult(scheduler.ApplySkySwordHit()&&scheduler.Completed,"Final sword completes scheduler");
            // Dedicated API also accepts the P15 DamageInfo contract, precisely one segment.
            var standalone=new GameObject("Sword API fixture").AddComponent<SkyBeastVitality>();standalone.Initialize(2);var sword=DamageInfo.Create(1,Element.Kim,DamageSource.Skill,Vector3.zero,Vector3.down);sword.skillId="thien-kiem";
            CheckResult(standalone.ApplyDamage(sword)&&standalone.RemainingSegments==1,"P15 DamageInfo thien-kiem contract");Destroy(standalone.gameObject);
            SkyBeastPresence.StopAll();cycle.StopCycle();world.player.enabled=true;world.End();
            Directory.CreateDirectory("Artifacts/SkyBeast");File.WriteAllText("Artifacts/SkyBeast/SkyBeast.json",JsonUtility.ToJson(report,true));File.WriteAllText("Artifacts/SkyBeast/SkyBeast-DONE.txt",report.passed+" PASS / "+report.failed+" FAIL");Debug.Log("[P14] "+report.passed+" PASS / "+report.failed+" FAIL");Destroy(gameObject);
        }
    }
}
#endif
