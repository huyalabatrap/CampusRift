#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using CampusRift.UI;
using CampusRift.Progression;
using CampusRift.Levels;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Skills;
namespace CampusRift.Validation
{
    public sealed class P17RelatedSmoke:MonoBehaviour
    {
        [Serializable] sealed class Report{public List<string> passed=new List<string>(),failed=new List<string>();}
        Report report=new Report();
        void Check(bool value,string label){(value?report.passed:report.failed).Add(label);}
        IEnumerator Start()
        {
            var original=SettingsManager.Instance.Current.Copy();var s=original.Copy();s.LocalTelemetryEnabled=true;s.TelemetryConsentAsked=true;s.ControlMode=Controls.ControlMode.Mobile;s.ReduceSkillFlashes=false;SettingsManager.Instance.Apply(s,false);
            ProfileService.Instance.UseTransient(new ProfileData());TutorialDirector.Suppress=false;UIStateManager.Instance.EnterScene(true);LocalTelemetry.TestFolder="task/p17/related-telemetry";
            var director=LevelDirector.Ensure();director.Begin(LevelCatalog.Instance.Get(8));director.enabled=false;yield return null;yield return null;
            Check(TutorialDirector.Instance.ActiveGroup=="fire"&&TutorialDirector.Instance.ActiveStep==0,"first level8 shelter tip");TutorialDirector.Instance.Advance();yield return null;Check(TutorialDirector.Instance.ActiveStep==1,"level8 Heaven Sword outdoor tip");TutorialDirector.Instance.Skip();Check(ProfileService.Instance.Data.tutorial.skipFire,"fire tip skip saved");
            var player=FindAnyObjectByType<CampusExplorer>();var dummy=GameObject.CreatePrimitive(PrimitiveType.Capsule);dummy.name="P17 reaction target";dummy.layer=7;dummy.transform.position=player.transform.position+Vector3.forward*6+Vector3.up;
            var v=dummy.AddComponent<MonsterVitality>();v.SetMaxHealth(5000,true);var status=dummy.GetComponent<StatusEffectHost>()??dummy.AddComponent<StatusEffectHost>();status.Apply(StatusType.Freeze,3,1,player.gameObject);
            var resolver=dummy.GetComponent<ReactionResolver>()??dummy.AddComponent<ReactionResolver>();var hit=DamageInfo.Create(10,Element.Loi,DamageSource.Skill,dummy.transform.position,Vector3.up,player.gameObject);v.ApplyDamage(hit);
            LocalTelemetry.Instance.Finish("abandoned",0,2);var file=Directory.GetFiles(LocalTelemetry.TestFolder,"*.jsonl").First();var row=JsonUtility.FromJson<LocalTelemetry.Row>(File.ReadLines(file).First());Check(resolver.TriggerCount>0&&row.reactions.Any(x=>x.id==ReactionType.IceLightning.ToString()&&x.count==1),"actual IceLightning event counted");Destroy(dummy);
            s.LocalTelemetryEnabled=false;SettingsManager.Instance.Apply(s,false);LocalTelemetry.TestFolder=null;
            var pool=player.GetComponent<SkillVfxPool>();var normal=pool.Spawn(SkillVfxKind.Burst,player.transform.position+Vector3.up,Color.white,.7f,1);yield return null;Check(normal.surface.enabled,"normal victim burst remains visible");
            s.ReduceSkillFlashes=true;SettingsManager.Instance.Apply(s,false);var soft=pool.Spawn(SkillVfxKind.Burst,player.transform.position+Vector3.up,Color.white,.7f,1);var sword=pool.Spawn(SkillVfxKind.SwordImpact,player.transform.position+Vector3.up,Color.white,.7f,1);var ring=pool.Spawn(SkillVfxKind.Ring,player.transform.position,Color.red,.7f,2);yield return null;Check(!soft.surface.enabled&&!sword.surface.enabled&&ring.line.enabled,"reduced victim flashes off while ring stays visible");pool.Clear();
            foreach(int level in new[]{1,3,8}){director.Begin(LevelCatalog.Instance.Get(level));director.enabled=false;yield return null;var music=director.GetComponent<Audio.LevelMusicDirector>();var source=music.GetComponent<AudioSource>();Check(source.clip!=null&&(level==1?source.clip.name=="dusk":level==3?source.clip.name=="night":source.clip.name=="BossPhase1"),"music group level "+level);}
            director.End();UIStateManager.Instance.Pause();UIStateManager.Instance.OpenSettings();yield return null;var ui=FindAnyObjectByType<SettingsUI>(FindObjectsInactive.Include);ui.SelectTab(3);ui.Language.value=0;yield return null;Check(ui.ReduceCameraShake.GetComponentInChildren<TMPro.TMP_Text>().text.Contains("REDUCE CAMERA"),"comfort language preview English");ui.Language.value=1;yield return null;Check(ui.ReduceCameraShake.GetComponentInChildren<TMPro.TMP_Text>().text.Contains("GIẢM RUNG"),"comfort language preview Vietnamese");
            SettingsManager.Instance.Apply(original,false);Localization.LocalizationService.Instance?.Restore();ProfileService.Instance.EndTransient();TutorialDirector.Suppress=true;
            File.WriteAllText("task/p17/related-smoke.json",JsonUtility.ToJson(report,true));File.WriteAllText("task/p17/related-smoke-DONE.txt",report.passed.Count+" PASS / "+report.failed.Count+" FAIL");Destroy(gameObject);
        }
    }
}
#endif
