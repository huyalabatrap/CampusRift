#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.UI;
namespace CampusRift.Skills
{
    // One short pass per mechanic; no rank matrix, repeated benchmark or balance bot.
    public sealed class SkillSet2PlayTest:MonoBehaviour
    {
        [Serializable]public sealed class Report{public string capturedAt,mode="smoke",error;public List<string> passed=new List<string>(),failed=new List<string>();public int catalog,baseArtifacts,allyIncoming;}
        Report report=new Report();public bool failedOnly;SkillSet1TestWorld w;SkillVfxPool pool;SkillLoadout loadout;SpiritPower spirit;PlayerStats stats;PlayerMonsterHealth hp;
        bool ended;readonly List<EnemyInstance> extras=new List<EnemyInstance>();
        void Check(bool ok,string label){report.passed.Remove(label);report.failed.Remove(label);(ok?report.passed:report.failed).Add(label);Save();Debug.Log("P18 "+(ok?"PASS ":"FAIL ")+label);}
        void Save(){Directory.CreateDirectory("Artifacts/Skills/Set2");File.WriteAllText("Artifacts/Skills/Set2/SkillSet2.json",JsonUtility.ToJson(report,true));}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);if(failedOnly)report=JsonUtility.FromJson<Report>(File.ReadAllText("Artifacts/Skills/Set2/SkillSet2.json"));else report.capturedAt=DateTime.UtcNow.ToString("o");w=new SkillSet1TestWorld{GameplayCamera=true};var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){var r=stack.Peek();bool more=false;object next=null;try{more=r.MoveNext();if(more)next=r.Current;}catch(Exception e){report.error=e.ToString();Check(false,"Exception: "+e.Message);break;}if(!more){stack.Pop();continue;}var nested=next as IEnumerator;if(nested!=null)stack.Push(nested);else yield return next;}
            MonsterVitality.AnyDamaged-=Incoming;foreach(var e in extras)if(e!=null)EnemyPool.Instance?.Release(e);Time.timeScale=1;
            if(!ended)try{w.player.GetComponent<SkillMasteryFields>().Clear();w.End();}catch(Exception e){Check(false,"Restore: "+e.Message);}
            Progression.ProfileService.Instance.EndTransient();Save();File.WriteAllText("Artifacts/Skills/Set2/SkillSet2-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed; "+report.error);Destroy(gameObject);
        }
        void Incoming(MonsterVitality target,DamageInfo hit){if(target.Faction==CombatFaction.Ally&&hit.attacker!=null&&hit.attacker.GetComponent<EnemyInstance>()!=null)report.allyIncoming++;}
        void Prepare(SkillRuntime skill,bool line=false){P18TestSupport.Prepare(w,line);P18TestSupport.SetRank(skill,4);loadout.Equip(2,skill.Id);}
        IEnumerator Run()
        {
            w.Begin();yield return null;pool=w.player.GetComponent<SkillVfxPool>();loadout=w.player.GetComponent<SkillLoadout>();spirit=w.player.GetComponent<SpiritPower>();stats=w.player.GetComponent<PlayerStats>();hp=w.player.GetComponent<PlayerMonsterHealth>();MonsterVitality.AnyDamaged+=Incoming;
            if(failedOnly){if(report.failed.Contains("True Fire12ticks then persistent fire wall"))yield return NewMastery(w.player.GetComponent<TrueFireRuntime>());if(report.failed.Contains("Avatar sweep / damage reduction / camera / mastery control immunity"))yield return NewMastery(w.player.GetComponent<MartialAvatarRuntime>());if(report.failed.Contains("Mastered wall reflects a real projectile back into enemy"))yield return OldMastery(w.player.GetComponent<VoidWallRuntime>());w.End();ended=true;if(report.failed.Contains("Real preparation loadout displays/selects all21 skills"))yield return UiProof();yield break;}
            report.catalog=SkillCatalog.Instance.skills.Count;Check(report.catalog==21&&w.player.GetComponents<Set2SkillRuntime>().Length==11,"21 definitions / 11 Set2 runtimes / every icon present");
            bool icons=true,mastery=true,gate=true;foreach(var s in w.player.GetComponents<SkillRuntime>()){icons&=s.definition!=null&&s.definition.icon!=null;mastery&=s.definition.descriptionVN.Contains("Viên Mãn:");s.ApplyRank(3);gate&=!s.Mastered;s.ApplyRank(4);gate&=s.Mastered;}
            Check(icons&&mastery&&gate,"All21 icons/descriptions; mastery gate switches at rank4");
            foreach(var s in w.player.GetComponents<Set2SkillRuntime>())
            {
                string path="Artifacts/Skills/Set2/"+s.Id+"-smoke.json";var result=File.Exists(path)?JsonUtility.FromJson<P18SkillSmoke.Result>(File.ReadAllText(path)):null;
                bool ok=result!=null&&result.rank==1&&result.cast&&result.finished&&result.finite&&result.poolExhaustion==0;if(ok)report.baseArtifacts++;Check(ok,s.Id+" recorded base smoke / photograph");
            }
            var dragon=w.player.GetComponent<DragonPalmRuntime>();Prepare(dragon,true);w.victims[0].resistHardControl=true;Vector3 boss=w.victims[0].transform.position;dragon.ApplyRank(1);Check(dragon.CastAt(w.origin+Vector3.right*12),"Dragon base cast");yield return P18TestSupport.FinishFast(dragon);
            bool nav=true;foreach(var m in w.victims)nav&=m.GetComponent<MinionMotor>().OnMesh;Check(dragon.LastHitCount>1&&nav&&(w.victims[0].transform.position-boss).sqrMagnitude<.02f,"Dragon pierces multiple; boss stays; all targets remain on NavMesh");
            foreach(var s in w.player.GetComponents<Set2SkillRuntime>())yield return NewMastery(s);
            foreach(var s in w.player.GetComponents<SkillRuntime>())if(!(s is Set2SkillRuntime))yield return OldMastery(s);
            // Touch routing uses CampusInput.SkillDrag and real GroundAimIndicator.
            Prepare(dragon);dragon.ApplyRank(1);w.Mode(true);var input=w.player.GetComponent<CampusRift.Controls.CampusInput>();Check(input.BeginAim(SkillLoadout.SlotAction(2)),"Mobile round skill button enters aim");input.DragAim(new Vector2(.45f,.4f));yield return null;Vector3 aimed=w.player.GetComponent<GroundAimIndicator>().Point;Check(input.SkillDrag.sqrMagnitude>.1f&&input.EndAim(false)&&dragon.IsCasting,"Mobile drag steers aim and release casts");yield return P18TestSupport.FinishFast(dragon);w.Mode(false);
            Prepare(w.player.GetComponent<NorthernDrainRuntime>());var drain=w.player.GetComponent<NorthernDrainRuntime>();drain.ApplyRank(1);drain.CastAt(w.origin);yield return new WaitForSeconds(.15f);bool dodged=w.player.GetComponent<DodgeAbility>().TryDodge();yield return null;Check(dodged&&drain.Interrupted&&!drain.IsCasting,"Northern Drain interrupted by actual dodge");
            bool suggestions=true;for(int level=7;level<=10;level++)foreach(var id in LoadoutUI.SuggestedIds(level))suggestions&=SkillCatalog.Instance.Find(id)!=null;Check(suggestions,"Suggested sets of levels7–10 resolve to full catalog");
            foreach(var s in w.player.GetComponents<SkillRuntime>())s.ReadyOnRestEquip();w.player.GetComponent<SkillMasteryFields>().Clear();Time.timeScale=6;yield return new WaitForSeconds(3);Time.timeScale=1;Check(pool.FiniteState&&pool.ExhaustedCount==0&&pool.Shapes.ExhaustedCount==0,"Pooled geometry finite, no slot exhaustion");w.End();ended=true;
            yield return UiProof();
        }
        IEnumerator NewMastery(Set2SkillRuntime s)
        {
            Prepare(s,s is DragonPalmRuntime||s is KunpengSpeedRuntime);
            if(s is NorthernDrainRuntime||s is WoodRenewalRuntime)hp.Revive(.01f,0);
            if(s is WoodRenewalRuntime)w.player.GetComponent<PlayerEnemyControl>().Chill(20,.5f);
            P18ConcealmentProbe hidden=null;if(s is SpiritSightRuntime)hidden=w.victims[0].gameObject.AddComponent<P18ConcealmentProbe>();
            if(s is HeavenThunderRuntime){w.victims[0].GetComponent<MinionMotor>().Place(w.origin+Vector3.right*8);w.victims[1].GetComponent<MinionMotor>().Place(w.origin+Vector3.right*8);w.victims[0].Element=Element.Am;}
            if(s is SoulSummonRuntime)for(int i=0;i<5;i++)w.victims[i].ApplyDamage(DamageInfo.Create(20000,Element.None,DamageSource.Skill,w.victims[i].transform.position,Vector3.right,w.player.gameObject));
            float before=spirit.Current,cd=s.CooldownDuration;bool cast=s.CastAt(w.origin+Vector3.right*8);Check(cast&&P18TestSupport.Close(before-spirit.Current,s.SpiritCost)&&s.CooldownRemaining>cd-.1f,s.Id+" mastery cast spends resource / starts cooldown");
            float wait=s is TrueFireRuntime?3.4f:s is HeavenThunderRuntime?1.43f:s is SoulSummonRuntime?.35f:.7f;
            if(s is KunpengSpeedRuntime){w.victims[0].GetComponent<MinionMotor>().Place(w.origin+new Vector3(2,0,.65f));w.player.Dash(Vector3.right,5,1);}
            if(s is MartialAvatarRuntime){w.PlacePlayer(w.origin+Vector3.right*3);w.player.GetComponent<PlayerCombat>().TrySwing();}
            yield return new WaitForSeconds(wait);
            bool representative=s is DragonPalmRuntime||s is TrueFireRuntime||s is NorthernDrainRuntime||s is KunpengSpeedRuntime||s is HeavenThunderRuntime||s is SpiritSightRuntime||s is SoulSummonRuntime||s is DomainRuntime||s is MartialAvatarRuntime;
            if(representative)yield return P18TestSupport.Capture(s.Id+"-mastery");
            if(s is TrueFireRuntime){Debug.Log("P18 FIRE ticks="+((TrueFireRuntime)s).Ticks+" fields="+w.player.GetComponent<SkillMasteryFields>().ActiveCount+" rank="+s.rank);Check(((TrueFireRuntime)s).Ticks==12&&w.player.GetComponent<SkillMasteryFields>().ActiveCount>0,"True Fire12ticks then persistent fire wall");}
            if(s is NorthernDrainRuntime){var d=(NorthernDrainRuntime)s;Check(d.Ticks==1&&P18TestSupport.Close(d.HealingApplied,d.LastDamage*.5f)&&d.SpiritDrained>0,"Drain heals50% actual damage / mastery returns spirit");w.player.GetComponent<PlayerEnemyControl>().Stun(.4f);yield return null;Check(d.Interrupted&&!d.IsCasting,"Drain stun interrupts channel");w.player.GetComponent<PlayerEnemyControl>().Clear();}
            if(s is KunpengSpeedRuntime){w.player.CancelDash();float energy=w.player.Energy;bool dodge=w.player.GetComponent<DodgeAbility>().TryDodge();Check(dodge&&P18TestSupport.Close(energy,w.player.Energy)&&stats.MoveSpeedBonus<=PlayerStats.TotalMoveCap&&s.LastHitCount>0,"Kunpeng ghost deals damage; capped speed / zero stamina dodge");}
            if(s is WoodRenewalRuntime)Check(P18TestSupport.Close(w.player.GetComponent<PlayerEnemyControl>().Multiplier,1),"Wood mastery cleanses slow");
            if(s is SpiritSightRuntime){Check(((SpiritSightRuntime)s).MarkedCount==3&&P18TestSupport.Close(((SpiritSightRuntime)s).DamageBonus(w.victims[0],true),1.625f),"Sight marks3 weakest and multiplies critical/weakpoint bonuses");Check(hidden.Calls>0&&hidden.Until>Time.time,"Sight calls P19 concealment reveal contract");Destroy(hidden);}
            if(s is HeavenThunderRuntime)Check(P18TestSupport.Close(10000-w.victims[0].Health,(10000-w.victims[1].Health)*1.5f),"Thunder deals exactly50% more against Am without double scaling");
            if(s is ImmortalSwordArrayRuntime){w.PlacePlayer(w.origin+Vector3.right*8);yield return null;Check(((ImmortalSwordArrayRuntime)s).PlayerBuff,"Sword Array critical buff inside");w.PlacePlayer(w.origin+Vector3.right*18);yield return null;Check(!((ImmortalSwordArrayRuntime)s).PlayerBuff,"Sword Array buff removed on exit");}
            if(s is SoulSummonRuntime)yield return Allies((SoulSummonRuntime)s);
            if(s is DomainRuntime){var d=(DomainRuntime)s;Check(P18TestSupport.Close(w.victims[0].GetComponent<StatusEffectHost>().SpeedMultiplier,.6f)&&DomainRuntime.Suppresses(w.victims[0].transform.position),"Mastered domain slows40% / suppresses abilities inside");float c=s.CooldownRemaining,t=Time.time;yield return new WaitForSeconds(.3f);Check(P18TestSupport.Close(c-s.CooldownRemaining,2*(Time.time-t)),"Domain cooldown runs twice as fast inside");w.PlacePlayer(w.origin+Vector3.right*18);w.victims[0].GetComponent<MinionMotor>().Place(w.origin+Vector3.right*18);yield return null;Check(!DomainRuntime.Suppresses(w.victims[0].transform.position)&&P18TestSupport.Close(w.victims[0].GetComponent<StatusEffectHost>().SpeedMultiplier,1),"Domain effects excluded outside");c=s.CooldownRemaining;t=Time.time;yield return new WaitForSeconds(.3f);Check(P18TestSupport.Close(c-s.CooldownRemaining,Time.time-t),"Cooldown normal outside domain");}
            if(s is MartialAvatarRuntime){var a=(MartialAvatarRuntime)s;w.player.GetComponent<PlayerEnemyControl>().Stun(2);Debug.Log("P18 AVATAR hits="+s.LastHitCount+" immune="+a.ControlImmune+" stunned="+w.player.GetComponent<PlayerEnemyControl>().Stunned+" taken="+stats.DamageTaken+" camera="+w.player.cameraDistance);Check(s.LastHitCount>1&&a.ControlImmune&&!w.player.GetComponent<PlayerEnemyControl>().Stunned&&P18TestSupport.Close(stats.DamageTaken,.7f)&&w.player.cameraDistance>=7.5f,"Avatar sweep / damage reduction / camera / mastery control immunity");}
            yield return P18TestSupport.FinishFast(s);Check(!s.IsCasting&&pool.FiniteState&&pool.ExhaustedCount==0,s.Id+" mastery completes with finite pool");
            if(s is WoodRenewalRuntime)Check(P18TestSupport.Close(((WoodRenewalRuntime)s).HealingApplied,hp.maxHealth*.25f*s.EffectMultiplier),"Wood total heal25% maxHP times rank power");
            if(s is HeavenThunderRuntime)Check(((HeavenThunderRuntime)s).Bolts==8,"Mastered Heavenly Thunder8bolts");
            if(s is ImmortalSwordArrayRuntime)Check(((ImmortalSwordArrayRuntime)s).Ticks==30,"Mastered Sword Array15s /30ticks");
            if(s is SoulSummonRuntime)Check(((SoulSummonRuntime)s).LiveAllies==0&&((SoulSummonRuntime)s).AlliedHits>0,"Soul allies attack enemies and dissolve by20s");
            if(s is MartialAvatarRuntime)Check(!((MartialAvatarRuntime)s).Active&&P18TestSupport.Close(stats.DamageTaken,1)&&w.player.cameraDistance<7.5f,"Avatar restores stats / basic-attack mode / camera");
        }
        IEnumerator Allies(SoulSummonRuntime soul)
        {
            Check(soul.Raised==5&&soul.LiveAllies==5,"Soul mastery raises5 normal corpses");SoulAlly ally=null;foreach(var a in HealingAllies.Active)if(a is SoulAlly){ally=(SoulAlly)a;break;}
            if(ally==null){Check(false,"Soul healing fixture");yield break;}Check(ally.Body.Faction==CombatFaction.Ally&&!MonsterVitality.Active.Contains(ally.Body)&&ally.GetComponent<EnemyInstance>()==null&&P18TestSupport.Close(ally.MaxHealth,10000*.6f*soul.EffectMultiplier),"Soul faction /60% corpse stats / no sword-intent instance");
            float original=ally.Body.Health;bool friendly=ally.Body.ApplyDamage(DamageInfo.Create(30,Element.None,DamageSource.Melee,ally.transform.position,Vector3.right,w.player.gameObject));Check(!friendly&&P18TestSupport.Close(ally.Body.Health,original),"Player cannot damage allied spirit");
            ally.Body.ApplyDamage(DamageInfo.Create(1000,Element.None,DamageSource.Melee,ally.transform.position,Vector3.left,w.victims[6].gameObject));float damaged=ally.Body.Health;w.PlacePlayer(ally.transform.position-Vector3.right*2);var wood=w.player.GetComponent<WoodRenewalRuntime>();wood.ApplyRank(1);wood.ReadyOnRestEquip();spirit.Refill();wood.CastAt(w.player.transform.position);yield return new WaitForSeconds(1.1f);Check(ally.Body.Health>damaged&&wood.AllyHeals>0,"Wood field heals a real allied spirit");
            int intent=SkyBeast.SwordIntent.Instance!=null?SkyBeast.SwordIntent.Instance.DefeatedWeight:0;ally.Body.ApplyDamage(DamageInfo.Create(100000,Element.None,DamageSource.Melee,ally.transform.position,Vector3.left,w.victims[6].gameObject));Check(!ally.Alive&&(SkyBeast.SwordIntent.Instance==null||SkyBeast.SwordIntent.Instance.DefeatedWeight==intent),"Allied death awards no second sword intent");
            w.PlacePlayer(w.origin);var enemy=w.victims[6].GetComponent<EnemyInstance>();enemy.Brain.enabled=true;enemy.Brain.Configure(enemy);int incoming=report.allyIncoming;yield return new WaitForSeconds(3.4f);Check(report.allyIncoming>incoming,"Hostile minion attacks allied spirit");enemy.Brain.enabled=false;enemy.Motor.Stop();
        }
        IEnumerator OldMastery(SkillRuntime s)
        {
            Prepare(s,s is LightningFlashRuntime);if(s is ChainLightningRuntime)
            {var template=w.victims[0].GetComponent<EnemyInstance>().archetype;for(int i=0;i<2;i++){var e=EnemyPool.Instance.Spawn(template,w.origin+new Vector3(8+i,0,1),EnemyScaling.Default,false);e.Vitality.SetMaxHealth(10000,true);e.Vitality.Element=Element.None;e.Brain.enabled=false;e.Motor.Stop();extras.Add(e);}}
            if(s is GoldenBellRuntime)w.PlacePlayer(w.origin+Vector3.right*2);
            bool cast=s is GiantHandRuntime?w.player.GetComponent<GiantHandSkill>().CastAt(w.origin+Vector3.right*6):s is PhantomRuntime?w.player.GetComponent<PhantomDecoySkill>().Cast(Vector3.right):s is VoidWallRuntime?s.QuickCast():((Set1SkillRuntime)s).CastAt(w.origin+Vector3.right*8);Check(cast,s.Id+" mastery cast");
            if(s is VoidWallRuntime)
            {var wall=w.player.GetComponent<VoidWallSkill>().LastDeployed;if(wall!=null){var m=w.victims[0];var normal=wall.transform.forward;m.GetComponent<MinionMotor>().Place(wall.transform.position+normal*3);m.GetComponent<MinionMotor>().Stop();for(int i=1;i<w.victims.Length;i++){w.victims[i].GetComponent<MinionMotor>().Place(w.origin+Vector3.right*(18+i));w.victims[i].GetComponent<MinionMotor>().Stop();}Physics.SyncTransforms();float before=m.Health;var bolt=EnemyProjectilePool.Ensure().Fire(m.transform.position+Vector3.up*1.1f,-normal,12,40,Element.None,m.gameObject);yield return new WaitForSeconds(1f);Debug.Log("P18 WALL master="+wall.ReflectsProjectiles+" reflected="+bolt.Reflected+" damage="+(before-m.Health)+" bolt="+bolt.transform.position+" enemy="+m.transform.position);Check(wall.ReflectsProjectiles&&bolt.Reflected&&m.Health<before,"Mastered wall reflects a real projectile back into enemy");wall.Dissolve(false);}yield break;}
            if(s is PhantomRuntime){Check(w.player.GetComponent<PhantomDecoySkill>().LiveDecoys==2,"Phantom mastery launches2 real actors");}
            if(s is FireLotusRuntime){yield return new WaitForSeconds(.5f);Check(((FireLotusRuntime)s).FlowerCount==3,"Fire Lotus mastery splits into3 smaller flowers");}
            if(s is GoldenBellRuntime){var bell=(GoldenBellRuntime)s;float raw=(bell.ShieldRemaining+1)/Mathf.Max(.01f,1-hp.DamageReduction);var hit=DamageInfo.Create(raw,Element.None,DamageSource.Melee,w.player.transform.position,Vector3.left,w.victims[0].gameObject);hit.ignoreInvulnerability=true;hp.ApplyDamage(hit);Check(bell.Shattered&&bell.MasteryExplosionHits>0,"Bell breaks and explosion hits nearby enemies");}
            if(s is BlackHoleRuntime){var b=EnemyProjectilePool.Ensure().Fire(w.origin+Vector3.right*7+Vector3.up*1.8f,Vector3.forward,1,10,Element.None,w.victims[0].gameObject);yield return new WaitForSeconds(.7f);Check(!b.Active,"Mastered Black Hole consumes an actual projectile");}
            yield return P18TestSupport.FinishFast(s);
            if(s is GiantHandRuntime){Check(w.player.GetComponent<SkillMasteryFields>().ActiveCount>0,"Giant Hand leaves blocking Five Finger Mountain");Time.timeScale=6;yield return new WaitForSeconds(5.5f);Time.timeScale=1;Check(w.player.GetComponent<SkillMasteryFields>().ActiveCount==0,"Mountain collider/obstacle releases after5s");}
            if(s is LightningFlashRuntime){var f=(LightningFlashRuntime)s;float remaining=f.CooldownRemaining,energy=spirit.Current;bool again=f.CastAt(w.origin+Vector3.right*8);Check(again&&P18TestSupport.Close(energy,spirit.Current)&&P18TestSupport.Close(remaining,f.CooldownRemaining),"Lightning Flash second free dash preserves first cooldown");yield return P18TestSupport.FinishFast(f);Check(!f.ExtraReady,"Flash mastery has exactly2 casts");}
            if(s is IceSealRuntime)Check(w.player.GetComponent<SkillMasteryFields>().ActiveCount>0&&w.victims[0].GetComponent<StatusEffectHost>().Has(StatusType.Chill),"Shattered Ice leaves real3s Chill fields");
            if(s is ChainLightningRuntime){Check(((ChainLightningRuntime)s).BounceCount==9,"Mastered lightning bounces9 times across9 real enemies");foreach(var e in extras)EnemyPool.Instance.Release(e);extras.Clear();}
            if(s is SwordRainRuntime)Check(((SwordRainRuntime)s).SwordsLaunched==50&&((SwordRainRuntime)s).SwordImpacts==50&&pool.RainSwordCount==50,"Mastered Sword Rain50 pooled launches/impacts");
            Check(!P18TestSupport.Casting(s)&&pool.ExhaustedCount==0,s.Id+" mastery completes / no exhausted VFX slots");
        }
        IEnumerator UiProof()
        {
            Progression.ProfileService.Instance.UseTransient(new Progression.ProfileData{cultivation=new Progression.CultivationData{realm=6,tier=5},tutorial=new TutorialProgress{skipHub=true,skipCombat=true,skipFire=true}});GameSceneManager.Instance.LoadMainMenu();float deadline=Time.realtimeSinceStartup+30;while(GameSceneManager.Instance.IsLoading||HubUI.Instance==null){if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Hub proof");yield return null;}yield return null;UIStateManager.Instance.OpenHub();HubUI.Instance.Select(HubUI.Tab.Skills);yield return P18TestSupport.Capture("cong-phap-21");
            int rows=0;foreach(var rt in HubUI.Instance.ContentRect.GetComponentsInChildren<RectTransform>(true))if(rt.name.StartsWith("Skill "))rows++;Check(rows==21,"Real skill book has21 scrollable cards");LoadoutUI.Level=Levels.LevelCatalog.Instance.Get(10);UIStateManager.Instance.OpenLoadout();yield return P18TestSupport.Capture("loadout-21");rows=0;foreach(var rt in LoadoutUI.Instance.GetComponentsInChildren<RectTransform>(true))if(rt.name.StartsWith("Skill ")&&rt.name!="Skill List")rows++;bool selectable=true;foreach(var d in SkillCatalog.Instance.skills){LoadoutUI.Instance.Apply(new[]{d.id,"","",""});selectable&=LoadoutUI.Instance.Slots[0]==d.id;}Check(rows==21&&selectable,"Real preparation loadout displays/selects all21 skills");
        }
    }
}
#endif
