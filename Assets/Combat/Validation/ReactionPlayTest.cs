#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.Monsters;
using CampusRift.Enemies;
using CampusRift.Progression;
using CampusRift.UI;
namespace CampusRift.Combat
{
    public sealed class ReactionPlayTest : MonoBehaviour
    {
        [Serializable] public sealed class Report {public string capturedAt,error,mode;public List<string> passed=new List<string>(),failed=new List<string>();public int objectsBefore,objectsAfter,peakPC,peakMobile;}
        public bool smokeOnly=true;
        public bool photoOnly;
        readonly Report report=new Report();SkillSet1TestWorld world;
        MonsterVitality target;StatusEffectHost status;GenerationChainTracker chain;SkillVfxPool pool;
        readonly int[] events=new int[9];int reactionHits;float reactionAmount;DamageSource lastSource;
        void Check(bool ok,string message){(ok?report.passed:report.failed).Add(message);Debug.Log("REACTION QA "+(ok?"PASS ":"FAIL ")+message);Save();}
        void Save(){Directory.CreateDirectory("Artifacts/Reactions");File.WriteAllText("Artifacts/Reactions/"+(photoOnly?"P18Capture":"Validation")+".json",JsonUtility.ToJson(report,true));}
        static bool Close(float actual,float expected)=>Mathf.Abs(actual-expected)<Mathf.Max(.1f,Mathf.Abs(expected)*.002f);
        IEnumerator Start()
        {
            report.capturedAt=DateTime.UtcNow.ToString("o");report.mode=smokeOnly?"smoke":"legacy full";world=new SkillSet1TestWorld{GameplayCamera=true};
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){var routine=stack.Peek();bool more=false;object next=null;try{more=routine.MoveNext();if(more)next=routine.Current;}catch(Exception e){report.error=e.ToString();Check(false,"Exception "+e);break;}if(!more){stack.Pop();continue;}var nested=next as IEnumerator;if(nested!=null){stack.Push(nested);continue;}yield return next;}
            ReactionResolver.ReactionTriggered-=Triggered;MonsterVitality.AnyDamaged-=Damaged;
            try{world.End();}catch(Exception e){Check(false,"restore "+e.Message);}
            Save();File.WriteAllText("Artifacts/Reactions/"+(photoOnly?"P18Capture-DONE":"DONE")+".txt",report.passed.Count+" passed; "+report.failed.Count+" failed; "+report.error);Destroy(gameObject);
        }
        void Triggered(ReactionType type,MonsterVitality victim){events[(int)type]++;}
        void Damaged(MonsterVitality victim,DamageInfo hit){if(hit.source==DamageSource.Reaction){reactionHits++;reactionAmount+=hit.amount;lastSource=hit.source;}}
        void Reset()
        {
            world.Arrange();pool.Clear();DamageNumberPool.Instance.ReactionLabels.Clear();world.player.GetComponent<ReactionFeedback>().Hint.Clear();chain.ResetChain();
            for(int i=0;i<world.victims.Length;i++)
            {var m=world.victims[i];m.defense=0;m.Element=Element.None;m.GetComponent<StatusEffectHost>().Clear();m.ResetVitality();m.GetComponent<MinionMotor>().Place(world.origin+new Vector3(i==0?6:15+i*2,0,i==0?2:8));m.GetComponent<MinionMotor>().Stop();}
            target=world.victims[0];status=target.GetComponent<StatusEffectHost>();Array.Clear(events,0,events.Length);reactionHits=0;reactionAmount=0;
            Physics.SyncTransforms();
        }
        void Place(int index,float distance)
        {var m=world.victims[index];m.GetComponent<MinionMotor>().Place(target.transform.position+Vector3.forward*distance);m.GetComponent<MinionMotor>().Stop();Physics.SyncTransforms();}
        DamageInfo Hit(Element element,float amount=100,bool area=false,DamageSource source=DamageSource.Skill)
        {var hit=DamageInfo.Create(amount,element,source,target.transform.position+Vector3.up,Vector3.right,world.player.gameObject);hit.attackPower=100;hit.isArea=area;hit.skillId="reaction-qa";return hit;}
        StatusType TriggerStatus(ReactionType type)=>type==ReactionType.IceLightning?StatusType.Freeze:type==ReactionType.ElectricFlow?StatusType.Wet:type==ReactionType.FireExplosion?StatusType.Burn:type==ReactionType.Convergence?StatusType.Pulled:StatusType.Stun;
        Element TriggerElement(ReactionType type)=>type==ReactionType.IceLightning||type==ReactionType.ElectricFlow?Element.Loi:type==ReactionType.FireExplosion||type==ReactionType.Convergence?Element.Hoa:Element.Kim;
        IEnumerator Run()
        {
            if(smokeOnly){yield return P18ReactionSmoke.Run(world,Check,photoOnly);yield break;}
            world.Begin();yield return null;pool=world.player.GetComponent<SkillVfxPool>();chain=world.player.GetComponent<GenerationChainTracker>();
            ReactionResolver.ReactionTriggered+=Triggered;MonsterVitality.AnyDamaged+=Damaged;
            Check(ReactionConfig.Current!=null&&ReactionConfig.Current.rules.Length==5,"five data-driven reaction rules installed");
            foreach(ReactionType type in Enum.GetValues(typeof(ReactionType)))
            {
                Reset();target.ApplyDamage(Hit(TriggerElement(type),100,type==ReactionType.Convergence));Check(events[(int)type]==0&&Close(target.Health,9900),type+" requires status");
                Reset();status.Apply(TriggerStatus(type),10);target.ApplyDamage(Hit(Element.Moc,100,false));Check(events[(int)type]==0,type+" requires matching element / area");
                Reset();status.Apply(TriggerStatus(type),10);target.ApplyDamage(Hit(TriggerElement(type),100,type==ReactionType.Convergence,DamageSource.Reaction));Check(events[(int)type]==0,type+" rejects Reaction source");
                Reset();status.Apply(TriggerStatus(type),10);target.ApplyDamage(Hit(TriggerElement(type),100,type==ReactionType.Convergence,DamageSource.Environment));Check(events[(int)type]==0,type+" rejects Environment source");
                Reset();status.Apply(TriggerStatus(type),10);target.ApplyDamage(Hit(TriggerElement(type),100,type==ReactionType.Convergence));Check(events[(int)type]==1,type+" valid condition fires exactly once");
                status.Apply(TriggerStatus(type),10);target.ApplyDamage(Hit(TriggerElement(type),100,type==ReactionType.Convergence));Check(events[(int)type]==1,type+" per-target ICD blocks immediate hit");
                yield return new WaitForSeconds(.9f);status.Apply(TriggerStatus(type),10);target.ApplyDamage(Hit(TriggerElement(type),100,type==ReactionType.Convergence));Check(events[(int)type]==1,type+" ICD still blocks at 0.9 s");
                yield return new WaitForSeconds(.13f);status.Apply(TriggerStatus(type),10);target.ApplyDamage(Hit(TriggerElement(type),100,type==ReactionType.Convergence));Check(events[(int)type]==2,type+" ICD reopens after 1 s");
                Check(ReactionConfig.Current.Rule(type).transient!=null&&ReactionConfig.Current.Rule(type).body!=null&&ReactionConfig.Current.Rule(type).tail!=null,type+" three distinct SFX layers bound");
            }
            Reset();Place(1,2.9f);Place(2,3.1f);status.Apply(StatusType.Freeze,2.5f);target.ApplyDamage(Hit(Element.Loi));
            Check(Close(target.Health,9750),"IceLightning primary 100 + 150% = 250");Check(Close(world.victims[1].Health,9850)&&Close(world.victims[2].Health,10000),"IceLightning splash inside 3 m, outside excluded");
            Check(!status.Has(StatusType.Freeze)&&lastSource==DamageSource.Reaction,"Freeze consumed; bonus hits carry Reaction source");
            Reset();status.Apply(StatusType.Freeze,2.5f);status.Apply(StatusType.Chill,2.5f,.5f);target.ApplyDamage(Hit(Element.Loi));Check(events[0]==1&&events[1]==0,"Freeze takes precedence over Chill");
            Reset();target.resistHardControl=true;status.Apply(StatusType.Freeze,2.5f);target.ApplyDamage(Hit(Element.Loi));Check(events[0]==0&&events[1]==1&&!status.Has(StatusType.Freeze),"boss Freeze converts to Chill and ElectricFlow");
            Reset();Place(1,4.9f);Place(2,5.1f);status.Apply(StatusType.Wet,3);target.ApplyDamage(Hit(Element.Loi));
            Check(status.Has(StatusType.Shock)&&world.victims[1].GetComponent<StatusEffectHost>().Has(StatusType.Shock)&&!world.victims[2].GetComponent<StatusEffectHost>().Has(StatusType.Shock),"ElectricFlow Shock range 5 m");
            Check(Close(status.Remaining(StatusType.Shock),.5f)&&Close(world.victims[1].Health,10000),"ElectricFlow Shock 0.5 s, no invented splash damage");yield return new WaitForSeconds(.53f);Check(!status.Has(StatusType.Shock),"Shock expires");
            Reset();status.Apply(StatusType.Chill,3,.5f);target.ApplyDamage(Hit(Element.Loi));Check(events[1]==1,"Chill also triggers ElectricFlow");
            Reset();Place(1,3.9f);Place(2,4.1f);status.Apply(StatusType.Burn,3);world.victims[1].GetComponent<StatusEffectHost>().Apply(StatusType.Burn,3);target.ApplyDamage(Hit(Element.Hoa));
            Check(Close(target.Health,9820)&&Close(world.victims[1].Health,9920)&&Close(world.victims[2].Health,10000),"FireExplosion 80% attack, radius 4 m");Check(events[2]==1&&world.victims[1].GetComponent<ReactionResolver>().TriggerCount==0,"Burning neighbor cannot explode recursively");
            Reset();status.Apply(StatusType.Pulled,3);target.ApplyDamage(Hit(Element.Hoa,100,false));Check(events[3]==0&&Close(target.Health,9900),"Convergence excludes single-target hits");
            target.ApplyDamage(Hit(Element.Hoa,100,true));Check(events[3]==1&&Close(target.Health,9760),"Convergence +40% area hit");
            Reset();status.Apply(StatusType.Pulled,3);status.Apply(StatusType.Burn,3);target.ApplyDamage(Hit(Element.Hoa,100,true));Check(events[2]==1&&events[3]==1&&Close(target.Health,9780),"different reaction types have independent ICDs on same target");
            Reset();target.defense=.6f;status.Apply(StatusType.Stun,3);target.ApplyDamage(Hit(Element.Kim));
            Check(status.Has(StatusType.ArmorBreak)&&Close(status.Remaining(StatusType.ArmorBreak),8)&&Close(status.Magnitude(StatusType.ArmorBreak),.3f),"ArmorShatter 8 s / -0.30 defense");Check(Close(target.Health,9930),"ArmorShatter applies before defense to triggering Metal hit");
            yield return new WaitForSeconds(8.03f);Check(!status.Has(StatusType.ArmorBreak),"ArmorBreak expires after 8 s");target.ApplyDamage(Hit(Element.Kim));Check(Close(target.Health,9890),"defense restored after ArmorBreak");
            Reset();status.Apply(StatusType.Freeze,3);target.ApplyDamage(Hit(Element.Loi));target.ResetVitality();status.Apply(StatusType.Freeze,3);target.ApplyDamage(Hit(Element.Loi));Check(events[0]==2,"pool ResetVitality clears all reaction cooldowns");
            yield return Chains();yield return RealCombos();yield return PoolChecks();
            var old=ProfileService.Instance.Data;var clone=JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(old));Check(clone.seenReactions.Count>0,"first encounter markers survive JSON roundtrip");
            var disk=new JsonProfileStore(Path.GetFullPath("Artifacts/Reactions/profile-test/campusrift-v2.json"));disk.Save(clone);var read=disk.Load();Check(read.seenReactions.Count==clone.seenReactions.Count,"first encounter markers survive real profile disk store");
            var feedback=world.player.GetComponent<ReactionFeedback>();Check(feedback.AudioLayers>0&&feedback.AudioLayers%3==0,"layered SFX actually scheduled");
            Check(world.player.GetComponent<SkillImpact>().LastHitStop>=.06f&&Time.timeScale==1,"reaction local hit-stop 60–80 ms, global timeScale unchanged");
        }
        IEnumerator Chains()
        {
            Reset();var spirit=world.player.GetComponent<SpiritPower>();spirit.Refill();spirit.TrySpend(80);float before=spirit.Current;int completions=chain.Completions;
            Check(Close(chain.Record(Element.Moc),1)&&chain.Count==1&&chain.Next==Element.Hoa,"generation first element and expected next color");Check(Close(chain.Record(Element.Hoa),1)&&chain.Count==2,"generation second valid element");
            Check(Close(chain.Record(Element.Tho),1.3f)&&chain.Count==3&&chain.Completions==completions+1,"Moc→Hoa→Tho grants +30% only on third cast");Check(Close(spirit.Current-before,20),"generation restores exactly 20 spirit");
            chain.Record(Element.Kim);Check(chain.Count==1,"fourth cast begins new chain, no overlapping completion");
            chain.ResetChain();chain.Record(Element.Moc);chain.Record(Element.Kim);Check(chain.Count==1&&chain.Used(0)==Element.Kim,"wrong order resets and starts with latest element");
            chain.Record(Element.Loi);Check(chain.Count==0,"non-generating Lightning resets chain");
            chain.Record(Element.Kim);chain.Record(Element.Thuy);yield return new WaitForSeconds(6.05f);Check(chain.Count==0,"generation resets after 6 s window");chain.Record(Element.Moc);Check(chain.Count==1,"late third element cannot complete expired chain");
            chain.ResetChain();chain.Record(Element.Hoa);target.ApplyDamage(Hit(Element.Tho,10,false,DamageSource.Melee));Check(chain.Count==1,"basic attacks do not count as skill casts");
            var ice=world.player.GetComponent<IceSealRuntime>();ice.ResetCooldownForValidation();spirit.Refill();world.player.GetComponent<SkillLoadout>().Equip(2,ice.Id);Check(ice.BeginAim()&&chain.Count==1,"aim preview does not count");ice.Cancel();Check(chain.Count==1,"canceled aim does not count");
            spirit.TrySpend(spirit.Current);Check(!ice.CastAt(world.origin+Vector3.right*6)&&chain.Count==1,"failed resource payment does not count");
            var settings=SettingsManager.Instance.Current.Copy();settings.ReduceSkillFlashes=true;SettingsManager.Instance.Apply(settings,false);world.player.GetComponent<SkillImpact>().Pulse(.6f);yield return null;Check(SettingsManager.Instance.Current.ReduceSkillFlashes,"reduce flash flag accepted by shared impact and impulse");settings.ReduceSkillFlashes=false;SettingsManager.Instance.Apply(settings,false);
        }
        IEnumerator RealCombos()
        {
            Reset();world.Arrange();var spirit=world.player.GetComponent<SpiritPower>();var ice=world.player.GetComponent<IceSealRuntime>();var bolt=world.player.GetComponent<ChainLightningRuntime>();
            foreach(var m in world.victims)m.GetComponent<StatusEffectHost>().Clear();ice.ResetCooldownForValidation();bolt.ResetCooldownForValidation();spirit.Refill();world.player.GetComponent<SkillLoadout>().Equip(0,ice.Id);world.player.GetComponent<SkillLoadout>().Equip(1,bolt.Id);
            Check(ice.CastAt(world.origin+Vector3.right*8),"real scene Ice Seal casts");yield return new WaitForSeconds(.55f);Check(world.victims[0].GetComponent<StatusEffectHost>().Has(StatusType.Freeze),"real Ice Seal produces Freeze");
            spirit.Refill();Check(bolt.CastAt(world.origin+Vector3.right*8),"real scene Lightning Sword casts");yield return new WaitForSeconds(1.45f);Check(events[0]>0,"real Ice Seal→Lightning Sword triggers IceLightning");while(ice.IsCasting)yield return null;
            Reset();world.Arrange();var hole=world.player.GetComponent<BlackHoleRuntime>();var fire=world.player.GetComponent<FireLotusRuntime>();hole.ResetCooldownForValidation();fire.ResetCooldownForValidation();spirit.Refill();
            world.player.GetComponent<SkillLoadout>().Equip(0,hole.Id);world.player.GetComponent<SkillLoadout>().Equip(1,fire.Id);
            Vector3 center=world.origin+new Vector3(7,0,2);Check(hole.CastAt(center),"real Black Hole casts");yield return new WaitForSeconds(.35f);spirit.Refill();Check(fire.CastAt(center),"real Fire Lotus casts during pull");
            yield return new WaitForSeconds(2.2f);Check(events[3]>0,"real Black Hole→Fire Lotus triggers Convergence while Pulled");while(hole.IsCasting||fire.IsCasting)yield return null;
            Reset();world.Arrange();var hand=world.player.GetComponent<GiantHandSkill>();var rain=world.player.GetComponent<SwordRainRuntime>();
            typeof(GiantHandSkill).GetField("readyAt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(hand,0f);spirit.Refill();Check(hand.CastAt(world.victims[0].transform.position),"real Giant Hand casts");while(hand.IsCasting)yield return null;
            Check(world.victims[0].GetComponent<StatusEffectHost>().Has(StatusType.Stun),"Giant Hand impact now exposes actual Stun");
            rain.ResetCooldownForValidation();spirit.Refill();rain.CastAt(world.victims[0].transform.position);rain.SwordImpact(world.victims[0].transform.position);Check(events[4]>0,"Giant Hand→real Rain sword impact triggers ArmorShatter");while(rain.IsCasting)yield return null;
            Reset();world.Arrange();fire.ResetCooldownForValidation();rain.ResetCooldownForValidation();typeof(GiantHandSkill).GetField("readyAt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(hand,0f);spirit.Refill();
            chain.ResetChain();fire.CastAt(world.origin+Vector3.right*7);hand.CastAt(world.victims[0].transform.position);float spiritBefore=spirit.Current;rain.CastAt(world.origin+Vector3.right*7);
            Check(chain.Count==3&&Close(rain.CastDamageMultiplier,1.3f)&&Close(spiritBefore-spirit.Current,rain.SpiritCost-20),"actual Fire→Hand→Rain successful casts boost third skill and refund 20");
            float hp=world.victims[6].Health;rain.SwordImpact(world.victims[6].transform.position);float expected=world.player.GetComponent<PlayerStats>().Attack*world.player.GetComponent<PlayerStats>().DamageDealt*.4f*rain.EffectMultiplier*1.3f;
            Check(Close(hp-world.victims[6].Health,expected),"third Rain impact uses cast snapshot +30% damage");
            Check(Close(fire.CastDamageMultiplier,1)&&Close(hand.GetComponent<GiantHandRuntime>().CastDamageMultiplier,1),"first two casts retain unboosted damage snapshots");while(fire.IsCasting||rain.IsCasting||hand.IsCasting)yield return null;
            Reset();var bell=world.player.GetComponent<GoldenBellRuntime>();bell.ResetCooldownForValidation();spirit.Refill();chain.Record(Element.Hoa);chain.Record(Element.Tho);bell.CastAt(world.player.transform.position);
            Check(chain.Count==3&&Close(bell.CastDamageMultiplier,1.3f)&&Close(bell.ShieldCapacity,world.player.GetComponent<PlayerMonsterHealth>().maxHealth*.4f*bell.EffectMultiplier),"generation bonus affects damage, does not inflate third-cast shield");
            float before=target.Health;bell.Absorb(10,DamageInfo.Create(10,Element.None,DamageSource.Melee,world.player.transform.position,Vector3.left,target.gameObject));Check(Close(before-target.Health,2.6f),"third-cast bell reflection receives damage bonus");while(bell.IsCasting)yield return null;
        }
        IEnumerator PoolChecks()
        {
            Reset();yield return new WaitForSeconds(3);pool.ResetMetrics();report.objectsBefore=world.player.GetComponentsInChildren<Transform>(true).Length+DamageNumberPool.Instance.GetComponentsInChildren<Transform>(true).Length;
            int created=pool.CreatedCount;
            for(int i=0;i<10;i++)
            {
                foreach(var m in world.victims){m.ResetVitality();m.GetComponent<StatusEffectHost>().Clear();}
                target=world.victims[0];status=target.GetComponent<StatusEffectHost>();status.Apply(StatusType.Freeze,3);target.ApplyDamage(Hit(Element.Loi));
                var t=world.victims[2];t.GetComponent<StatusEffectHost>().Apply(StatusType.Burn,3);var hit=Hit(Element.Hoa);t.ApplyDamage(hit);
                t=world.victims[4];t.GetComponent<StatusEffectHost>().Apply(StatusType.Pulled,3);hit.isArea=true;t.ApplyDamage(hit);
                yield return new WaitForSeconds(1.1f);
            }
            report.peakPC=pool.PeakParticles;yield return new WaitForSeconds(3);
            report.objectsAfter=world.player.GetComponentsInChildren<Transform>(true).Length+DamageNumberPool.Instance.GetComponentsInChildren<Transform>(true).Length;
            Check(report.objectsBefore==report.objectsAfter&&created==pool.CreatedCount,"no new pooled objects after ten triple-reaction bursts");Check(pool.ExhaustedCount==0&&pool.ActiveCount==0&&pool.FiniteState,"pool returns idle, finite, no exhaustion");Check(DamageNumberPool.Instance.ReactionLabels.Merged>0,"crowd labels merge same type within 0.2 s");
            world.Mode(true);pool.ResetMetrics();foreach(ReactionType type in Enum.GetValues(typeof(ReactionType))){Reset();status.Apply(TriggerStatus(type),3);target.ApplyDamage(Hit(TriggerElement(type),100,type==ReactionType.Convergence));yield return new WaitForSeconds(.2f);Check(events[(int)type]==1,type+" gameplay also works in Mobile control mode");}report.peakMobile=pool.PeakParticles;Check(report.peakMobile<=400,"mobile VFX particle budget <=400");
            chain.ResetChain();chain.Record(Element.Hoa);chain.Record(Element.Tho);chain.Record(Element.Kim);yield return new WaitForSeconds(.18f);
            var mobileUI=world.player.GetComponent<GenerationChainUI>();bool updated=false;
            foreach(var text in world.player.GetComponentsInChildren<TMPro.TMP_Text>())if(text.name=="Chain countdown"&&text.text.Contains("20"))updated=true;
            Check(mobileUI!=null&&mobileUI.isActiveAndEnabled&&updated,"mobile generation HUD keeps updating while PC skill bar is hidden");
            world.Mode(false);
        }
    }
}
#endif
