#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P10_BENCH)
using System.Collections;
using UnityEngine;
using CampusRift.Enemies;
namespace CampusRift.Skills
{
    public sealed class SkillSet1StandaloneBench:MonoBehaviour
    {
        public EnemyArchetype enemyTemplate;
        IEnumerator Start()
        {
            if(Application.isEditor)yield break;
            Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            var rendering=gameObject.AddComponent<CampusRift.Combat.ReactionGpuBenchRender>();rendering.Initialize(FindAnyObjectByType<CampusExplorer>().followCamera);
            for(int i=0;i<30;i++)yield return null;
            SkillSet1TestWorld.RuntimeEnemyTemplate=enemyTemplate;
            var runner=new GameObject("P10 standalone performance").AddComponent<SkillSet1Performance>();
            while(runner!=null)yield return null;
            rendering.Snapshot(System.IO.Path.GetFullPath("Artifacts/Skills/fix3/Player-render.png"));
            Application.Quit();
        }
    }
}
#endif
