using UnityEngine;
using UnityEngine.AI;
namespace CampusRift.SkyBeast
{
    [DisallowMultipleComponent]
    public sealed class BurningGround:MonoBehaviour
    {
        public const float Radius=2.5f,Lifetime=10;
        sealed class Patch
        {public GameObject root;public ParticleSystem fire,smoke;public SpriteRenderer scorch;public float expires,radius=Radius;public bool active;}
        readonly Patch[] pool=new Patch[12];
        FireBreathProfile profile;float accumulator;
        public int ActiveCount{get;private set;}
        public int CreatedCount=>pool.Length;
        public Vector3[] Positions
        {get{var result=new Vector3[ActiveCount];int n=0;foreach(var p in pool)if(p!=null&&p.active)result[n++]=p.root.transform.position;return result;}}
        void Awake()
        {
            for(int i=0;i<pool.Length;i++)
            {
                var root=new GameObject("Pooled afterfire "+i);root.transform.SetParent(transform,false);
                var fire=FireVisualFactory.RollingFire(root.transform,"Rolling afterfire flame",2.1f,1.5f);
                var shape=fire.shape;shape.radius=1.8f;
                var smoke=FireVisualFactory.Particles(root.transform,"Dark smoke","smoke",false,4,2.4f,3,new Color(.11f,.065f,.04f,.7f),1.6f);
                var mark=new GameObject("Scorched ground",typeof(SpriteRenderer));mark.transform.SetParent(root.transform,false);mark.transform.localPosition=Vector3.up*.04f;mark.transform.localRotation=Quaternion.Euler(90,0,0);mark.transform.localScale=Vector3.one*5;
                var sr=mark.GetComponent<SpriteRenderer>();sr.sprite=Resources.Load<Sprite>("P13/scorch");sr.sharedMaterial=Resources.Load<Material>("P13/scorch-alpha");
                pool[i]=new Patch{root=root,fire=fire,smoke=smoke,scorch=sr};root.SetActive(false);
            }
        }
        public bool SpawnAt(Vector3 point,FireBreathProfile data)
        {
            if(data==null||ShelterDetector.AtFeet(point)!=Shelter.Outdoor||!NavMesh.SamplePosition(point,out var nav,1.5f,NavMesh.AllAreas)||ShelterDetector.AtFeet(nav.position)!=Shelter.Outdoor)return false;
            foreach(var p in pool)if(!p.active)
            {
                profile=data;p.root.transform.localScale=Vector3.one;p.radius=Radius;p.root.transform.position=nav.position;p.expires=Time.time+Lifetime;p.active=true;ActiveCount++;
                p.root.SetActive(true);p.scorch.color=Color.white;
                var e=p.fire.emission;e.rateOverTime=FireVisualQuality.Choose(10,7,5);var s=p.smoke.emission;s.rateOverTime=FireVisualQuality.Choose(4,3,2);
                p.fire.Play();p.smoke.Play();return true;
            }
            return false;
        }
        public void SpawnPatches(FireBreathProfile data)
        {
            Clear();profile=data;var player=FireBreathCycle.Instance?.Player;if(player==null)return;
            int desired=Random.Range(8,13);
            for(int i=0;i<150&&ActiveCount<desired;i++)
            {
                Vector2 offset=Random.insideUnitCircle*(ActiveCount<4?10:45);
                Vector3 p=player.transform.position+new Vector3(offset.x,0,offset.y);
                if(!NavMesh.SamplePosition(p,out var nav,4,NavMesh.AllAreas))continue;
                bool spaced=true;foreach(var old in pool)if(old.active&&(old.root.transform.position-nav.position).sqrMagnitude<20){spaced=false;break;}
                if(spaced)SpawnAt(nav.position,data);
            }
        }
        public bool SpawnSmallAt(Vector3 point,FireBreathProfile data)
        {
            if(!SpawnAt(point,data))return false;
            foreach(var p in pool)if(p.active&&(p.root.transform.position-point).sqrMagnitude<.2f&&p.radius==Radius){p.radius=1;p.root.transform.localScale=Vector3.one*.4f;return true;}
            return true;
        }
        public bool Contains(Vector3 feet)
        {foreach(var p in pool)if(p.active&&Mathf.Abs(feet.y-p.root.transform.position.y)<1.5f&&Vector3.ProjectOnPlane(feet-p.root.transform.position,Vector3.up).sqrMagnitude<=p.radius*p.radius)return true;return false;}
        public void ApplyTick(float seconds)
        {
            var player=FireBreathCycle.Instance?.Player;if(player==null||profile==null||player.IsDead||!Contains(player.transform.position)||ShelterDetector.AtFeet(player.transform.position)!=Shelter.Outdoor)return;
            var buffs=player.GetComponent<Progression.BuffSystem>();if(buffs!=null&&buffs.EmberImmune)return;
            FireBreathCycle.DamagePlayer(player,profile.recommendedHealth*.04f*seconds,"du-hoa");
        }
        void Update()
        {
            if(FireBreathCycle.Instance!=null&&FireBreathCycle.Instance.CinematicPaused){foreach(var p in pool)if(p.active)p.expires+=Time.deltaTime;return;}
            foreach(var p in pool)if(p.active)
            {
                float left=p.expires-Time.time;p.scorch.color=new Color(1,1,1,Mathf.Clamp01(left/2));
                if(left<=1){p.fire.Stop(true,ParticleSystemStopBehavior.StopEmitting);p.smoke.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
                if(left<=0){p.active=false;ActiveCount--;p.fire.Clear();p.smoke.Clear();p.root.SetActive(false);}
            }
            accumulator+=Time.deltaTime;if(accumulator>=.5f){float step=accumulator;accumulator=0;ApplyTick(step);}
        }
        public void Clear()
        {foreach(var p in pool)if(p!=null){p.active=false;p.fire.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);p.smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);p.root.SetActive(false);}ActiveCount=0;accumulator=0;}
        void OnDisable()=>Clear();
    }
}
