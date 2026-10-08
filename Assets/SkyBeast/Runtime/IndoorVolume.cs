using System.Collections.Generic;
using UnityEngine;
namespace CampusRift.SkyBeast
{
    [RequireComponent(typeof(BoxCollider)),DisallowMultipleComponent]
    public sealed class IndoorVolume:MonoBehaviour
    {
        public Shelter shelter=Shelter.Indoor;
        public int priority;
        public string reason;
        static readonly List<IndoorVolume> active=new List<IndoorVolume>();
        BoxCollider box;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics()=>active.Clear();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void RebuildForPlay()
        {foreach(var v in Object.FindObjectsByType<IndoorVolume>(FindObjectsSortMode.None)){v.box=v.GetComponent<BoxCollider>();if(v.isActiveAndEnabled&&!active.Contains(v))active.Add(v);}}
        void Awake(){box=GetComponent<BoxCollider>();box.isTrigger=true;}
        void OnEnable(){box=GetComponent<BoxCollider>();if(!active.Contains(this))active.Add(this);}
        void OnDisable()=>active.Remove(this);
        void Reset(){GetComponent<BoxCollider>().isTrigger=true;gameObject.layer=2;}
        public bool Contains(Vector3 point)
        {var p=transform.InverseTransformPoint(point)-box.center;var s=box.size*.5f;return Mathf.Abs(p.x)<=s.x&&Mathf.Abs(p.y)<=s.y&&Mathf.Abs(p.z)<=s.z;}
        public static bool TryOverride(Vector3 point,out Shelter value)
        {
            value=Shelter.Outdoor;int best=int.MinValue;bool found=false;
            foreach(var v in active)if(v!=null&&v.isActiveAndEnabled&&v.priority>=best&&v.Contains(point)){best=v.priority;value=v.shelter;found=true;}
            return found;
        }
    }
}
