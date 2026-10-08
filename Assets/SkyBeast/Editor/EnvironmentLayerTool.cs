#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CampusRift.SkyBeast
{
    public static class EnvironmentLayerTool
    {
        [MenuItem("Campus Rift/V2/Assign Environment Layer")]
        public static void Assign()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Exit Play first");
            int layer=LayerMask.NameToLayer("Environment");if(layer<0)throw new InvalidOperationException("Environment layer missing");
            var campus=GameObject.Find("Comic_Vibrant_Elevator_System_T77");if(campus==null)throw new InvalidOperationException("Campus missing");
            int count=0,masks=0;
            foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(c.isTrigger||c.GetComponentInParent<CampusExplorer>()!=null||c.GetComponentInParent<Monsters.MonsterVitality>()!=null||c.GetComponentInParent<Skills.VoidWall>()!=null)continue;
                // Collider copies for automatic doors and elevators live outside the FBX hierarchy.
                if(!c.transform.IsChildOf(campus.transform)&&!c.name.StartsWith("Block_")&&!c.name.StartsWith("Entrance")&&!c.transform.root.name.Contains("Door")&&!c.transform.root.name.Contains("Elevator"))continue;
                Undo.RecordObject(c.gameObject,"Environment layer");c.gameObject.layer=layer;PrefabUtility.RecordPrefabInstancePropertyModifications(c.gameObject);count++;
            }
            foreach(var b in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var so=new SerializedObject(b);var it=so.GetIterator();bool edit=false;
                while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.LayerMask&&(it.intValue&1)!=0&&(it.intValue&(1<<layer))==0){it.intValue|=1<<layer;edit=true;masks++;}
                if(edit)so.ApplyModifiedProperties();
            }
            var player=UnityEngine.Object.FindAnyObjectByType<CampusExplorer>();if(player!=null&&player.followCamera!=null){player.followCamera.farClipPlane=Mathf.Max(350,player.followCamera.farClipPlane);EditorUtility.SetDirty(player.followCamera);}
            EditorSceneManager.MarkSceneDirty(campus.scene);EditorSceneManager.SaveScene(campus.scene);
            Directory.CreateDirectory("Artifacts/SkyBeast");File.WriteAllText("Artifacts/SkyBeast/EnvironmentLayer.json","{\"colliders\":"+count+",\"masksAdded\":"+masks+",\"farClip\":350}");
        }
    }
}
#endif
