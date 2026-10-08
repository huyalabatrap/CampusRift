#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
namespace CampusRift.AR.Editor
{
    public static class ARRiftMilestones
    {
        [MenuItem("Campus Rift/AR/Install M7")]
        public static void M7()
        {
            foreach(var path in new[]{"Assets/ARRift/Scenes/ARRiftBattle.unity","Assets/ARRift/Scenes/ARGestureDebug.unity"})
            {EditorSceneManager.OpenScene(path);var bridge=Object.FindAnyObjectByType<GestureRecognizerBridge>();if(bridge.GetComponent<ARAdaptiveQuality>()==null)bridge.gameObject.AddComponent<ARAdaptiveQuality>();if(bridge.GetComponent<ARGestureTelemetry>()==null)bridge.gameObject.AddComponent<ARGestureTelemetry>();EditorSceneManager.SaveScene(bridge.gameObject.scene);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
        [MenuItem("Campus Rift/AR/Install M6")]
        public static void M6()
        {
            foreach(var path in new[]{"Assets/ARRift/Scenes/ARRiftBattle.unity","Assets/ARRift/Scenes/ARGestureDebug.unity"})
            {
                EditorSceneManager.OpenScene(path);var loader=Object.FindAnyObjectByType<ARXRLoaderControl>();if(loader.GetComponent<ARSessionBootstrap>()==null)loader.gameObject.AddComponent<ARSessionBootstrap>();
                var f=Object.FindAnyObjectByType<ARBattlefield>();if(f!=null){foreach(var b in new MonoBehaviour[]{f.GetComponent<ARBattleStatus>(),f.GetComponent<ARPlacementHUD>()})if(b!=null)Object.DestroyImmediate(b);if(f.GetComponent<ARBattleHUD>()==null)f.gameObject.AddComponent<ARBattleHUD>();}
                var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/ARRift/Settings/RunicCircle.mat");if(mat==null){mat=new Material(Shader.Find("Campus Rift/AR/Runic Circle"));AssetDatabase.CreateAsset(mat,"Assets/ARRift/Settings/RunicCircle.mat");}
                loader.runeMaterial=mat;
                EditorSceneManager.SaveScene(loader.gameObject.scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");
        }
        [MenuItem("Campus Rift/AR/Install M5")]
        public static void M5()
        {
            const string mapPath="Assets/ARRift/Data/GestureSkillMap.asset";
            System.IO.Directory.CreateDirectory("Assets/ARRift/Data");AssetDatabase.Refresh();
            var map=AssetDatabase.LoadAssetAtPath<GestureSkillMapper>(mapPath);
            if(map==null){map=ScriptableObject.CreateInstance<GestureSkillMapper>();AssetDatabase.CreateAsset(map,mapPath);}
            map.entries=new GestureSkillMapper.Entry[5];
            for(int i=0;i<5;i++)foreach(var guid in AssetDatabase.FindAssets("t:SkillDefinition")){var def=AssetDatabase.LoadAssetAtPath<CampusRift.Skills.SkillDefinition>(AssetDatabase.GUIDToAssetPath(guid));if(def.id==GestureSkillMapper.Ids[i])map.entries[i]=new GestureSkillMapper.Entry{label=GestureSkillMapper.Labels[i],skill=def};}
            foreach(var entry in map.entries)if(entry.skill==null)throw new System.Exception("Missing AR skill definition");EditorUtility.SetDirty(map);
            EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");var f=Object.FindAnyObjectByType<ARBattlefield>();
            var caster=f.GetComponent<ARSkillCaster>()??f.gameObject.AddComponent<ARSkillCaster>();caster.field=f;caster.source=f.GetComponent<GestureRecognizerBridge>();caster.map=map;
            caster.handConfig=AssetDatabase.LoadAssetAtPath<CampusRift.Skills.GiantHandConfig>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:GiantHandConfig")[0]));
            EditorSceneManager.SaveScene(f.gameObject.scene);AssetDatabase.SaveAssets();
        }
        [MenuItem("Campus Rift/AR/Install M4")]
        public static void M4()
        {
            EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");var p=Object.FindAnyObjectByType<RiftPlacementService>();
            AddGestures(p.gameObject);EditorSceneManager.SaveScene(p.gameObject.scene);
            const string path="Assets/ARRift/Scenes/ARGestureDebug.unity";EditorSceneManager.SaveScene(p.gameObject.scene,path,true);EditorSceneManager.OpenScene(path);
            p=Object.FindAnyObjectByType<RiftPlacementService>();var owner=p.gameObject;
            foreach(var b in new MonoBehaviour[]{owner.GetComponent<ARBattleStatus>(),owner.GetComponent<ARMonsterDirector>(),owner.GetComponent<ARBattlefield>(),owner.GetComponent<ARPlacementHUD>(),p})if(b!=null)Object.DestroyImmediate(b);
            var debug=owner.GetComponent<ARGestureDebug>()??owner.AddComponent<ARGestureDebug>();debug.bridge=owner.GetComponent<GestureRecognizerBridge>();EditorSceneManager.SaveScene(owner.scene);
            var scenes=new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);if(!scenes.Exists(x=>x.path==path))scenes.Add(new EditorBuildSettingsScene(path,true));EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
        static void AddGestures(GameObject owner)
        {var bridge=owner.GetComponent<GestureRecognizerBridge>()??owner.AddComponent<GestureRecognizerBridge>();var sampler=owner.GetComponent<FrameSampler>()??owner.AddComponent<FrameSampler>();sampler.bridge=bridge;sampler.cameraManager=Object.FindAnyObjectByType<ARCameraManager>();}
        [MenuItem("Campus Rift/AR/Install M3")]
        public static void M3()
        {
            EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");var p=Object.FindAnyObjectByType<RiftPlacementService>();var f=p.GetComponent<ARBattlefield>()??p.gameObject.AddComponent<ARBattlefield>();
            f.placement=p;f.cameraManager=Object.FindAnyObjectByType<ARCameraManager>();f.occlusion=Object.FindAnyObjectByType<AROcclusionManager>(FindObjectsInactive.Include);f.estimatedLight=Object.FindAnyObjectByType<Light>();
            f.tableAgentType=Agent("ARMonster Table",.15f);f.floorAgentType=Agent("ARMonster Floor",.6f);
            var pipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/ARRift/Settings/AR_RPAsset.asset");pipeline.shadowDistance=8;pipeline.shadowDepthBias=.2f;pipeline.shadowNormalBias=.2f;EditorUtility.SetDirty(pipeline);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/ARRift/Settings/ShadowCatcher.mat");if(material==null){material=new Material(Shader.Find("Campus Rift/AR/Shadow Catcher"));AssetDatabase.CreateAsset(material,"Assets/ARRift/Settings/ShadowCatcher.mat");}f.shadowMaterial=material;
            var d=p.GetComponent<ARMonsterDirector>()??p.gameObject.AddComponent<ARMonsterDirector>();d.field=f;if(p.GetComponent<ARBattleStatus>()==null)p.gameObject.AddComponent<ARBattleStatus>();
            EditorSceneManager.SaveScene(p.gameObject.scene);AssetDatabase.SaveAssets();
        }
        static int Agent(string name,float scale)
        {
            for(int i=0;i<UnityEngine.AI.NavMesh.GetSettingsCount();i++){int id=UnityEngine.AI.NavMesh.GetSettingsByIndex(i).agentTypeID;if(UnityEngine.AI.NavMesh.GetSettingsNameFromID(id)==name)return id;}
            var created=UnityEngine.AI.NavMesh.CreateSettings();
            var obj=UnityEditor.Unsupported.GetSerializedAssetInterfaceSingleton("NavMeshProjectSettings");var s=new SerializedObject(obj);var list=s.FindProperty("m_Settings");
            for(int i=0;i<list.arraySize;i++){var row=list.GetArrayElementAtIndex(i);if(row.FindPropertyRelative("agentTypeID").intValue!=created.agentTypeID)continue;s.FindProperty("m_SettingNames").GetArrayElementAtIndex(i).stringValue=name;row.FindPropertyRelative("agentRadius").floatValue=.28f*scale;row.FindPropertyRelative("agentHeight").floatValue=1.8f*scale;row.FindPropertyRelative("agentClimb").floatValue=.25f*scale;row.FindPropertyRelative("minRegionArea").floatValue=.02f*scale*scale;}
            s.ApplyModifiedPropertiesWithoutUndo();return created.agentTypeID;
        }
        [MenuItem("Campus Rift/AR/Install M2")]
        public static void M2()
        {
            EditorSceneManager.OpenScene("Assets/ARRift/Scenes/ARRiftBattle.unity");
            var settings=AssetDatabase.LoadAssetAtPath<ARModeSettings>("Assets/ARRift/Settings/ARModeSettings.asset");if(settings==null){settings=ScriptableObject.CreateInstance<ARModeSettings>();AssetDatabase.CreateAsset(settings,"Assets/ARRift/Settings/ARModeSettings.asset");}
            var service=Object.FindAnyObjectByType<RiftPlacementService>();if(service==null)service=new GameObject("AR Rift").AddComponent<RiftPlacementService>();
            service.settings=settings;service.planes=Object.FindAnyObjectByType<ARPlaneManager>();service.anchors=Object.FindAnyObjectByType<ARAnchorManager>();service.raycasts=Object.FindAnyObjectByType<ARRaycastManager>();service.view=Camera.main;
            if(service.GetComponent<ARPlacementHUD>()==null)service.gameObject.AddComponent<ARPlacementHUD>();
            EditorSceneManager.SaveScene(service.gameObject.scene);AssetDatabase.SaveAssets();
        }
    }
}
#endif
