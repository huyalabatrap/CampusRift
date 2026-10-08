#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.Monsters;
using CampusRift.Enemies;
namespace CampusRift.Combat
{
    public static class P18ReactionSmoke
    {
        public static IEnumerator Run(SkillSet1TestWorld w,Action<bool,string> check,bool photoOnly=false)
        {
            w.Begin();yield return null;var player=w.player;var pool=player.GetComponent<SkillVfxPool>();int[] events=new int[9];Action<ReactionType,MonsterVitality> listener=(type,target)=>events[(int)type]++;ReactionResolver.ReactionTriggered+=listener;
            try
            {
                check(ReactionConfig.Current.rules.Length==9,"Nine data-driven reactions installed");
                for(int i=0;i<(photoOnly?0:5);i++)
                {
                    P18TestSupport.Prepare(w);var m=w.victims[0];var status=m.GetComponent<StatusEffectHost>();var type=(ReactionType)i;var trigger=i==0?StatusType.Freeze:i==1?StatusType.Wet:i==2?StatusType.Burn:i==3?StatusType.Pulled:StatusType.Stun;var element=i<2?Element.Loi:i<4?Element.Hoa:Element.Kim;
                    var hit=DamageInfo.Create(100,element,DamageSource.Skill,m.transform.position,Vector3.right,player.gameObject);hit.attackPower=100;hit.isArea=i==3;hit.skillId="reaction-smoke";int before=events[i];m.ApplyDamage(hit);check(events[i]==before,type+" requires condition");status.Apply(trigger,3,i==2?10:0,player.gameObject);m.ApplyDamage(hit);check(events[i]==before+1,type+" existing reaction fires");yield return new WaitForSeconds(.15f);
                }
                foreach(var rule in ReactionConfig.Current.rules)check(rule.transient!=null&&rule.body!=null&&rule.tail!=null,rule.id+" three CC0 SFX layers");
                P18TestSupport.Prepare(w);var burn=w.victims[0];burn.GetComponent<MinionMotor>().Place(w.origin+new Vector3(2,0,.65f));w.victims[1].GetComponent<MinionMotor>().Place(w.origin+new Vector3(3,0,.65f));w.victims[2].GetComponent<MinionMotor>().Place(w.origin+new Vector3(2,0,5.6f));burn.GetComponent<StatusEffectHost>().Apply(StatusType.Burn,3,20,player.gameObject);var wind=player.GetComponent<KunpengSpeedRuntime>();wind.ApplyRank(1);wind.CastAt(w.origin);player.Dash(Vector3.right,5,1);yield return new WaitForSeconds(.7f);
                check(events[5]>0&&w.victims[1].GetComponent<StatusEffectHost>().Has(StatusType.Burn)&&!w.victims[2].GetComponent<StatusEffectHost>().Has(StatusType.Burn),"Wildfire: actual Kunpeng ghost spreads Burn nearby, excludes outside4m");yield return P18TestSupport.Capture("phong-hoa-lieu-nguyen");
                P18TestSupport.Prepare(w);for(int i=0;i<3;i++)w.victims[i].ApplyDamage(DamageInfo.Create(20000,Element.None,DamageSource.Skill,w.victims[i].transform.position,Vector3.right,player.gameObject));var soul=player.GetComponent<SoulSummonRuntime>();soul.ApplyRank(1);soul.CastAt(w.origin);var array=player.GetComponent<ImmortalSwordArrayRuntime>();array.ApplyRank(1);player.GetComponent<SpiritPower>().Refill();array.CastAt(w.origin+Vector3.right*5);yield return new WaitForSeconds(.25f);SoulAlly ally=null;foreach(var a in HealingAllies.Active)if(a is SoulAlly){ally=(SoulAlly)a;break;}
                var target=w.victims[6];float hp=target.Health;if(ally!=null)ally.Strike(target);check(ally!=null&&ally.SwordSoul&&events[6]>0&&P18TestSupport.Close(hp-target.Health,ally.Damage*1.5f),"Sword Soul: allied attack deals150% inside formation");if(photoOnly)yield return new WaitForSeconds(.55f);yield return P18TestSupport.Capture("kiem-hon");if(ally!=null){ally.Motor.Place(w.origin+Vector3.right*18);yield return null;check(!ally.SwordSoul,"Sword Soul removed outside formation");}
                P18TestSupport.Prepare(w);var drain=player.GetComponent<NorthernDrainRuntime>();drain.ApplyRank(1);var health=player.GetComponent<PlayerMonsterHealth>();health.Revive(.01f,0);drain.CastAt(w.origin);yield return new WaitForSeconds(.55f);float ordinary=drain.HealingApplied;check(P18TestSupport.Close(ordinary,drain.LastDamage*.5f),"Northern baseline healing50% actual damage");
                P18TestSupport.Prepare(w);health.Revive(.01f,0);var bell=player.GetComponent<GoldenBellRuntime>();bell.ApplyRank(1);bell.CastAt(w.origin);drain.ApplyRank(1);drain.CastAt(w.origin);yield return new WaitForSeconds(.55f);check(events[7]>0&&P18TestSupport.Close(drain.HealingApplied,drain.LastDamage)&&P18TestSupport.Close(drain.HealingApplied,ordinary*2),"Guarded Absorption doubles actual healing with live shield");yield return P18TestSupport.Capture("ho-the-hap-nguyen");
                P18TestSupport.Prepare(w,true);var domain=player.GetComponent<DomainRuntime>();domain.ApplyRank(1);domain.CastAt(w.origin);var dragon=player.GetComponent<DragonPalmRuntime>();dragon.ApplyRank(1);dragon.CastAt(w.origin+Vector3.right*12);check(events[8]>0&&P18TestSupport.Close(dragon.CastDamageMultiplier,1.2f),"Domain Resonance snapshots120% at actual cast");yield return new WaitForSeconds(.4f);yield return P18TestSupport.Capture("lanh-dia-cong-huong");w.PlacePlayer(w.origin+Vector3.right*18);yield return P18TestSupport.FinishFast(dragon);check(dragon.LastHitCount>0&&P18TestSupport.Close(dragon.LastDamage/dragon.LastHitCount,player.GetComponent<PlayerStats>().Attack*3.5f*1.2f),"Resonant damage stays120% after caster exits");
                var renewal=player.GetComponent<WoodRenewalRuntime>();renewal.ApplyRank(1);renewal.ReadyOnRestEquip();player.GetComponent<SpiritPower>().Refill();renewal.CastAt(player.transform.position);check(P18TestSupport.Close(renewal.CastDamageMultiplier,1),"Skill cast outside domain gets no resonance bonus");
                P18TestSupport.Prepare(w);Time.timeScale=6;yield return new WaitForSeconds(3);Time.timeScale=1;check(pool.FiniteState&&pool.ExhaustedCount==0&&player.GetComponent<ReactionFeedback>().VisualCount>=(photoOnly?4:9),"Reactions retain feedback / finite pooled state");
            }
            finally{ReactionResolver.ReactionTriggered-=listener;}
        }
    }
}
#endif
