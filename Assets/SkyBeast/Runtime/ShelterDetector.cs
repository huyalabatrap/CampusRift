using System;
using UnityEngine;
namespace CampusRift.SkyBeast
{
    [DisallowMultipleComponent]
    public sealed class ShelterDetector:MonoBehaviour
    {
        public float headHeight=1.6f;
        public bool isMonster;
        public Shelter Current{get;private set;}
        public event Action<Shelter> Changed;
        float next;
        static int stagger;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStagger()=>stagger=0;
        public static int EnvironmentMask=>1<<LayerMask.NameToLayer("Environment");
        void OnEnable(){if(GetComponent<Monsters.MonsterVitality>()!=null)isMonster=true;Sample();next=Time.time+(isMonster?(++stagger%17)/34f:.2f);}
        void Update(){if(Time.time>=next){Sample();next=Time.time+(isMonster?.5f:.2f);}}
        public void Sample(){var value=Evaluate(transform.position+Vector3.up*headHeight);if(value==Current)return;Current=value;Changed?.Invoke(value);}
        // Point is head height, as in §14.4. Ground callers must add their own head offset.
        public static Shelter Evaluate(Vector3 point)
        {
            if(IndoorVolume.TryOverride(point,out var overridden))return overridden;
            int hits=0;
            for(int i=-1;i<=1;i++)if(Physics.Raycast(point+Vector3.right*(i*.6f),Vector3.up,60,EnvironmentMask,QueryTriggerInteraction.Ignore))hits++;
            return hits==3?Shelter.Indoor:hits>0?Shelter.Partial:Shelter.Outdoor;
        }
        public static Shelter AtFeet(Vector3 feet)=>Evaluate(feet+Vector3.up*1.6f);
        public static Shelter ForFire(Vector3 feet)
        {
            var s=AtFeet(feet);if(s!=Shelter.Outdoor)return s;
            foreach(var w in Skills.VoidWall.Active)
            {
                if(w==null||!w.IsSolid)continue;
                var p=w.transform.InverseTransformPoint(feet);
                if(p.z<0&&p.z>=-3&&Mathf.Abs(p.x)<=w.Width*.5f&&p.y>=-.3f&&p.y<=w.config.height)return Shelter.Partial;
            }
            return s;
        }
    }
}
