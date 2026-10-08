#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CampusRift;
using CampusRift.Monsters;
using CampusRift.Skills;
using CampusRift.UI;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// Explicit Play Mode regression harness; never attached to shipped scenes.
public sealed class ShabanPressurePlayTest : MonoBehaviour
{
    [Serializable] sealed class Report
    { public List<string> passed=new List<string>(), failed=new List<string>(), notes=new List<string>(); }
    readonly Report report=new Report();
    const string Output="Artifacts/ShabanPressure/";
    readonly Vector3 origin=new Vector3(1000,0,1000);
    MonsterBrain brain; MonsterNavigation nav; MonsterPerception sight; MonsterMemory memory;
    MonsterCombat combat; MonsterHearing hearing; MonsterVitality vitality;
    CampusExplorer player; CharacterController controller; PlayerMonsterHealth health;
    GameObject floor,wall; NavMeshDataInstance mesh;
    bool running,allowPause,escalationBefore;
    void Update()
    { if(running && !allowPause && UIStateManager.Instance!=null && UIStateManager.Instance.State!=UIState.Gameplay) UIStateManager.Instance.EnterScene(true); }
    void Check(bool ok,string label)
    {
        (ok?report.passed:report.failed).Add(label);
        File.WriteAllText(Output+"Validation.json",JsonUtility.ToJson(report,true));
        Debug.Log("PRESSURE QA "+(ok?"PASS ":"FAIL ")+label);
    }
    void Pressure(float seconds) => typeof(MonsterBrain).GetProperty("PressureSeconds").SetValue(brain,seconds);
    void PlacePlayer(Vector3 p)
    {
        controller.enabled=false;player.transform.position=p+Vector3.up*.1f;controller.enabled=true;
        controller.Move(Vector3.down*.2f);Physics.SyncTransforms();
    }
    void PlaceMonster()
    { nav.Stop();combat.Interrupt();nav.Agent.Warp(origin);brain.transform.rotation=Quaternion.identity;Physics.SyncTransforms(); }
    IEnumerator Start()
    {
        Directory.CreateDirectory(Output);running=true;Application.runInBackground=true;
        UIStateManager.Instance?.EnterScene(true);
        brain=Object.FindAnyObjectByType<MonsterBrain>();nav=brain.GetComponent<MonsterNavigation>();
        sight=brain.GetComponent<MonsterPerception>();memory=brain.GetComponent<MonsterMemory>();
        combat=brain.GetComponent<MonsterCombat>();hearing=brain.GetComponent<MonsterHearing>();vitality=brain.GetComponent<MonsterVitality>();
        player=Object.FindAnyObjectByType<CampusExplorer>();controller=player.GetComponent<CharacterController>();health=player.GetComponent<PlayerMonsterHealth>();
        player.enabled=false;brain.enabled=false;sight.enabled=false;hearing.enabled=false;
        // V2 ships with the time ramp off; this harness exercises the ramp itself, so it switches it on for its own run.
        escalationBefore=brain.config.enableTimeEscalation;
        brain.config.enableTimeEscalation=false;Pressure(900);
        Check(brain.MovementMultiplier==1 && brain.AttackRateMultiplier==1 && brain.Pressure==0,"With time escalation disabled speed and attack rate stay at their base values");
        brain.config.enableTimeEscalation=true;Pressure(0);
        brain.GetComponent<MonsterBelief>().enabled=false;
        health.maxHealth=10000;typeof(PlayerMonsterHealth).GetField("currentHealth",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(health,10000f);
        floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Pressure QA floor";
        floor.transform.position=origin+new Vector3(0,-.1f,100);floor.transform.localScale=new Vector3(50,.2f,340);
        var source=new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(floor.transform.position,Quaternion.identity,Vector3.one),size=floor.transform.localScale};
        var settings=NavMesh.GetSettingsByID(nav.Agent.agentTypeID);settings.overrideVoxelSize=true;settings.voxelSize=.12f;
        mesh=NavMesh.AddNavMeshData(NavMeshBuilder.BuildNavMeshData(settings,new List<NavMeshBuildSource>{source},
            new Bounds(origin+Vector3.forward*100,new Vector3(55,8,345)),Vector3.zero,Quaternion.identity));
        PlaceMonster();PlacePlayer(origin+Vector3.forward*12);Pressure(0);
        Check(nav.Ready && Mathf.Approximately(nav.ChaseSpeed,5.5f),"New run starts at 5.5 m/s chase speed");
        brain.enabled=true;yield return new WaitForSeconds(.25f);
        Check(brain.PressureSeconds>.15f,"Difficulty accumulates active gameplay time");
        allowPause=true;UIStateManager.Instance.Pause();float paused=brain.PressureSeconds;
        yield return new WaitForSecondsRealtime(.25f);
        Check(brain.PressureSeconds==paused,"Pause does not advance difficulty");
        UIStateManager.Instance.Resume();allowPause=false;brain.enabled=false;PlaceMonster();
        Pressure(4.99f);Check(Mathf.Approximately(nav.ChaseSpeed,5.5f),"Chase speed stays at 5.5 until the first full five seconds");
        Pressure(5);Check(Mathf.Approximately(nav.ChaseSpeed,5.6f),"First five-second tick adds 0.1 m/s");
        Pressure(9.99f);Check(Mathf.Approximately(nav.ChaseSpeed,5.6f),"Speed remains constant between ticks");
        Pressure(10);Check(Mathf.Approximately(nav.ChaseSpeed,5.7f),"Second tick accumulates another 0.1 m/s");
        Pressure(90);nav.SetSpeed(brain.config.ChaseSpeed);
        Check(Mathf.Abs(nav.Agent.speed-7.3f)<.02f && Mathf.Abs(combat.CooldownSeconds-1.2f)<.01f,
            "90 seconds raises chase speed to 7.3 m/s and attack interval to 1.2s");
        Pressure(180);nav.SetSpeed(brain.config.ChaseSpeed);
        Check(Mathf.Abs(nav.Agent.speed-9.1f)<.02f && Mathf.Abs(combat.CooldownSeconds-.9f)<.01f && Mathf.Abs(combat.WindupSeconds-.175f)<.01f,
            "180 seconds reaches 9.1 m/s, 0.9s attack interval and 0.175s windup");
        Pressure(724.99f);Check(Mathf.Approximately(nav.ChaseSpeed,19.9f),"Penultimate tick stays below the cap");
        Pressure(725);Check(Mathf.Approximately(nav.ChaseSpeed,20f),"725 seconds reaches exactly 20 m/s");
        Pressure(900);Check(Mathf.Approximately(nav.ChaseSpeed,20f) && brain.AttackRateMultiplier==2,
            "Long sessions respect configured speed caps");
        nav.Stop();

        PlacePlayer(origin+Vector3.forward*60);sight.Scan();
        Check(sight.CanSeePlayer,"Player acquired at 60m in clear sight");
        PlacePlayer(origin+Vector3.forward*85);sight.Scan();
        Check(sight.CanSeePlayer,"Known running target remains visible at 85m");
        PlacePlayer(origin+Vector3.forward*95);sight.Scan();
        Check(!sight.CanSeePlayer,"Tracking still respects its 90m maximum");
        PlacePlayer(origin+Vector3.right*15);sight.Scan();
        Check(sight.CanSeePlayer,"Wider tracking cone retains a target moving past the flank");
        PlacePlayer(origin+Vector3.back*15);sight.Scan();
        Check(!sight.CanSeePlayer,"Tracking does not see directly behind the monster");
        PlacePlayer(origin+Vector3.forward*20);yield return null;sight.Scan();Vector3 known=memory.LastSeenPosition;
        wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=origin+new Vector3(0,2,10);wall.transform.localScale=new Vector3(40,4,.3f);
        PlacePlayer(origin+new Vector3(3,0,30));sight.Scan();
        Check(!sight.CanSeePlayer && memory.LastSeenPosition==known,"Wall blocks sight and hidden movement does not reveal a new position");
        Destroy(wall);yield return null;

        PlaceMonster();PlacePlayer(origin+Vector3.back*55);sight.Scan();memory.Forget();
        hearing.Hear(new SoundEvent(player.transform.position,1.5f,PlayerSoundType.Sprint,Time.time,player.transform));
        brain.Decide();
        Check(memory.HasHeard && brain.CurrentState==MonsterState.Investigate && Vector3.Distance(nav.CurrentDestination,player.transform.position)<1,
            "Sprint at 55m produces a navigable sound destination");
        Check(nav.Agent.speed>14 && !nav.Agent.isStopped,"Fresh distant footsteps are pursued at escalated chase speed");
        nav.Stop();memory.Forget();
        hearing.Hear(new SoundEvent(origin+Vector3.back*80,5,PlayerSoundType.Combat,Time.time));
        Check(!memory.HasHeard,"Out-of-range sounds remain undetected");
        hearing.Hear(new SoundEvent(origin+Vector3.forward*5,3,PlayerSoundType.Sprint,Time.time-3));
        Check(!memory.HasHeard,"Expired sounds cannot renew a hunt");
        memory.RememberSound(origin+Vector3.forward*10,.5f,Time.time);yield return new WaitForSeconds(.2f);
        memory.RememberSound(origin+Vector3.forward*16,.5f,Time.time);
        Check(memory.LatestVelocity.magnitude>12,"Sound tracking retains motion faster than the old 12 m/s cutoff");
        nav.Stop();PlaceMonster();PlacePlayer(origin+Vector3.forward*1.1f);

        // Actual animation timing and repeated damage, not only configured multipliers.
        Pressure(180);yield return new WaitForSeconds(2);float hp=health.CurrentHealth;
        sight.Scan();combat.TryAttack();float swing=Time.time;
        yield return new WaitForSeconds(.08f);
        Check(combat.WindingUp && health.CurrentHealth==hp,"Escalated attack still has a damage-free windup");
        yield return new WaitForSeconds(.13f);
        Check(health.CurrentHealth==hp-combat.config.Damage,"Fast animation applies exactly one strike after 0.175s");
        while(Time.time<swing+.94f)yield return null;
        sight.Scan();combat.TryAttack();Check(combat.WindingUp,"Next attack starts after the shortened 0.9s interval");
        combat.Interrupt();yield return new WaitForSeconds(1);

        // Full AI must catch and damage a continuously sprinting character on a clear lane.
        PlaceMonster();PlacePlayer(origin+Vector3.forward*18);Pressure(725);sight.Scan();
        brain.enabled=true;sight.enabled=true;int hits=combat.Hits;float began=Time.time;
        float runSpeed=player.runSpeed;float travelled=0;bool retained=true;float nextNote=0,minDistance=float.PositiveInfinity,maxSightGap=0;
        UnityEngine.Events.UnityAction swingNote=()=>report.notes.Add("Swing at "+(Time.time-began).ToString("F2")+"s, distance="+Vector3.Distance(brain.transform.position,player.transform.position).ToString("F2"));
        combat.AttackStarted.AddListener(swingNote);
        while(Time.time-began<12 && combat.Hits==hits)
        {
            Vector3 before=player.transform.position;
            controller.Move((Vector3.forward*runSpeed+Vector3.down*2)*Time.deltaTime);
            travelled+=Vector3.ProjectOnPlane(player.transform.position-before,Vector3.up).magnitude;
            // At contact the faster monster can briefly overtake the player, putting them
            // outside its 220-degree cone. Test the configured tracking retention,
            // not uninterrupted vision (which incorrectly requires seeing behind itself).
            float sightGap=Time.time-sight.ObservationTime;maxSightGap=Mathf.Max(maxSightGap,sightGap);
            retained &= sight.TrackingPlayer || Time.time-began<.25f;
            float distance=Vector3.Distance(brain.transform.position,player.transform.position);minDistance=Mathf.Min(minDistance,distance);
            if(Time.time>=nextNote)
            {
                nextNote=Time.time+1;
                report.notes.Add("Chase "+(Time.time-began).ToString("F2")+"s gap="+distance.ToString("F2")+" state="+brain.CurrentState+" speed="+nav.Agent.velocity.magnitude.ToString("F2")+" destination="+nav.CurrentDestination+" intent="+brain.Intent);
            }
            yield return null;
        }
        combat.AttackStarted.RemoveListener(swingNote);
        report.notes.Add("Sprint="+runSpeed+" m/s; elapsed="+(Time.time-began).ToString("F2")+"s; travelled="+travelled.ToString("F1")+"m; monster speed="+nav.Agent.speed+" min gap="+minDistance);
        report.notes.Add("Maximum time since visual observation during chase: "+maxSightGap.ToString("F3")+"s");
        Check(retained,"Fast moving target stays tracked through brief exits from the vision cone");
        Check(combat.Hits>hits && travelled>15,"Escalated monster catches and damages an uninterrupted full-speed sprint");

        // F must remain useful against the fastest monster.
        brain.enabled=false;sight.enabled=false;PlaceMonster();PlacePlayer(origin+Vector3.forward*8);Pressure(725);
        var skill=player.GetComponent<GiantHandSkill>();float beforeSeal=vitality.Health;
        Check(skill.CastNearest() && vitality.Suppressed,"F still binds the fully escalated monster immediately");
        Vector3 held=brain.transform.position;brain.enabled=true;sight.enabled=true;
        while(skill.LastHitCount==0 && skill.IsCasting)yield return null;
        Check(vitality.Health<beforeSeal && vitality.SuppressionRemaining>2.9f,"F still applies three seconds of stun at maximum difficulty");
        bool stayed=true;float until=Time.time+vitality.SuppressionRemaining;
        while(Time.time<until-.1f)
        {stayed &= Vector3.Distance(held,brain.transform.position)<.05f && !combat.IsAttacking;yield return null;}
        Check(stayed,"Escalation cannot override the F position lock or attack suppression");
        while(vitality.Suppressed)yield return null;
        yield return new WaitForSeconds(.3f);
        Check(brain.Pressure>=1 && !nav.Agent.isStopped && Vector3.Distance(held,brain.transform.position)>.1f,
            "After stun the monster resumes pursuit without resetting accumulated difficulty");
        brain.enabled=false;nav.Stop();running=false;
        File.WriteAllText(Output+"DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");
    }
    void OnDestroy()
    { if(brain!=null && brain.config!=null)brain.config.enableTimeEscalation=escalationBefore;
      if(mesh.valid)mesh.Remove();if(floor!=null)Destroy(floor);if(wall!=null)Destroy(wall); }
}
#endif
