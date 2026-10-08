#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.Progression;
using CampusRift.Skills;
namespace CampusRift.Validation
{
 public class P12BalancePlayTest:P12PlayTest
 {
  protected virtual bool BossOnly=>false;
  public int FirstLevel=1,LastLevel=10;
  [Serializable] public class Row {public int level,realm,tier,kills,total,swings,hits,dodges,skillCasts,dodgeRefusals;public float seconds,par,bossSeconds,healthLeft,maxHealth,attack,energyLeft,spiritLeft;public bool won,durationTarget;public string[] normalSamples,loadout;}
  [Serializable] public class Audit {public string at,method;public List<Row> levels=new List<Row>();}
  readonly Audit audit=new Audit();LevelDirector director;CampusInput input;PlayerCombat combat;PlayerMonsterHealth health;PlayerStats stats;TargetLock targetLock;
  readonly Dictionary<EnemyInstance,float> evadedWarnings=new Dictionary<EnemyInstance,float>();int dodgeRefusals;
  NavMeshPath path;float pathAt,nextSkill,nextInteraction;Vector3 goal,walkDirection;EnemyInstance target;float bossEncounter=-1,bossDefeat=-1;float oldScale;SkillLoadout loadout;DodgeAbility dodge;ContextInteraction interaction;int skillCasts;LevelDefinition trial;
  void BossDied(EnemyInstance enemy){if(enemy.archetype.isBoss)bossDefeat=director.Elapsed;}
  void SaveBalance(){Directory.CreateDirectory("Artifacts/Levels");audit.at=DateTime.UtcNow.ToString("o");File.WriteAllText("Artifacts/Levels/"+(BossOnly?"P12-BossBalance":"P12-Balance")+".json",JsonUtility.ToJson(audit,true));}
  protected override IEnumerator Run(){path=new NavMeshPath();Begin();oldScale=Time.timeScale;world.Mode(true);Time.timeScale=1;
   audit.method="Actual recommended realm/tier, ordinary HP/spirit/stamina, no healing items, revive or stat buffs. Crit suppressed to compare tuning. Four legally unlocked/equipped skills with real cooldown/spirit costs; real swords, dodge and CharacterController mobile input. NavMesh plans movement, no warp during fights. Real boss/AI. Normal 1x time, original 15s inter-wave rests. Automated strategy is not a human playtest; failed clears/duration targets stay failed.";
   combat=world.player.GetComponent<PlayerCombat>();input=world.player.GetComponent<CampusInput>();health=world.player.GetComponent<PlayerMonsterHealth>();stats=world.player.GetComponent<PlayerStats>();targetLock=world.player.GetComponent<TargetLock>();
   loadout=world.player.GetComponent<SkillLoadout>();dodge=world.player.GetComponent<DodgeAbility>();interaction=world.player.GetComponent<ContextInteraction>();
   director=LevelDirector.Ensure();director.enabled=true;director.SeedOverride=12042;LevelEvents.EnemyKilled+=BossDied;
   if(BossOnly)audit.method+=" Boss-only trial: clone level with one empty wave, original boss/AI/scaling at the open court; no normal-wave clear claimed.";
   for(int level=FirstLevel;level<=LastLevel;level++){if(BossOnly&&level!=5&&level!=7)continue;UI.UIStateManager.Instance.EnterScene(true);Time.timeScale=1;var d=LevelCatalog.Instance.Get(level);ProfileService.Instance.Cultivation.SetState((Realm)d.requiredRealm,d.requiredTier,0);yield return null;
    if(BossOnly){trial=Instantiate(d);trial.spawnTable=null;trial.waves.Clear();trial.waves.Add(new WaveDefinition());trial.zones.Clear();trial.zones.Add(new RiftZoneData{id="boss-balance-court",position=world.origin+Vector3.right*10,radius=.1f});trial.spawnPoint=world.origin;d=trial;}
    var samples=new List<string>();
    if(!BossOnly)foreach(var row in d.spawnTable.roster.Where(x=>x.archetype.id!="thiet-giap-nguu")){var e=EnemyPool.Ensure().Spawn(row.archetype,world.origin+Vector3.right*7,d.Scaling);e.Brain.enabled=false;e.Motor.Stop();float hp=e.Vitality.Health;int hits=0;while(!e.Vitality.Defeated&&hits<8){combat.ResolveHit(e.Vitality,combat.config.chainPercent[hits%3],hits%3==2);hits++;}
     health.Revive(1,0);while(health.Invulnerable)yield return null;float before=health.CurrentHealth;bool accepted=health.ApplyDamage(DamageInfo.Create(e.Damage,e.archetype.element,DamageSource.Melee,world.player.transform.position,Vector3.left,e.gameObject));float damagePct=(before-health.CurrentHealth)/stats.MaxHealth*100;samples.Add(e.archetype.id+": HP="+hp.ToString("F1")+", comboHits="+hits+", actual regular hit="+damagePct.ToString("F2")+"%");Check(hits>=2&&hits<=3,"L"+level+" "+e.archetype.id+" dies in2–3 real combo hits ("+hits+")");Check(accepted&&damagePct>=7.95f&&damagePct<=9.05f,"L"+level+" "+e.archetype.id+" actual accepted regular hit takes8–9% HP ("+damagePct.ToString("F2")+"%)");EnemyPool.Instance.Release(e);}
    string[] equipped=level<3?new[]{"dai-thu-an","tich-lich-nhat-thiem","hu-khong-ket-gioi","anh-phan-than"}:level<5?new[]{"han-bang-phong-an","than-kiem-ngu-loi","dai-thu-an","tich-lich-nhat-thiem"}:new[]{"han-bang-phong-an","than-kiem-ngu-loi","phat-no-hoa-lien","kim-chung-trao"};
    for(int slot=0;slot<4;slot++){var skill=loadout.Find(equipped[slot]);loadout.Equip(slot,skill!=null&&skill.IsUnlocked?equipped[slot]:"");}
    var result=new Row{level=level,realm=d.requiredRealm,tier=d.requiredTier,par=d.parTimeSeconds,maxHealth=stats.MaxHealth,attack=stats.Attack,normalSamples=samples.ToArray(),loadout=Enumerable.Range(0,4).Select(i=>loadout.Get(i)!=null?loadout.Get(i).Id+" rank"+loadout.Get(i).rank:"empty").ToArray()};
    LevelSession.Loadout=Enumerable.Range(0,4).Select(i=>loadout.Get(i)?.Id??"").ToArray();UI.UIStateManager.Instance.EnterScene(true);director.Begin(d);Time.timeScale=1;
    result.loadout=Enumerable.Range(0,4).Select(i=>loadout.Get(i)!=null?loadout.Get(i).Id+" rank"+loadout.Get(i).rank:"empty").ToArray();
    input.TouchSprint=false;bossEncounter=bossDefeat=-1;target=null;pathAt=nextSkill=nextInteraction=0;evadedWarnings.Clear();dodgeRefusals=0;int oldSwings=combat.SwingCount,oldHits=combat.HitCount,oldDodges=dodge.DodgeCount,oldCasts=skillCasts;
    float deadline=Time.time+(BossOnly?240:d.parTimeSeconds*1.5f+120);
    while(director.State!=LevelDirector.Phase.Won&&director.State!=LevelDirector.Phase.Lost&&Time.time<deadline){Drive();yield return null;}
    input.TouchMove=Vector2.zero;input.TouchSprint=false;result.won=director.State==LevelDirector.Phase.Won;result.seconds=director.Elapsed;result.healthLeft=health.CurrentHealth;result.total=director.TotalPlanned;result.kills=director.Kills;result.swings=combat.SwingCount-oldSwings;result.hits=combat.HitCount-oldHits;result.bossSeconds=bossEncounter>=0&&bossDefeat>=0?bossDefeat-bossEncounter:0;result.durationTarget=result.won&&result.seconds>=result.par*.7f&&result.seconds<=result.par*1.3f;
    result.dodges=dodge.DodgeCount-oldDodges;result.skillCasts=skillCasts-oldCasts;
    result.dodgeRefusals=dodgeRefusals;result.energyLeft=world.player.Energy;result.spiritLeft=world.player.GetComponent<SpiritPower>().Current;
    audit.levels.Add(result);SaveBalance();Check(result.won,"L"+level+" recommended realm real combat clear");Measure("L"+level+" "+result.seconds.ToString("F2")+"s / par"+result.par+"; boss="+result.bossSeconds.ToString("F2")+"; HP="+result.healthLeft.ToString("F1")+"; "+result.kills+"/"+result.total+" kills; "+result.hits+" sword hits; durationTarget="+result.durationTarget);
    if(!BossOnly&&level>=3&&level<=7)Check(result.durationTarget,"L"+level+" total duration within design±30%");
    if(level==5)Check(result.won&&result.bossSeconds>=60&&result.bossSeconds<=90,"L5 boss defeated in60–90s at KetDan1");
    if(level==7)Check(result.won&&result.bossSeconds>=90&&result.bossSeconds<=120,"L7 boss defeated in90–120s at NguyenAnh1");
    if(level==7&&result.won)yield return new WaitForSecondsRealtime(4.5f);director.End();if(trial!=null){Destroy(trial);trial=null;}UI.UIStateManager.Instance.EnterScene(true);Time.timeScale=1;health.Revive(1,0);yield return new WaitForSecondsRealtime(6.5f);
   }
  }
  void Drive(){if(health.IsDead)return;Vector3 p=world.player.transform.position;
   var live=director.Alive.Where(e=>e.Alive).ToArray();target=live.OrderBy(e=>Vector3.Distance(e.transform.position,p)+(e.archetype.id=="thiet-giap-nguu"?8:0)-(e.archetype.ranged?3:0)).FirstOrDefault();if(target==null){input.TouchMove=Vector2.zero;return;}
   if(target.archetype.isBoss&&bossEncounter<0&&Vector3.Distance(target.transform.position,p)<18)bossEncounter=director.Elapsed;
   Vector3 flat=Vector3.ProjectOnPlane(target.transform.position-p,Vector3.up);float distance=flat.magnitude;float yaw=Mathf.Atan2(flat.x,flat.z)*Mathf.Rad2Deg;world.Look(yaw,14);targetLock.Set(target.Vitality);
   bool clear=CombatLine.Clear(p+Vector3.up*1.2f,CombatLine.Chest(target.Vitality),world.player.transform);
   // Reserve against the cooldown, including while recovering the cost. IsReady also
   // checks affordability; using it here would spend every regenerated point on swords
   // and prevent the shield from ever becoming affordable again.
   bool shieldNeeded=health.CurrentHealth<stats.MaxHealth*.7f||live.Any(e=>e.GetComponent<BossController>()?.Busy==true&&Vector3.Distance(e.transform.position,p)<9);
   var bellSkill=loadout.Find("kim-chung-trao");bool reserveBell=shieldNeeded&&levelHasBell()&&bellSkill!=null&&bellSkill.IsUnlocked&&bellSkill.CooldownRemaining<=0;float spiritReserve=reserveBell?bellSkill.SpiritCost+8:12;
   if(clear&&distance<17&&input.Allowed&&Time.time>=nextSkill){foreach(int slot in new[]{3,0,1,2}){var skill=loadout.Get(slot);if(skill==null||!skill.IsReady||skill.GetState()==SkillState.Casting)continue;if(skill.Id=="kim-chung-trao"&&health.CurrentHealth>stats.MaxHealth*.8f&&!live.Any(e=>e.GetComponent<BossController>()?.Busy==true))continue;if(skill.Id=="phat-no-hoa-lien"&&distance>8)continue;if(skill.Id=="tich-lich-nhat-thiem"&&distance>4)continue;if(skill.Id=="hu-khong-ket-gioi"||skill.Id=="anh-phan-than")continue;if(skill.GetComponent<SpiritPower>().Current<skill.SpiritCost+(skill.Id=="kim-chung-trao"?0:spiritReserve))continue;bool cast=skill is Set1SkillRuntime set?set.CastAt(target.transform.position):skill.QuickCast();if(cast){skillCasts++;nextSkill=Time.time+1.1f;break;}}}
   if(clear&&distance<=combat.config.range-.35f&&input.Allowed&&(!reserveBell||world.player.GetComponent<SpiritPower>().Current>=combat.config.swingSpiritCost+spiritReserve))combat.TrySwing();
   if(Time.time>=pathAt){pathAt=Time.time+.18f;Vector3 away=(p-target.transform.position);away.y=0;if(away.sqrMagnitude<.01f)away=Vector3.back;
    var boss=target.GetComponent<BossController>();float wanted=10;
    Vector3 want=target.transform.position+Quaternion.Euler(0,20,0)*away.normalized*wanted;
    Vector3 pressure=Vector3.zero;foreach(var enemy in live){Vector3 gap=Vector3.ProjectOnPlane(p-enemy.transform.position,Vector3.up);if(gap.magnitude<6)pressure+=gap.normalized*(6-gap.magnitude);}if(pressure.sqrMagnitude>1)want=p+pressure.normalized*4;
    // Walk sideways around warnings. The warning locks its strike point/direction.
    if(boss!=null&&boss.Busy||live.Any(e=>e.GetComponent<EnemyAbilityRunner>()?.Busy==true&&Vector3.Distance(e.transform.position,p)<10))want=p+Quaternion.Euler(0,70,0)*away.normalized*4;
    if(!clear)want=target.transform.position+away.normalized*3;
    var filter=new NavMeshQueryFilter{agentTypeID=target.GetComponent<NavMeshAgent>().agentTypeID,areaMask=NavMesh.AllAreas};
    goal=p;if(NavMesh.SamplePosition(p,out var from,2,filter))for(int attempt=0;attempt<5;attempt++){Vector3 candidate=attempt==0?want:target.transform.position+Quaternion.Euler(0,attempt*60,0)*away.normalized*wanted;if(NavMesh.SamplePosition(candidate,out var hit,3,filter)&&NavMesh.CalculatePath(from.position,hit.position,filter,path)&&path.status==NavMeshPathStatus.PathComplete&&path.corners.Length>=2){goal=path.corners.Last();foreach(var corner in path.corners.Skip(1))if(Vector3.ProjectOnPlane(corner-p,Vector3.up).magnitude>.12f){goal=corner;break;}break;}}
   }
   walkDirection=Vector3.ProjectOnPlane(goal-p,Vector3.up);if(walkDirection.magnitude<.08f)input.TouchMove=Vector2.zero;else {var local=Quaternion.Euler(0,-yaw,0)*walkDirection.normalized;input.TouchMove=new Vector2(local.x,local.z);}
   bool phaseTwo=live.Any(e=>e.GetComponent<BossController>()?.PhaseTwo==true);
   float staminaReserve=dodge.energyCost*(phaseTwo?3:2)+5;
   input.TouchSprint=world.player.Energy>staminaReserve&&live.Any(e=>Vector3.Distance(e.transform.position,p)<4);
   var warned=live.FirstOrDefault(e=>LateWarning(e,p)&&(WarningStamp(e)<0||!evadedWarnings.TryGetValue(e,out var stamp)||stamp!=WarningStamp(e)));
   bool bolt=FindObjectsByType<EnemyProjectile>().Any(b=>b.Active&&Vector3.Distance(b.transform.position,p+Vector3.up)<3);
   if((warned!=null||bolt)&&input.Allowed&&dodge.CanDodge){Vector2 planned=input.TouchMove;
    Vector3 side=warned!=null?Vector3.Cross(Vector3.up,warned.transform.forward):Quaternion.Euler(0,yaw,0)*Vector3.right;
    var localSide=Quaternion.Euler(0,-yaw,0)*side.normalized;input.TouchMove=new Vector2(localSide.x,localSide.z);
    if(dodge.TryDodge()){if(warned!=null)evadedWarnings[warned]=WarningStamp(warned);}else{dodgeRefusals++;input.TouchMove=planned;}}
   if(Time.time>nextInteraction){nextInteraction=Time.time+.6f;if(interaction!=null&&interaction.Available&&interaction.Label=="OPEN DOOR")interaction.Activate();}
   // NavMesh paths can start at a sampled boundary while the player's physical capsule is
   // against a wall. Slide around that wall using real input, rather than walking into it.
   Vector3 desired=Quaternion.Euler(0,yaw,0)*new Vector3(input.TouchMove.x,0,input.TouchMove.y);
   if(desired.sqrMagnitude>.01f&&Physics.CapsuleCast(p+Vector3.up*.4f,p+Vector3.up*1.3f,.27f,desired.normalized,out var obstruction,.12f,~((1<<6)|(1<<7)|(1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)){
    Vector3 slide=Vector3.ProjectOnPlane(desired,obstruction.normal);slide.y=0;
    if(slide.sqrMagnitude<.04f){slide=Vector3.Cross(Vector3.up,obstruction.normal);if(Vector3.Dot(slide,goal-p)<0)slide=-slide;}
    if(slide.sqrMagnitude>.01f){slide.Normalize();if(Physics.CapsuleCast(p+Vector3.up*.4f,p+Vector3.up*1.3f,.27f,slide,.12f,~((1<<6)|(1<<7)|(1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))slide=-slide;var local=Quaternion.Euler(0,-yaw,0)*slide;input.TouchMove=new Vector2(local.x,local.z);}
   }
  }
  bool levelHasBell()=>Enumerable.Range(0,4).Any(i=>loadout.Get(i)?.Id=="kim-chung-trao");
  float WarningStamp(EnemyInstance e){var b=e.GetComponent<BossController>();if(b!=null&&b.Busy)return b.WarningAt;var a=e.GetComponent<EnemyAbilityRunner>();if(a!=null&&a.Busy)return a.WarningAt;return e.Brain!=null&&e.Brain.State==MinionState.Windup?e.Brain.WindupStartedAt:-1;}
  bool LateWarning(EnemyInstance e,Vector3 p){float distance=Vector3.Distance(e.transform.position,p);if(e.Brain!=null&&e.Brain.State==MinionState.Windup&&distance<6&&Time.time-e.Brain.WindupStartedAt>e.Brain.LastWindupSeconds*.65f)return true;
   var boss=e.GetComponent<BossController>();if(boss!=null&&boss.Busy&&boss.ImpactAt<boss.WarningAt&&distance<15){float seconds=boss.LastAttack==BossAttack.Roar?1.2f:boss.LastAttack==BossAttack.LeapSlam?.8f:.4f;return Time.time>=boss.WarningAt+seconds-.18f;}
   var runner=e.GetComponent<EnemyAbilityRunner>();if(runner!=null&&runner.Busy&&runner.ImpactAt<runner.WarningAt&&distance<12){var ability=e.archetype.abilities.FirstOrDefault(u=>u.ability!=null&&u.ability.id==runner.LastAbility).ability;float seconds=ability!=null?ability.telegraphSeconds:.8f;return Time.time>=runner.WarningAt+seconds-.18f;}
   return e.GetComponent<MonsterCombat>()?.WindingUp==true&&distance<3;
  }
  protected override void Cleanup(){LevelEvents.EnemyKilled-=BossDied;if(input!=null){input.TouchMove=Vector2.zero;input.TouchSprint=false;}director?.End();if(trial!=null)Destroy(trial);Time.timeScale=oldScale;SaveBalance();base.Cleanup();}
 }
 public sealed class P12BossBalancePlayTest:P12BalancePlayTest{protected override bool BossOnly=>true;}
}
#endif
