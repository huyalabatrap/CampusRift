#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.UI;
using CampusRift.Progression;
using CampusRift.Localization;
using CampusRift.Controls;
namespace CampusRift.Combat
{
    public sealed class ReactionVisualCapture : MonoBehaviour
    {
        [Serializable] public sealed class Report {public string capturedAt,error="",camera="default gameplay distance3.4/FOV60/pitch14/yaw90; 12 real tieu-yeu; passive swords hidden only in capture and restored";public int crowd=12,issues;public List<ComicTextAudit.ScreenReport> audits=new List<ComicTextAudit.ScreenReport>();public List<string> captured=new List<string>();public bool iceCombo,convergenceCombo;}
        readonly Report report=new Report();ReactionTestWorld fixture;ReactionType watching;float eventAt=-1;bool captureImpact;string impactFile;
        [Serializable] public sealed class VfxMeasurement {public string id,background;public bool mobile;public int peak,exhausted;}
        readonly List<VfxMeasurement> vfxMeasurements=new List<VfxMeasurement>();
        const string Root="task/p11/screens/fix3/";
        IEnumerator Start()
        {
            report.capturedAt=DateTime.UtcNow.ToString("o");Directory.CreateDirectory(Root+"frames");Directory.CreateDirectory("Artifacts/Reactions/fix3");fixture=new ReactionTestWorld();
            var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){var r=stack.Peek();bool more=false;object next=null;try{more=r.MoveNext();if(more)next=r.Current;}catch(Exception e){report.error=e.ToString();break;}if(!more){stack.Pop();continue;}var nested=next as IEnumerator;if(nested!=null){stack.Push(nested);continue;}yield return next;}
            ReactionResolver.Feedback-=Triggered;try{fixture.End();}catch(Exception e){report.error+=" Restore "+e;}
            File.WriteAllText("Artifacts/Reactions/fix3/Visual.json",JsonUtility.ToJson(report,true));File.WriteAllText("Artifacts/Reactions/fix3/Visual-Vfx.json",JsonUtility.ToJson(new VfxReport{measurements=vfxMeasurements},true));File.WriteAllText("Artifacts/Reactions/fix3/Visual-DONE.txt",report.error.Length==0&&report.issues==0&&report.iceCombo&&report.convergenceCombo?"PASS":"FAIL");Destroy(gameObject);
        }
        void Triggered(ReactionEvent ev){if(ev.type!=watching||eventAt>=0)return;eventAt=Time.time;if(captureImpact){ScreenCapture.CaptureScreenshot(impactFile);captureImpact=false;}}
        void Update(){if(fixture!=null&&fixture.world.player!=null){fixture.world.Look(90,14);if(UIStateManager.Instance.State!=UIState.Gameplay)UIStateManager.Instance.EnterScene(true);}}
        IEnumerator Run()
        {
            fixture.Begin();ReactionResolver.Feedback+=Triggered;
            foreach(ReactionType type in Enum.GetValues(typeof(ReactionType)))
            {
                for(int dark=0;dark<2;dark++)yield return Capture(type,dark==1,false);
                yield return Capture(type,false,true);
            }
            fixture.world.Mode(false);fixture.world.Lighting(false);fixture.Arrange();
            var pool=fixture.world.player.GetComponent<SkillVfxPool>();pool.Clear();DamageNumberPool.Instance.ReactionLabels.Clear();
            var hint=fixture.world.player.GetComponent<ReactionFeedback>().Hint;hint.Clear();
            foreach(ReactionType type in Enum.GetValues(typeof(ReactionType)))
            {
                string id=ReactionConfig.Current.Rule(type).id;
                if(!ProfileService.Instance.Data.seenReactions.Contains(id))ProfileService.Instance.Data.seenReactions.Add(id);
            }
            fixture.Triple();hint.Clear();yield return new WaitForSeconds(.18f);yield return Shot("crowd-three-reactions");
            yield return UIAudit();
        }
        IEnumerator Capture(ReactionType type,bool dark,bool mobile)
        {
            fixture.world.Mode(mobile);fixture.world.Lighting(dark);fixture.Arrange();var player=fixture.world.player;var pool=player.GetComponent<SkillVfxPool>();pool.Clear();
            pool.ResetMetrics();
            DamageNumberPool.Instance.ReactionLabels.Clear();player.GetComponent<ReactionFeedback>().Hint.Clear();ProfileService.Instance.Data.seenReactions.Clear();
            var spirit=player.GetComponent<SpiritPower>();spirit.Refill();watching=type;eventAt=-1;string id=ReactionConfig.Current.Rule(type).id;string mode=dark?"dark":"light";
            var ice=player.GetComponent<IceSealRuntime>();var bolt=player.GetComponent<ChainLightningRuntime>();var fire=player.GetComponent<FireLotusRuntime>();var hole=player.GetComponent<BlackHoleRuntime>();var rain=player.GetComponent<SwordRainRuntime>();
            for(int i=0;i<10;i++)yield return null;
            // Status setup uses real skills for both required combinations; remaining isolated showcases use the same live resolver.
            if(type==ReactionType.IceLightning||type==ReactionType.ElectricFlow)
            {
                if(type==ReactionType.ElectricFlow)foreach(var m in fixture.crowd)m.resistHardControl=true;
                ice.ResetCooldownForValidation();ice.CastAt(fixture.Center);yield return new WaitForSeconds(.55f);
            }
            else if(type==ReactionType.Convergence)
            {hole.ResetCooldownForValidation();hole.CastAt(fixture.Center);yield return new WaitForSeconds(.35f);}
            else foreach(var m in fixture.crowd)m.GetComponent<StatusEffectHost>().Apply(type==ReactionType.FireExplosion?StatusType.Burn:StatusType.Stun,12);
            if(!mobile){ScreenCapture.CaptureScreenshot(Root+"frames/"+id+"-"+mode+"-0.png");yield return new WaitForEndOfFrame();yield return null;}
            impactFile=Root+id+"-impact-"+mode+".png";captureImpact=!mobile;
            spirit.Refill();
            if(type==ReactionType.IceLightning||type==ReactionType.ElectricFlow){bolt.ResetCooldownForValidation();bolt.CastAt(fixture.Center);}
            else if(type==ReactionType.Convergence){fire.ResetCooldownForValidation();fire.CastAt(fixture.Center);}
            else
            {
                foreach(var m in fixture.crowd)
                {var info=DamageInfo.Create(150,type==ReactionType.FireExplosion?Element.Hoa:Element.Kim,DamageSource.Skill,m.transform.position+Vector3.up,Vector3.right,player.gameObject);info.attackPower=player.GetComponent<PlayerStats>().Attack;info.skillId="reaction-visual";m.ApplyDamage(info);}
            }
            float deadline=Time.time+4;while(eventAt<0&&Time.time<deadline)yield return null;if(eventAt<0)throw new InvalidOperationException(type+" not triggered in visual fixture");
            if(type==ReactionType.IceLightning)report.iceCombo=true;if(type==ReactionType.Convergence)report.convergenceCombo=true;
            if(mobile){while(Time.time-eventAt<.23f)yield return null;ScreenCapture.CaptureScreenshot(Root+id+"-mobile.png");yield return new WaitForEndOfFrame();}
            else
            {
                float[] times={.015f,.10f,.22f,.40f,.65f,1.0f,type==ReactionType.ArmorShatter?4.5f:2.15f};
                for(int frame=1;frame<8;frame++){while(Time.time-eventAt<times[frame-1])yield return null;yield return null;ScreenCapture.CaptureScreenshot(Root+"frames/"+id+"-"+mode+"-"+frame+".png");yield return new WaitForEndOfFrame();}
            }
            report.captured.Add(id+"-"+(mobile?"mobile":mode));while(ice.IsCasting||bolt.IsCasting||fire.IsCasting||hole.IsCasting||rain.IsCasting)yield return null;yield return new WaitForSeconds(2.5f);
            vfxMeasurements.Add(new VfxMeasurement{id=id,background=mode,mobile=mobile,peak=pool.PeakParticles,exhausted=pool.ExhaustedCount});
            if(pool.ExhaustedCount>0)report.error+=" VFX pool exhausted "+id+"/"+mode+"/"+mobile+": "+pool.ExhaustedCount;
        }
        IEnumerator Shot(string id)
        {yield return new WaitForEndOfFrame();var scan=ComicTextAudit.Scan(id);report.audits.Add(scan);report.issues+=scan.issues.Count;ScreenCapture.CaptureScreenshot(Root+id+".png");yield return new WaitForEndOfFrame();}
        IEnumerator UIAudit()
        {
            var settings=SettingsManager.Instance.Current.Copy();var tracker=fixture.world.player.GetComponent<GenerationChainTracker>();var hint=fixture.world.player.GetComponent<ReactionFeedback>().Hint;
            foreach(var language in new[]{GameLanguage.Vietnamese,GameLanguage.English})foreach(var control in new[]{ControlMode.PC,ControlMode.Mobile})foreach(int width in new[]{1280,1920})
            {
                settings.Language=language;settings.ControlMode=control;SettingsManager.Instance.Apply(settings,false);UIValidation.SetResolution(width,width==1280?720:1080);fixture.world.Mode(control==ControlMode.Mobile);
                for(int i=0;i<5;i++)yield return null;
                for(int count=0;count<4;count++)
                {
                    tracker.ResetChain();if(count>=1)tracker.Record(Element.Hoa);if(count>=2)tracker.Record(Element.Tho);if(count>=3)tracker.Record(Element.Kim);
                    yield return new WaitForSeconds(.18f);yield return Shot("generation-"+count+"-"+language+"-"+control+"-"+width);
                }
                foreach(ReactionType type in Enum.GetValues(typeof(ReactionType)))
                {
                    hint.Clear();ProfileService.Instance.Data.seenReactions.Clear();hint.Encounter(type);yield return new WaitForSeconds(.12f);yield return Shot("hint-"+ReactionConfig.Current.Rule(type).id+"-"+language+"-"+control+"-"+width);
                }
            }
            UIValidation.SetResolution(1920,1080);
        }
        [Serializable] public sealed class VfxReport {public List<VfxMeasurement> measurements;}
    }
}
#endif
