using UnityEngine;
namespace CampusRift.SkyBeast
{
    public sealed class MeteorShower:MonoBehaviour
    {
        SkyBeastScheduler scheduler;SkyBeastController beast;SkyStrikePool pool;float clock;
        public int LastCount{get;private set;}
        public int Showers{get;private set;}
        public void Initialize(SkyBeastScheduler owner,SkyBeastController source){scheduler=owner;beast=source;pool=owner.GetComponent<SkyStrikePool>()??owner.gameObject.AddComponent<SkyStrikePool>();}
        public bool TryDrop()
        {
            var cycle=FireBreathCycle.Instance;var player=cycle?.Player;if(player==null||scheduler.Phase!=3||cycle.Profile==null||(scheduler.Fury!=null&&scheduler.Fury.Warning))return false;
            LastCount=0;
            // Keep six gameplay strikes on every platform; cosmetic particle rates alone halve on mobile.
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var p=player.transform.position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(i==0?0:7);if(pool.Launch(p,SkyStrikeKind.Meteor,beast,cycle.Profile))LastCount++;}
            if(LastCount>0)Showers++;return LastCount>0;
        }
        public void Advance(float dt){clock+=dt;if(clock>=15){clock=0;TryDrop();}}
        void Update()=>Advance(Time.deltaTime);
    }
}
