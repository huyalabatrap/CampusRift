#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using CampusRift.UI;
using UnityEngine;
namespace CampusRift.Learning
{
    public sealed class LearningRegressionRunner : MonoBehaviour
    {
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;
            var types=new[]{typeof(Controls.MobileControlPlayTest),typeof(Controls.BoostEnergyPlayTest),typeof(ShabanPressurePlayTest)};
            var folders=new[]{"MobileControls","BoostEnergy","ShabanPressure"};
            for(int i=0;i<types.Length;i++)
            {
                GameSceneManager.Instance.StartSandbox();float until=Time.realtimeSinceStartup+60;
                while(GameSceneManager.Instance.IsLoading && Time.realtimeSinceStartup<until)yield return null;
                UIStateManager.Instance.EnterScene(true);
                yield return null;yield return null;
                var hud=FindAnyObjectByType<Controls.MobileControlsHUD>();
                if(hud==null || hud.SafeRoot==null)throw new Exception("Gameplay HUD did not initialize before regression test.");
                string folder="Artifacts/"+folders[i]+"/";string done=folder+"DONE.txt";
                var stamp=File.Exists(done)?File.GetLastWriteTimeUtc(done):DateTime.MinValue;
                new GameObject("Learning regression / "+folders[i]).AddComponent(types[i]);
                until=Time.realtimeSinceStartup+180;
                while((!File.Exists(done) || File.GetLastWriteTimeUtc(done)==stamp) && Time.realtimeSinceStartup<until)yield return null;
                if(Time.realtimeSinceStartup>=until)throw new Exception("Regression timeout: "+folders[i]);
                File.Copy(folder+"Validation.json","Artifacts/Learning/Regression-"+folders[i]+".json",true);
            }
            GameSceneManager.Instance.LoadMainMenu();
            File.WriteAllText("Artifacts/Learning/Regression-DONE.txt","MobileControls, BoostEnergy, ShabanPressure finished");
            Destroy(gameObject);
        }
    }
}
#endif
