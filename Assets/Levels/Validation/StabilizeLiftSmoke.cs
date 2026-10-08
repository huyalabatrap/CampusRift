#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Monsters;

public sealed class StabilizeLiftSmoke : MonoBehaviour
{
    string Probe()
    {
        var brain=FindAnyObjectByType<MonsterBrain>();var belief=brain.GetComponent<MonsterBelief>();belief.EnsureGraph();
        var filter=new NavMeshQueryFilter{agentTypeID=brain.GetComponent<NavMeshAgent>().agentTypeID,areaMask=NavMesh.AllAreas};
        int count=0,blocked=0,origins=0;var path=new NavMeshPath();
        foreach(var info in belief.Graph.Lifts)
        {
            var el=info.Elevator;if(el.IsMoving||el.DoorAmount>.3f||info.Landing[0]<0)continue;
            Vector3 inside=el.cabinSpaces[0].center;inside.y=el.floors[0].height;count++;
            if(NavMesh.SamplePosition(info.Lobby[0],out var origin,.15f,filter))origins++;
            bool complete=NavMesh.CalculatePath(info.Lobby[0],inside,filter,path)&&path.status==NavMeshPathStatus.PathComplete&&Vector3.Distance(path.corners[path.corners.Length-1],inside)<.5f;
            if(!complete)blocked++;
        }
        return $"guards={ElevatorShaftGuard.GuardCount}; blocked={blocked}/{count}; walkableOrigins={origins}/{count}; frame={Time.frameCount}";
    }
    IEnumerator Start()
    {
        Directory.CreateDirectory("Artifacts/V2/Stabilize/lifts");
        string before=Probe();
        // NavMesh carving is asynchronous; a synchronous Editor validation in the entry
        // frame observes the uncarved mesh. Wait for the obstacle stationary interval.
        yield return new WaitForSecondsRealtime(.8f);yield return null;
        File.WriteAllText("Artifacts/V2/Stabilize/lifts/carving-readiness.txt",before+"\nsettled: "+Probe());
        var validator=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ShabanHunterValidation")).First(t=>t!=null);
        string result=(string)validator.GetMethod("Lifts",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static).Invoke(null,null);
        File.WriteAllText("Artifacts/V2/Stabilize/lifts/DONE.txt",result);Destroy(gameObject);
    }
}
#endif
