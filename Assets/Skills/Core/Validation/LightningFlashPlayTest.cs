#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    public sealed class LightningFlashPlayTest:MonoBehaviour
    {
        [System.Serializable] sealed class Result{public bool cast,piercesThree,immune,wallStops,poolStable;public float distance,wallDistance;public int hits,particles,objectsBefore,objectsAfter;}
        IEnumerator Start()
        {
            var world=new SkillSet1TestWorld();world.Begin();var player=world.player;var skill=player.GetComponent<LightningFlashRuntime>();var pool=player.GetComponent<SkillVfxPool>();var spirit=player.GetComponent<SpiritPower>();var result=new Result();
            Directory.CreateDirectory("task/p10/screens/frames");Directory.CreateDirectory("Artifacts/Skills");
            yield return null;world.Arrange();yield return null;
            result.objectsBefore=pool.GetComponentsInChildren<Transform>(true).Length;result.cast=skill.CastAt(world.origin+Vector3.right*10);
            float[] times={.03f,.11f,.16f,.22f,.35f,.48f,.7f,.95f};float start=Time.time;
            for(int i=0;i<times.Length;i++)
            {
                while(Time.time-start<times[i])yield return null;
                if(i==2)result.immune=player.GetComponent<PlayerMonsterHealth>().Invulnerable;
                // Keep timing samples free of repeated framebuffer readback stalls.
                yield return new WaitForEndOfFrame();
            }
            result.hits=skill.LastHitCount;result.piercesThree=result.hits>=3;result.distance=player.LastDashDistance;result.particles=pool.PeakParticles;
            yield return new WaitForSeconds(.6f);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="P10 QA wall";wall.transform.position=world.origin+Vector3.right*4+Vector3.up;wall.transform.localScale=new Vector3(.3f,3,5);Physics.SyncTransforms();world.Arrange();skill.ResetCooldownForValidation();spirit.Refill();skill.CastAt(world.origin+Vector3.right*10);
            yield return new WaitForSeconds(1.8f);result.wallDistance=player.LastDashDistance;result.wallStops=result.wallDistance<4;Destroy(wall);yield return null;
            for(int i=0;i<10;i++){world.Arrange();skill.ResetCooldownForValidation();spirit.Refill();skill.CastAt(world.origin+Vector3.right*10);yield return new WaitForSeconds(1.8f);}
            result.objectsAfter=pool.GetComponentsInChildren<Transform>(true).Length;result.poolStable=result.objectsBefore==result.objectsAfter;
            world.Mode(true);world.Arrange();skill.ResetCooldownForValidation();spirit.Refill();skill.CastAt(world.origin+Vector3.right*10);yield return new WaitForSeconds(1.8f);
            File.WriteAllText("Artifacts/Skills/LightningFlash.json",JsonUtility.ToJson(result,true));File.WriteAllText("Artifacts/Skills/LightningFlash-DONE.txt",result.cast&&result.piercesThree&&result.immune&&result.wallStops&&result.poolStable?"PASS":"FAIL");
            world.End();Debug.Log("P10 FLASH QA "+JsonUtility.ToJson(result));Destroy(gameObject);
        }
    }
}
#endif
