#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public sealed class SkillVisualCapture:MonoBehaviour
    {
        public string skillId="tich-lich-nhat-thiem";
        public bool captureMobile=true;
        public string outputRoot="task/p10/screens/fix3",artifactRoot="Artifacts/Skills/fix3";
        public bool Complete {get;private set;}
        [System.Serializable] public sealed class CaptureReport {public string skill,camera,enemy,revision="fix3",capturedAt;public float hitStop,impulse,lightBrightness=1,darkBrightness=.2f;public int peakPC,peakMobile,poolObjectsBefore,poolObjectsAfter;public bool flashCaptured,bellNaturalExpiry,passiveSwordsHidden=true;}
        IEnumerator Start()
        {
            var world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();
            var skill=world.player.GetComponent<SkillLoadout>().Find(skillId) as Set1SkillRuntime;if(skill==null)throw new System.InvalidOperationException("Skill not installed: "+skillId);
            var report=new CaptureReport{skill=skillId,camera="CampusExplorer default: distance 3.4m, pitch 14deg, FOV60; normal orbit yaw105 for hole/rain, yaw90 others, yaw250 after flash dash. Three passive jade swords hidden only for QA composition, restored afterwards.",enemy="tieu-yeu (real EnemyPool prefab)"};
            var passive=new System.Collections.Generic.List<FlyingSword>();foreach(var sword in world.player.GetComponent<PlayerCombat>().Swords)if(sword!=null&&sword.gameObject.activeSelf){passive.Add(sword);sword.gameObject.SetActive(false);}report.passiveSwordsHidden=passive.Count==3;
            var pool=world.player.GetComponent<SkillVfxPool>();report.poolObjectsBefore=pool.GetComponentsInChildren<Transform>(true).Length;
            Directory.CreateDirectory(outputRoot+"/frames");Directory.CreateDirectory(artifactRoot);
            for(int mode=0;mode<2;mode++)
            {
                world.Mode(false);world.Lighting(mode==1);world.Arrange(skillId=="tich-lich-nhat-thiem");pool.Clear();pool.ResetMetrics();
                if(skillId=="than-kiem-ngu-loi")world.ChainLayout();
                Compose(world);
                skill.ResetCooldownForValidation();world.player.GetComponent<SpiritPower>().Refill();world.player.GetComponent<SkillLoadout>().Equip(2,skillId);
                for(int i=0;i<8;i++){world.Look(CaptureYaw,14);yield return null;}
                string background=mode==0?"light":"dark";
                var capture=Capture(world,skill,background,report);while(capture.MoveNext())yield return capture.Current;
                report.peakPC=Mathf.Max(report.peakPC,pool.PeakParticles);
                yield return new WaitForSeconds(3.1f);
            }
            if(captureMobile)
            {
                world.Mode(true);world.Lighting(false);world.Arrange();pool.Clear();pool.ResetMetrics();skill.ResetCooldownForValidation();world.player.GetComponent<SpiritPower>().Refill();
                if(skillId=="than-kiem-ngu-loi")world.ChainLayout();
                Compose(world);skill.CastAt(CastPoint(world));float t=Time.time;
                float mobileAt=skillId=="phat-no-hoa-lien"?2.1f:skillId=="hac-dong-than-la"?2:skillId=="van-kiem-quyet"?2.8f:.7f;
                while(Time.time-t<mobileAt){world.Look(skillId=="tich-lich-nhat-thiem"&&Time.time-t>.32f?250:CaptureYaw,14);yield return null;}
                ScreenCapture.CaptureScreenshot(outputRoot+"/"+skillId+"-mobile.png");yield return new WaitForEndOfFrame();
                while(skill.IsCasting)yield return null;report.peakMobile=pool.PeakParticles;
                if(skillId=="kim-chung-trao"){report.bellNaturalExpiry=!(skill as GoldenBellRuntime).Shattered;yield return new WaitForSeconds(.25f);ScreenCapture.CaptureScreenshot(outputRoot+"/kim-chung-trao-expiry.png");yield return new WaitForEndOfFrame();}
                yield return new WaitForSeconds(3);
            }
            var impact=world.player.GetComponent<SkillImpact>();report.hitStop=impact.LastHitStop;report.impulse=impact.LastImpulse;report.poolObjectsAfter=pool.GetComponentsInChildren<Transform>(true).Length;
            report.capturedAt=System.DateTime.UtcNow.ToString("o");report.camera="Default gameplay camera: distance3.4/FOV60/pitch14, orbit yaw90 (250 after dash); hole aim10m+z3, rain aim10m-z4 for visibility; seven real enemies, passive swords hidden during QA only.";File.WriteAllText(artifactRoot+"/"+skillId+"-visual.json",JsonUtility.ToJson(report,true));Complete=true;world.End();foreach(var sword in passive)if(sword!=null)sword.gameObject.SetActive(true);Debug.Log("P10 VISUAL CAPTURE DONE "+skillId);Destroy(gameObject);
        }
        IEnumerator Capture(SkillSet1TestWorld world,Set1SkillRuntime skill,string background,CaptureReport report)
        {
            float[] timeline=Timeline(skillId);int frame=0,bellHit=0;bool impactCaptured=false;
            skill.CastAt(CastPoint(world));float start=Time.time;
            while(frame<8||skill.IsCasting)
            {
                yield return null;
                float t=Time.time-start;
                world.Look(skillId=="tich-lich-nhat-thiem"&&t>.32f?250:CaptureYaw,14);
                var fire=skill as FireLotusRuntime;if(fire!=null&&fire.HasExploded){timeline[4]=fire.ExplosionTime+.07f;timeline[5]=fire.ExplosionTime+.28f;}
                if(skillId=="kim-chung-trao"&&bellHit<3&&t>=(bellHit==0?1.2f:bellHit==1?2.2f:4.2f))
                {
                    var bell=(GoldenBellRuntime)skill;float damage=bellHit==2?bell.ShieldRemaining+1:bell.ShieldCapacity*.25f;
                    bell.Absorb(damage,DamageInfo.Create(damage,Element.None,DamageSource.Melee,world.player.transform.position+Vector3.up,-Vector3.right,world.victims[0].gameObject));bellHit++;
                }
                yield return new WaitForEndOfFrame();
                var impact=world.player.GetComponent<SkillImpact>();
                if(!impactCaptured&&impact.FlashVisible&&(skillId!="kim-chung-trao"||bellHit==3))
                {
                    impactCaptured=true;report.flashCaptured=true;
                    ScreenCapture.CaptureScreenshot(outputRoot+"/"+skillId+"-impact-"+background+".png");
                    yield return new WaitForEndOfFrame();
                    continue;
                }
                if(frame<8&&t>=timeline[frame])
                {ScreenCapture.CaptureScreenshot(outputRoot+"/frames/"+skillId+"-"+background+"-"+frame+".png");frame++;}
            }
        }
        float CaptureYaw=>90;
        Vector3 CastPoint(SkillSet1TestWorld world)=>world.origin+(skillId=="hac-dong-than-la"?new Vector3(10,0,3):skillId=="van-kiem-quyet"?new Vector3(10,0,-4):Vector3.right*8);
        void Compose(SkillSet1TestWorld world)
        {
            if(skillId!="hac-dong-than-la"&&skillId!="van-kiem-quyet")return;
            var center=CastPoint(world);
            for(int i=0;i<7;i++){var motor=world.victims[i].GetComponent<CampusRift.Enemies.MinionMotor>();motor.Place(center+new Vector3(-2+(i%3)*1.4f,0,(i/3-1)*1.5f));motor.Stop();}Physics.SyncTransforms();
        }
        public static float[] Timeline(string id)
        {
            switch(id)
            {
                case "tich-lich-nhat-thiem":return new[]{.02f,.07f,.28f,.46f,.55f,.76f,.95f,1.75f};
                case "phat-no-hoa-lien":return new[]{.15f,.65f,1.15f,1.5f,1.9f,2.6f,4.7f,6.3f};
                case "han-bang-phong-an":return new[]{.04f,.18f,.28f,.45f,.7f,1.8f,2.73f,3.0f};
                case "than-kiem-ngu-loi":return new[]{.04f,.2f,.3f,.4f,.5f,.65f,.9f,1.3f};
                case "kim-chung-trao":return new[]{.04f,.16f,.3f,1.4f,2.4f,4.5f,6.1f,6.45f};
                case "hac-dong-than-la":return new[]{.04f,.3f,.8f,1.8f,2.8f,3.1f,3.45f,3.9f};
                default:return new[]{.04f,.3f,.9f,2.0f,2.45f,2.8f,3.15f,4.3f};
            }
        }
    }
}
#endif
