#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CampusRift.Monsters;
namespace CampusRift.SkyBeast
{
    public static class P13ShelterVolumes
    {
        [System.Serializable]sealed class Entry{public string name,reason;public Vector3 center,size;}
        [System.Serializable]sealed class Manifest{public List<Entry> volumes=new List<Entry>();}
        public static void Apply()
        {
            var old=GameObject.Find("P13 Indoor Volumes");if(old!=null)Object.DestroyImmediate(old);
            var root=new GameObject("P13 Indoor Volumes");var graph=ShelterGraphReference.Graph;var manifest=new Manifest();
            // Enclosed stair shafts have narrow ramps that straddle the 0.6m roof probes.
            // Cover each physical shaft as one volume, never one test-node volume.
            foreach(var group in graph.Nodes.Where(n=>n.Kind==RoomNodeKind.StairLanding&&n.BuildingID!="Campus").GroupBy(n=>n.BuildingID))
            {
                var left=group.ToList();int serial=0;
                while(left.Count>0)
                {
                    var cluster=new List<RoomNode>{left[0]};left.RemoveAt(0);bool grew=true;
                    while(grew){grew=false;for(int i=left.Count-1;i>=0;i--)if(cluster.Any(n=>Vector3.ProjectOnPlane(n.WorldPosition-left[i].WorldPosition,Vector3.up).sqrMagnitude<25)){cluster.Add(left[i]);left.RemoveAt(i);grew=true;}}
                    if(cluster.All(n=>ShelterDetector.AtFeet(n.WorldPosition)==Shelter.Indoor))continue;
                    var bounds=new Bounds(cluster[0].WorldPosition+Vector3.up*1.6f,Vector3.zero);foreach(var n in cluster)bounds.Encapsulate(n.WorldPosition+Vector3.up*1.6f);bounds.Expand(new Vector3(.9f,.8f,.9f));
                    Add(root.transform,group.Key+" enclosed stair shaft "+(++serial),bounds,"Enclosed stair shaft: narrow ramp/landing probes cross wall edge; top landing has missing roof collider.",manifest);
                }
            }
            // Observation nodes beside the same stair shafts can sit just past the ramp samples.
            // Extend an existing shaft volume by <=1m where the physical stair-room label matches.
            foreach(var n in graph.Nodes.Where(n=>n.Kind==RoomNodeKind.Observation&&ShelterDetector.AtFeet(n.WorldPosition)!=Shelter.Indoor))
            {
                if(!n.RoomID.Contains("Stair"))continue;
                var p=n.WorldPosition+Vector3.up*1.6f;var nearby=root.GetComponentsInChildren<BoxCollider>().OrderBy(b=>Vector3.ProjectOnPlane(b.bounds.center-p,Vector3.up).sqrMagnitude).FirstOrDefault();
                if(nearby!=null&&Vector3.ProjectOnPlane(nearby.bounds.center-p,Vector3.up).magnitude<5){var b=nearby.bounds;b.Encapsulate(p+new Vector3(.35f,.35f,.35f));b.Encapsulate(p-new Vector3(.35f,.35f,.35f));nearby.transform.position=b.center;nearby.size=b.size;}
                else Add(root.transform,n.BuildingID+" enclosed stair observation",new Bounds(p,new Vector3(1.8f,3.2f,1.8f)),"Observation point in enclosed stair room at a roof-probe edge.",manifest);
            }
            EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);File.WriteAllText("Artifacts/SkyBeast/IndoorVolumes.json",JsonUtility.ToJson(manifest,true));
        }
        static void Add(Transform root,string name,Bounds bounds,string reason,Manifest manifest)
        {
            var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=bounds.center;go.layer=2;
            var box=go.AddComponent<BoxCollider>();box.isTrigger=true;box.size=bounds.size;var volume=go.AddComponent<IndoorVolume>();volume.shelter=Shelter.Indoor;volume.reason=reason;volume.priority=5;
            manifest.volumes.Add(new Entry{name=name,reason=reason,center=bounds.center,size=bounds.size});
        }
    }
}
#endif
