#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CampusRift.Validation
{
 [InitializeOnLoad]
 public static class P12BenchmarkBuild
 {
  const string Status="Artifacts/P12/performance/Build-status.txt";
  static bool building,queued;
  static P12BenchmarkBuild(){queued=File.Exists(Status)&&File.ReadAllText(Status)=="Queued";EditorApplication.update+=Tick;}
  [MenuItem("Tools/P12/Build Windows benchmark")]
  public static void Request(){Directory.CreateDirectory(Path.GetDirectoryName(Status));File.WriteAllText(Status,"Queued");queued=true;EditorApplication.QueuePlayerLoopUpdate();}
  static void Tick(){if(!queued||building||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;queued=false;building=true;
   try {File.WriteAllText(Status,"Building");var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");foreach(var brain in UnityEngine.Object.FindObjectsByType<Monsters.MonsterBrain>())brain.gameObject.SetActive(false);
    new GameObject("P12 standalone benchmark boot").AddComponent<P12StandaloneBench>();Directory.CreateDirectory("Assets/Enemies/Validation/Benchmark");EditorSceneManager.SaveScene(scene,"Assets/Enemies/Validation/Benchmark/SampleScene.unity");
    var options=new BuildPlayerOptions{scenes=new[]{"Assets/Enemies/Validation/Benchmark/SampleScene.unity","Assets/Scenes/MainMenu.unity"},locationPathName="Builds/P12Benchmark/CampusRift.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=new[]{"P10_BENCH","P11_BENCH","P12_BENCH"}};
    var report=BuildPipeline.BuildPlayer(options);File.WriteAllText(Status,report.summary.result+" "+report.summary.totalSize+" bytes");
   }catch(Exception ex){File.WriteAllText(Status,ex.ToString());}
   finally {building=false;EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");}
  }
 }
}
#endif
