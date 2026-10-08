#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Monsters;
using CampusRift.Enemies;
using CampusRift.Progression;
using CampusRift.UI;
namespace CampusRift.Skills
{
    public sealed class SkillSet1PlayTest:MonoBehaviour
    {
        [Serializable] public sealed class Measurement {public string skill;public int objectsBefore,objectsAfter,casts,peakPC,peakMobile;public float[] damageByRank=new float[5];}
        [Serializable] public sealed class Report {public string capturedAt,mode;public List<string> passed=new List<string>(),failed=new List<string>();public List<Measurement> measurements=new List<Measurement>();}
        Report report=new Report();SkillSet1TestWorld world;
        public bool resume;
        public bool smokeOnly=true;
        public bool failedOnly;
        public bool LegacyFailedOnly;
        public string[] FailureLabels;
        Set1SkillRuntime[] skills;SkillVfxPool pool;SpiritPower spirit;PlayerStats stats;SkillLoadout loadout;CampusInput input;
        readonly List<DamageInfo> hits=new List<DamageInfo>();readonly List<MonsterVitality> targets=new List<MonsterVitality>();
        readonly DangerZone[] zones=new DangerZone[64];
        string currentId;float maxStatus;bool shockObserved,freezeObserved,bossChill,burnObserved,pulledObserved;
        const string Output="Artifacts/Skills/";
        void Check(bool ok,string label){if(ok){report.failed.Remove(label);if(!report.passed.Contains(label))report.passed.Add(label);}else{report.passed.Remove(label);if(!report.failed.Contains(label))report.failed.Add(label);}Save();Debug.Log("P10 "+(ok?"PASS ":"FAIL ")+label);}
        void Save(){Directory.CreateDirectory(Output);File.WriteAllText(Output+"SkillSet1.json",JsonUtility.ToJson(report,true));}
        IEnumerator Start()
        {
            if(resume&&File.Exists(Output+"SkillSet1.json"))report=JsonUtility.FromJson<Report>(File.ReadAllText(Output+"SkillSet1.json"));
            report.capturedAt=DateTime.UtcNow.ToString("o");report.mode=smokeOnly?"smoke":"legacy full";world=new SkillSet1TestWorld{GameplayCamera=true};
            var stack=new Stack<IEnumerator>();stack.Push(LegacyFailedOnly?FailedLegacy():Run());while(stack.Count>0){var run=stack.Peek();bool next=false;object value=null;try{next=run.MoveNext();if(next)value=run.Current;}catch(Exception e){Check(false,"Exception: "+e);break;}if(!next){stack.Pop();continue;}var nested=value as IEnumerator;if(nested!=null)stack.Push(nested);else yield return value;}
            MonsterVitality.AnyDamaged-=Hit;try{world.End();}catch(Exception e){Check(false,"Restoration: "+e.Message);}
            Save();File.WriteAllText(Output+"SkillSet1-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);
        }
        void Hit(MonsterVitality victim,DamageInfo info){if(info.skillId!=currentId||info.source!=DamageSource.Skill)return;hits.Add(info);targets.Add(victim);}
        void Update()
        {
            if(world==null||world.player==null)return;
            if(UIStateManager.Instance.State!=UIState.Gameplay)UIStateManager.Instance.EnterScene(true);
            world.Look(90,14);
            for(int i=0;i<world.victims.Length;i++){var s=world.victims[i].GetComponent<StatusEffectHost>();
                if(s.Has(StatusType.Shock)){shockObserved=true;maxStatus=Mathf.Max(maxStatus,s.Remaining(StatusType.Shock));}
                if(s.Has(StatusType.Freeze)){freezeObserved=true;maxStatus=Mathf.Max(maxStatus,s.Remaining(StatusType.Freeze));}
                if(world.victims[i].resistHardControl&&s.Has(StatusType.Chill)&&Mathf.Approximately(s.Magnitude(StatusType.Chill),.5f)&&!s.Has(StatusType.Freeze))bossChill=true;
                if(s.Has(StatusType.Burn)){burnObserved=true;maxStatus=Mathf.Max(maxStatus,s.Remaining(StatusType.Burn));}if(s.Has(StatusType.Pulled))pulledObserved=true;
            }
        }
        void Prepare(Set1SkillRuntime skill,int rank,bool mobile=false)
        {
            world.Mode(mobile);world.Arrange(skill is LightningFlashRuntime);if(skill is ChainLightningRuntime)world.ChainLayout();
            pool.Clear();pool.ResetMetrics();skill.ApplyRank(rank);skill.ResetCooldownForValidation();spirit.Refill();loadout.Equip(2,skill.Id);
            currentId=skill.Id;hits.Clear();targets.Clear();maxStatus=0;shockObserved=freezeObserved=bossChill=burnObserved=pulledObserved=false;
        }
        IEnumerator Settle(Set1SkillRuntime skill)
        {float until=Time.realtimeSinceStartup+10;while(skill.IsCasting&&Time.realtimeSinceStartup<until)yield return null;Check(!skill.IsCasting,skill.Id+" cast finishes");yield return new WaitForSeconds(3.2f);}
        static bool Close(float a,float b)=>Mathf.Abs(a-b)<=Mathf.Max(.12f,b*.002f);

        bool Selected(string label) => FailureLabels!=null&&Array.IndexOf(FailureLabels,label)>=0;
        IEnumerator FailedLegacy()
        {
            // Existing checks only. Catalog/upgrade setup may populate all ranks,
            // but only the originally failed price and JSON assertions are run.
            world.Begin();yield return null;yield return new WaitForFixedUpdate();
            skills=world.player.GetComponents<Set1SkillRuntime>();pool=world.player.GetComponent<SkillVfxPool>();
            spirit=world.player.GetComponent<SpiritPower>();stats=world.player.GetComponent<PlayerStats>();
            loadout=world.player.GetComponent<SkillLoadout>();input=world.player.GetComponent<CampusInput>();
            MonsterVitality.AnyDamaged+=Hit;
            if(Selected("catalog has ten MVP skills"))Check(SkillCatalog.Instance.skills.Count==21,"catalog has21 skills");
            var profile=ProfileService.Instance;profile.Data.wallet.linhThach=SkillCatalog.Instance.skills.Count*3050+1000;
            foreach(var def in SkillCatalog.Instance.skills)
            {
                int start=profile.Wallet.Balance;bool prices=true;int[] expected={150,400,900,1600};
                for(int r=0;r<4;r++)
                {
                    int realm=Mathf.Min(6,def.unlockRealm+r+1);int balance=profile.Wallet.Balance;
                    profile.Data.cultivation.realm=realm-1;profile.UseTransient(profile.Data);
                    prices&=!profile.Skills.TryUpgrade(def)&&profile.Wallet.Balance==balance;
                    profile.Data.cultivation.realm=realm;profile.UseTransient(profile.Data);
                    prices&=profile.Skills.NextCost(def)==expected[r]&&profile.Skills.RequiredRealm(def)==realm&&profile.Skills.TryUpgrade(def)&&profile.Skills.GetRank(def.id)==r+2;
                }
                prices&=profile.Wallet.Balance==start-3050&&!profile.Skills.TryUpgrade(def);
                var label=def.id+" rank prices / realm cap / max guard";if(Selected(label))Check(prices,label);
            }
            if(Selected("ten ranks survive profile JSON roundtrip"))
            {
                var saved=JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(profile.Data));bool persisted=true;
                foreach(var entry in saved.skills.ranks)persisted&=entry.count==5;
                Check(persisted&&saved.skills.ranks.Count==21,"21 ranks survive profile JSON roundtrip");
            }
            profile.Data.skills.ranks.Clear();profile.Data.cultivation.realm=6;profile.Data.cultivation.tier=5;profile.UseTransient(profile.Data);
            foreach(var skill in skills)
            {
                for(int rank=1;rank<=5;rank++)
                {
                    string label=skill.Id+" rank"+rank+" real damage / element / finite";
                    bool flash=rank==1&&skill is LightningFlashRuntime&&Selected("flash 10m / pierces / Shock 0.5s");
                    bool rain=rank==1&&skill is SwordRainRuntime&&Selected("rain 30 swords / 30 separate landings / 30 warm bodies");
                    if(!Selected(label)&&!flash&&!rain)continue;
                    P18TestSupport.Prepare(world,skill is LightningFlashRuntime);P18TestSupport.SetRank(skill,rank);
                    loadout.Equip(2,skill.Id);currentId=skill.Id;hits.Clear();targets.Clear();
                    maxStatus=0;shockObserved=freezeObserved=bossChill=burnObserved=pulledObserved=false;
                    if(skill is FireLotusRuntime)world.victims[0].Element=Element.Kim;
                    if(skill is ChainLightningRuntime)for(int i=0;i<7;i++)world.victims[i].Element=Element.Am;
                    // Wait for fixture colliders/controller to settle before its first dash.
                    yield return new WaitForFixedUpdate();yield return null;
                    bool cast=skill.CastAt(world.origin+Vector3.right*8);
                    if(skill is SwordRainRuntime&&Selected(label))
                    {
                        while(((SwordRainRuntime)skill).SwordsLaunched==0&&skill.IsCasting)yield return null;
                        foreach(var sword in pool.GetComponentsInChildren<FlyingSword>(true))if(sword.gameObject.activeSelf&&sword.State==FlyingSword.Mode.Rain)
                        {var landing=(Vector3)typeof(FlyingSword).GetField("rainPoint",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(sword);world.victims[0].GetComponent<MinionMotor>().Place(landing);world.victims[0].GetComponent<MinionMotor>().Stop();break;}
                    }
                    float until=Time.realtimeSinceStartup+10;while(skill.IsCasting&&Time.realtimeSinceStartup<until)yield return null;
                    float percent=skill is LightningFlashRuntime?1.8f:skill is FireLotusRuntime?4.5f:skill is IceSealRuntime?1.2f:skill is ChainLightningRuntime?2:skill is BlackHoleRuntime?2.5f:.4f;
                    bool damage=cast&&hits.Count>0,finite=true;
                    for(int j=0;j<hits.Count;j++)
                    {
                        float pct=(skill is FireLotusRuntime&&skill.Mastered?1.5f:percent)*(skill is ChainLightningRuntime?Mathf.Pow(.9f,j):1);
                        float elem=skill is FireLotusRuntime&&targets[j].Element==Element.Kim?1.5f:skill is ChainLightningRuntime?1.5f:1;
                        float expected=stats.Attack*stats.DamageDealt*pct*(1+.12f*(rank-1))*elem;
                        damage&=Close(hits[j].amount,expected)&&hits[j].element==skill.definition.element;
                        finite&=!float.IsNaN(hits[j].amount)&&!float.IsInfinity(hits[j].amount);
                    }
                    if(skill is FireLotusRuntime&&skill.Mastered)
                    {
                        damage&=((FireLotusRuntime)skill).FlowerCount==3;
                        // Three 150% impacts retain the original total450% requirement.
                        foreach(var target in world.victims)
                        {
                            float sum=0;int count=0;for(int j=0;j<hits.Count;j++)if(targets[j]==target){sum+=hits[j].amount;count++;}
                            float elem=target.Element==Element.Kim?1.5f:1;
                            damage&=count==3&&Close(sum,stats.Attack*stats.DamageDealt*4.5f*(1+.12f*(rank-1))*elem);
                        }
                    }
                    if(Selected(label))Check(damage&&finite,label);
                    if(flash)Check(hits.Count>=3&&Close(world.player.LastDashDistance,10)&&shockObserved&&maxStatus>.4f&&maxStatus<=.501f,"flash 10m / pierces / Shock 0.5s");
                    if(rain){var r=(SwordRainRuntime)skill;Check(r.SwordsLaunched==30&&r.SwordImpacts==30&&r.CreatedSwords==50,"rain 30 swords / 30 separate landings / 50 warm bodies");}
                    yield return new WaitForSeconds(3.2f);
                }
            }
        }

        IEnumerator Run()
        {
            if(smokeOnly){yield return P18LegacySmoke.Run(world,Check,failedOnly);yield break;}
            world.Begin();yield return null;
            skills=world.player.GetComponents<Set1SkillRuntime>();pool=world.player.GetComponent<SkillVfxPool>();spirit=world.player.GetComponent<SpiritPower>();stats=world.player.GetComponent<PlayerStats>();loadout=world.player.GetComponent<SkillLoadout>();input=world.player.GetComponent<CampusInput>();
            MonsterVitality.AnyDamaged+=Hit;
            Check(skills.Length==7,"seven new runtimes installed");Check(SkillCatalog.Instance.skills.Count==21,"catalog has21 skills");
            var profile=ProfileService.Instance;profile.Data.wallet.linhThach=SkillCatalog.Instance.skills.Count*3050+1000;
            foreach(var def in SkillCatalog.Instance.skills)
            {
                int start=profile.Wallet.Balance;bool prices=true;
                int[] expected={150,400,900,1600};for(int r=0;r<4;r++)
                {int realm=Mathf.Min(6,def.unlockRealm+r+1);int balance=profile.Wallet.Balance;profile.Data.cultivation.realm=realm-1;profile.UseTransient(profile.Data);prices&=!profile.Skills.TryUpgrade(def)&&profile.Wallet.Balance==balance;profile.Data.cultivation.realm=realm;profile.UseTransient(profile.Data);prices&=profile.Skills.NextCost(def)==expected[r]&&profile.Skills.RequiredRealm(def)==realm&&profile.Skills.TryUpgrade(def)&&profile.Skills.GetRank(def.id)==r+2;}
                prices&=profile.Wallet.Balance==start-3050&&!profile.Skills.TryUpgrade(def);Check(prices,def.id+" rank prices / realm cap / max guard");
            }
            var saved=JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(profile.Data));bool persisted=true;foreach(var r in saved.skills.ranks)persisted&=r.count==5;Check(persisted&&saved.skills.ranks.Count==21,"21 ranks survive profile JSON roundtrip");
            profile.Data.cultivation.realm=0;profile.Data.cultivation.tier=1;profile.UseTransient(profile.Data);
            var locked=SkillCatalog.Instance.Find("van-kiem-quyet");Check(!profile.Skills.TryUpgrade(locked),"locked skill upgrade rejected");
            profile.Data.skills.ranks.Clear();profile.Data.cultivation.realm=6;profile.Data.cultivation.tier=5;profile.UseTransient(profile.Data);
            foreach(var skill in skills)
            {
                var m=resume?report.measurements.Find(x=>x.skill==skill.Id):null;
                bool retestRanks=m==null||skill is ChainLightningRuntime||skill is SwordRainRuntime;
                if(m==null){m=new Measurement{skill=skill.Id,objectsBefore=pool.GetComponentsInChildren<Transform>(true).Length};report.measurements.Add(m);}
                float expectedPercent=skill is LightningFlashRuntime?1.8f:skill is FireLotusRuntime?4.5f:skill is IceSealRuntime?1.2f:skill is ChainLightningRuntime?2:skill is BlackHoleRuntime?2.5f:skill is SwordRainRuntime?.4f:0;
                float baseCD=skill is LightningFlashRuntime?8:skill is FireLotusRuntime?20:skill is IceSealRuntime?14:skill is ChainLightningRuntime?12:skill is GoldenBellRuntime?30:skill is BlackHoleRuntime?22:18;
                float cost=skill is LightningFlashRuntime?20:skill is FireLotusRuntime?45:skill is IceSealRuntime?30:skill is ChainLightningRuntime?35:skill is GoldenBellRuntime?40:50;
                for(int rank=1;retestRanks&&rank<=5;rank++)
                {
                    Prepare(skill,rank);if(skill is IceSealRuntime)world.victims[6].resistHardControl=true;
                    if(skill is FireLotusRuntime)world.victims[0].Element=Element.Kim;if(skill is ChainLightningRuntime)for(int i=0;i<7;i++)world.victims[i].Element=Element.Am;
                    yield return null;float resource=spirit.Current;bool cast=skill.CastAt(world.origin+Vector3.right*8);m.casts++;
                    Check(cast&&Close(resource-spirit.Current,cost),skill.Id+" rank"+rank+" PC cast / spirit "+cost);
                    Check(Close(skill.CooldownRemaining,stats.ScaleCooldown(baseCD*(1-.05f*(rank-1)))),skill.Id+" rank"+rank+" actual cooldown");
                    if(skill is GoldenBellRuntime)Check(Close(((GoldenBellRuntime)skill).ShieldCapacity,world.player.GetComponent<PlayerMonsterHealth>().maxHealth*.4f*(1+.12f*(rank-1))),skill.Id+" rank"+rank+" shield capacity");
                    yield return new WaitForSeconds(.05f);
                    if(skill.definition.castType==CastType.Aimed)Check(DangerZoneRegistry.CopyActive(zones)>0,skill.Id+" active danger registry");
                    if(skill is SwordRainRuntime)
                    {
                        while(((SwordRainRuntime)skill).SwordsLaunched==0&&skill.IsCasting)yield return null;
                        foreach(var sword in pool.GetComponentsInChildren<FlyingSword>(true))if(sword.gameObject.activeSelf&&sword.State==FlyingSword.Mode.Rain)
                        {Vector3 landing=(Vector3)typeof(FlyingSword).GetField("rainPoint",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(sword);world.victims[0].GetComponent<MinionMotor>().Place(landing);world.victims[0].GetComponent<MinionMotor>().Stop();break;}
                    }
                    float until=Time.realtimeSinceStartup+10;while(skill.IsCasting&&Time.realtimeSinceStartup<until)yield return null;
                    Check(!skill.IsCasting,skill.Id+" rank"+rank+" finishes");
                    if(expectedPercent>0)
                    {
                        bool damage=hits.Count>0,finite=true;for(int j=0;j<hits.Count;j++)
                        {
                            float pct=(skill is FireLotusRuntime&&skill.Mastered?1.5f:expectedPercent)*(skill is ChainLightningRuntime?Mathf.Pow(.9f,j):1);float elem=skill is FireLotusRuntime&&targets[j].Element==Element.Kim?1.5f:skill is ChainLightningRuntime?1.5f:1;
                            float expected=stats.Attack*stats.DamageDealt*pct*(1+.12f*(rank-1))*elem;
                            damage&=Close(hits[j].amount,expected)&&hits[j].element==skill.definition.element;finite&=!float.IsNaN(hits[j].amount)&&!float.IsInfinity(hits[j].amount);
                        }
                        m.damageByRank[rank-1]=hits.Count>0?hits[0].amount:0;Check(damage&&finite,skill.Id+" rank"+rank+" real damage / element / finite");
                    }
                    if(rank==1)
                    {
                        if(skill is LightningFlashRuntime){var f=(LightningFlashRuntime)skill;Check(hits.Count>=3&&Close(world.player.LastDashDistance,10)&&shockObserved&&maxStatus>.4f&&maxStatus<=.501f,"flash 10m / pierces / Shock 0.5s");Check(f.SlashStartedAt-f.DashStoppedAt>=.15f&&f.SlashStartedAt-f.DashStoppedAt<=.25f,"flash delayed slash after stopping");}
                        if(skill is FireLotusRuntime)Check(burnObserved&&maxStatus>2.85f&&maxStatus<=3.01f,"fire Burn 3s and Kim multiplier observed");
                        if(skill is IceSealRuntime)Check(freezeObserved&&maxStatus>2.35f&&maxStatus<=2.51f&&bossChill,"ice Freeze 2.5s / boss Chill50%");
                        if(skill is ChainLightningRuntime){bool unique=targets.Count==6;for(int a=0;a<targets.Count;a++)for(int b=a+1;b<targets.Count;b++)unique&=targets[a]!=targets[b];Check(unique,"chain six unique targets / diminishing damage / Am x1.5");}
                        if(skill is BlackHoleRuntime){bool nav=((BlackHoleRuntime)skill).NavigationFailures==0;for(int i=0;i<7;i++){NavMeshHit n;var agent=world.victims[i].GetComponent<NavMeshAgent>();nav&=agent.isOnNavMesh&&agent.updatePosition&&NavMesh.SamplePosition(world.victims[i].transform.position,out n,.5f,NavMesh.AllAreas)&&world.victims[i].GetComponent<MinionMotor>().enabled;}Check(pulledObserved&&nav,"black hole Pulled / restores motor-agent / on NavMesh");}
                        if(skill is SwordRainRuntime){var rain=(SwordRainRuntime)skill;Check(rain.SwordsLaunched==30&&rain.SwordImpacts==30&&rain.CreatedSwords==50,"rain 30 swords / 30 separate landings / 50 warm bodies");}
                    }
                    m.peakPC=Mathf.Max(m.peakPC,pool.PeakParticles);yield return new WaitForSeconds(3.2f);
                    Check(pool.ActiveCount==0&&DangerZoneRegistry.CopyActive(zones)==0,skill.Id+" rank"+rank+" VFX / zones released");
                }
                Prepare(skill,1,true);yield return null;int before=skill.CastCount;
                if(skill.definition.castType==CastType.Aimed)
                {
                    bool aim=input.BeginAim(CampusAction.Skill3);input.DragAim(new Vector2(-.35f,.25f));yield return null;
                    var indicator=world.player.GetComponent<GroundAimIndicator>();Vector3 first=indicator.Point;input.DragAim(new Vector2(.35f,.25f));yield return null;
                    Check(aim&&(indicator.Point-first).sqrMagnitude>.05f,skill.Id+" mobile drag moves ground aim");
                    float beforeCancel=spirit.Current;input.EndAim(true);Check(skill.CastCount==before&&Close(beforeCancel,spirit.Current)&&skill.CooldownRemaining==0,skill.Id+" cancel spends nothing");
                    input.BeginAim(CampusAction.Skill3);input.DragAim(new Vector2(0,-.35f));yield return null;input.EndAim(false);
                }
                else skill.QuickCast();
                Check(skill.CastCount==before+1,skill.Id+" mobile confirms cast");m.casts++;yield return Settle(skill);m.peakMobile=pool.PeakParticles;
                for(int c=6;retestRanks&&c<10;c++){Prepare(skill,1);yield return null;Check(skill.CastAt(world.origin+Vector3.right*8),skill.Id+" repeat cast "+c);m.casts++;yield return Settle(skill);m.peakPC=Mathf.Max(m.peakPC,pool.PeakParticles);}
                m.objectsAfter=pool.GetComponentsInChildren<Transform>(true).Length;
                Check(m.objectsBefore==m.objectsAfter&&pool.ExhaustedCount==0,skill.Id+" pool stable after ten casts / no exhaustion");Check(m.peakPC<=1500&&m.peakMobile<=400,skill.Id+" particle budgets PC/mobile");
                Save();
            }
            // Actual incoming player damage, rather than calling the shield directly.
            var bell=world.player.GetComponent<GoldenBellRuntime>();Prepare(bell,1);yield return null;bell.CastAt(world.origin);
            var health=world.player.GetComponent<PlayerMonsterHealth>();float hp=health.CurrentHealth,capacity=bell.ShieldRemaining,enemyHP=world.victims[0].Health;
            var melee=DamageInfo.Create(100,Element.None,DamageSource.Melee,world.player.transform.position,Vector3.left,world.victims[0].gameObject);melee.ignoreInvulnerability=true;health.ApplyDamage(melee);
            Check(Close(health.CurrentHealth,hp)&&Close(capacity-bell.ShieldRemaining,DamageCalculator.AfterDefense(100,health.DamageReduction))&&Close(enemyHP-world.victims[0].Health,20),"bell absorbs before HP / reflects raw20% melee");
            capacity=bell.ShieldRemaining;health.ApplyDamage(DamageInfo.Create(100,Element.Hoa,DamageSource.Environment,world.player.transform.position,Vector3.zero));
            Check(Close(capacity-bell.ShieldRemaining,DamageCalculator.AfterDefense(40,health.DamageReduction)),"bell skyfire resistance60%");yield return Settle(bell);Check(!bell.ShieldActive&&!bell.Shattered,"bell natural 6s expiry differs from break");
            // Three old skills are selectable alongside the seven new ones through the preparation model.
            LoadoutUI.Level=Levels.LevelCatalog.Instance.Get(1);var prep=LoadoutUI.Instance;
            if(prep==null){var go=new GameObject("P10 real preparation fixture",typeof(RectTransform));go.transform.SetParent(FindAnyObjectByType<Canvas>().transform,false);go.AddComponent<LoadoutUI>();yield return null;prep=LoadoutUI.Instance;}
            bool selectable=prep!=null;
            foreach(var def in SkillCatalog.Instance.skills){if(prep!=null){prep.Apply(new[]{def.id,"","",""});selectable&=prep.Slots[0]==def.id;}selectable&=loadout.Equip(2,def.id)&&loadout.Get(2).Id==def.id;}
            Check(selectable,"all ten MVP selectable in DEV preparation / runtime loadout");
            world.Arrange();var phantom=world.player.GetComponent<PhantomDecoySkill>();var runtime=world.player.GetComponent<PhantomRuntime>();runtime.ApplyRank(3);spirit.Refill();
            currentId="anh-phan-than";hits.Clear();targets.Clear();bool launched=phantom.Cast(Vector3.right);yield return null;
            if(launched){world.victims[0].GetComponent<MinionMotor>().Place(phantom.Decoy.transform.position+Vector3.right);phantom.Decoy.Dissolve(false);}
            Check(launched&&phantom.LastDissolveHits>0&&hits.Count>0&&Close(hits[0].amount,stats.Attack*stats.DamageDealt*1.5f*1.24f),"Phantom rank3 dissolution explosion 150% / real damage");
        }
    }
}
#endif
