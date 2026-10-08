#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CampusRift;
using CampusRift.Monsters;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public sealed class ShabanBehaviorPlayTest : MonoBehaviour
{
    [Serializable] sealed class Report { public List<string> passed = new List<string>(); public List<string> failed = new List<string>(); }
    readonly Report report = new Report();
    MonsterBrain brain; MonsterPerception sight; MonsterMemory memory; MonsterNavigation nav;
    CampusExplorer player; CharacterController controller; GameObject wall;
    Vector3 origin;
    void Check(bool value,string label)
    {
        (value?report.passed:report.failed).Add(label);
        Debug.Log("Shaban QA "+(value?"PASS ":"FAIL ")+label);
        File.WriteAllText("Temp/shaban-behavior-progress.txt",JsonUtility.ToJson(report,true));
    }
    void PlacePlayer(Vector3 position)
    { controller.enabled=false;player.transform.position=position;controller.enabled=true;Physics.SyncTransforms(); }
    void PlaceMonster()
    {
        nav.Stop();nav.Agent.enabled=false;brain.transform.SetPositionAndRotation(origin,Quaternion.identity);nav.Agent.enabled=true;Physics.SyncTransforms();
    }
    void WallAt(Vector3 center,Vector3 size)
    {
        wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Shaban QA Wall";wall.transform.position=center;wall.transform.localScale=size;Physics.SyncTransforms();
    }
    void RemoveWall(){if(wall!=null)Object.DestroyImmediate(wall);Physics.SyncTransforms();}
    IEnumerator Start()
    {
        Application.runInBackground=true;
        brain=Object.FindAnyObjectByType<MonsterBrain>();sight=brain.GetComponent<MonsterPerception>();memory=brain.GetComponent<MonsterMemory>();nav=brain.GetComponent<MonsterNavigation>();
        player=Object.FindAnyObjectByType<CampusExplorer>();controller=player.GetComponent<CharacterController>();
        player.enabled=false;brain.enabled=false;sight.enabled=false;
        NavMeshHit hit;NavMesh.SamplePosition(new Vector3(8,0,-4),out hit,1,NavMesh.AllAreas);origin=hit.position;
        var original=brain.config;var config=Object.Instantiate(original);config.SearchDuration=3;config.MemoryDuration=2;
        brain.config=config;memory.config=config;brain.GetComponent<MonsterSearch>().config=config;
        Check(Object.FindObjectsByType<MonsterBrain>().Length==1,"Exactly one Monster_Shaban");
        PlaceMonster();PlacePlayer(origin+Vector3.forward*7);sight.Scan();brain.Decide();
        Check(sight.CanSeePlayer && brain.CurrentState==MonsterState.Chase,"Yard FOV enters Chase");
        nav.Stop();float outsideRange=Mathf.Max(sight.config.VisionDistance,sight.config.TrackingDistance)+5f;PlacePlayer(origin+Vector3.forward*outsideRange);sight.Scan();Check(!sight.CanSeePlayer,"Vision respects configured maximum range ("+outsideRange.ToString("F0")+"m)");
        PlacePlayer(origin+Vector3.back*7);sight.Scan();Check(!sight.CanSeePlayer,"Vision respects FOV");
        PlacePlayer(origin+Vector3.forward*7);yield return null;sight.Scan();brain.Decide();Vector3 recorded=memory.LastSeenPosition;
        WallAt(origin+Vector3.forward*3.5f+Vector3.up*1.5f,new Vector3(8,3,0.3f));
        PlacePlayer(origin+new Vector3(2,0,7));sight.Scan();brain.Decide();
        // Brief occlusion keeps chasing (LostSightGraceTime); after it, Search starts from the last evidence.
        bool graceChase=brain.CurrentState==MonsterState.Chase;
        // This fixture verifies occluded sight and memory at a fixed viewpoint. The cube
        // was created after the campus bake and is not a NavMesh obstacle: letting the
        // agent continue its previous chase crosses it before the grace timer expires.
        nav.Stop();
        yield return new WaitForSeconds(config.LostSightGraceTime+0.05f);
        sight.Scan();brain.Decide();
        Debug.Log("Shaban occlusion probe grace="+graceChase+" visible="+sight.CanSeePlayer+" state="+brain.CurrentState+" monster="+brain.transform.position+" recorded="+recorded+" retained="+memory.LastSeenPosition+" search="+brain.GetComponent<MonsterSearch>().Target);
        Check(graceChase && !sight.CanSeePlayer && brain.CurrentState==MonsterState.Search && memory.LastSeenPosition==recorded,"Wall blocks sight; Search retains last evidence");
        Check(brain.GetComponent<MonsterSearch>().Target==recorded,"Search begins at last seen position");
        RemoveWall();PlacePlayer(origin+Vector3.back*50);sight.Scan();
        float confidence=memory.MemoryConfidence;yield return new WaitForSeconds(0.6f);memory.Decay();
        Check(memory.MemoryConfidence<confidence,"Memory confidence decays");
        var patrol=brain.patrolPoints;brain.patrolPoints=new[]{brain.transform.position};
        float end=Time.time+12f;bool returned=false;
        while(Time.time<end){brain.Decide();if(brain.CurrentState==MonsterState.Patrol)returned=true;yield return new WaitForSeconds(0.25f);}
        Check(returned,"Search expires and returns to Patrol");brain.patrolPoints=patrol;
        PlaceMonster();PlacePlayer(origin+Vector3.back*12);sight.Scan();memory.Forget();
        player.GetComponent<PlayerSoundEmitter>().Emit(PlayerSoundType.HeavyLanding);brain.Decide();
        Check(brain.CurrentState==MonsterState.Investigate && memory.LastHeardPosition==player.transform.position,"Unseen heavy landing enters Investigate");
        nav.Stop();memory.Forget();
        brain.GetComponent<MonsterHearing>().Hear(new SoundEvent(origin+Vector3.back*5,3,PlayerSoundType.HeavyLanding,Time.time-3));
        Check(!memory.HasHeard,"Stale sound events ignored");
        PlaceMonster();PlacePlayer(origin+Vector3.forward*1.15f);sight.Scan();var health=player.GetComponent<PlayerMonsterHealth>();float hp=health.CurrentHealth;
        brain.Decide();Check(brain.CurrentState==MonsterState.Attack,"Attack starts in range");
        yield return new WaitForSeconds(original.AttackWindup+0.15f);
        Check(Mathf.Approximately(health.CurrentHealth,hp-original.Damage),"Attack applies configured damage");
        hp=health.CurrentHealth;brain.Decide();yield return new WaitForSeconds(0.5f);
        Check(Mathf.Approximately(health.CurrentHealth,hp),"Attack cooldown prevents repeated damage");
        yield return new WaitForSeconds(original.AttackCooldown);
        sight.Scan();brain.Decide();WallAt(origin+Vector3.forward*0.6f+Vector3.up*1.2f,new Vector3(3,2.4f,0.12f));
        yield return new WaitForSeconds(original.AttackWindup+0.15f);
        Check(Mathf.Approximately(health.CurrentHealth,hp),"Wall introduced during windup prevents damage");RemoveWall();
        var prediction=brain.GetComponent<MonsterPrediction>();Vector3 right,left,uncertain;
        bool a=prediction.TryLanding(origin+Vector3.up*5,new Vector3(6,-3,0),-25,player.transform,out right);
        bool b=prediction.TryLanding(origin+Vector3.up*5,new Vector3(-6,-3,0),-25,player.transform,out left);
        Check(a&&b&&right.x>origin.x&&left.x<origin.x,"Ballistic prediction follows observed direction");
        Check(!prediction.TryLanding(origin+Vector3.up*35,new Vector3(40,10,0),-25,player.transform,out uncertain),"Uncertain landing prediction rejected");
        nav.Stop();PlacePlayer(origin+Vector3.back*20);Camera.main.transform.position=origin+Vector3.forward*5+Vector3.up*1.5f;
        var source=brain.GetComponent<AudioSource>();var audio=brain.GetComponent<MonsterAudio>();var low=brain.GetComponent<AudioLowPassFilter>();
        Check(source.isPlaying&&source.spatialBlend==1&&source.rolloffMode==AudioRolloffMode.Logarithmic&&source.minDistance==original.AudioMinDistance&&source.maxDistance==original.AudioMaxDistance,"3D audio uses configured distance rolloff");
        WallAt(origin+Vector3.forward*2.5f+Vector3.up*1.5f,new Vector3(6,3,0.3f));yield return new WaitForSeconds(1.5f);
        float muffledVolume=source.volume;Check(audio.Occluded&&low.cutoffFrequency<2000,"Wall muffles spatial audio");
        RemoveWall();yield return new WaitForSeconds(1.5f);
        Check(!audio.Occluded&&low.cutoffFrequency>21000&&source.volume>muffledVolume,"Direct audio recovers smoothly");
        brain.config=original;memory.config=original;brain.GetComponent<MonsterSearch>().config=original;
        Object.Destroy(config);nav.Stop();
        File.WriteAllText("Assets/MonsterShaban/Validation/BehaviorPlayMode.json",JsonUtility.ToJson(report,true));
        File.WriteAllText("Temp/shaban-behavior-progress.txt","DONE\n"+JsonUtility.ToJson(report,true));
    }
}
#endif
