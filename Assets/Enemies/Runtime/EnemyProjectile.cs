using System.Collections.Generic;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;
using CampusRift.Skills;

namespace CampusRift.Enemies
{
    // A bolt fired by a ranged monster (Độc Nhãn's poison). The player can outrun it or block it with a Void Wall.
    [DisallowMultipleComponent]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        public const float Radius = 0.3f, Lifetime = 3.5f;
        public bool Active { get; private set; }
        public float Damage { get; private set; }
        Vector3 velocity; Element element; GameObject owner; float until;
        bool curse;Transform homing;
        bool reflected;float pulledUntil;
        public bool Reflected=>reflected;
        TrailRenderer trail;
        TrailRenderer inkTrail;Renderer core;MaterialPropertyBlock tint;
        void Awake(){tint=new MaterialPropertyBlock();}
        static readonly RaycastHit[] hits = new RaycastHit[16];
        // Everything except monsters and attack volumes: the player, level geometry and Void Walls are hit.
        static readonly int Mask = ~((1 << 7) | (1 << 8) | (1 << 9) | (1 << 10));
        public static int ImpactsOnPlayer, ImpactsOnWalls;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { ImpactsOnPlayer = 0; ImpactsOnWalls = 0; }

        public void Build(Mesh mesh, Material body, Material trailMaterial)
        {
            var visual = new GameObject("Bolt"); visual.transform.SetParent(transform, false); visual.transform.localScale = Vector3.one * 0.42f;
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = visual.AddComponent<MeshRenderer>(); r.sharedMaterial = body; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            core=r;var ink=new GameObject("Ink silhouette");ink.transform.SetParent(transform,false);ink.transform.localScale=Vector3.one*.56f;ink.AddComponent<MeshFilter>().sharedMesh=mesh;
            var inkRenderer=ink.AddComponent<MeshRenderer>();inkRenderer.sharedMaterial=body;inkRenderer.sortingOrder=-1;inkRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;inkRenderer.receiveShadows=false;
            tint.SetColor("_Tint",new Color(.02f,.006f,.03f,1));inkRenderer.SetPropertyBlock(tint);
            trail = gameObject.AddComponent<TrailRenderer>(); trail.sharedMaterial = trailMaterial; trail.time = 0.25f; trail.widthMultiplier = 0.3f; trail.minVertexDistance = 0.1f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trail.receiveShadows = false;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(.7f, 1f, .3f), 0), new GradientColorKey(new Color(.2f, .8f, .2f), 1) }, new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(0, 1) });
            trail.colorGradient = g; gameObject.SetActive(false);
            var inkTrailObject=new GameObject("Ink trail");inkTrailObject.transform.SetParent(transform,false);
            inkTrail=inkTrailObject.AddComponent<TrailRenderer>();inkTrail.sharedMaterial=Resources.Load<Material>("EnemyVfx/P12Telegraph");inkTrail.time=.25f;inkTrail.widthMultiplier=.42f;inkTrail.minVertexDistance=.1f;inkTrail.sortingOrder=-1;inkTrail.startColor=inkTrail.endColor=new Color(.035f,.01f,.05f,.9f);
        }

        public void Launch(Vector3 position, Vector3 direction, float speed, float damage, Element type, GameObject shooter)
        {
            transform.position = position; transform.rotation = Quaternion.LookRotation(direction);
            velocity = direction.normalized * speed; Damage = damage; element = type; owner = shooter; until = Time.time + Lifetime;reflected=false;pulledUntil=0;
            var enemy=shooter!=null?shooter.GetComponent<EnemyInstance>():null;curse=enemy!=null&&enemy.archetype.id=="quang-ma";
            homing=curse?EnemyDirector.Ensure().FindPlayer():null;
            var color=curse?new Color(2.5f,.24f,1.2f):new Color(.4f,2.2f,.25f);tint.SetColor("_Tint",color);core.SetPropertyBlock(tint);trail.startColor=color;trail.endColor=new Color(color.r,color.g,color.b,0);
            gameObject.SetActive(true); Active = true;
            if (trail != null) trail.Clear();
            if(inkTrail!=null)inkTrail.Clear();
        }

        void Update()
        {
            if (!Active) return;
            float dt = Time.deltaTime;
            if(curse&&homing!=null&&Time.time>=pulledUntil)velocity=Vector3.RotateTowards(velocity,(homing.position+Vector3.up-transform.position).normalized*velocity.magnitude,20*Mathf.Deg2Rad*dt,0);
            Vector3 step = velocity * dt; float distance = step.magnitude;
            if (Time.time > until) { End(); return; }
            int count = Physics.SphereCastNonAlloc(transform.position, Radius, velocity.normalized, hits, distance, reflected?~((1<<8)|(1<<9)|(1<<10)):Mask, QueryTriggerInteraction.Ignore);
            int best = -1; float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var c = hits[i].collider;
                if (owner != null && c.transform.IsChildOf(owner.transform)) continue;
                var monster=c.GetComponentInParent<MonsterVitality>();if(monster!=null&&(reflected?monster.Faction==CombatFaction.Ally:monster.Faction==CombatFaction.Hostile))continue;
                if(reflected&&c.GetComponentInParent<PlayerMonsterHealth>()!=null)continue;
                if (hits[i].distance < nearest) { nearest = hits[i].distance; best = i; }
            }
            if (best >= 0) { Impact(hits[best]); return; }
            transform.position += step;
        }

        void Impact(RaycastHit hit)
        {
            transform.position = hit.point;
            var wall = hit.collider.GetComponentInParent<VoidWall>();
            var player = hit.collider.GetComponentInParent<PlayerMonsterHealth>();
            if(wall!=null&&wall.ReflectsProjectiles&&!reflected){velocity=Vector3.Reflect(velocity,hit.normal);transform.position=hit.point+hit.normal*(Radius+.12f);owner=wall.Caster;reflected=true;curse=false;homing=null;until=Time.time+Lifetime;var color=new Color(.5f,.5f,2);tint.SetColor("_Tint",color);core.SetPropertyBlock(tint);trail.startColor=color;trail.endColor=new Color(.5f,.5f,2,0);wall.ReflectionFlash(hit.point);return;}
            if (wall != null) { wall.Damage(Damage, hit.point); ImpactsOnWalls++; }
            else if (player != null)
            {
                var info = DamageInfo.Create(Damage, element, DamageSource.Projectile, hit.point, velocity.normalized, owner);
                info.skillId=curse?"quang-ma-curse":"doc-nhan-poison";
                if(player.ApplyDamage(info)){ImpactsOnPlayer++;if(curse)PlayerEnemyControl.Ensure(player.gameObject).Chill(2,.4f);EnemyAbilityRunner.Feedback(hit.point,.65f);}
            }
            else {var victim=hit.collider.GetComponentInParent<MonsterVitality>();if(victim!=null){var info=DamageInfo.Create(Damage,element,DamageSource.Projectile,hit.point,velocity.normalized,owner);info.skillId=reflected?"hu-khong-ket-gioi-reflection":"enemy-projectile";victim.ApplyDamage(info);}}
            End();
        }

        internal void PullToward(Vector3 center,float dt){if(!Active||reflected)return;Vector3 d=center-transform.position;if(d.sqrMagnitude<.5f){End();return;}pulledUntil=Time.time+.1f;velocity=Vector3.Lerp(velocity,d.normalized*Mathf.Max(12,velocity.magnitude),Mathf.Clamp01(dt*12));}

        internal void End() { Active = false; gameObject.SetActive(false); }
    }

    // Pool of bolts; the materials load from Resources so player builds keep them.
    public sealed class EnemyProjectilePool : MonoBehaviour
    {
        public const int Capacity = 24;
        public static EnemyProjectilePool Instance { get; private set; }
        readonly List<EnemyProjectile> bolts = new List<EnemyProjectile>(Capacity);
        Mesh mesh; Material body, trail;
        public int CreatedCount => bolts.Count;
        public int TotalFired {get;private set;}
        public int ActiveCount { get { int n = 0; foreach (var b in bolts) if (b.Active) n++; return n; } }
        public void Clear(){foreach(var bolt in bolts)if(bolt!=null)bolt.End();}
        public void PullInto(Vector3 point,float radius,float dt){foreach(var b in bolts)if(b.Active&&(b.transform.position-point).sqrMagnitude<=radius*radius&&CombatLine.Clear(point,b.transform.position,transform))b.PullToward(point,dt);}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public static EnemyProjectilePool Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("Enemy Projectiles"); return go.AddComponent<EnemyProjectilePool>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            body = Resources.Load<Material>("EnemyVfx/P12Bolt");if(body==null)body=Resources.Load<Material>("EnemyVfx/EnemyProjectile"); trail = Resources.Load<Material>("EnemyVfx/EnemyTrail");
            // A small sphere with a bright vertex colour (the additive shader takes its hue from vertex colours).
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mesh = Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh); Destroy(primitive);
            var colors = new Color[mesh.vertexCount];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            mesh.colors = colors;
        }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public EnemyProjectile Fire(Vector3 position, Vector3 direction, float speed, float damage, Element element, GameObject shooter)
        {
            EnemyProjectile bolt = null;
            foreach (var b in bolts) if (!b.Active) { bolt = b; break; }
            if (bolt == null)
            {
                if (bolts.Count >= Capacity) return null;
                var go = new GameObject("Poison Bolt"); go.transform.SetParent(transform, false);
                bolt = go.AddComponent<EnemyProjectile>(); bolt.Build(mesh, body, trail); bolts.Add(bolt);
            }
            bolt.Launch(position, direction, speed, damage, element, shooter);
            TotalFired++;
            return bolt;
        }
    }
}
