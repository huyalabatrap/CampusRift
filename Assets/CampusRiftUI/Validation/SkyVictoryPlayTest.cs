#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CampusRift.Monsters;
using CampusRift.Skills;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CampusRift.UI
{
    public sealed class SkyVictoryPlayTest : MonoBehaviour
    {
        [Serializable] sealed class Report { public List<string> passed=new List<string>(),failed=new List<string>(); }
        readonly Report report=new Report();const string Output="Artifacts/SkyVictory/",Prefs="CampusRift.Settings.v1";
        GameSettings originalSettings;string originalPrefs;bool hadPrefs,restored;
        SettingsManager Manager=>SettingsManager.Instance;
        UIStateManager State=>UIStateManager.Instance;
        UIManager UI=>FindAnyObjectByType<UIManager>();
        SettingsUI Settings=>UI.Settings.GetComponent<SettingsUI>();
        void Check(bool ok,string label)
        {
            (ok?report.passed:report.failed).Add(label);
            File.WriteAllText(Output+"Validation.json",JsonUtility.ToJson(report,true));
            Debug.Log("SKY/WIN QA "+(ok?"PASS ":"FAIL ")+label);
        }
        IEnumerator Loaded(string path)
        {
            float until=Time.realtimeSinceStartup+45;
            while((GameSceneManager.Instance.IsLoading || SceneManager.GetActiveScene().path!=path) && Time.realtimeSinceStartup<until)yield return null;
            yield return null;yield return null;
            Check(SceneManager.GetActiveScene().path==path,"Scene loaded: "+path);
        }
        void CameraShot(string name)
        {
            var camera=Camera.main;var old=camera.targetTexture;var active=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Output+name+".png",texture.EncodeToPNG());}
            finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Destroy(texture);}
        }
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Directory.CreateDirectory(Output);Application.runInBackground=true;
            yield return null;yield return null;
            originalSettings=Manager.Current.Copy();hadPrefs=PlayerPrefs.HasKey(Prefs);originalPrefs=PlayerPrefs.GetString(Prefs,"");
            File.WriteAllText(Output+"Settings-before.json",originalPrefs);
            State.EnterScene(true);State.Pause();State.OpenSettings();yield return new WaitForSecondsRealtime(.35f);
            Check(Settings.SkyBrightness!=null && Settings.SkyBrightness.gameObject.activeInHierarchy,"Sky brightness slider exists in the pause menu Video tab");
            var sourceSky=(Material)typeof(SkyLightingController).GetField("originalSky",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Manager.Sky);
            float assetExposure=sourceSky.GetFloat("_Exposure");
            Settings.SkyBrightness.value=1;float daySun=RenderSettings.sun.intensity;float dayExposure=RenderSettings.skybox.GetFloat("_Exposure");
            CameraShot("Day");Settings.SkyBrightness.value=0;yield return null;
            Check(RenderSettings.sun.intensity<daySun*.05f && RenderSettings.skybox.GetFloat("_Exposure")<dayExposure*.05f,
                "Night preview darkens sky and sunlight immediately while paused");
            Check(RenderSettings.skybox!=sourceSky && sourceSky.GetFloat("_Exposure")==assetExposure,"Preview edits only a runtime sky copy, preserving the source material");
            Check(Manager.Current.SkyBrightness==originalSettings.SkyBrightness,"Preview does not overwrite saved settings");
            CameraShot("Night");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+"Settings-Night.png");yield return null;
            Settings.Cancel();Check(Mathf.Approximately(Manager.Sky.Brightness,originalSettings.SkyBrightness),"Cancel immediately restores saved sky brightness");
            State.OpenSettings();Settings.SkyBrightness.value=.35f;Settings.Apply();
            Check(Mathf.Abs(Manager.ReadSaved().SkyBrightness-.35f)<.001f,"Apply persists sky brightness in player settings");
            Settings.SkyBrightness.value=0;State.Back();
            Check(Mathf.Abs(Manager.Sky.Brightness-.35f)<.001f,"Back discards a subsequent unsaved sky preview");
            GameSceneManager.Instance.LoadMainMenu();yield return Loaded("Assets/Scenes/MainMenu.unity");
            if(State.State==UIState.Hub)State.Back();yield return null;State.OpenSettings();yield return new WaitForSecondsRealtime(.3f);
            Check(Settings.SkyBrightness!=null && Mathf.Abs(Settings.SkyBrightness.value-.35f)<.001f,"P09 Hub → Main Menu exposes the saved sky setting");
            Settings.SkyBrightness.value=0;Settings.Apply();
            GameSceneManager.Instance.StartSandbox();yield return Loaded("Assets/Scenes/SampleScene.unity");
            State.Pause();
            Check(Manager.Sky.Brightness==0 && Mathf.Abs(RenderSettings.sun.intensity-daySun*.03f)<.001f,
                "Starting a game applies the night choice to the newly loaded scene");
            var legacy=SettingsManager.Defaults();JsonUtility.FromJsonOverwrite("{\"MasterVolume\":0.5}",legacy);
            Check(legacy.SkyBrightness==1,"Older saved settings retain the default daylight");
            State.OpenSettings();Settings.SkyBrightness.value=1;Settings.Apply();Settings.Cancel();State.Resume();

            var brain=FindAnyObjectByType<MonsterBrain>();var nav=brain.GetComponent<MonsterNavigation>();
            var monster=brain.GetComponent<MonsterVitality>();var combat=brain.GetComponent<MonsterCombat>();
            var player=FindAnyObjectByType<CampusExplorer>();var health=player.GetComponent<PlayerMonsterHealth>();var skill=player.GetComponent<GiantHandSkill>();
            brain.enabled=false;brain.GetComponent<MonsterPerception>().enabled=false;brain.GetComponent<MonsterHearing>().enabled=false;combat.Interrupt();nav.Stop();
            player.enabled=false;player.spawnPosition=new Vector3(0,.13f,-10);player.spawnYaw=0;player.ReturnToSpawn();
            NavMesh.SamplePosition(new Vector3(0,.05f,-6),out var ground,1,NavMesh.AllAreas);nav.Agent.Warp(ground.position);nav.Stop();Physics.SyncTransforms();
            Check(monster.maxHealth==500 && monster.Health==500,"Production monster starts with exactly 500 HP");
            Check(Mathf.Approximately(skill.config.damagePercent,3f) && Mathf.Approximately(skill.GetComponent<CampusRift.Combat.PlayerStats>().Attack,20f),"Production seal is 300% of 20 Công = 60 damage");
            var qaStats=skill.GetComponent<CampusRift.Combat.PlayerStats>();qaStats.suppressCrit=true;var qaSpirit=skill.GetComponent<CampusRift.Combat.SpiritPower>();
            var production=skill.config;skill.config=Instantiate(production);skill.config.cooldown=.05f;skill.config.aftermathTime=.05f;
            int defeats=0;monster.DefeatedOnce+=()=>defeats++;
            for(int hit=1;hit<=9;hit++)
            {
                Vector3 point=monster.transform.position+(hit==2?Vector3.right*2.5f:Vector3.zero);
                qaSpirit.Refill();
                Check(skill.CastAt(point),"Seal cast accepted: "+hit);
                float until=Time.realtimeSinceStartup+4;
                while(skill.LastHitCount==0 && State.State!=UIState.Victory && Time.realtimeSinceStartup<until)yield return null;
                float expected=Mathf.Max(0,500-60*hit);
                Check(skill.LastHitCount==1 && skill.LastDamage==60 && monster.Health==expected,"Hit "+hit+" deals 60 damage; remaining HP="+expected);
                if(hit<9)
                {
                    Check(State.State==UIState.Gameplay && !monster.Defeated,"No premature victory after hit "+hit);
                    while(skill.IsCasting || skill.CooldownRemaining>0)yield return null;
                }
            }
            Check(State.State==UIState.Victory && defeats==1 && monster.Defeated,"Ninth hit defeats the monster and raises Victory exactly once");
            Check(Time.timeScale==0 && !State.GameplayInputEnabled && !combat.IsAttacking && nav.Agent.isStopped,"Victory stops gameplay and monster combat");
            Check(!monster.ReceiveSeal(60,5) && defeats==1,"Additional hits cannot retrigger victory");
            State.Defeat();Check(State.State==UIState.Victory,"Late damage cannot replace a completed victory with defeat");
            yield return new WaitForSecondsRealtime(.35f);
            Check(UI.Victory.gameObject.activeInHierarchy && UI.Victory.GetComponent<CanvasGroup>().alpha>.99f,"Victory panel becomes visible while gameplay is frozen");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+"Victory.png");yield return null;
            var temporary=skill.config;skill.config=production;Destroy(temporary);
            UI.Victory.transform.Find("Victory Card/REPLAY").GetComponent<Button>().onClick.Invoke();
            yield return Loaded("Assets/Scenes/SampleScene.unity");
            var freshMonster=FindAnyObjectByType<MonsterVitality>();
            Check(freshMonster.Health==500 && !freshMonster.Defeated && State.State==UIState.Gameplay && Time.timeScale==1,"Play Again reloads a live 500 HP monster and resumes gameplay");
            FindAnyObjectByType<PlayerMonsterHealth>().TakeDamage(10000);
            Check(State.State==UIState.GameOver && !UI.Victory.gameObject.activeSelf,"Player death still produces defeat rather than victory");
            Restore();GameSceneManager.Instance.ReloadCurrentScene();yield return Loaded("Assets/Scenes/SampleScene.unity");
            File.WriteAllText(Output+"DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");
            Destroy(gameObject);
        }
        void Restore()
        {
            if(restored || originalSettings==null)return;restored=true;
            if(Manager!=null)Manager.Apply(originalSettings,false);
            if(hadPrefs)PlayerPrefs.SetString(Prefs,originalPrefs);else PlayerPrefs.DeleteKey(Prefs);
            PlayerPrefs.Save();
        }
        void OnDestroy()=>Restore();
    }
}
#endif
