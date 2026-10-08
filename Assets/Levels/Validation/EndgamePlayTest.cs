#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using CampusRift.Progression;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.UI;
using CampusRift.Learning;
namespace CampusRift.Levels
{
    public sealed class EndgamePlayTest:MonoBehaviour
    {
        [Serializable]public sealed class Check {public string name;public bool pass;}
        [Serializable]public sealed class Evidence {public int passed,failed;public string method="One compact Editor smoke. Transient profile; accelerated waves and DEV lethal hits, real prefab/AI/death feeds. Not a balance or device test.";public List<Check> checks=new List<Check>();}
        public Evidence report=new Evidence();public bool Done {get;private set;}
        const string Root="task/p22/";
        void CheckThat(bool ok,string name){report.checks.Add(new Check{name=name,pass=ok});if(ok)report.passed++;else{report.failed++;Debug.LogError("P22 smoke: "+name);}Save();}
        void Save(){Directory.CreateDirectory(Root);File.WriteAllText(Root+"EndgamePlayTest.json",JsonUtility.ToJson(report,true));}
        public static ProfileData CompleteProfile()
        {
            var p=new ProfileData();p.cultivation.realm=6;p.cultivation.tier=5;
            for(int i=1;i<=10;i++){var l=p.Level(i,true);l.cleared=true;l.stars=7;l.bestTime=100+i;}
            p.tutorial.skipHub=p.tutorial.skipCombat=p.tutorial.skipFire=true;return p;
        }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);yield return null;yield return null;
            var owner=ProfileService.Ensure();owner.UseTransient(new ProfileData());Pure(owner);
            owner.UseTransient(CompleteProfile());EndgameTracker.Ensure();
            var d=LevelDirector.Ensure();d.introSeconds=.02f;d.spawnInterval=.005f;d.portalLead=0;d.winDelay=.05f;
            var hp=FindAnyObjectByType<PlayerMonsterHealth>();var qa=hp.gameObject.GetComponent<V2QaPlayer>()??hp.gameObject.AddComponent<V2QaPlayer>();qa.God=true;
            UIStateManager.Instance.EnterScene(true);
            for(int floor=1;floor<=5;floor++)
            {
                var level=EndgameFactory.Tower(floor,DateTime.UtcNow);LevelSession.Select(level);d.Begin(level);
                bool eliteSeen=false;float deadline=Time.realtimeSinceStartup+20;
                while(d.State!=LevelDirector.Phase.Won&&Time.realtimeSinceStartup<deadline)
                {
                    foreach(var e in d.Alive)if(e.Elite.IsElite){eliteSeen=true;CheckAffix(e,floor);}
                    V2DevTools.KillCurrentWave();yield return null;
                }
                CheckThat(d.State==LevelDirector.Phase.Won&&d.Kills==d.TotalPlanned,"Tower floor "+floor+" real spawn/death/clear");
                if(floor==5)CheckThat(eliteSeen,"Floor five real elite spawned");
                CheckThat(owner.Data.endgame.highestFloor==floor,"Floor "+floor+" highest floor persisted");
                d.End();UIStateManager.Instance.EnterScene(true);
            }
            var normal=JsonUtility.ToJson(owner.Data.levels[0]);var nm=EndgameFactory.Nightmare(1);LevelSession.Select(nm);d.Begin(nm);
            float until=Time.realtimeSinceStartup+20;bool scaled=false;
            while(d.State!=LevelDirector.Phase.Won&&Time.realtimeSinceStartup<until)
            {foreach(var enemy in d.Alive)scaled|=Mathf.Approximately(enemy.scaling.health,nm.healthMultiplier)&&Mathf.Approximately(enemy.scaling.damage,nm.damageMultiplier);V2DevTools.KillCurrentWave();yield return null;}
            CheckThat(d.State==LevelDirector.Phase.Won&&scaled,"Nightmare one runtime scaling/spawn/death/clear");
            CheckThat(JsonUtility.ToJson(owner.Data.levels[0])==normal,"Nightmare does not overwrite normal stars/time/attempts");
            CheckThat(owner.Data.endgame.Nightmare(1)?.cleared??false,"Nightmare records its own badge and best time");
            CheckThat(owner.Data.endgame.achievements.Contains("night1"),"Gameplay clear unlocks achievement");
            CheckThat(PlayerCostume.Equip(owner,"dream")&&hp.GetComponent<PlayerCostume>().Applied=="dream","Equip unlocked costume on gameplay rig");
            CheckThat(!PlayerCostume.Equip(owner,"cloud"),"Locked costume cannot be equipped");
            d.End();Done=true;Save();File.WriteAllText(Root+"SMOKE-DONE.txt",report.passed+" PASS / "+report.failed+" FAIL");
        }
        bool affixChecked;
        void CheckAffix(EnemyInstance e,int floor){if(affixChecked)return;affixChecked=true;CheckThat(e.Elite.Affixes.Count==2&&e.Elite.Affixes[0]!=e.Elite.Affixes[1],"Tower elite has two distinct runtime affixes");}
        void Pure(ProfileService owner)
        {
            CheckThat(!EndgameService.TowerOpen(owner.Data)&&!EndgameService.NightmareOpen(owner.Data,1),"Fresh profile modes locked");
            owner.Data.Level(10,true).cleared=true;owner.Data.Level(1,true).stars=7;
            CheckThat(EndgameService.TowerOpen(owner.Data)&&EndgameService.NightmareOpen(owner.Data,1)&&!EndgameService.NightmareOpen(owner.Data,2),"Modes unlock only at their exact normal milestones");
            var utc=new DateTime(2026,10,4,12,0,0,DateTimeKind.Utc);
            CheckThat(EndgameService.Claim(owner,80,utc)==80&&EndgameService.Claim(owner,80,utc)==20&&EndgameService.Claim(owner,10,utc)==0,"Shared 100/day limit truncates payouts");
            CheckThat(EndgameService.Claim(owner,10,utc.AddDays(-1))==0,"Clock rollback does not restore payout allowance");
            CheckThat(EndgameService.Claim(owner,10,utc.AddDays(1))==10,"UTC next day resets allowance");
            CheckThat(EndgameService.Claim(owner,-20,utc.AddDays(1))==0,"Negative request pays nothing");
            var monday=new DateTime(2026,10,5,0,0,0,DateTimeKind.Utc);
            CheckThat(EndgameFactory.Weekly(monday.AddSeconds(-1))!=EndgameFactory.Weekly(monday)&&EndgameFactory.Weekly(monday)==EndgameFactory.Weekly(monday.AddDays(21)),"Monday UTC boundary and three-week rotation");
            CheckThat(Mathf.Approximately(EndgameFactory.Multiplier(2),1.08f)&&Mathf.Approximately(EndgameFactory.Multiplier(1000000),5),"Linear eight-percent tower formula and cap");
            for(int floor=1;floor<=30;floor++)
            {
                var t=EndgameFactory.Tower(floor,utc);var entries=t.spawnTable.Generate(t.TotalMonsters,0,42,true);
                CheckThat(entries.Sum(x=>x.count)==t.TotalMonsters&&entries.Select(x=>x.archetype.id).Distinct().Count()==11&&t.zones.Count>0&&t.bosses.Count==(floor%10==0?1:0)&&(entries.Sum(x=>x.eliteCount)>0)==(floor%5==0),"Tower formula floor "+floor+": roster/count/zones/elite/boss");EndgameFactory.Dispose(t);
            }
            for(int i=1;i<=10;i++)
            {
                var original=LevelCatalog.Instance.Get(i);var n=EndgameFactory.Nightmare(i);
                CheckThat(Mathf.Approximately(n.healthMultiplier,original.healthMultiplier*1.5f)&&Mathf.Approximately(n.damageMultiplier,original.damageMultiplier*1.5f)&&Mathf.Approximately(n.speedMultiplier,original.speedMultiplier*1.5f)&&n.TotalMonsters==original.TotalMonsters&&n.aiTier==Mathf.Min(4,original.aiTier+1),"Nightmare "+i+" factors and unchanged wave counts");
                if(i>=8)CheckThat(n.spawnTable.roster.Count==11&&n.spawnTable.finalWave.Select(x=>x.archetype.id).Distinct().Count()==11,"Nightmare "+i+" keeps seven original + four P19 roster");EndgameFactory.Dispose(n);
            }
            foreach(var a in EndgameService.Achievements)
            {
                var p=new ProfileData();CheckThat(!a.met(p),a.id+" unmet condition rejects");var s=EndgameService.State(p);
                switch(a.id){case "first-read":p.learning.Lesson("qa").readRewarded=true;break;case "five-lessons":for(int i=0;i<5;i++)p.learning.Lesson("qa"+i).completed=true;break;case "gold":p.learning.Lesson("qa").mastery=3;break;case "scholar":foreach(var id in EndgameService.LessonIds())p.learning.Lesson(id).mastery=3;break;case "streak7":p.learning.studyDaily.streak=7;break;case "streak30":p.learning.studyDaily.streak=30;break;case "clear1":p.Level(1,true).cleared=true;break;case "clear5":for(int i=1;i<=5;i++)p.Level(i,true).cleared=true;break;case "rift":p.Level(10,true).cleared=true;break;case "perfect":for(int i=1;i<=10;i++)p.Level(i,true).stars=7;break;case "elements":for(int i=0;i<9;i++)p.seenReactions.Add("reaction"+i);break;case "kills100":s.kills=100;break;case "elites10":s.eliteKills=10;break;case "boss":s.bossKills=1;break;case "tower5":s.highestFloor=5;break;case "tower10":s.highestFloor=10;break;case "tower30":s.highestFloor=30;break;case "night1":s.Nightmare(1,true).cleared=true;break;case "night10":for(int i=1;i<=10;i++)s.Nightmare(i,true).cleared=true;break;case "skyguard":s.fireSafeMask=7;break;}
                CheckThat(a.met(p),a.id+" exact condition accepts");
            }
            var data=CompleteProfile();owner.UseTransient(data);var run=new StarRun();var nightmare=EndgameFactory.Nightmare(2);
            EndgameService.Record(new LevelResult{level=nightmare,won=true,seconds=80,starMask=3},run);EndgameService.Record(new LevelResult{level=nightmare,won=true,seconds=100,starMask=5},run);
            CheckThat(data.endgame.Nightmare(2).bestTime==80&&data.endgame.Nightmare(2).stars==7,"Best time only improves and nightmare stars accumulate");
            var path=Path.GetFullPath(Root+"qa-save/campusrift-v2.json");var store=new JsonProfileStore(path);store.Save(data);var read=store.Load();
            CheckThat(read.endgame.Nightmare(2).bestTime==80&&read.endgame.Nightmare(2).stars==7,"Endgame local profile disk roundtrip");
            EndgameFactory.Dispose(nightmare);
            var older=JsonUtility.FromJson<ProfileData>("{\"version\":2,\"cultivation\":{\"tier\":1},\"learning\":{\"lessons\":[]},\"levels\":[]}");CheckThat(EndgameService.State(older)!=null,"Older profile gains empty endgame section");
        }
    }
}
#endif
