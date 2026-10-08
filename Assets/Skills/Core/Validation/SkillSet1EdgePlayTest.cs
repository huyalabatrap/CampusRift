#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Progression;
using CampusRift.UI;
namespace CampusRift.Skills
{
    public sealed class SkillSet1EdgePlayTest:MonoBehaviour
    {
        [Serializable] sealed class Report {public List<string> passed=new List<string>(),failed=new List<string>();}
        readonly Report report=new Report();SkillSet1TestWorld world;SpiritPower spirit;SkillVfxPool pool;
        public bool IceExpiryOnly;
        readonly DangerZone[] zones=new DangerZone[64];
        void Check(bool ok,string label){(ok?report.passed:report.failed).Add(label);File.WriteAllText("Artifacts/Skills/SkillSet1-Edges.json",JsonUtility.ToJson(report,true));}
        void Update(){if(world!=null&&world.player!=null&&UIStateManager.Instance.State!=UIState.Gameplay)UIStateManager.Instance.EnterScene(true);}
        void Prep(Set1SkillRuntime s){world.Arrange();pool.Clear();spirit.Refill();s.ResetCooldownForValidation();s.ApplyRank(1);world.player.GetComponent<TargetLock>().Set(null);}
        static Transform VisualRig(MonsterVitality victim) { var skin=victim.GetComponentInChildren<SkinnedMeshRenderer>(); var rig=skin.rootBone; while(rig.parent!=null&&rig.parent!=victim.transform)rig=rig.parent; return rig; }
        void Place(int i,Vector3 p){world.victims[i].GetComponent<MinionMotor>().Place(p);world.victims[i].GetComponent<MinionMotor>().Stop();}
        IEnumerator FailedIceExpiry()
        {
            var ice=world.player.GetComponent<IceSealRuntime>();Prep(ice);
            Place(0,world.origin+Vector3.right*4);Place(1,world.origin+Vector3.right*5);world.victims[1].resistHardControl=true;
            var freeze=world.victims[0].GetComponent<StatusEffectHost>();var chill=world.victims[1].GetComponent<StatusEffectHost>();
            float started=-1,freezeDuration=-1,chillDuration=-1;
            Action<StatusType,bool> onFreeze=(type,active)=>{if(type==StatusType.Freeze&&active){started=Time.time;freezeDuration=freeze.Remaining(type);}};
            Action<StatusType,bool> onChill=(type,active)=>{if(type==StatusType.Chill&&active)chillDuration=chill.Remaining(type);};
            freeze.Changed+=onFreeze;chill.Changed+=onChill;
            bool cast=ice.CastAt(world.origin+Vector3.right*8);float timeout=Time.time+4;
            while(started<0&&Time.time<timeout)yield return null;
            while(started>=0&&Time.time<started+2.52f)yield return null;
            // Duration is still exactly2.5s. The start is actual application,
            // rather than cast start plus an assumed0.18s render-frame windup.
            Check(cast&&started>=0&&Mathf.Abs(freezeDuration-2.5f)<.002f&&Mathf.Abs(chillDuration-2.5f)<.002f&&
                !freeze.Has(StatusType.Freeze)&&!chill.Has(StatusType.Chill),"ice control expires at 2.5s");
            freeze.Changed-=onFreeze;chill.Changed-=onChill;
        }

        IEnumerator Start()
        {
            world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();spirit=world.player.GetComponent<SpiritPower>();pool=world.player.GetComponent<SkillVfxPool>();yield return new WaitForSeconds(1); // P12 spawn pose must finish before checking the normal model bounds.
            if(IceExpiryOnly){yield return FailedIceExpiry();world.End();File.WriteAllText("Artifacts/Skills/SkillSet1-Edges-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);yield break;}
            foreach(var s in world.player.GetComponents<Set1SkillRuntime>())if(s.definition.castType==CastType.Aimed)
            {Prep(s);float before=spirit.Current;Check(!s.CastAt(world.origin+Vector3.right*50)&&!s.CastAt(new Vector3(float.NaN,0,0))&&Mathf.Approximately(before,spirit.Current)&&s.CooldownRemaining==0,s.Id+" rejects range / NaN without charge");}
            // Low orbit is a normal player look angle; pitch14 stays above the enemy bounds.
            var flash=world.player.GetComponent<LightningFlashRuntime>();Prep(flash);float cameraHeight=world.player.cameraHeight;world.player.cameraHeight=.8f;Place(0,world.origin-Vector3.right*2);world.Look(90,0); // Camera crosses a stationary rear target before dash knockback can displace it.
            yield return new WaitForEndOfFrame();
            world.player.cameraHeight=Mathf.Max(.5f,world.victims[0].GetComponentInChildren<SkinnedMeshRenderer>().bounds.center.y-world.player.transform.position.y);world.Look(90,0);yield return new WaitForEndOfFrame();
            var cameraDetails=new System.Text.StringBuilder();
            var rigs=new SkinnedMeshRenderer[7][];var originallyOff=new bool[7][];for(int i=0;i<7;i++){rigs[i]=world.victims[i].GetComponentsInChildren<SkinnedMeshRenderer>();originallyOff[i]=new bool[rigs[i].Length];for(int j=0;j<rigs[i].Length;j++)originallyOff[i][j]=rigs[i][j].forceRenderingOff;}
            flash.CastAt(world.origin+Vector3.right*8);int clipSamples=0;bool clipSafe=true;
            while(flash.IsCasting){yield return new WaitForEndOfFrame();Vector3 cameraPos=world.player.followCamera.transform.position;if(cameraDetails.Length<2500)cameraDetails.AppendLine("camera="+cameraPos+"; player="+world.player.transform.position+"; victim="+rigs[0][0].bounds);for(int i=0;i<7;i++)for(int j=0;j<rigs[i].Length;j++){var b=rigs[i][j].bounds;b.Expand(.2f);if(b.Contains(cameraPos)){clipSamples++;clipSafe&=rigs[i][j].forceRenderingOff;}}}
            File.WriteAllText("Artifacts/Skills/camera-clipping-details.txt",cameraDetails.ToString());
            Check(clipSamples>0,"flash real gameplay camera low orbit actually traverses enemy bounds");Check(clipSafe,"flash camera never renders enclosing enemy model during dash");world.player.cameraHeight=cameraHeight;world.Look(235,14);yield return new WaitForEndOfFrame();bool restoredRender=true;for(int i=0;i<7;i++)for(int j=0;j<rigs[i].Length;j++)restoredRender&=rigs[i][j].forceRenderingOff==originallyOff[i][j];Check(restoredRender,"flash camera restores original enemy rendering after leaving bounds");yield return new WaitForSeconds(3);
            var ice=world.player.GetComponent<IceSealRuntime>();Prep(ice);Place(0,world.origin+Vector3.right*4);Place(1,world.origin+Vector3.right*5);world.victims[1].resistHardControl=true;Place(2,world.origin+new Vector3(2,0,7));Place(3,world.origin+Vector3.right*11);
            var expiryHost=world.victims[0].GetComponent<StatusEffectHost>();float effectStarted=-1;
            Action<StatusType,bool> expiryStart=(type,active)=>{if(type==StatusType.Freeze&&active)effectStarted=Time.time;};expiryHost.Changed+=expiryStart;
            Check(ice.CastAt(world.origin+Vector3.right*8),"ice boundary cast");yield return new WaitForSeconds(.4f);
            Check(world.victims[0].GetComponent<StatusEffectHost>().Has(StatusType.Freeze)&&world.victims[1].GetComponent<StatusEffectHost>().Has(StatusType.Chill)&&!world.victims[1].GetComponent<StatusEffectHost>().Has(StatusType.Freeze)&&world.victims[2].Health==10000&&world.victims[3].Health==10000,"ice 90deg / 10m excludes side and range / boss conversion");
            while(effectStarted>=0&&Time.time<effectStarted+2.52f)yield return null;
            expiryHost.Changed-=expiryStart;Check(effectStarted>=0&&!world.victims[0].GetComponent<StatusEffectHost>().Has(StatusType.Freeze)&&!world.victims[1].GetComponent<StatusEffectHost>().Has(StatusType.Chill),"ice control expires at 2.5s");while(ice.IsCasting)yield return null;yield return new WaitForSeconds(3);
            var fire=world.player.GetComponent<FireLotusRuntime>();Prep(fire);Vector3 center=world.origin+Vector3.right*8;Place(0,center);Place(1,center+Vector3.back*6.8f);Place(2,center+Vector3.back*7.2f);for(int i=3;i<7;i++)Place(i,world.origin+Vector3.right*(20+i));
            fire.CastAt(center);yield return new WaitForSeconds(.2f);Check(Mathf.Approximately(world.player.SkillMoveMultiplier,.35f),"fire charging permits slow movement35%");yield return new WaitForSeconds(2);
            Check(world.victims[0].Health<10000&&world.victims[1].Health<10000&&world.victims[2].Health==10000,"fire radius7m includes6.8 / excludes7.2");
            int count=DangerZoneRegistry.CopyActive(zones);bool field=false;for(int i=0;i<count;i++)field|=Mathf.Approximately(zones[i].radius,7)&&(zones[i].center-center).sqrMagnitude<.2f;
            Check(field,"fire danger registry follows actual impact center");while(fire.IsCasting)yield return null;yield return new WaitForSeconds(3);
            var chain=world.player.GetComponent<ChainLightningRuntime>();Prep(chain);for(int i=0;i<7;i++)Place(i,world.origin+Vector3.right*(2+i*1.1f));
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=world.origin+Vector3.right*4.5f+Vector3.up;wall.transform.localScale=new Vector3(.2f,3,20);Physics.SyncTransforms();
            world.player.GetComponent<TargetLock>().Set(world.victims[0]);chain.CastAt(world.origin+Vector3.right*8);yield return new WaitForSeconds(1.6f);
            Check(chain.BounceCount<=3&&world.victims[3].Health==10000&&world.victims[6].Health==10000,"chain locked first target / LOS blocks further bounces");Destroy(wall);world.player.GetComponent<TargetLock>().Set(null);yield return new WaitForSeconds(3);
            var hole=world.player.GetComponent<BlackHoleRuntime>();Prep(hole);yield return null;center=world.origin+Vector3.right*6;world.victims[6].resistHardControl=true;Vector3 boss=world.victims[6].transform.position;float oldDistance=Vector3.Distance(world.victims[0].transform.position,center);
            var agent=world.victims[0].GetComponent<NavMeshAgent>();bool rot=agent.updateRotation,pos=agent.updatePosition,stop=agent.isStopped,brain=world.victims[0].GetComponent<MinionBrain>().enabled;
            var model=VisualRig(world.victims[0]);Vector3 modelPos=model.localPosition;Quaternion modelRot=model.localRotation;
            hole.CastAt(center);yield return new WaitForSeconds(2.8f);Vector3 grouped=world.victims[0].transform.position;
            Check(Vector3.Distance(grouped,center)<oldDistance*.3f&&Vector3.Distance(boss,world.victims[6].transform.position)<.1f&&world.victims[6].GetComponent<StatusEffectHost>().Magnitude(StatusType.Chill)==.5f,"black hole gathers / boss stays still with Chill50%");
            Check(model.localPosition.y>modelPos.y+.2f&&Vector3.Distance(model.localPosition,modelPos)>1,"black hole visibly lifts and orbits rig while agent stays on ground");yield return new WaitForSeconds(.7f);
            Check(Mathf.Abs(Vector3.Distance(grouped,world.victims[0].transform.position)-4)<.3f&&agent.updatePosition==pos&&agent.updateRotation==rot&&agent.isStopped==stop&&world.victims[0].GetComponent<MinionBrain>().enabled==brain,"black hole 4m repulsion / restores exact agent-brain flags");yield return new WaitForSeconds(3);
            Check(Vector3.Distance(model.localPosition,modelPos)<.001f&&Quaternion.Angle(model.localRotation,modelRot)<.01f,"black hole restores rig pose after normal expulsion");
            Prep(hole);yield return null;modelPos=model.localPosition;modelRot=model.localRotation;hole.CastAt(world.origin+Vector3.right*6);yield return new WaitForSeconds(1);hole.enabled=false;yield return null;
            Check(Vector3.Distance(model.localPosition,modelPos)<.001f&&Quaternion.Angle(model.localRotation,modelRot)<.01f&&agent.updatePosition==pos&&agent.updateRotation==rot&&world.victims[0].GetComponent<MinionBrain>().enabled==brain&&!world.victims[0].GetComponent<StatusEffectHost>().Has(StatusType.Pulled),"black hole cancellation restores rig / navigation / control");hole.enabled=true;yield return new WaitForSeconds(3);
            var bell=world.player.GetComponent<GoldenBellRuntime>();Prep(bell);bell.CastAt(world.origin);var health=world.player.GetComponent<PlayerMonsterHealth>();float hp=health.CurrentHealth,cap=bell.ShieldRemaining;float raw=(cap+10)/Mathf.Max(.01f,1-health.DamageReduction);
            health.ApplyDamage(DamageInfo.Create(raw,Element.None,DamageSource.Environment,world.player.transform.position,Vector3.zero));Check(bell.Shattered&&!bell.ShieldActive&&Mathf.Abs((hp-health.CurrentHealth)-10)<.1f,"bell depletion passes only residual damage to HP");yield return new WaitForSeconds(3);
            Prep(bell);var buffs=world.player.GetComponent<BuffSystem>();buffs.SetSkillBuff("QA-fire",StatType.FireResistance,.5f,10);bell.CastAt(world.origin);Check(world.player.GetComponent<PlayerStats>().FireResistance==.8f,"bell respects combined fire resistance80% cap");yield return new WaitForSeconds(6.1f);buffs.RemoveSkillBuff("QA-fire");Check(world.player.GetComponent<PlayerStats>().FireResistance==0,"bell expiry removes its buff");yield return new WaitForSeconds(3);
            int token=DangerZoneRegistry.Register(world.origin,2,.1f,DangerShape.Circle);Check(DangerZoneRegistry.IsDangerous(world.origin)&&!DangerZoneRegistry.IsDangerous(world.origin+Vector3.right*3),"registry geometric containment");yield return new WaitForSeconds(.15f);Check(DangerZoneRegistry.CopyActive(zones)==0,"registry expiry without owner Update");DangerZoneRegistry.Remove(token);
            var profile=ProfileService.Instance;profile.Data.skills.ranks.Add(new KeyCount{key="van-kiem-quyet",count=4});var store=new JsonProfileStore("Artifacts/Skills/P10-profile-roundtrip.json");store.Save(profile.Data);var restored=store.Load();Check(restored.skills.ranks.Exists(x=>x.key=="van-kiem-quyet"&&x.count==4),"skills ranks survive real JsonProfileStore disk save/load");
            world.Arrange();var hand=world.player.GetComponent<GiantHandSkill>();var handRank=world.player.GetComponent<GiantHandRuntime>();handRank.ApplyRank(5);spirit.Refill();
            bool handCast=hand.CastAt(world.victims[0].transform.position);yield return new WaitForSeconds(hand.config.summonTime+hand.config.descentTime+.2f);
            Check(handCast&&hand.LastHitCount>0&&Mathf.Abs(hand.LastDamage/hand.LastHitCount-world.player.GetComponent<PlayerStats>().Attack*hand.config.damagePercent*1.48f)<.2f&&Mathf.Abs(hand.EffectiveCooldown-hand.config.cooldown*.8f)<.01f,"old hand rank5 damage and cooldown change on actual cast");yield return new WaitForSeconds(2);
            world.Arrange();var wallSkill=world.player.GetComponent<VoidWallSkill>();world.player.GetComponent<VoidWallRuntime>().ApplyRank(5);wallSkill.RefillCharges();spirit.Refill();
            bool wallCast=wallSkill.QuickCast();yield return null;
            Check(wallCast&&Mathf.Abs(wallSkill.LastDeployed.MaxHealth-world.player.GetComponent<PlayerStats>().MaxHealth*wallSkill.config.healthShare*1.48f)<.2f&&Mathf.Abs(wallSkill.RechargeSeconds-wallSkill.config.rechargeSeconds*.8f)<.01f,"old wall rank5 shield HP and charge cooldown");
            foreach(var body in VoidWall.Active.ToArray())body.gameObject.SetActive(false);
            world.Arrange();var phantom=world.player.GetComponent<PhantomDecoySkill>();world.player.GetComponent<PhantomRuntime>().ApplyRank(5);spirit.Refill();bool phantomCast=phantom.Cast(Vector3.right);
            Check(phantomCast&&Mathf.Abs(phantom.Decoy.Lifetime-phantom.duration*1.48f)<.01f&&Mathf.Abs(phantom.EffectiveCooldown-phantom.cooldown*.8f)<.01f,"old Phantom rank5 lifetime and cooldown on actual cast");if(phantomCast){phantom.enabled=false;yield return null;phantom.enabled=true;}yield return new WaitForSeconds(3);
            Check(pool.FiniteState&&pool.LiveRainSwordCount==0&&pool.ActiveCount==0,"VFX transforms finite / all warm swords and nodes idle");
            world.End();File.WriteAllText("Artifacts/Skills/SkillSet1-Edges-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);
        }
    }
}
#endif
