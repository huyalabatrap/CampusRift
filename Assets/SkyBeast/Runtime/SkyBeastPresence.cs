using System.Collections;
using UnityEngine;
namespace CampusRift.SkyBeast
{
    public sealed class SkyBeastPresence : MonoBehaviour
    {
        static SkyBeastPresence active;
        public static SkyBeastController Spawn(string id,float offset=0){var d=Resources.Load<SkyBeastDefinition>("P12/Dragon"+id);if(d==null)return null;var go=Instantiate(d.prefab);go.name="Sky Beast "+id;var c=go.GetComponent<SkyBeastController>();if(c==null)c=go.AddComponent<SkyBeastController>();c.Initialize(d,offset);if(active!=null)go.transform.SetParent(active.transform);return c;}
        public static void Begin(int level){StopAll();SkyBeastScheduler.Begin(level);}
        public static void StopAll(){SkyBeastScheduler.StopAll();if(active!=null){Destroy(active.gameObject);active=null;}foreach(var c in FindObjectsByType<SkyBeastController>(FindObjectsSortMode.None))Destroy(c.gameObject);}
    }
}
