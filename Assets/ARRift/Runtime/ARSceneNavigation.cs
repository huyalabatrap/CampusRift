using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARSceneNavigation:MonoBehaviour
    {
        static bool loading;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){loading=false;}
        public static void Enter(string scene="ARRiftBattle"){Go(scene,false);}
        public static void Exit(){Go("MainMenu",true);}
        static void Go(string scene,bool hub){if(loading)return;loading=true;var host=new GameObject("AR scene transition").AddComponent<ARSceneNavigation>();DontDestroyOnLoad(host.gameObject);host.StartCoroutine(host.Load(scene,hub));}
        IEnumerator Load(string scene,bool hub)
        {
            var loader=FindAnyObjectByType<ARXRLoaderControl>();if(loader!=null)loader.Shutdown();
            yield return SceneManager.LoadSceneAsync(scene);
            UIStateManager.Instance?.EnterScene(false);
            if(hub){float until=Time.realtimeSinceStartup+10;while(HubUI.Instance==null&&Time.realtimeSinceStartup<until)yield return null;UIStateManager.Instance?.OpenHub();}
            loading=false;Destroy(gameObject);
        }
    }
}
