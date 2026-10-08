using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRift.Skills
{
    [DisallowMultipleComponent]
    public sealed class VoidWall : MonoBehaviour
    {
        public VoidWallConfig config;
        public MeshRenderer surface;
        public ParticleSystem motes;
        public BoxCollider solid;
        public NavMeshObstacle obstacle;
        public AudioSource voice, drone;
        public static readonly List<VoidWall> Active = new List<VoidWall>(20);
        public static int Revision { get; private set; }
        public bool IsSolid { get; private set; }
        public bool ReflectsProjectiles {get;private set;}public GameObject Caster {get;private set;}
        public void ReflectionFlash(Vector3 p){Burst(p,12,2);Play(config.impact,.5f);}
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public float Width { get; private set; }
        public float Remaining => Mathf.Max(0, expiresAt - Time.time);
        public int HitCount { get; private set; }
        public bool WasBroken { get; private set; }
        public Vector3 Center => transform.position + Vector3.up * config.height * 0.5f;
        float born, expiresAt, endingAt, lastWarning;
        bool ending;
        MaterialPropertyBlock properties;
        Collider[] ownerColliders;
        static readonly int Age = Shader.PropertyToID("_Age"), Integrity = Shader.PropertyToID("_Integrity"), Reveal = Shader.PropertyToID("_Reveal"), Fade = Shader.PropertyToID("_Fade"), Hit = Shader.PropertyToID("_Hit"), Preview = Shader.PropertyToID("_Preview");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Active.Clear(); Revision = 0; }
        void Awake() { properties = new MaterialPropertyBlock(); }

        public void Deploy(Vector3 feet, Quaternion rotation, float width, Collider[] owner, float health = 0,GameObject caster=null,bool reflects=false)
        {
            transform.SetPositionAndRotation(feet, rotation);
            gameObject.SetActive(true);
            properties.Clear();
            Caster=caster;ReflectsProjectiles=reflects;properties.SetColor("_Edge",reflects?new Color(.7f,1.5f,3):new Color(.12f,1.8f,2.5f));properties.SetColor("_Violet",reflects?new Color(1.8f,.7f,2.5f):new Color(1,.12f,2.5f));
            Width = width; Health = MaxHealth = health > 0 ? health : config.health; HitCount = 0; WasBroken = false;
            born = Time.time; expiresAt = born + config.lifetime; ending = false; lastWarning = 0;
            surface.transform.localScale = new Vector3(width, config.height, 1);
            solid.center = new Vector3(0, config.height / 2, 0); solid.size = new Vector3(width, config.height, config.thickness); solid.enabled = true;
            obstacle.center = solid.center; obstacle.size = solid.size; obstacle.enabled = true;
            ownerColliders = owner;
            if (owner != null) foreach (var c in owner) if (c != null) Physics.IgnoreCollision(solid, c, true);
            IsSolid = true; if (!Active.Contains(this)) Active.Add(this); Revision++;
            properties.SetFloat(Preview, 0); properties.SetVector(Hit, new Vector4(0.5f,0.5f,-100,0));
            var shape = motes.shape; shape.scale = new Vector3(width,config.height,0.12f);
            motes.transform.localPosition = new Vector3(0,config.height/2,0);
            motes.Play(); Burst(Center, 22, 1.6f);
            Skills.SkillAudio.Apply(voice, drone);
            voice.pitch = 1; Play(config.deploy, 0.6f);
            drone.clip = config.hum; drone.volume = 0.045f; drone.loop = true; if (drone.clip != null) drone.Play();
            UpdateVisual();
        }

        public bool Damage(float amount, Vector3 point)
        {
            if (!IsSolid || amount <= 0) return false;
            Health = Mathf.Max(0, Health - amount); HitCount++;
            var local = transform.InverseTransformPoint(point);
            properties.SetVector(Hit, new Vector4(local.x / Width + 0.5f, local.y / config.height, Time.time - born, 0));
            Burst(point, 16, 2.4f); voice.pitch = Random.Range(0.93f,1.07f); Play(config.impact, 0.7f);
            if (Health <= 0) Dissolve(true); else UpdateVisual();
            return true;
        }

        public void Dissolve(bool broken)
        {
            if (!IsSolid) return;
            WasBroken = broken; IsSolid = false; ending = true; endingAt = Time.time;
            solid.enabled = false; obstacle.enabled = false;
            Active.Remove(this); Revision++;
            drone.Stop(); motes.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            voice.pitch = broken ? 0.85f : 1.15f;
            Play(broken ? config.broken : config.expire, broken ? 0.85f : 0.35f);
            if (broken) Burst(Center, 64, 4.5f);
        }

        void Update()
        {
            if (IsSolid && Time.time >= expiresAt) Dissolve(false);
            if (IsSolid && (Health <= MaxHealth * 0.34f || Remaining < 2.5f) && Time.time > lastWarning)
            { lastWarning = Time.time + 1.2f; Play(config.unstable, 0.18f); }
            UpdateVisual();
            if (ending && Time.time - endingAt >= 1.3f) gameObject.SetActive(false);
        }

        void UpdateVisual()
        {
            properties.SetFloat(Age, Time.time - born);
            properties.SetFloat(Integrity, Health / MaxHealth);
            properties.SetFloat(Reveal, Mathf.SmoothStep(0,1,(Time.time-born)/0.2f));
            properties.SetFloat(Fade, ending ? Mathf.Clamp01((Time.time-endingAt)/(WasBroken ? 0.16f : 0.65f)) : Mathf.Clamp01((2-Remaining)/2) * 0.2f);
            surface.SetPropertyBlock(properties);
        }

        void Burst(Vector3 point, int count, float speed)
        {
            for (int i=0;i<count;i++)
            {
                var e = new ParticleSystem.EmitParams();
                e.position = point + Random.insideUnitSphere * (count > 40 ? 0.8f : 0.12f);
                e.velocity = Random.onUnitSphere * Random.Range(speed * 0.4f,speed) + Vector3.up * 0.5f;
                e.startLifetime = Random.Range(0.2f,0.65f); e.startSize = Random.Range(0.02f,count > 40 ? 0.16f : 0.07f);
                e.startColor = i % 5 == 0 ? new Color(1,0.65f,0.16f) : i % 2 == 0 ? new Color(0.1f,0.9f,1) : new Color(0.65f,0.15f,1);
                motes.Emit(e,1);
            }
        }
        void Play(AudioClip clip, float volume) { if (clip != null) voice.PlayOneShot(clip,volume); }

        public Vector3 StrikePoint(Vector3 from)
        { return solid.ClosestPoint(new Vector3(from.x, transform.position.y + 1.15f, from.z)); }

        public bool BlocksSegment(Vector3 from, Vector3 to, float padding = 0)
        {
            if (!IsSolid) return false;
            var a = transform.InverseTransformPoint(from + Vector3.up * 0.8f);
            var b = transform.InverseTransformPoint(to + Vector3.up * 0.8f);
            var bounds = new Bounds(new Vector3(0,config.height/2,0), new Vector3(Width+padding*2,config.height,config.thickness+padding*2));
            var delta = b-a;
            return bounds.Contains(a) || (delta.sqrMagnitude > 0.0001f && bounds.IntersectRay(new Ray(a,delta.normalized),out float hit) && hit <= delta.magnitude);
        }

        public static VoidWall Blocking(Vector3 from, Vector3 to, float padding = 0)
        {
            VoidWall closest=null; float best=float.PositiveInfinity;
            foreach(var wall in Active)
                if(wall != null && wall.BlocksSegment(from,to,padding))
                { float d=(wall.Center-from).sqrMagnitude; if(d<best){best=d;closest=wall;} }
            return closest;
        }

        void OnDisable()
        {
            if (IsSolid) { IsSolid=false; Active.Remove(this); Revision++; }
            if (solid != null) solid.enabled=false;
            if (obstacle != null) obstacle.enabled=false;
            if (ownerColliders != null && solid != null) foreach(var c in ownerColliders) if(c!=null) Physics.IgnoreCollision(solid,c,false);
            if (motes != null) motes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
