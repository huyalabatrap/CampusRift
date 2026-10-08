#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public sealed class P18SkillSmoke:MonoBehaviour
    {
        public string skillId="hang-long-thap-bat-chuong";public int testRank=1;public float captureAt=.70f;
        public bool SkipPerformance,SkipPhoto;
        [Serializable] public sealed class Result{public string skill,capturedAt,performanceScope;public int rank,hits,raised,peakParticles,poolExhaustion,quality,peakVertices,pcShapeVertices,mobileShapeVertices;public float damage,idleFPS,castFPS;public bool cast,finished,finite;}
        bool sampling;int frames,peakVertices;float frameSeconds,sampleUntil,skipUntil;SkillVfxPool samplePool;
        void Update(){if(!sampling||Time.realtimeSinceStartup<skipUntil||Time.realtimeSinceStartup>sampleUntil)return;frames++;frameSeconds+=Time.unscaledDeltaTime;if(samplePool!=null&&samplePool.Shapes!=null)peakVertices=Mathf.Max(peakVertices,samplePool.Shapes.VertexCount);}
        IEnumerator Start()
        {
            var w=new SkillSet1TestWorld{GameplayCamera=true};w.Begin();yield return null;P18TestSupport.Prepare(w);
            var skill=w.player.GetComponent<SkillLoadout>().Find(skillId) as Set2SkillRuntime;P18TestSupport.SetRank(skill,testRank);skill.ResetCooldownForValidation();
            w.player.GetComponent<SpiritPower>().Restore(10000);var pool=w.player.GetComponent<SkillVfxPool>();pool.Clear();pool.ResetMetrics();
            if(skill is NorthernDrainRuntime||skill is WoodRenewalRuntime)w.player.GetComponent<CampusRift.Monsters.PlayerMonsterHealth>().Revive(.15f,0);
            w.player.GetComponent<SkillLoadout>().Equip(0,skillId);yield return null;
            if(skill is SoulSummonRuntime){for(int i=0;i<(testRank>=4?5:3);i++)w.victims[i].ApplyDamage(DamageInfo.Create(20000,Element.None,DamageSource.Skill,w.victims[i].transform.position,Vector3.right,w.player.gameObject));}
            bool heavy=!SkipPerformance&&testRank==1&&(skill is ImmortalSwordArrayRuntime||skill is SoulSummonRuntime||skill is DomainRuntime||skill is MartialAvatarRuntime);samplePool=pool;float idle=0;
            if(heavy){frames=0;frameSeconds=0;sampling=true;sampleUntil=Time.realtimeSinceStartup+1.1f;yield return new WaitForSeconds(1.2f);idle=frames/Mathf.Max(.001f,frameSeconds);frames=0;frameSeconds=0;sampleUntil=Time.realtimeSinceStartup+5.2f;}
            bool cast=skill.CastAt(w.origin+Vector3.right*8);
            if(skill is KunpengSpeedRuntime)w.player.Dash(Vector3.right,3,1);
            if(skill is MartialAvatarRuntime){w.PlacePlayer(w.origin+Vector3.right*2);w.player.GetComponent<PlayerCombat>().TrySwing();}
            yield return new WaitForSeconds(captureAt);
            Directory.CreateDirectory("task/p18/screens");Directory.CreateDirectory("Artifacts/Skills/Set2");
            if(!SkipPhoto)ScreenCapture.CaptureScreenshot("task/p18/screens/"+skillId+(testRank>=4?"-mastery":"")+".png");
            skipUntil=Time.realtimeSinceStartup+.2f;
            float timeout=Time.realtimeSinceStartup+25;while(skill.IsCasting&&Time.realtimeSinceStartup<timeout)yield return null;
            sampling=false;var result=new Result{skill=skillId,rank=testRank,capturedAt=DateTime.UtcNow.ToString("o"),cast=cast,finished=!skill.IsCasting,hits=skill is SoulSummonRuntime?((SoulSummonRuntime)skill).AlliedHits:skill.LastHitCount,raised=skill is SoulSummonRuntime?((SoulSummonRuntime)skill).Raised:0,damage=skill.LastDamage,peakParticles=pool.PeakParticles,poolExhaustion=pool.ExhaustedCount,finite=pool.FiniteState,idleFPS=idle,castFPS=heavy?frames/Mathf.Max(.001f,frameSeconds):0,quality=QualitySettings.GetQualityLevel(),peakVertices=peakVertices,performanceScope=heavy?"One Editor sample; idle 1 s / active 5 s, screenshot pause 200 ms. Not native or Android device.":"Not measured"};
            if(heavy){var shape=skill is ImmortalSwordArrayRuntime?SkillShape.SwordWheel:skill is SoulSummonRuntime?SkillShape.Soul:skill is DomainRuntime?SkillShape.Domain:SkillShape.Avatar;result.pcShapeVertices=pool.Shapes.ShapeVertexCount(shape,false);result.mobileShapeVertices=pool.Shapes.ShapeVertexCount(shape,true);}
            File.WriteAllText("Artifacts/Skills/Set2/"+skillId+(testRank>=4?"-mastery":"")+"-smoke.json",JsonUtility.ToJson(result,true));
            yield return new WaitForSeconds(.3f);pool.Clear();w.End();Debug.Log("P18 SMOKE DONE "+skillId);Destroy(gameObject);
        }
    }
}
#endif
