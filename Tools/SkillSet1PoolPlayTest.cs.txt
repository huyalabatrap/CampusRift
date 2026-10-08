#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.UI;
namespace CampusRift.Skills
{
    public sealed class SkillSet1PoolPlayTest:MonoBehaviour
    {
        [Serializable] public sealed class Metric {public string skill;public int casts,before,after,peakPC,peakMobile,exhausted;public long maxFrameGCBytes;public bool finite=true;}
        [Serializable] public sealed class Report {public string capturedAt,error;public List<string> passed=new List<string>(),failed=new List<string>();public List<Metric> metrics=new List<Metric>();}
        readonly Report report=new Report();SkillSet1TestWorld world;SkillVfxPool pool;SpiritPower spirit;
        readonly DangerZone[] zones=new DangerZone[64];
        void Check(bool ok,string text){(ok?report.passed:report.failed).Add(text);Save();}
        void Save(){File.WriteAllText("Artifacts/Skills/SkillSet1-Pool.json",JsonUtility.ToJson(report,true));}
        void Update(){if(world!=null&&world.player!=null&&UIStateManager.Instance.State!=UIState.Gameplay)UIStateManager.Instance.EnterScene(true);}
        IEnumerator Start()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){var run=stack.Peek();bool more=false;object next=null;try{more=run.MoveNext();if(more)next=run.Current;}catch(Exception e){report.error=e.ToString();report.failed.Add("unhandled validation exception");break;}if(!more){stack.Pop();continue;}var child=next as IEnumerator;if(child!=null){stack.Push(child);continue;}yield return next;}
            if(world!=null)world.End();report.capturedAt=DateTime.UtcNow.ToString("o");Save();File.WriteAllText("Artifacts/Skills/SkillSet1-Pool-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory("Artifacts/Skills");world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();pool=world.player.GetComponent<SkillVfxPool>();spirit=world.player.GetComponent<SpiritPower>();for(int i=0;i<5;i++)yield return null;
            foreach(var s in world.player.GetComponents<Set1SkillRuntime>())
            {
                pool.Clear();pool.ResetMetrics();world.Mode(false);s.ApplyRank(1);var metric=new Metric{skill=s.Id,before=pool.GetComponentsInChildren<Transform>(true).Length};report.metrics.Add(metric);
                for(int cast=0;cast<10;cast++)
                {
                    world.Arrange(s is LightningFlashRuntime);if(s is ChainLightningRuntime)world.ChainLayout();s.ResetCooldownForValidation();spirit.Refill();yield return null;
                    Check(s.CastAt(world.origin+Vector3.right*8),s.Id+" repeat cast "+(cast+1));float deadline=Time.realtimeSinceStartup+12;
                    while(s.IsCasting){if(Time.realtimeSinceStartup>deadline)throw new TimeoutException(s.Id);world.Look(s is LightningFlashRuntime&&world.player.LastDashDistance>8?235:90,14);metric.finite&=pool.FiniteState;yield return null;}metric.casts++;
                }
                yield return new WaitForSeconds(3.3f);metric.after=pool.GetComponentsInChildren<Transform>(true).Length;metric.peakPC=pool.PeakParticles;metric.exhausted=pool.ExhaustedCount;metric.maxFrameGCBytes=pool.MaxFrameGCBytes;
                Check(metric.before==metric.after&&metric.casts==10,s.Id+" object count unchanged after ten casts");Check(metric.exhausted==0,s.Id+" warm pool never exhausted");Check(metric.peakPC<=1500,s.Id+" PC particle budget");Check(metric.maxFrameGCBytes==0,s.Id+" zero bytes per VFX update");Check(metric.finite&&pool.ActiveCount==0&&pool.LiveRainSwordCount==0&&DangerZoneRegistry.CopyActive(zones)==0,s.Id+" finite and naturally idle after tails");
                world.Mode(true);world.Arrange(s is LightningFlashRuntime);if(s is ChainLightningRuntime)world.ChainLayout();pool.ResetMetrics();s.ResetCooldownForValidation();spirit.Refill();Check(s.CastAt(world.origin+Vector3.right*8),s.Id+" mobile quality actual cast");while(s.IsCasting){world.Look(s is LightningFlashRuntime&&world.player.LastDashDistance>8?235:90,14);yield return null;}yield return new WaitForSeconds(3.3f);metric.peakMobile=pool.PeakParticles;Check(metric.peakMobile<=400&&pool.FiniteState&&pool.ActiveCount==0,s.Id+" mobile particle budget / tails returned");Save();
            }
            world.Arrange();var popup=DamageNumberPool.Instance;int merged=popup.MergedHits;var target=world.victims[0];target.ApplyDamage(DamageInfo.Create(10,Element.Loi,DamageSource.Skill,target.transform.position,Vector3.zero,world.player.gameObject));target.ApplyDamage(DamageInfo.Create(20,Element.Loi,DamageSource.Skill,target.transform.position,Vector3.zero,world.player.gameObject));Check(popup.MergedHits==merged+1&&popup.CreatedCount==64,"same-victim hits merge inside 100ms / popup pool stays warm");
            var settings=SettingsManager.Instance.Current.Copy();settings.ReduceSkillFlashes=true;SettingsManager.Instance.Apply(settings,false);var impact=world.player.GetComponent<SkillImpact>();impact.Pulse(1);yield return new WaitForEndOfFrame();var flash=(UnityEngine.UI.Image)typeof(SkillImpact).GetField("flash",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(impact);Check(flash.color.a<=.1001f&&impact.FlashVisible,"reduced flashes flag decreases real rendered alpha");
        }
    }
}
#endif
