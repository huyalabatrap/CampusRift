#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.ARFoundation;
namespace CampusRift.AR.Editor
{
    public static class ARFix3Setup
    {
        public static void Install()
        {
            foreach(var path in new[]{"Assets/ARRift/Scenes/ARRiftBattle.unity","Assets/ARRift/Scenes/ARGestureDebug.unity"})
            {
                var scene=EditorSceneManager.OpenScene(path);var loader=Object.FindAnyObjectByType<ARXRLoaderControl>();var camera=Object.FindAnyObjectByType<ARCameraManager>(FindObjectsInactive.Include);camera.autoFocusRequested=true;
                var mode=AssetDatabase.LoadAssetAtPath<ARModeSettings>("Assets/ARRift/Settings/ARModeSettings.asset");var debug=Object.FindAnyObjectByType<ARGestureDebug>();if(debug!=null)debug.settings=mode;EditorUtility.SetDirty(mode);
                var planes=Object.FindAnyObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
                if(planes!=null){var cloud=planes.GetComponent<ARPointCloudManager>()??planes.gameObject.AddComponent<ARPointCloudManager>();cloud.enabled=false;var components=new System.Collections.Generic.List<Behaviour>(loader.sessionComponents);if(!components.Contains(cloud))components.Add(cloud);loader.sessionComponents=components.ToArray();}
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }
    }
}
#endif
