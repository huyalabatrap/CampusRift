#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    public static class P18LegacySmoke
    {
        public static IEnumerator Run(SkillSet1TestWorld w,Action<bool,string> check,bool failedOnly=false)
        {
            w.Begin();yield return null;var player=w.player;var pool=player.GetComponent<SkillVfxPool>();check(player.GetComponents<Set1SkillRuntime>().Length==7&&SkillCatalog.Instance.skills.Count==21,"Seven Set1 runtimes remain available in21-skill catalog");
            foreach(var s in player.GetComponents<Set1SkillRuntime>())
            {
                if(failedOnly&&!(s is LightningFlashRuntime))continue;
                P18TestSupport.Prepare(w,s is LightningFlashRuntime);s.ApplyRank(1);player.GetComponent<SkillLoadout>().Equip(2,s.Id);bool cast=s.CastAt(w.origin+Vector3.right*6);check(cast,s.Id+" baseline cast");
                if(s is GoldenBellRuntime){var bell=(GoldenBellRuntime)s;var hp=player.GetComponent<PlayerMonsterHealth>();float before=hp.CurrentHealth,shield=bell.ShieldRemaining;var hit=DamageInfo.Create(50,Element.None,DamageSource.Melee,player.transform.position,Vector3.left,w.victims[0].gameObject);hit.ignoreInvulnerability=true;hp.ApplyDamage(hit);check(hp.CurrentHealth==before&&bell.ShieldRemaining<shield,"Baseline bell absorbs actual incoming damage");}
                yield return P18TestSupport.FinishFast(s);bool behavior=s is GoldenBellRuntime?!((GoldenBellRuntime)s).ShieldActive&&!((GoldenBellRuntime)s).Shattered:s is SwordRainRuntime?((SwordRainRuntime)s).SwordsLaunched==30&&((SwordRainRuntime)s).SwordImpacts==30:s.LastHitCount>0;
                check(behavior&&!s.IsCasting&&pool.ExhaustedCount==0&&pool.FiniteState,s.Id+" baseline behavior / expiry / pool");
                if(s is ChainLightningRuntime)check(((ChainLightningRuntime)s).BounceCount==6,"Baseline lightning remains6bounces");
            }
            if(failedOnly)yield break;
            var hand=player.GetComponent<GiantHandRuntime>();P18TestSupport.Prepare(w);hand.ApplyRank(1);check(player.GetComponent<GiantHandSkill>().CastAt(w.origin+Vector3.right*6),"Legacy Giant Hand baseline cast");yield return P18TestSupport.FinishFast(hand);check(player.GetComponent<GiantHandSkill>().LastHitCount>0&&player.GetComponent<SkillMasteryFields>().ActiveCount==0,"Baseline Giant Hand hits without mastery mountain");
            var phantom=player.GetComponent<PhantomRuntime>();P18TestSupport.Prepare(w);phantom.ApplyRank(1);check(player.GetComponent<PhantomDecoySkill>().Cast(Vector3.right)&&player.GetComponent<PhantomDecoySkill>().LiveDecoys==1,"Legacy Phantom baseline launches1actor");yield return P18TestSupport.FinishFast(phantom);
            var wall=player.GetComponent<VoidWallRuntime>();P18TestSupport.Prepare(w);wall.ApplyRank(1);bool placed=wall.QuickCast();var barrier=player.GetComponent<VoidWallSkill>().LastDeployed;check(placed&&barrier.IsSolid&&!barrier.ReflectsProjectiles,"Legacy wall baseline blocks without reflection");barrier?.Dissolve(false);
        }
    }
}
#endif
