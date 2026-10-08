using System.Collections.Generic;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Monsters;

namespace CampusRift.Enemies
{
    // One pool for every monster kind (P04-T06). Recycled monsters get a full reset: health, statuses,
    // animation, colliders, attack tokens and scale, so nothing from their previous life leaks through.
    public sealed class EnemyPool : MonoBehaviour
    {
        public static EnemyPool Instance { get; private set; }
        readonly Dictionary<EnemyArchetype, List<EnemyInstance>> idle = new Dictionary<EnemyArchetype, List<EnemyInstance>>();
        readonly Dictionary<EnemyArchetype, List<EnemyInstance>> arIdle = new Dictionary<EnemyArchetype, List<EnemyInstance>>();
        Transform arConstruction;
        readonly List<EnemyInstance> all = new List<EnemyInstance>(32);
        ParticleSystem burst; Material burstMaterial;
        public int CreatedCount => all.Count;
        public int ActiveCount { get { int n = 0; foreach (var e in all) if (e != null && e.gameObject.activeSelf) n++; return n; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public static EnemyPool Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("Enemy Pool"); return go.AddComponent<EnemyPool>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            burstMaterial = Resources.Load<Material>("EnemyVfx/EnemyBurst");
            BuildBurst();
        }
        void OnDestroy() { if (Instance == this) Instance = null; }

        // Spawns one monster on the NavMesh at (or within 3 m of) a point. Null when the point has no NavMesh.
        public EnemyInstance Spawn(EnemyArchetype archetype, Vector3 position, EnemyScaling scaling, bool countsForSwordIntent = true)
        {
            if(archetype!=null&&archetype.id=="duc-yeu"){if(!FlyingMotor.OutdoorPoint(position,out position))return null;}
            if (archetype == null || archetype.prefab == null) { Debug.LogWarning("EnemyPool: archetype without a prefab: " + (archetype != null ? archetype.id : "null")); return null; }
            var director = EnemyDirector.Ensure(); var player = director.FindPlayer();
            EnemyInstance e = null;
            if (idle.TryGetValue(archetype, out var list))
                while (list.Count > 0 && e == null) { e = list[list.Count - 1]; list.RemoveAt(list.Count - 1); }
            if (e == null)
            {
                var go = Instantiate(archetype.prefab, transform); go.name = archetype.id + " " + (all.Count + 1);
                e = go.GetComponent<EnemyInstance>(); e.Pool = this; all.Add(e);
                go.SetActive(false);
            }
            e.archetype=archetype;
            var motor = e.GetComponent<MinionMotor>();
            e.gameObject.SetActive(true);
            bool placed;
            if(motor!=null)placed=motor.Place(position);
            else {
                var agent=e.GetComponent<UnityEngine.AI.NavMeshAgent>();
                UnityEngine.AI.NavMeshHit hit;
                placed=UnityEngine.AI.NavMesh.SamplePosition(position,out hit,3,UnityEngine.AI.NavMesh.AllAreas) && agent!=null;
                if(placed){agent.enabled=false;e.transform.position=hit.position;agent.enabled=true;placed=agent.Warp(hit.position);}
            }
            if (!placed) { Release(e); Debug.LogWarning("EnemyPool: no NavMesh near " + position); return null; }
            if (player != null)
            {
                Vector3 toPlayer = Vector3.ProjectOnPlane(player.position - e.transform.position, Vector3.up);
                if (toPlayer.sqrMagnitude > 0.01f) e.transform.rotation = Quaternion.LookRotation(toPlayer);
            }
            e.Configure(archetype, scaling, countsForSwordIntent);
            director.Register(e);
            return e;
        }

        public EnemyInstance SpawnAR(EnemyArchetype archetype, Transform root, Vector3 position, CampusRift.AR.ARBattlefield field)
        {
            if(archetype==null||archetype.prefab==null)return null;
            EnemyInstance e=null;if(arIdle.TryGetValue(archetype,out var list))while(list.Count>0&&e==null){e=list[list.Count-1];list.RemoveAt(list.Count-1);}
            if(e==null)
            {
                if(arConstruction==null){arConstruction=new GameObject("Inactive AR construction").transform;arConstruction.SetParent(transform,false);arConstruction.gameObject.SetActive(false);}
                var go=Instantiate(archetype.prefab,arConstruction);go.SetActive(false);e=go.GetComponent<EnemyInstance>();e.ARSession=true;e.Pool=this;all.Add(e);
                foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))
                    if(!(b is EnemyInstance)&&!(b is MonsterVitality)&&!(b is StatusEffectHost)&&!(b is EnemyAnimationDriver)&&!(b is ReactionResolver))b.enabled=false;
                go.AddComponent<CampusRift.AR.ARCombatContext>();go.AddComponent<CampusRift.AR.ARMinionBrain>();
            }
            e.archetype=archetype;var agent=e.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent!=null)agent.enabled=false;
            var context=e.GetComponent<CampusRift.AR.ARCombatContext>();context.battlefield=field;context.scale=field.Scale;
            e.transform.SetParent(root,false);e.transform.localScale=Vector3.one;e.transform.position=position;
            e.gameObject.SetActive(true);e.ConfigureAR(archetype,field.Scale);e.GetComponent<CampusRift.AR.ARMinionBrain>().Configure(field);
            foreach(var lod in e.GetComponentsInChildren<LODGroup>())lod.ForceLOD(Mathf.Min(1,lod.lodCount-1));
            EnemyDirector.EnsureAR().Register(e);return e;
        }

        // Back to the pool without counting as a kill (level end, respawn).
        public void Release(EnemyInstance e)
        {
            if (e == null) return;
            StopAllCoroutinesOn(e);
            var director = EnemyDirector.Instance;
            if (director != null && director.IsActive(e)) director.Unregister(e, false);
            else if (director != null) director.Release(e);
            e.gameObject.SetActive(false);
            if(e.ARSession)e.transform.SetParent(transform,false);
            var target=e.ARSession?arIdle:idle;
            if (!target.TryGetValue(e.archetype, out var list)) target[e.archetype] = list = new List<EnemyInstance>();
            if (!list.Contains(e)) list.Add(e);
        }

        static void StopAllCoroutinesOn(EnemyInstance e) { if (e.Brain != null) e.Brain.StopAllCoroutines(); e.GetComponent<EnemyAbilityRunner>()?.Cancel(); e.GetComponent<ExpandedEnemyRuntime>()?.Cancel(); e.GetComponent<ShabanEnemyBridge>()?.Cancel(); }

        public void ReleaseAll()
        {
            EnemyProjectilePool.Instance?.Clear();
            var live = new List<EnemyInstance>(all);
            foreach (var e in live) if (e != null && e.gameObject.activeSelf) Release(e);
        }

        public void PlayBurst(Vector3 position, Element element)
        {
            if (burst == null) return;
            burst.transform.position = position;
            var main = burst.main; main.startColor = ElementChart.ColorOf(element);
            burst.Emit(14);
        }

        void BuildBurst()
        {
            var go = new GameObject("Death Burst"); go.transform.SetParent(transform, false);
            burst = go.AddComponent<ParticleSystem>();
            var main = burst.main; main.playOnAwake = false; main.loop = false; main.startLifetime = 0.6f; main.startSpeed = 2.4f; main.startSize = 0.28f;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = -0.15f; main.maxParticles = 200;
            var emission = burst.emission; emission.enabled = false;
            var shape = burst.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.3f;
            var fade = burst.colorOverLifetime; fade.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            fade.color = g;
            var size = burst.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));
            var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = burstMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        }
    }
}
