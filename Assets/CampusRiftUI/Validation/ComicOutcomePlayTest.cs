#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Combat;
using CampusRift.Levels;
using CampusRift.Localization;
using CampusRift.Progression;
using UnityEngine;

namespace CampusRift.UI
{
    public sealed class ComicOutcomePlayTest : MonoBehaviour
    {
        public static void Begin(){new GameObject("Comic combat and outcome review").AddComponent<ComicOutcomePlayTest>();}
        IEnumerator Frames(int n=5){for(int i=0;i<n;i++)yield return null;}
        readonly List<ComicTextAudit.ScreenReport> audits=new List<ComicTextAudit.ScreenReport>();
        IEnumerator Capture(string name,bool image=true)
        {
            yield return new WaitForSecondsRealtime(.3f);yield return Frames();audits.Add(ComicTextAudit.Scan(name));
            if(image&&LevelHUD.Vietnamese){ScreenCapture.CaptureScreenshot("task/ui-comic/screens/round3/"+name+(Screen.width==2340?"-wide":"")+".png");yield return Frames();}
        }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);var original=SettingsManager.Instance.Current.Copy();var settings=original.Copy();settings.Language=GameLanguage.Vietnamese;settings.Quality=1;settings.ComicEffects=true;settings.ControlMode=Controls.ControlMode.PC;settings.VSync=false;settings.SkyBrightness=1;
            ProfileService.Instance.UseTransient(new ProfileData());GameSceneManager.Instance.StartLevel(1);
            while(GameSceneManager.Instance.IsLoading||LevelDirector.Instance==null||LevelDirector.Instance.Level==null)yield return null;
            yield return Frames();SettingsManager.Instance.Apply(settings,false);UIStateManager.Instance.EnterScene(true);UIValidation.SetResolution(1920,1080);
            var director=LevelDirector.Instance;while(director.AliveCount<2)yield return null;
            Time.timeScale=0;var player=FindAnyObjectByType<CampusExplorer>();player.enabled=false;
            player.GetComponent<CharacterController>().enabled=false;player.transform.position=new Vector3(10,.1f,-5);
            foreach(var renderer in player.GetComponentsInChildren<Renderer>())renderer.forceRenderingOff=false;
            int posed=0;foreach(var enemy in director.Alive)
            {
                var agent=enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();var pos=new Vector3(posed++==0?6:14,.1f,6);
                if(agent!=null&&agent.isOnNavMesh)agent.Warp(pos);
            }
            Camera.main.transform.position=player.transform.position+new Vector3(0,4,-10);Camera.main.transform.LookAt(player.transform.position+new Vector3(0,2,5));
            var binding=new System.Text.StringBuilder();
            foreach(var definition in Skills.SkillCatalog.Instance.skills)
                binding.AppendLine(definition.id+" -> "+UnityEditor.AssetDatabase.GetAssetPath(HubUI.SkillIcon(definition)));
            var loadout=player.GetComponent<Skills.SkillLoadout>();var bar=FindAnyObjectByType<SkillBarBinder>();int icons=0;
            for(int i=0;i<4;i++)
            {
                var runtime=loadout.Get(i);if(runtime==null)continue;
                var slot=bar.SlotAt(i);bool correct=slot!=null&&slot.Icon!=null&&slot.Icon.isActiveAndEnabled&&slot.Icon.sprite==HubUI.SkillIcon(runtime.Definition);
                binding.AppendLine("HUD slot "+i+" / "+runtime.Id+" -> "+(slot!=null&&slot.Icon!=null?UnityEditor.AssetDatabase.GetAssetPath(slot.Icon.sprite):"missing")+" visible/correct="+correct);
                if(!correct)throw new System.InvalidOperationException("HUD icon missing or hidden: "+runtime.Id);icons++;
            }
            binding.AppendLine("Verified visible HUD icons: "+icons);
            File.WriteAllText("task/ui-comic/tests/round3/IconBindings.txt",binding.ToString());
            yield return Capture("hud-combat");
            director.winDelay=.1f;Time.timeScale=1;
            while(director.State!=LevelDirector.Phase.Won)
            {
                foreach(var live in director.Alive.ToArray())if(live.Alive)live.Vitality.ApplyDamage(DamageInfo.Create(1e7f,Element.None,DamageSource.Melee,live.transform.position,Vector3.forward));
                yield return new WaitForSecondsRealtime(.12f);
            }
            while(UIStateManager.Instance.State!=UIState.Victory)yield return null;
            // Remove only the fixture's oversized floating numbers; the real result card remains untouched.
            if(DamageNumberPool.Instance!=null)foreach(Transform number in DamageNumberPool.Instance.transform)number.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(1.3f);
            foreach(var language in new[]{GameLanguage.Vietnamese,GameLanguage.English})
            {
                settings.Language=language;SettingsManager.Instance.Apply(settings,false);FindAnyObjectByType<LevelResultUI>().Refresh();yield return new WaitForSecondsRealtime(1.2f);
                foreach(int width in new[]{1920,2340}){UIValidation.SetResolution(width,1080);yield return Capture("result");}
            }
            foreach(var language in new[]{GameLanguage.Vietnamese,GameLanguage.English})
            {
                settings.Language=language;SettingsManager.Instance.Apply(settings,false);UIStateManager.Instance.EnterScene(true);UIStateManager.Instance.Defeat();
                foreach(int width in new[]{1920,2340}){UIValidation.SetResolution(width,1080);yield return Capture("game-over");}
            }
            File.WriteAllText("task/ui-comic/tests/round3/OutcomeAudit.json","["+string.Join(",",audits.Select(a=>JsonUtility.ToJson(a,true)))+"]");
            File.WriteAllText("task/ui-comic/tests/round3/Outcome-DONE.txt",audits.Sum(a=>a.issues.Count)+" issues; "+audits.Count+" audits");
            ProfileService.Instance.EndTransient();LevelSession.Clear();SettingsManager.Instance.Apply(original,false);Time.timeScale=1;UIValidation.SetResolution(1920,1080);
            GameSceneManager.Instance.LoadMainMenu();Destroy(gameObject);
        }
    }
}
#endif
