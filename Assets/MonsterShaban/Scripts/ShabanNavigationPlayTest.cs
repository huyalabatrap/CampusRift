#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CampusRift;
using CampusRift.Monsters;
using UnityEngine;

// Editor-only harness, attached temporarily through MCP. It never ships in a build.
public sealed class ShabanNavigationPlayTest : MonoBehaviour
{
    IEnumerator Start()
    {
        var brain = Object.FindAnyObjectByType<MonsterBrain>();
        brain.enabled = false;
        brain.GetComponent<MonsterPerception>().enabled = false;
        brain.GetComponent<MonsterHearing>().enabled = false;
        Object.FindAnyObjectByType<CampusExplorer>().enabled = false;
        var nav = brain.GetComponent<MonsterNavigation>();
        nav.Stop(); nav.SetSpeed(6.2f);
        float originalScale = Time.timeScale; Time.timeScale = 3; Application.runInBackground = true;
        var results = new List<string>();
        var targets = new[] { new Vector3(-6.24f,7.22f,-17.59f), new Vector3(-37.94f,17.7f,-19.94f), new Vector3(32.76f,4.02f,27.96f) };
        for (int index = 0; index < targets.Length; index++)
        {
            Vector3 target = targets[index]; float deadline = Time.time + 160;
            float maxJump = 0, highest = brain.transform.position.y;
            int closedDoorOverlaps = 0;
            Vector3 previous = brain.transform.position;
            while (Time.time < deadline && Vector3.Distance(brain.transform.position,target) > 1)
            {
                nav.MoveTo(target);
                maxJump = Mathf.Max(maxJump,Vector3.Distance(brain.transform.position,previous));
                highest = Mathf.Max(highest,brain.transform.position.y); previous = brain.transform.position;
                foreach(var c in Physics.OverlapCapsule(brain.transform.position+Vector3.up*0.25f,brain.transform.position+Vector3.up*1.5f,0.2f,~0,QueryTriggerInteraction.Ignore))
                {var door=c.GetComponentInParent<CampusAutomaticDoor>();if(door!=null&&door.OpenAmount<0.9f)closedDoorOverlaps++;}
                File.WriteAllText("Temp/shaban-nav-progress.txt",$"Route {index+1}: {brain.transform.position} -> {target}; reachable={nav.DestinationReachable}; remaining={deadline-Time.time:F1}");
                yield return null;
            }
            bool pass = Vector3.Distance(brain.transform.position,target) <= 1 && closedDoorOverlaps==0;
            string result = $"Route {index+1} {(pass?"PASS":"FAIL")}: end={brain.transform.position}, target={target}, highest={highest:F2}, maxFrameDisplacement={maxJump:F3}, closedDoorOverlaps={closedDoorOverlaps}";
            results.Add(result); Debug.Log(result);
        }
        Time.timeScale = originalScale; nav.Stop();
        File.WriteAllLines("Assets/MonsterShaban/Validation/NavigationPlayMode.txt",results);
        File.WriteAllText("Temp/shaban-nav-progress.txt","DONE\n"+string.Join("\n",results));
    }
}

#endif
