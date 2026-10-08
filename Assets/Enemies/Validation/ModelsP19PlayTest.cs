#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.Skills;
using CampusRift.SkyBeast;
namespace CampusRift.Validation
{
    public sealed class ModelsP19PlayTest:P12PlayTest
    {
        public bool FailedBranchesOnly;
        EnemyInstance Spawn(string id,int level=10,Vector3? point=null)
        {
            var a=UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/"+id+".asset");var scale=LevelCatalog.Instance.Get(level).Scaling;
            var e=EnemyPool.Ensure().Spawn(a,point??(world.origin+Vector3.right*7),scale);if(e!=null){e.Brain.enabled=false;e.Motor.Stop();}return e;
        }
        void Kill(EnemyInstance e){var h=DamageInfo.Create(1000000,Element.None,DamageSource.Skill,e.transform.position,Vector3.forward,world.player.gameObject);h.skillId="p19-smoke";e.Vitality.ApplyDamage(h);}
        void Clean(){EnemyPool.Instance.ReleaseAll();EnemyTelegraph.Clear();world.player.GetComponent<SkillVfxPool>().Clear();world.PlacePlayer(world.origin);}
        IEnumerator Shot(string name)
        {
            Directory.CreateDirectory("task/models/screens");yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("task/models/screens/"+name+".png",frame.EncodeToPNG());Destroy(frame);yield return null;
        }
        protected override IEnumerator Run()
        {
            Begin();world.Mode(false);world.Lighting(false);world.Look(90,14);world.player.enabled=false;
            var hp=world.player.GetComponent<PlayerMonsterHealth>();hp.SetProgressionMaxHealth(10000);hp.Revive(1,0);hp.hitInvulnerability=0;hp.respawnOnDefeat=false;
            foreach(string id in new[]{"anh-yeu","trieu-hon-su","duc-yeu","hoa-linh"})
            {
                var e=Spawn(id);Check(e!=null&&e.Alive&&e.Animation.CurrentState=="Spawn",id+" actual spawn/profile");if(e==null)continue;
                e.GetComponent<EnemyConcealment>()?.RevealFor(10);var flight=e.GetComponent<FlyingMotor>();if(flight!=null)flight.Manual=true;
                yield return new WaitForSeconds(.9f);e.Animation.Play(e.Animation.profile.idle,1,5);
                var rs=e.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                Measure(id+" root="+e.transform.position+" visibleBounds="+b+" height="+b.size.y+" state="+e.Animation.CurrentState);
                Check(b.size.y>.7f&&b.size.y<3.8f&&rs.All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader!=null&&m.shader.name!="Hidden/InternalErrorShader")),id+" readable scale/material");
                var group=e.GetComponent<LODGroup>();Check(group!=null&&group.lodCount==3&&group.GetLODs().All(l=>l.renderers.Length>0),id+" three skinned LODs");
                if(flight!=null)world.Look(90,-8);yield return Shot(id+"-static");world.Look(90,14);
                for(int lod=1;lod<=2;lod++){group.ForceLOD(lod);yield return null;var bounds=group.GetLODs()[lod].renderers[0].bounds;Check(bounds.size.y>.5f&&bounds.size.y<4,id+" LOD"+lod+" actual skinned bounds");}group.ForceLOD(-1);
                if(id=="anh-yeu"){var feet=e.GetComponent<EnemyFootPlant>();feet.ResetMetrics();e.Animation.Play("Run_Forward",1,1.5f);e.Motor.MoveTo(world.origin+Vector3.right*12);yield return new WaitForSeconds(1.2f);Measure("Night Demon planted samples="+feet.PlantSamples+" worldSlide="+feet.MaxWorldSlide+" residual="+feet.MaxPlantSlip);Check(feet.PlantSamples>0&&feet.MaxWorldSlide<.08f,"Night Demon feet stay planted during real NavMesh run");e.Motor.Stop();}
                float before=e.Vitality.Health;world.player.GetComponent<PlayerCombat>().ResolveHit(e.Vitality,1,false);Check(e.Vitality.Health<before,id+" real player hit");
                Kill(e);yield return new WaitForSeconds(1.6f);Check(!e.gameObject.activeSelf,id+" death/dissolve/pool");Clean();
            }
            if(FailedBranchesOnly){yield return AiAndStars();yield return ExtraHooks();yield break;}
            // Teleport destination is committed before the half-second warning; movement avoids next strike.
            var shadow=Spawn("anh-yeu",8);var special=shadow.GetComponent<ExpandedEnemyRuntime>();shadow.GetComponent<EnemyConcealment>().RevealFor(2);
            special.Force("teleport",world.player.transform);yield return new WaitForSeconds(.2f);yield return Shot("anh-yeu-telegraph");var committed=special.WarnedPoint;int hits=hp.DamageCount;world.PlacePlayer(world.origin+Vector3.forward*6);
            yield return new WaitForSeconds(.8f);Check(special.Teleports==1&&Vector3.Distance(shadow.transform.position,committed)<.25f&&special.ImpactAt-special.WarningAt>=.49f&&hp.DamageCount==hits,"Teleport0.5s, committed NavMesh destination, dodge avoids hit");Clean();
            shadow=Spawn("anh-yeu",8,world.origin+Vector3.right*12);var conceal=shadow.GetComponent<EnemyConcealment>();yield return null;yield return null;
            Check(conceal.Hidden,"Shadow concealed beyond5m");var sight=world.player.GetComponent<SpiritSightRuntime>();sight.ResetCooldownForValidation();world.player.GetComponent<SpiritPower>().Refill();Check(sight.CastAt(world.origin),"Real Spirit Sight cast");yield return new WaitForSeconds(.5f);Check(!conceal.Hidden,"P18 Spirit Sight reveals real Shadow enemy");sight.ReadyOnRestEquip();conceal.ResetLife();yield return null;
            shadow.Vitality.ApplyDamage(DamageInfo.Create(1,Element.None,DamageSource.Projectile,shadow.transform.position,Vector3.forward,world.player.gameObject));yield return null;Check(!conceal.Hidden,"Any accepted hit reveals Shadow");Clean();
            var caster=Spawn("trieu-hon-su",10);special=caster.GetComponent<ExpandedEnemyRuntime>();special.Force("summon",world.player.transform);yield return new WaitForSeconds(.3f);yield return Shot("trieu-hon-su-telegraph");yield return new WaitForSeconds(.9f);
            Check(special.SummonCount==1&&special.Owned.Count==2&&special.Owned.All(e=>!e.countsForSwordIntent),"Summoner creates two excluded minions");
            var ally=special.Owned[0];ally.Brain.enabled=false;ally.Motor.Stop();ally.Vitality.ApplyDamage(DamageInfo.Create(ally.Vitality.maxHealth*.4f,Element.None,DamageSource.Skill,ally.transform.position,Vector3.forward));float allyHp=ally.Vitality.Health;special.HealAllies(1);
            Check(Mathf.Abs(ally.Vitality.Health-allyHp-ally.Vitality.maxHealth*.03f)<.05f,"Summoner R6 heals3% maxHP per second");
            Check(special.ShieldAllies()==2&&Mathf.Abs(ally.GetComponent<EnemyWard>().Remaining-ally.Vitality.maxHealth*.2f)<.05f,"Summoner shields up to three allies with20%maxHP");
            var owned=special.Owned.ToArray();Kill(caster);float dissolveEnd=Time.time+2;while(owned.Any(e=>e.gameObject.activeSelf)&&Time.time<dissolveEnd)yield return null;Check(owned.All(e=>!e.gameObject.activeSelf),"Master death dissolves all owned minions");Clean();
            var wing=Spawn("duc-yeu");var fm=wing.GetComponent<FlyingMotor>();fm.Manual=true;yield return new WaitForSeconds(.9f);Check(fm.Height>=3&&fm.Height<=6&&!wing.Motor.Agent.enabled,"FlyingMotor3â€“6m above outdoor NavMesh");
            Vector3 indoor=ShelterGraphReference.Graph.Nodes.First(n=>n.WorldPosition.y<1&&ShelterDetector.AtFeet(n.WorldPosition)==Shelter.Indoor).WorldPosition;
            Check(!fm.MoveSafely(indoor+Vector3.up*4.5f),"Flying motor rejects flight into shelter / walls");special=wing.GetComponent<ExpandedEnemyRuntime>();special.Force("dive",world.player.transform);yield return new WaitForSeconds(.25f);world.Look(90,-8);yield return Shot("duc-yeu-telegraph");hits=hp.DamageCount;world.PlacePlayer(world.origin+Vector3.forward*6);yield return new WaitForSeconds(1.1f);Check(special.Dives==1&&hp.DamageCount==hits,"Dive0.6s circle, committed point can be dodged");
            world.PlacePlayer(world.origin);special.Force("fireball",world.player.transform);yield return new WaitForSeconds(1.4f);Check(special.Fireballs==1,"Level9+ fireball lands with telegraph and aftermath");
            Clean();wing=Spawn("duc-yeu",10,world.origin+Vector3.right*5);wing.GetComponent<FlyingMotor>().Manual=true;wing.Vitality.SetMaxHealth(10000,true);var chain=world.player.GetComponent<ChainLightningRuntime>();chain.ResetCooldownForValidation();world.player.GetComponent<SpiritPower>().Refill();chain.CastAt(world.origin+Vector3.right*5);yield return new WaitForSeconds(.35f);Check(wing.Vitality.ReceivedHits>0,"Real auto-target lightning hits airborne enemy above3m");chain.ReadyOnRestEquip();Clean();
            var fire=Spawn("hoa-linh");var state=fire.GetComponent<FireEnemyState>();float speed=fire.Speed;state.AfterBreath();Check(state.Immune&&Mathf.Abs(fire.Speed-speed*1.2f)<.01f,"Fire Spirit immune and+20%speed after breath");
            fire.Motor.MoveTo(world.origin+Vector3.right*11);yield return new WaitForSeconds(1.5f);yield return Shot("hoa-linh-telegraph");Check(FindObjectsByType<BurningGround>().Any(g=>g.name=="P19 Fire Spirit trails"&&g.ActiveCount>0),"Moving Fire Spirit leaves real small fire patches");Clean();
            yield return AffixSmoke();yield return AiAndStars();yield return ExtraHooks();
            for(int level=6;level<=10;level++)
            {var d=LevelCatalog.Instance.Get(level);var es=Enumerable.Range(0,d.waves.Count).SelectMany(w=>d.spawnTable.Generate(d.waves[w].TotalCount,w,99,w==d.waves.Count-1)).ToArray();Check(es.Sum(e=>e.count)+d.bosses.Count==new[]{26,30,32,36,45}[level-6],"L"+level+" exact counted budget");Check(es.Sum(e=>e.eliteCount)==d.spawnTable.elitesPerWave.Sum(),"L"+level+" elite budget / unique affix runtime");}
            // Dense visual fixture contains all actual models without changing the production cap.
            world.Lighting(false);world.Look(90,6);int index=0;
            foreach(var row in LevelCatalog.Instance.Get(10).spawnTable.roster)
            {var e=Spawn(row.archetype.id,10,world.origin+new Vector3(10+(index%3)*2.5f,0,(index/3-1.5f)*2.3f));if(e!=null){e.GetComponent<EnemyConcealment>()?.RevealFor(10);if(e.GetComponent<FlyingMotor>()!=null)e.GetComponent<FlyingMotor>().Manual=true;}index++;}
            yield return new WaitForSeconds(1);yield return Shot("level10-crowd");Check(EnemyDirector.Instance.Active.Count==11,"Dense level10 fixture all11 types");
        }
        IEnumerator AffixSmoke()
        {
            foreach(EliteAffixKind kind in Enum.GetValues(typeof(EliteAffixKind)))
            {
                var e=Spawn("tieu-yeu");e.Elite.Configure(10,19,kind);yield return new WaitForSeconds(.9f);float baseHp=e.archetype.baseHealth*e.scaling.health*e.archetype.lateHealthMultiplier;
                Check(e.Elite.Affixes.Count==1&&e.SwordIntentWeight==4&&Mathf.Abs(e.Vitality.maxHealth-baseHp*3)<.1f,kind+" elite HPÃ—3/weight4");
                if(kind==EliteAffixKind.Berserk)Check(e.Elite.SpeedMultiplier==1.3f&&Mathf.Abs(e.Elite.AttackIntervalMultiplier-1/1.3f)<.001f,"Berserk move/attack+30%");
                if(kind==EliteAffixKind.MetalBody){float old=e.Vitality.Health;e.Vitality.ApplyDamage(DamageInfo.Create(100,Element.None,DamageSource.Skill,e.transform.position,Vector3.forward));Check(Mathf.Abs(old-e.Vitality.Health-60)<.05f,"Metal Body40% reduction");e.Status.Apply(StatusType.Stun,1);e.Vitality.ApplyDamage(DamageInfo.Create(10,Element.Kim,DamageSource.Skill,e.transform.position,Vector3.forward,world.player.gameObject));Check(e.Elite.MetalBroken,"Real ArmorShatter disables Metal Body");}
                if(kind==EliteAffixKind.Split){var intent=new GameObject("P19 split intent").AddComponent<SwordIntent>();intent.BeginWave(4,1);intent.Track(e);Kill(e);Check(e.Elite.ChildrenSpawned==2&&EnemyDirector.Instance.Active.All(c=>!c.countsForSwordIntent),"Split two30% children excluded from intent");intent.MarkWaveCleared();Check(intent.Full&&intent.Fraction==1&&intent.DefeatedWeight==4,"Split original death reaches exact100%");yield return new WaitForSeconds(.75f);Check(EnemyDirector.Instance.Active.Count==0,"Split children dissolve at full intent");Destroy(intent.gameObject);}
                if(kind==EliteAffixKind.Vampiric){e.Vitality.ApplyDamage(DamageInfo.Create(200,Element.None,DamageSource.Skill,e.transform.position,Vector3.forward));float before=e.Vitality.Health,hpBefore=world.player.GetComponent<PlayerMonsterHealth>().CurrentHealth;var damage=DamageInfo.Create(40,Element.None,DamageSource.Melee,world.origin,Vector3.left,e.gameObject);damage.ignoreInvulnerability=true;world.player.GetComponent<PlayerMonsterHealth>().ApplyDamage(damage);float loss=hpBefore-world.player.GetComponent<PlayerMonsterHealth>().CurrentHealth;Check(Mathf.Abs(e.Vitality.Health-before-loss*.2f)<.1f,"Vampiric heals20% actual accepted damage");}
                if(kind==EliteAffixKind.Explosion){world.PlacePlayer(e.transform.position+Vector3.right*3);int before=world.player.GetComponent<PlayerMonsterHealth>().DamageCount;Kill(e);yield return new WaitForSeconds(.8f);Check(world.player.GetComponent<PlayerMonsterHealth>().DamageCount>before,"Death explosion R4 warned then damages player");}
                if(kind==EliteAffixKind.Invisible){Check(e.GetComponent<EnemyConcealment>().Hidden,"Invisible affix conceals idle body");e.GetComponent<EnemyConcealment>().RevealFor(1);Check(!e.GetComponent<EnemyConcealment>().Hidden,"Invisible affix respects reveal lease");}
                if(kind==EliteAffixKind.Guardian){var v=Spawn("tieu-yeu",10,e.transform.position+Vector3.forward*2);float before=v.Vitality.Health;v.Vitality.ApplyDamage(DamageInfo.Create(100,Element.None,DamageSource.Skill,v.transform.position,Vector3.forward));Check(Mathf.Abs(before-v.Vitality.Health-70)<.1f,"Guardian nearby ally receives30% less damage");}
                if(kind==EliteAffixKind.FireHeart)Check(e.GetComponent<FireEnemyState>().Immune,"Fire Heart grants true fire immunity");
                Clean();yield return null;
            }
            var one=Spawn("thiet-giap-nguu",6);one.Elite.Configure(6,20);Check(one.Elite.Affixes.Count==1&&!one.Elite.Has(EliteAffixKind.FireHeart),"L6 elite one affix and no FireHeart");one.GetComponent<EnemyConcealment>()?.RevealFor(10);yield return Shot("elite-one-affix");Clean();
            var two=Spawn("anh-yeu",10);two.Elite.Configure(10,21,EliteAffixKind.Berserk,EliteAffixKind.FireHeart);two.GetComponent<EnemyConcealment>().RevealFor(10);yield return new WaitForSeconds(.9f);Check(two.Elite.Affixes.Count==2&&two.Elite.Affixes[0]!=two.Elite.Affixes[1],"L10 elite two distinct affixes");yield return Shot("elite-two-affixes");Clean();
        }
        IEnumerator AiAndStars()
        {
            var summoner=Spawn("trieu-hon-su",8,world.origin+Vector3.right*11);var hurt=Spawn("tieu-yeu",6,world.origin+Vector3.right*7);yield return new WaitForSeconds(2.6f);hurt.Vitality.ApplyDamage(DamageInfo.Create(hurt.Vitality.maxHealth*.8f,Element.None,DamageSource.Skill,hurt.transform.position,Vector3.forward));Check(ExpandedEnemyRuntime.RetreatTarget(hurt)==summoner,"T2 hurt25% finds nearest reachable Summoner");hurt.Brain.enabled=true;yield return new WaitForSeconds(.65f);Check(hurt.Motor.Agent.hasPath||Vector3.Distance(hurt.transform.position,summoner.transform.position)<3.2f,"T2 real brain retreats / settles inside healing range");Clean();
            var ed=EnemyDirector.Instance;ed.ResetCounters();var t4=Spawn("tieu-yeu",10);float before=t4.Brain.DodgeChance;yield return new WaitForSeconds(.5f);float radius=ed.RingRadius;
            var area=world.player.GetComponent<HeavenThunderRuntime>();world.player.GetComponent<SpiritPower>().Refill();area.CommitCast();area.CommitCast();area.CommitCast();yield return new WaitForSeconds(.5f);Check(ed.AreaCastsLastMinute==3&&ed.Adapting(t4)&&ed.RingRadius>radius+1,"Three real area commits in60s widen T4 surround");Check(before==.6f&&t4.Brain.DodgeRoll(.59f)&&!t4.Brain.DodgeRoll(.61f),"T4 deterministic dodge boundary60% (no statistical trials)");Clean();
            var pass=new StarRun{summonersSeen=2,summonersKilled=2};Check((StarEvaluator.Evaluate(6,true,900,660,pass)&4)!=0,"L6 challenge passes even beyond old11min when summoners defeated early");pass.secondSummon=true;Check((StarEvaluator.Evaluate(6,true,100,660,pass)&4)==0,"L6 second summon fails challenge");
        }
        IEnumerator ExtraHooks()
        {
            var stars=new GameObject("P19 live stars").AddComponent<StarEvaluator>();stars.BeginRun(world.player.gameObject);
            var caster=Spawn("trieu-hon-su",6);var special=caster.GetComponent<ExpandedEnemyRuntime>();special.Force("summon",world.player.transform);yield return new WaitForSeconds(1.3f);special.Force("summon",world.player.transform);yield return new WaitForSeconds(1.3f);
            Check(stars.Run.summonersSeen==1&&stars.Run.secondSummon,"L6 actual second summon event records failed challenge");
            stars.Unhook();Clean();stars.BeginRun(world.player.gameObject);caster=Spawn("trieu-hon-su",6);Kill(caster);LevelEvents.RaiseEnemyKilled(caster);
            Check(stars.Run.summonersSeen==1&&stars.Run.summonersKilled==1&&!stars.Run.secondSummon&&(StarEvaluator.Evaluate(6,true,700,660,stars.Run)&4)!=0,"L6 actual counted death before second summon earns challenge");stars.Unhook();Destroy(stars.gameObject);Clean();
            var scheduler=SkyBeastScheduler.Begin(10);FireBreathCycle.Instance.StartDev(10);FireBreathCycle.Instance.AutoAdvance=false;scheduler.ApplySkySwordHit();scheduler.ApplySkySwordHit();var drops=scheduler.Beasts[0].GetComponent<FireSpiritDrops>();
            yield return drops.Drop();Check(drops.Dropped==4&&EnemyDirector.Instance.Active.All(e=>e.archetype.id=="hoa-linh"&&!e.countsForSwordIntent),"Real phase3 drop produces four excluded Fire Spirits");
            var intent=new GameObject("P19 drop intent").AddComponent<SwordIntent>();var e=Spawn("tieu-yeu");intent.BeginWave(1,1);intent.Track(e);Kill(e);intent.MarkWaveCleared();yield return new WaitForSeconds(.8f);
            Check(intent.Full&&intent.Fraction==1&&EnemyDirector.Instance.Active.Count==0,"Dropped Fire Spirits do not block100% and dissolve at Ready");Destroy(intent.gameObject);SkyBeastScheduler.StopAll();Clean();
        }
        protected override void Cleanup(){world.player.enabled=true;base.Cleanup();Directory.CreateDirectory("task/models");File.WriteAllText("task/models/ModelsP19PlayTest.json",JsonUtility.ToJson(report,true));}
    }
}
#endif
