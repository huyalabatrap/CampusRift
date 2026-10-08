#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Skills;
using CampusRift.Progression;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    public sealed class FireBreathPlayTest:MonoBehaviour
    {
        [Serializable]sealed class Check{public string name;public bool pass;public float expected,actual;}
        [Serializable]sealed class Report{public int passed,failed;public List<Check> checks=new List<Check>();public string method="One smoke per table cell, advancing production state/ticks; one real-time DEV cycle; no balance bot/benchmark.";}
        Report report=new Report();SkillSet1TestWorld world;FireBreathCycle cycle;PlayerMonsterHealth health;
        Vector3 outdoor=new Vector3(-5,.13f,2),indoor,partial;
        void CheckResult(bool pass,string name,float expected=0,float actual=0){report.checks.Add(new Check{name=name,pass=pass,expected=expected,actual=actual});if(pass)report.passed++;else report.failed++;}
        void Refill(){health.Revive(1,0);}
        IEnumerator OneRealBreath()
        {
            float deadline=Time.realtimeSinceStartup+25;
            while(cycle.State!=FireBreathCycle.Phase.Afterfire&&Time.realtimeSinceStartup<deadline)yield return null;
            cycle.StopCycle();
        }
        float CycleDamage(int level,Vector3 position)
        {
            world.PlacePlayer(position);Refill();float before=health.CurrentHealth;
            cycle.StartCycle(FireBreathProfile.Load(level));cycle.Advance(cycle.Profile.warningSeconds);cycle.Advance(4);
            return before-health.CurrentHealth;
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("Artifacts/SkyBeast");world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();yield return new WaitForSecondsRealtime(.8f);
            world.player.enabled=false;health=world.player.GetComponent<PlayerMonsterHealth>();health.respawnOnDefeat=false;
            cycle=FireBreathCycle.Ensure();cycle.AutoAdvance=false;cycle.Ground.enabled=false;cycle.GetComponent<FireBreathVisuals>().enabled=false;
            // Find real roof/threshold positions in the campus, with no synthetic roof colliders.
            var graph=ShelterGraphReference.Graph;bool gotIndoor=false,gotPartial=false;
            foreach(var n in graph.Nodes)
            {
                if(n.WorldPosition.y>1)continue;var s=ShelterDetector.AtFeet(n.WorldPosition);
                if(!gotIndoor&&s==Shelter.Indoor){indoor=n.WorldPosition;gotIndoor=true;}
                if(!gotPartial&&s==Shelter.Partial){partial=n.WorldPosition;gotPartial=true;}
            }
            if(!gotPartial)
                for(float x=25;x<55&&!gotPartial;x+=.6f)for(float z=15;z<40&&!gotPartial;z+=.6f)
                {var p=new Vector3(x,.13f,z);if(ShelterDetector.AtFeet(p)==Shelter.Partial&&NavMesh.SamplePosition(p,out var h,.2f,NavMesh.AllAreas)){partial=h.position;gotPartial=true;}}
            CheckResult(ShelterDetector.AtFeet(outdoor)==Shelter.Outdoor,"Real courtyard Outdoor");CheckResult(gotIndoor,"Real room Indoor");CheckResult(gotPartial,"Real threshold Partial");
            if(!gotIndoor||!gotPartial){Finish();yield break;}
            foreach(int level in new[]{8,9,10})foreach(var s in new[]{Shelter.Outdoor,Shelter.Partial,Shelter.Indoor})
            {
                float expected=FireBreathProfile.Load(level).Total(s),actual=CycleDamage(level,s==Shelter.Outdoor?outdoor:s==Shelter.Partial?partial:indoor);
                CheckResult(Mathf.Abs(actual-expected)<=1,"Table level "+level+" "+s,expected,actual);CheckResult(cycle.TickCount==8,"Eight 0.5s production ticks level "+level+" "+s,8,cycle.TickCount);
            }
            world.PlacePlayer(outdoor);Refill();health.GrantInvulnerability(30);float start=health.CurrentHealth;cycle.StartCycle(FireBreathProfile.Load(8));cycle.Advance(10);
            CheckResult(Mathf.Abs(start-health.CurrentHealth-280)<1,"Fire ignores existing invulnerability",280,start-health.CurrentHealth);
            var stats=world.player.GetComponent<PlayerStats>();var buffs=world.player.GetComponent<BuffSystem>();
            var items=Resources.LoadAll<ItemDefinition>("");ItemDefinition bead=null,ice=null;
            // ItemCatalog owns authored assets via Resources reference.
            var catalog=ItemCatalog.Instance;
            bead=catalog.Item("ti-hoa-chau");ice=catalog.Item("bang-tam-phu");
            CheckResult(bead!=null&&ice!=null,"Authored fire/ice items available");
            if(bead!=null){buffs.Apply(bead);float actual=CycleDamage(8,outdoor);CheckResult(Mathf.Abs(actual-140)<1,"Tị Hỏa Châu halves fire",140,actual);buffs.ClearAll();}
            var bell=(GoldenBellRuntime)world.player.GetComponent<SkillLoadout>().Find("kim-chung-trao");
            UIStateManager.Instance.EnterScene(true);bell.enabled=true;bell.ResetCooldownForValidation();world.player.GetComponent<SkillLoadout>().Equip(3,"kim-chung-trao");
            world.player.GetComponent<SpiritPower>().Refill();world.PlacePlayer(outdoor);bool cast=bell.CastAt(outdoor);CheckResult(cast&&bell.ShieldActive,"Actual Golden Bell cast");
            float bellDamage=CycleDamage(8,outdoor);CheckResult(Mathf.Abs(bellDamage-112)<1,"Golden Bell 60% fire resistance",112,bellDamage);
            if(bead!=null)buffs.Apply(bead);
            float capDamage=CycleDamage(8,outdoor);CheckResult(Mathf.Abs(capDamage-56)<1,"Combined resistance cap 80%",56,capDamage);
            buffs.ClearAll();bell.enabled=false;bell.enabled=true;
            cycle.StopCycle();world.PlacePlayer(outdoor);Refill();cycle.StartCycle(FireBreathProfile.Load(8));cycle.Ground.Clear();bool patch=cycle.Ground.SpawnAt(outdoor,cycle.Profile);start=health.CurrentHealth;cycle.Ground.ApplyTick(1);
            CheckResult(patch&&Mathf.Abs(start-health.CurrentHealth-20)<1,"Outdoor afterfire 4% recommended HP per second",20,start-health.CurrentHealth);
            CheckResult(!cycle.Ground.SpawnAt(indoor,cycle.Profile),"Afterfire rejects indoor NavMesh position");
            if(ice!=null){buffs.Apply(ice);start=health.CurrentHealth;cycle.Ground.ApplyTick(1);CheckResult(Mathf.Abs(start-health.CurrentHealth)<.01f,"Băng Tâm Phù ignores afterfire");float actual=CycleDamage(8,outdoor);CheckResult(Mathf.Abs(actual-210)<1,"Băng Tâm Phù 25% fire resistance",210,actual);buffs.ClearAll();}
            cycle.Ground.Clear();Refill();cycle.StartCycle(FireBreathProfile.Load(8));cycle.Advance(10);CheckResult(cycle.Ground.ActiveCount>=8&&cycle.Ground.ActiveCount<=12,"Pool creates 8-12 afterfire patches",10,cycle.Ground.ActiveCount);
            bool allOutside=true;foreach(var p in cycle.Ground.Positions)allOutside&=ShelterDetector.AtFeet(p)==Shelter.Outdoor;CheckResult(allOutside&&cycle.Ground.CreatedCount==12,"All pooled afterfire is outdoor / pool fixed at 12");cycle.Ground.Clear();
            var normal=world.victims[0];var fireEnemy=world.victims[1];foreach(var victim in world.victims){victim.GetComponent<Enemies.MinionMotor>().Place(outdoor+Vector3.right*3);victim.SetMaxHealth(10000,true);}
            Refill();fireEnemy.Element=Element.Hoa;normal.Element=Element.None;cycle.StartCycle(FireBreathProfile.Load(8));start=normal.Health;float fireStart=fireEnemy.Health;cycle.Advance(10);
            CheckResult(Mathf.Abs(start-normal.Health-140)<1,"Non-fire outdoor enemy takes 50%",140,start-normal.Health);CheckResult(fireEnemy.Health==fireStart&&fireEnemy.GetComponent<FireEnemyState>().SpeedMultiplier==1.2f,"Fire enemy immune and +20% speed after breath");
            // Actual death event keeps the normal intent flag/weight; no special kill path.
            Refill();bool died=false;normal.SetMaxHealth(10,true);normal.DefeatedOnce+=()=>died=true;var instance=normal.GetComponent<Enemies.EnemyInstance>();instance.countsForSwordIntent=true;cycle.StartCycle(FireBreathProfile.Load(8));cycle.Advance(6.5f);CheckResult(died&&instance.countsForSwordIntent&&instance.LastDamage.source==DamageSource.Environment,"Fire death preserves intent eligibility and normal death feed");
            cycle.StartCycle(FireBreathProfile.Load(10));world.PlacePlayer(partial);Refill();start=health.CurrentHealth;bool first=cycle.StartFury();cycle.Advance(12);CheckResult(first&&Mathf.Abs(start-health.CurrentHealth-393.6f)<1,"Long Nộ 12s / Partial 4% per second",393.6f,start-health.CurrentHealth);CheckResult(!cycle.StartFury(),"Long Nộ only once per run");
            var e=fireEnemy.GetComponent<Enemies.EnemyInstance>();e.scaling.level=6;fireEnemy.Element=Element.None;e.GetComponent<FireEnemyState>().ResetLife();e.GetComponent<Enemies.MinionMotor>().Place(indoor);e.GetComponent<ShelterDetector>().Sample();float baseSpeed=e.archetype.baseSpeed*e.scaling.speed;CheckResult(Mathf.Abs(e.Speed-baseSpeed)<.01f,"Blood Moon indoor has no boost");e.GetComponent<Enemies.MinionMotor>().Place(outdoor);e.GetComponent<ShelterDetector>().Sample();CheckResult(Mathf.Abs(e.Speed-baseSpeed*1.15f)<.01f,"Blood Moon outdoor +15%");
            cycle.StopCycle();cycle.Ground.enabled=true;cycle.GetComponent<FireBreathVisuals>().enabled=true;cycle.AutoAdvance=true;
            foreach(var v in world.victims)v.gameObject.SetActive(false);world.PlacePlayer(outdoor);health.SetProgressionMaxHealth(500);health.Revive(1,0);start=health.CurrentHealth;cycle.StartDev(8);
            yield return OneRealBreath();CheckResult(cycle.TickCount==8&&Mathf.Abs(start-health.CurrentHealth-280)<1,"DEV level 1 real-time outdoor loses 56% of recommended 500 HP",280,start-health.CurrentHealth);
            world.PlacePlayer(indoor);Refill();start=health.CurrentHealth;cycle.StartDev(8);yield return OneRealBreath();CheckResult(cycle.TickCount==8&&!health.IsDead&&Mathf.Abs(start-health.CurrentHealth-34)<1,"DEV real-time indoor survives / loses 34",34,start-health.CurrentHealth);
            Finish();
        }
        void Finish()
        {
            cycle?.StopCycle();world.player.enabled=true;world.End();File.WriteAllText("Artifacts/SkyBeast/FireBreath.json",JsonUtility.ToJson(report,true));File.WriteAllText("Artifacts/SkyBeast/FireBreath-DONE.txt",report.passed+" PASS / "+report.failed+" FAIL");Destroy(gameObject);
        }
    }
}
#endif
