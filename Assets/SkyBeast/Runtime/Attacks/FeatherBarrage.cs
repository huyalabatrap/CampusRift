using UnityEngine;
namespace CampusRift.SkyBeast
{
    public sealed class FeatherBarrage:MonoBehaviour
    {
        SkyBeastScheduler scheduler;SkyBeastController beast;SkyStrikePool pool;float clock;Vector3 previous,velocity;
        public int LastCount{get;private set;}
        public int Barrages{get;private set;}
        public void Initialize(SkyBeastScheduler owner,SkyBeastController source){scheduler=owner;beast=source;pool=owner.GetComponent<SkyStrikePool>()??owner.gameObject.AddComponent<SkyStrikePool>();}
        public bool TryDrop()
        {
            var cycle=FireBreathCycle.Instance;var p=cycle?.Player;
            if(p==null||p.IsDead||ShelterDetector.AtFeet(p.transform.position)!=Shelter.Outdoor||(cycle.State!=FireBreathCycle.Phase.Rest&&cycle.State!=FireBreathCycle.Phase.Afterfire))return false;
            int count=scheduler.Phase>=2?5:3;LastCount=0;
            var predicted=p.transform.position+Vector3.ClampMagnitude(velocity*1.2f,3.5f);
            for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count;var candidate=predicted+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(i==0?0:2.5f);if(pool.Launch(candidate,SkyStrikeKind.Feather,beast,cycle.Profile))LastCount++;}
            if(LastCount>0)Barrages++;return LastCount>0;
        }
        public void Advance(float dt)
        {
            var cycle=FireBreathCycle.Instance;var p=cycle?.Player;if(p==null)return;
            velocity=Vector3.ClampMagnitude((p.transform.position-previous)/Mathf.Max(.01f,dt),10);previous=p.transform.position;
            if(cycle.State!=FireBreathCycle.Phase.Rest&&cycle.State!=FireBreathCycle.Phase.Afterfire)return;
            clock+=dt;if(clock>=8){clock=0;TryDrop();}
        }
        void Update()=>Advance(Time.deltaTime);
    }
}
