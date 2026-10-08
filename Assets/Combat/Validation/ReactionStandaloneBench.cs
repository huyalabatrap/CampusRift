#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P11_BENCH)
using System.Collections;
using System.IO;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.Enemies;
namespace CampusRift.Combat
{
    public sealed class ReactionStandaloneBench : MonoBehaviour
    {
        public EnemyArchetype enemyTemplate;
        IEnumerator Start()
        {
            if(Application.isEditor)yield break;Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            var rendering=gameObject.AddComponent<ReactionGpuBenchRender>();rendering.Initialize(FindAnyObjectByType<CampusExplorer>().followCamera);
            for(int i=0;i<30;i++)yield return null;SkillSet1TestWorld.RuntimeEnemyTemplate=enemyTemplate;
            var runner=new GameObject("P11 performance").AddComponent<ReactionPerformance>();while(runner!=null)yield return null;
            rendering.Snapshot(Path.GetFullPath("Artifacts/Reactions/fix3/Player-render.png"));yield return new WaitForSeconds(1.5f);Application.Quit();
        }
    }
}
#endif
