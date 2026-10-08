#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Monsters;
namespace CampusRift.SkyBeast
{
    public sealed class ShelterAuditPlayTest:MonoBehaviour
    {
        [Serializable]public sealed class Mismatch{public string room,building,kind,actual,expected,reason;public int floor,index;public Vector3 position;}
        [Serializable]public sealed class Result{public int nodes,passed,failed;public float accuracy;public bool pass;public List<Mismatch> mismatches=new List<Mismatch>();public List<Mismatch> accepted=new List<Mismatch>();}
        public static Result Run()
        {
            Physics.SyncTransforms();var graph=ShelterGraphReference.Graph;var result=new Result();if(graph==null)throw new InvalidOperationException("Shelter graph reference missing");
            result.nodes=graph.Nodes.Length;
            for(int i=0;i<graph.Nodes.Length;i++)
            {
                var n=graph.Nodes[i];var s=ShelterDetector.AtFeet(n.WorldPosition);
                bool outdoor=n.Kind==RoomNodeKind.Outdoor;
                bool door=n.Kind==RoomNodeKind.Door&&n.WorldPosition.y<1;
                bool groundExit=n.Kind==RoomNodeKind.Exit&&n.WorldPosition.y<1;
                bool ok=outdoor?s!=Shelter.Indoor:door?s!=Shelter.Outdoor:groundExit?true:s==Shelter.Indoor;
                if(ok)result.passed++;else result.failed++;
                var record=new Mismatch{index=i,room=n.RoomID,building=n.BuildingID,floor=n.FloorID,kind=n.Kind.ToString(),position=n.WorldPosition,actual=s.ToString(),expected=outdoor?"Outdoor/Partial":door||groundExit?"Ground threshold": "Indoor"};
                if(!ok)result.mismatches.Add(record);
                else if((door||groundExit)&&s==Shelter.Outdoor){record.reason="Ground-floor approach/exit is on the outdoor side of the doorway; graph includes both sides.";result.accepted.Add(record);}
            }
            result.accuracy=result.passed*100f/Mathf.Max(1,result.nodes);result.pass=result.accuracy>=98;
            Directory.CreateDirectory("Artifacts/SkyBeast");File.WriteAllText("Artifacts/SkyBeast/ShelterAudit.json",JsonUtility.ToJson(result,true));File.WriteAllText("Artifacts/SkyBeast/ShelterAudit-DONE.txt",result.accuracy+"%; "+result.passed+"/"+result.nodes+" "+(result.pass?"PASS":"FAIL"));return result;
        }
        IEnumerator Start(){yield return null;Run();Destroy(gameObject);}
    }
}
#endif
