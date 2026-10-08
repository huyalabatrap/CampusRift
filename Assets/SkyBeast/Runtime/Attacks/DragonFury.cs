using UnityEngine;
namespace CampusRift.SkyBeast
{
    public sealed class DragonFury:MonoBehaviour
    {
        public bool Warning{get;private set;}
        public bool Active{get;private set;}
        public bool Completed{get;private set;}
        public float Remaining{get;private set;}
        bool requested;SkyBeastScheduler scheduler;FireBreathCycle cycle;
        public void Initialize(SkyBeastScheduler owner,FireBreathCycle fire){scheduler=owner;cycle=fire;cycle.BreathEnded+=Ended;}
        public bool Request()
        {
            if(requested||scheduler.Level!=10||scheduler.Phase!=3)return false;
            requested=true;cycle.StopCycle();Warning=true;Remaining=5;
            scheduler.Beasts[0].BeginWarning(5);return true;
        }
        public void Advance(float seconds)
        {
            if(!Warning)return;Remaining=Mathf.Max(0,Remaining-seconds);
            if(Remaining<=.0001f){Remaining=0;Warning=false;Active=cycle.StartFury();}
        }
        void Update()=>Advance(Time.deltaTime);
        void Ended(){if(Active&&cycle.IsFury){Active=false;Completed=true;}}
        public void Cancel(){Warning=false;Active=false;Remaining=0;}
        void OnDestroy(){if(cycle!=null)cycle.BreathEnded-=Ended;}
    }
}
