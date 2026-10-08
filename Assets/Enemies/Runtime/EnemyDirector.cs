using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Monsters;

namespace CampusRift.Enemies
{
    // Coordinates the horde (P04-T04): who may attack right now (attack tokens), where each melee monster
    // stands around the player, and a small feed of spawn/death events for the level and the Thiên Kiếm meter.
    [DefaultExecutionOrder(-50)]
    public sealed class EnemyDirector : MonoBehaviour
    {
        public static EnemyDirector Instance { get; private set; }
        public static event Action<EnemyInstance> EnemySpawned, EnemyDied;
        public AITierProfile profiles;
        public bool ARSession {get;private set;}
        public static EnemyDirector EnsureAR()
        {
            if(Instance!=null)return Instance;
            var go=new GameObject("AR enemy registry");go.SetActive(false);var d=go.AddComponent<EnemyDirector>();d.ARSession=true;go.SetActive(true);return d;
        }
        public IReadOnlyList<EnemyInstance> Active => active;
        public Transform PlayerTransform { get; private set; }
        public PlayerMonsterHealth Player { get; private set; }
        public int MeleeTokensInUse { get; private set; }
        public int RangedTokensInUse { get; private set; }
        public int MaxMeleeTokensSeen { get; private set; }
        public int MaxRangedTokensSeen { get; private set; }
        // Average AI time per frame over the last second, for the mobile budget (plan §3.6).
        public float AiMilliseconds { get; private set; }
        readonly List<EnemyInstance> active = new List<EnemyInstance>(32);
        readonly HashSet<EnemyInstance> holders = new HashSet<EnemyInstance>();
        readonly Dictionary<EnemyInstance, Vector3> slots = new Dictionary<EnemyInstance, Vector3>();
        readonly List<EnemyInstance> ring = new List<EnemyInstance>(32);
        float nextSlots, windowStart; double windowTicks; int windowFrames;
        float nextCoordinatedAttack; int coordinatedBeat;
        public EnemyTactics Tactics { get; private set; }
        public SquadTactics Squad { get; private set; }
        readonly Queue<float> areaCasts=new Queue<float>();
        public int AreaCastsLastMinute {get{while(areaCasts.Count>0&&Time.time-areaCasts.Peek()>60)areaCasts.Dequeue();return areaCasts.Count;}}
        public void RecordAreaCast(){areaCasts.Enqueue(Time.time);nextSlots=0;}
        public bool Adapting(EnemyInstance e)=>e.scaling.aiTier>=4&&AreaCastsLastMinute>=3;
        public float RingRadius {get;private set;}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; EnemySpawned = null; EnemyDied = null; }

        // Created on first use so a level or a test never has to place it in a scene.
        public static EnemyDirector Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("Enemy Director");
            var director = go.AddComponent<EnemyDirector>();
            director.profiles = Resources.Load<AITierProfile>("AITierProfiles");
            return director;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if(ARSession)return;
            if (profiles == null) profiles = Resources.Load<AITierProfile>("AITierProfiles");
            Tactics = GetComponent<EnemyTactics>() ?? gameObject.AddComponent<EnemyTactics>();
            Squad = new SquadTactics(this);
            gameObject.AddComponent<SquadEdgeIndicator>();
        }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public AITierProfile.Tier TierFor(int tier)
        {
            if (profiles != null) return profiles.Get(tier);
            return new AITierProfile.Tier { name = "T0", windup = 0.8f, meleeTokens = 2, rangedTokens = 2 };
        }

        public void Register(EnemyInstance e)
        {
            if (!active.Contains(e)) active.Add(e);
            Squad?.Register(e);
            UpdateVoices();
            EnemySpawned?.Invoke(e);
        }

        // Every minion in a run shares one clip; only the nearest source is audible.
        public AudioClip SmallMonsterVoice { get; private set; }
        public int SmallMonsterSourceLevel { get; private set; }
        public double SmallMonsterVoiceStartDsp { get; private set; }
        public EnemyInstance NearestVoice { get; private set; }
        public int AudibleVoices { get; private set; }
        public void BeginLevelVoice(CampusRift.Levels.LevelDefinition level)
        {
            int source = level.runMode == CampusRift.Levels.EndgameMode.Tower
                ? (Mathf.Max(1, level.towerFloor) - 1) % 10 + 1 : level.index;
            SelectVoice(source);
        }
        void SelectVoice(int source)
        {
            SmallMonsterSourceLevel = Mathf.Max(1, source);
            SmallMonsterVoice = CampusRift.Audio.GameSfx.SmallMonsterForLevel(SmallMonsterSourceLevel);
            SmallMonsterVoiceStartDsp = AudioSettings.dspTime;
        }
        public AudioClip VoiceForSpawn(int fallbackLevel)
        {
            if (SmallMonsterVoice == null) SelectVoice(fallbackLevel);
            return SmallMonsterVoice;
        }
        public void UpdateVoices()
        {
            var player = ARSession ? (Camera.main!=null?Camera.main.transform:null) : FindPlayer();
            EnemyInstance nearest = null; float bestDistance = float.PositiveInfinity;
            foreach (var e in active)
            {
                if (player == null || e == null || !e.Alive || e.VoiceClip == null || e.Voice == null || !e.Voice.isPlaying) continue;
                float distance = (e.transform.position - player.position).sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; nearest = e; }
            }
            // Mute outgoing sources before opening the incoming one, without restarting its aligned loop.
            foreach (var e in active)
            {
                if (e != null && e != nearest) e.SetVoiceAudible(false);
            }
            nearest?.SetVoiceAudible(true);
            NearestVoice = nearest; AudibleVoices = nearest != null ? 1 : 0;
        }

        public void Unregister(EnemyInstance e, bool died)
        {
            e.SetVoiceAudible(false);
            active.Remove(e); Release(e); slots.Remove(e);
            Squad?.Unregister(e);
            UpdateVoices();
            if (died) EnemyDied?.Invoke(e);
        }

        public bool Holds(EnemyInstance e) => holders.Contains(e);
        public bool IsActive(EnemyInstance e) => active.Contains(e);

        // Attack token: only a few monsters may wind up and strike at the same time (plan §3.3).
        public bool TryAcquire(EnemyInstance e)
        {
            if (holders.Contains(e)) return true;
            var tier = TierFor(e.scaling.aiTier);
            if (e.scaling.aiTier >= 3 && Time.time < nextCoordinatedAttack) return false;
            if (e.IsMelee)
            {
                if (MeleeTokensInUse >= tier.meleeTokens) return false;
                MeleeTokensInUse++; MaxMeleeTokensSeen = Mathf.Max(MaxMeleeTokensSeen, MeleeTokensInUse);
            }
            else
            {
                if (RangedTokensInUse >= tier.rangedTokens) return false;
                RangedTokensInUse++; MaxRangedTokensSeen = Mathf.Max(MaxRangedTokensSeen, RangedTokensInUse);
            }
            holders.Add(e);
            if (e.scaling.aiTier >= 3) { nextCoordinatedAttack = Time.time + .3f + .1f * (coordinatedBeat++ % 3); }
            return true;
        }

        public void Release(EnemyInstance e)
        {
            if (!holders.Remove(e)) return;
            if (e.archetype != null && !e.archetype.ranged) MeleeTokensInUse = Mathf.Max(0, MeleeTokensInUse - 1);
            else RangedTokensInUse = Mathf.Max(0, RangedTokensInUse - 1);
        }

        public void ResetCounters() { MaxMeleeTokensSeen = MaxRangedTokensSeen = 0; nextCoordinatedAttack = 0; coordinatedBeat = 0; Tactics?.ResetAssignments(); Squad?.Reset(); areaCasts.Clear(); }

        public Transform FindPlayer()
        {
            if (PlayerTransform != null) return PlayerTransform;
            Player = FindAnyObjectByType<PlayerMonsterHealth>();
            PlayerTransform = Player != null ? Player.transform : null;
            return PlayerTransform;
        }

        // Where a melee monster should stand: its slot on the ring around the player, or the player itself.
        public Vector3 DestinationFor(EnemyInstance e)
        {
            var player = FindPlayer();
            if (player == null) return e.transform.position;
            if (Squad != null && Squad.TryDestination(e, out var squadPoint)) return squadPoint;
            if (TierFor(e.scaling.aiTier).surround && slots.TryGetValue(e, out var slot)) {
                var explorer=player.GetComponent<CampusExplorer>();if(e.scaling.aiTier>=2&&explorer!=null&&explorer.PlanarVelocity.sqrMagnitude>1)
                {Vector3 intercept=slot+Vector3.ClampMagnitude(explorer.PlanarVelocity*.65f,4);if(NavMesh.SamplePosition(intercept,out var hit,1,NavMesh.AllAreas))return hit.position;}
                return slot;}
            return player.position;
        }

        // Called by brains: adds their measured AI time to the current window.
        public void AddAiTicks(long ticks) { windowTicks += ticks; }

        void Update()
        {
            windowFrames++;
            if (Time.unscaledTime - windowStart >= 1f)
            {
                AiMilliseconds = windowFrames > 0 ? (float)(windowTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency / windowFrames) : 0;
                windowStart = Time.unscaledTime; windowTicks = 0; windowFrames = 0;
            }
            UpdateVoices();
            if(ARSession)return;
            if (Time.timeScale > 0 && !(CampusRift.Levels.LevelDirector.Instance?.CinematicPaused ?? false))
            {
                long start = System.Diagnostics.Stopwatch.GetTimestamp();
                Squad?.Tick();
                AddAiTicks(System.Diagnostics.Stopwatch.GetTimestamp() - start);
            }
            if (Time.time >= nextSlots) { nextSlots = Time.time + 0.4f; RebuildSlots(); }
        }

        // Melee monsters that use the surround rule are spread evenly around the player, keeping their current
        // order by angle so nobody has to cross the ring.
        void RebuildSlots()
        {
            var player = FindPlayer(); if (player == null) return;
            ring.Clear();
            foreach (var e in active) if (e != null && e.Alive && e.IsMelee && TierFor(e.scaling.aiTier).surround) ring.Add(e);
            if (ring.Count == 0) { slots.Clear(); return; }
            Vector3 center = player.position;
            ring.Sort((a, b) => Angle(a, center).CompareTo(Angle(b, center)));
            float step = 360f / ring.Count;
            // Rotate the ring so that every monster has to move as little as possible (circular mean of the offsets).
            float sx = 0, sz = 0;
            for (int i = 0; i < ring.Count; i++) { float d = (Angle(ring[i], center) - i * step) * Mathf.Deg2Rad; sx += Mathf.Sin(d); sz += Mathf.Cos(d); }
            float start = Mathf.Atan2(sx, sz) * Mathf.Rad2Deg;
            float radius = Mathf.Max(1.35f, ring.Count * 0.7f / (2f * Mathf.PI));
            for (int i = 0; i < ring.Count; i++)
            {
                float angle = (start + i * step) * Mathf.Deg2Rad;
                Vector3 want = center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
                float r=Adapting(ring[i])?Mathf.Max(3.5f,radius*2):radius;RingRadius=r;
                want=center+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*r;
                slots[ring[i]] = NavMesh.SamplePosition(want, out var hit, 2f, NavMesh.AllAreas) ? hit.position : want;
            }
        }
        static float Angle(EnemyInstance e, Vector3 center)
        {
            Vector3 d = e.transform.position - center;
            return Mathf.Repeat(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360f);
        }
    }
}
