#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Skills;
using CampusRift.Combat;
using CampusRift.Monsters;
namespace CampusRift.SkyBeast
{
    public sealed class P13LayerSmoke:MonoBehaviour
    {
        [Serializable]sealed class Report{public int passed,failed;public List<string> checks=new List<string>();}
        Report report=new Report();
        void Check(bool ok,string text){if(ok)report.passed++;else report.failed++;report.checks.Add((ok?"PASS ":"FAIL ")+text);}
        IEnumerator Start()
        {
            var world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();yield return new WaitForSecondsRealtime(.8f);world.Arrange();
            int env=ShelterDetector.EnvironmentMask;
            Check((world.player.cameraObstacles&env)!=0,"Camera obstacle mask includes Environment");Check((CombatLine.SolidMask&env)!=0,"P12 AI / P10 line and aim mask includes Environment");
            var config=UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterAIConfig>("Assets/MonsterShaban/MonsterAIConfig.asset");
            if(config==null){foreach(var id in UnityEditor.AssetDatabase.FindAssets("t:MonsterAIConfig")){config=UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterAIConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(id));break;}}
            Check(config!=null&&(config.EnvironmentMask&env)!=0,"Shaban ClearLine mask includes Environment");Check(world.camera.farClipPlane>=350,"Camera far clip >=350m");
            var wall=world.player.GetComponent<VoidWallSkill>();var placement=wall.Evaluate(world.origin,Vector3.right);Check(placement.valid,"Void Wall placement on campus ground still valid");
            var deployed=Instantiate(wall.wallPrefab);deployed.Deploy(world.origin,Quaternion.identity,5,world.player.GetComponentsInChildren<Collider>());
            Check(ShelterDetector.ForFire(deployed.transform.position-Vector3.forward)==Shelter.Partial&&ShelterDetector.ForFire(deployed.transform.position+Vector3.forward)==Shelter.Outdoor,"Actual Void Wall shields only the rear as Partial");deployed.Dissolve(false);Destroy(deployed.gameObject);
            var hand=world.player.GetComponent<GiantHandSkill>();world.player.GetComponent<SpiritPower>().Refill();float hp=world.victims[0].Health;bool cast=hand.CastAt(world.victims[0].transform.position);yield return new WaitForSeconds(1);Check(cast&&world.victims[0].Health<hp,"Giant Hand actual cast hits real enemy after layer change");
            var path=new NavMeshPath();Check(NavMesh.CalculatePath(world.origin,new Vector3(10,.13f,-5),NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Courtyard traversal path complete");
            bool stair=false;var graph=ShelterGraphReference.Graph;
            foreach(var n in graph.Nodes){if(n.Kind!=RoomNodeKind.StairLanding)continue;foreach(var edge in n.Neighbors){var q=graph.Nodes[edge.Neighbor];if(Mathf.Abs(n.WorldPosition.y-q.WorldPosition.y)<1)continue;if(NavMesh.CalculatePath(n.WorldPosition,q.WorldPosition,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete){stair=true;break;}}if(stair)break;}
            Check(stair,"Stair traversal across floor heights remains connected");
            world.player.enabled=false;world.PlacePlayer(new Vector3(31,.13f,33));world.Look(90,14);
            typeof(CampusExplorer).GetMethod("UpdateCamera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(world.player,new object[]{0f,true});
            var delta=world.camera.transform.position-(world.player.transform.position+Vector3.up*world.player.cameraHeight);Check(delta.magnitude>=.19f&&delta.magnitude<=world.player.cameraDistance+.05f,"Production camera collision solve contracts within its configured range near corridor: "+delta.magnitude.ToString("F2")+"m");
            Check(Physics.Raycast(new Vector3(31,1.7f,33),Vector3.up,60,env,QueryTriggerInteraction.Ignore),"Environment roof probe works in room");
            world.player.enabled=true;world.End();Directory.CreateDirectory("Artifacts/SkyBeast");File.WriteAllText("Artifacts/SkyBeast/LayerSmoke.json",JsonUtility.ToJson(report,true));File.WriteAllText("Artifacts/SkyBeast/LayerSmoke-DONE.txt",report.passed+" PASS / "+report.failed+" FAIL");Destroy(gameObject);
        }
    }
}
#endif
