using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Skills;
using CampusRift.UI;

namespace CampusRift.Levels
{
    // Plays one LevelDefinition: opens rifts, spawns each wave under the concurrency cap, rests between waves and
    // decides win or loss (P05-T03). Created by LevelBootstrap when a level is selected, or by tests.
    [DefaultExecutionOrder(-40)]
    public sealed class LevelDirector : MonoBehaviour
    {
        public enum Phase { Idle, Intro, Wave, Rest, Won, Lost }
        public const int PcCap = 14, MobileCap = 9;
        public const float MinSpawnDistance = 9f;

        public static LevelDirector Instance { get; private set; }
        public LevelDefinition Level { get; private set; }
        public Phase State { get; private set; } = Phase.Idle;
        public int WaveIndex { get; private set; }            // 0-based index of the current wave
        public int WaveCount => Level != null ? Level.waves.Count : 0;
        public int TotalPlanned { get; private set; }
        public int Kills { get; private set; }
        public int SpawnedTotal { get; private set; }
        public int MaxConcurrentSeen { get; private set; }
        public int Remaining => Mathf.Max(0, TotalPlanned - Kills);
        public int AliveCount => alive.Count;
        public IReadOnlyCollection<EnemyInstance> Alive => alive;
        public float Elapsed { get; private set; }
        public bool AwaitingSkySword { get; private set; }
        public bool CinematicPaused { get; set; }
        public float RestRemaining { get; private set; }
        public bool RestLoadoutOpen { get; private set; }
        public bool CanChangeSkills => Level!=null && Level.index>=9 && Level.index<=10 && State==Phase.Rest && RestRemaining>0;
        public bool SetRestLoadoutOpen(bool open)
        {
            if(open&&!CanChangeSkills)return false;
            RestLoadoutOpen=open;return true;
        }
        public int WinsRaised { get; private set; }
        public int LossesRaised { get; private set; }
        public LevelResult Result { get; private set; }
        // Tu Vi paid for a first clear (null on replays).
        public Progression.StudyAward ClearAward { get; private set; }
        public Transform PlayerTransform => player != null ? player.transform : null;

        // Tests can pin the cap; 0 means "by platform".
        public int ConcurrentCapOverride;
        public int SeedOverride;
        public int RunSeed {get;private set;}
        public float introSeconds = 2.5f, winDelay = 1.2f, spawnInterval = 0.4f, portalLead = 0.35f;
        public int ConcurrentCap => ConcurrentCapOverride > 0 ? ConcurrentCapOverride : CampusInput.Mobile ? MobileCap : PcCap;

        struct Request { public EnemyArchetype archetype; public bool elite; }
        struct Pending { public Request request; public Vector3 position; public float at; }
        readonly List<Request> queue = new List<Request>();
        readonly List<Pending> pending = new List<Pending>();
        readonly HashSet<EnemyInstance> alive = new HashSet<EnemyInstance>();
        readonly HashSet<string> holds = new HashSet<string>();
        CampusExplorer player; PlayerMonsterHealth health; SpiritPower spirit; SkillLoadout loadout;
        GameObject hiddenShaban;
        float nextSpawn, phaseUntil; int zoneCursor;
        StarEvaluator starEvaluator;BlackoutEvent blackout;bool bossStarted,waveClearRaised;System.Random spawnRandom;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public static LevelDirector Ensure()
        {
            if (Instance != null) return Instance;
            return new GameObject("Level Director").AddComponent<LevelDirector>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            starEvaluator=gameObject.AddComponent<StarEvaluator>();blackout=gameObject.AddComponent<BlackoutEvent>();
            gameObject.AddComponent<EnemyQuality>();
        }
        void OnDestroy()
        {
            if (Instance != this) return;
            Unsubscribe(); RestoreShaban(); Instance = null;
        }

        // ---------------------------------------------------------------- start / stop

        public void Begin(LevelDefinition definition)
        {
            if (definition == null) return;
            Unsubscribe();
            StopAllCoroutines();SkyBeast.SkyBeastEnding.Cancel();blackout.Restore();SkyBeast.SkyBeastPresence.StopAll();bossStarted=false;
            AwaitingSkySword = CinematicPaused = false;
            RestLoadoutOpen=false;
            Level = definition; State = Phase.Intro; WaveIndex = -1; Kills = 0; SpawnedTotal = 0; MaxConcurrentSeen = 0; Elapsed = 0;
            WinsRaised = LossesRaised = 0; ClearAward = null; RestRemaining = 0; zoneCursor = 0;
            queue.Clear(); pending.Clear(); alive.Clear(); holds.Clear();
            TotalPlanned = definition.TotalMonsters+(definition.index==3?1:0)+definition.bosses.Count;
            RunSeed=SeedOverride!=0?SeedOverride:definition.index*977+LevelSession.Attempt*104729;
            spawnRandom=new System.Random(RunSeed^0x12A7);

            var pool = EnemyPool.Ensure(); pool.ReleaseAll();
            var enemyDirector = EnemyDirector.Ensure(); enemyDirector.ResetCounters();
            enemyDirector.BeginLevelVoice(definition);
            FindPlayer();
            player?.GetComponent<Progression.PlayerItems>()?.BeginLevel();
            HideShaban();
            PlacePlayer(definition);
            ApplyLoadout();
            var sky = SettingsManager.Instance != null ? SettingsManager.Instance.Sky : null;
            if (sky != null) sky.SetPreset(definition.sky);
            LevelHUD.Attach();
            RestLoadoutUI.Attach();
            starEvaluator.BeginRun(player!=null?player.gameObject:null);
            if(definition.index==4)blackout.Apply();
            if(definition.index==6){var blood=GetComponent<BloodMoonEvent>();if(blood==null)blood=gameObject.AddComponent<BloodMoonEvent>();blood.Apply();}
            SkyBeast.SkyBeastPresence.Begin(definition.index);
            SkyBeast.FireBreathCycle.BeginLevel(definition.index);
            if(definition.index>=8 && definition.index<=10)
            {
                var intent=GetComponent<SkyBeast.SwordIntent>()??gameObject.AddComponent<SkyBeast.SwordIntent>();intent.Consume();
                SkyBeast.HeavenSwordUltimate.Ensure(player.gameObject).Begin(this,intent);
            }
            else SkyBeast.HeavenSwordUltimate.Instance?.StopUltimate();
            (GetComponent<EndgameHazard>()??gameObject.AddComponent<EndgameHazard>()).Begin(definition);
            CampusRift.Progression.EndgameTracker.Ensure();
            CampusRift.UI.PlayerCostume.Attach(player!=null?player.gameObject:null);
            Subscribe();
            phaseUntil = Time.time + introSeconds;
            LevelEvents.RaiseStarted(definition);
            Learning.LearningShrines.Begin(definition);
        }

        void FindPlayer()
        {
            player = FindAnyObjectByType<CampusExplorer>();
            if (player == null) return;
            health = player.GetComponent<PlayerMonsterHealth>(); spirit = player.GetComponent<SpiritPower>(); loadout = player.GetComponent<SkillLoadout>();
        }

        void PlacePlayer(LevelDefinition definition)
        {
            if (player == null) return;
            player.spawnPosition = definition.spawnPoint; player.spawnYaw = definition.spawnYaw;
            player.ReturnToSpawn();
            if (health != null) health.Heal(health.maxHealth);
            if (spirit != null) spirit.Refill();
        }

        void ApplyLoadout()
        {
            if (loadout == null) return;
            var saved = LevelSession.Loadout;
            if (saved != null && saved.Length == SkillLoadout.SlotCount)
            {
                // Clear first so that swapping two skills never collides with a skill still sitting in another slot.
                for (int i = 0; i < SkillLoadout.SlotCount; i++) loadout.Equip(i, "");
                for (int i = 0; i < SkillLoadout.SlotCount; i++) loadout.Equip(i, saved[i]);
            }
            SaveLoadout();
            loadout.LoadoutChanged += SaveLoadout;
        }

        void SaveLoadout()
        {
            if (loadout == null) return;
            var ids = new string[SkillLoadout.SlotCount];
            for (int i = 0; i < ids.Length; i++) { var r = loadout.Get(i); ids[i] = r != null ? r.Id : ""; }
            LevelSession.Loadout = ids;
        }

        void HideShaban()
        {
            var brain = FindAnyObjectByType<MonsterBrain>();
            if (brain == null) return;
            hiddenShaban = brain.gameObject; hiddenShaban.SetActive(false);
        }
        void RestoreShaban() { if (hiddenShaban != null) hiddenShaban.SetActive(true); hiddenShaban = null; }

        void Subscribe()
        {
            EnemyDirector.EnemyDied += OnEnemyDied;
            if (health != null) health.Defeated.AddListener(OnPlayerDefeated);
        }
        void Unsubscribe()
        {
            EnemyDirector.EnemyDied -= OnEnemyDied;
            if (health != null) health.Defeated.RemoveListener(OnPlayerDefeated);
            if (loadout != null) loadout.LoadoutChanged -= SaveLoadout;
        }

        // Extra win conditions (bosses in P12, sky beasts in P15): the level is won only when all holds are released.
        public void HoldWin(string key) { holds.Add(key); }
        public void ReleaseWin(string key) { holds.Remove(key); }

        // ---------------------------------------------------------------- loop

        void Update()
        {
            if (Level == null || State == Phase.Idle || State == Phase.Won || State == Phase.Lost) return;
            if (!Gameplaying() || CinematicPaused) return;
            if(RestLoadoutOpen){phaseUntil+=Time.deltaTime;return;}
            Elapsed += Time.deltaTime;
            if(AwaitingSkySword)return;
            switch (State)
            {
                case Phase.Intro:
                    if (Time.time >= phaseUntil) StartNextWave();
                    break;
                case Phase.Wave:
                    TickSpawns();
                    if (queue.Count == 0 && pending.Count == 0 && CountedAlive()==0) WaveCleared();
                    break;
                case Phase.Rest:
                    RestRemaining = Mathf.Max(0, phaseUntil - Time.time);
                    if (RestRemaining <= 0) StartNextWave();
                    break;
            }
        }

        static bool Gameplaying()
        {
            var ui = UIStateManager.Instance;
            return ui == null || ui.State == UIState.Gameplay || ui.State == UIState.Modal;
        }

        void StartNextWave()
        {
            RestLoadoutOpen=false;
            waveClearRaised=false;
            WaveIndex++;
            if (WaveIndex >= Level.waves.Count) { CheckWin(); return; }
            State = Phase.Wave; RestRemaining = 0;
            var wave = Level.waves[WaveIndex];
            queue.Clear();
            var entries=Level.spawnTable!=null?Level.spawnTable.Generate(wave.TotalCount,WaveIndex,RunSeed,WaveIndex==Level.waves.Count-1):wave.entries;
            foreach (var entry in entries)
                for (int i = 0; i < entry.count; i++) queue.Add(new Request { archetype = entry.archetype, elite = i < entry.eliteCount });
            if(Level.index==3&&WaveIndex==2)queue.Add(new Request{archetype=Resources.Load<EnemyArchetype>("P12/ShabanElite"),elite=true});
            Shuffle(queue, RunSeed+WaveIndex*31);
            var intent=GetComponent<SkyBeast.SwordIntent>();
            if(Level.index>=8 && intent!=null){int weight=0;foreach(var r in queue)weight+=r.elite&&Level.index>=6?4:r.archetype.swordIntentWeight;intent.BeginWave(weight,queue.Count);}
            nextSpawn = Time.time + 0.25f;
            LevelEvents.RaiseWaveStarted(WaveIndex + 1, Level.waves.Count);
        }

        static void Shuffle(List<Request> list, int seed)
        {
            var random = new System.Random(seed);
            for (int i = list.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var t = list[i]; list[i] = list[j]; list[j] = t; }
        }

        void TickSpawns()
        {
            // Pending monsters were already promised a slot; the portal shows for a moment before the monster steps out.
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < pending[i].at) continue;
                var p = pending[i]; pending.RemoveAt(i);
                SpawnNow(p.request, p.position);
            }
            while (queue.Count > 0 && Time.time >= nextSpawn && alive.Count + pending.Count < ConcurrentCap)
            {
                int selected=queue.Count-1;
                if(Level.spawnTable!=null){while(selected>=0&&!CanPromise(queue[selected].archetype))selected--;if(selected<0)break;}
                var request = queue[selected]; queue.RemoveAt(selected);
                Vector3 position = PickPosition(Level.waves[WaveIndex]);
                Vector3 toPlayer = player != null ? player.transform.position - position : Vector3.forward;
                RiftPortal.Open(position, toPlayer);
                pending.Add(new Pending { request = request, position = position, at = Time.time + portalLead });
                nextSpawn = Time.time + spawnInterval;
            }
        }

        void SpawnNow(Request request, Vector3 position)
        {
            var scaling=Level.Scaling;
            if(Level.runMode==EndgameMode.Tower&&Level.towerRule==TowerRule.FireMoon&&request.archetype.element==Element.Hoa){scaling.health*=1.3f;scaling.damage*=1.3f;}
            var enemy = EnemyPool.Ensure().Spawn(request.archetype, position, scaling);
            if (enemy == null) { TotalPlanned = Mathf.Max(0, TotalPlanned - 1); GetComponent<SkyBeast.SwordIntent>()?.SpawnFailed(request.elite&&Level.index>=6?4:request.archetype.swordIntentWeight); return; }
            if(request.elite&&(Level.index>=6||Level.runMode!=EndgameMode.Normal))enemy.Elite.Configure(Level.runMode==EndgameMode.Normal?Level.index:10,RunSeed+SpawnedTotal*71);
            if(Level.index>=8)GetComponent<SkyBeast.SwordIntent>()?.Track(enemy);
            alive.Add(enemy); SpawnedTotal++;
            MaxConcurrentSeen = Mathf.Max(MaxConcurrentSeen, alive.Count);
        }
        bool CanPromise(EnemyArchetype archetype){if(archetype==null)return true;if(!Level.spawnTable.CanSpawn(archetype,alive))return false;
            if(archetype.id!="thiet-giap-nguu"&&archetype.id!="bao-thi")return true;
            int n=0;foreach(var e in alive)if(e.archetype==archetype)n++;foreach(var p in pending)if(p.request.archetype==archetype)n++;
            return n<(archetype.id=="bao-thi"?Level.spawnTable.maxBombersConcurrent:Level.spawnTable.maxHeavyConcurrent);}
        public void ForgetSummoned(EnemyInstance enemy){alive.Remove(enemy);}
        public void TrackSummoned(EnemyInstance enemy){if(enemy==null||!alive.Add(enemy))return;SpawnedTotal++;}

        Vector3 PickPosition(WaveDefinition wave)
        {
            var candidates = new List<RiftZoneData>();
            if (wave.riftZones != null && wave.riftZones.Length > 0)
                foreach (var id in wave.riftZones) { if (Level.TryGetZone(id, out var z)) candidates.Add(z); }
            if (candidates.Count == 0) candidates.AddRange(Level.zones);
            if (candidates.Count == 0) return player != null ? player.transform.position + player.transform.forward * 12f : Vector3.zero;

            Vector3 from = player != null ? player.transform.position : Vector3.zero;
            var far = candidates.FindAll(z => Vector3.ProjectOnPlane(z.position - from, Vector3.up).magnitude >= MinSpawnDistance);
            RiftZoneData zone;
            if (far.Count > 0) zone = far[zoneCursor++ % far.Count];
            else
            {
                zone = candidates[0]; float best = -1;
                foreach (var c in candidates) { float d = Vector3.Distance(c.position, from); if (d > best) { best = d; zone = c; } }
            }
            float angle=(float)spawnRandom.NextDouble()*Mathf.PI*2,radius=Mathf.Sqrt((float)spawnRandom.NextDouble())*zone.radius;
            Vector2 offset = new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
            return zone.position + new Vector3(offset.x, 0, offset.y);
        }

        void OnEnemyDied(EnemyInstance enemy)
        {
            if (!alive.Remove(enemy)) return;
            if(!enemy.countsForSwordIntent)return;
            Kills++;
            LevelEvents.RaiseEnemyKilled(enemy);
        }

        int CountedAlive(){int count=0;foreach(var e in alive)if(e!=null&&e.countsForSwordIntent)count++;return count;}
        void WaveCleared()
        {
            if(!waveClearRaised){waveClearRaised=true;LevelEvents.RaiseWaveCleared(WaveIndex + 1, Level.waves.Count);}
            if(Level.index>=8 && SkyBeast.SkyBeastScheduler.Instance!=null && !SkyBeast.SkyBeastScheduler.Instance.Completed)
            {AwaitingSkySword=true;GetComponent<SkyBeast.SwordIntent>()?.MarkWaveCleared();return;}
            if (WaveIndex + 1 >= Level.waves.Count) { CheckWin(); return; }
            State = Phase.Rest; phaseUntil = Time.time + Level.restSeconds; RestRemaining = Level.restSeconds;
            if (spirit != null) spirit.Restore(spirit.Max * 0.2f);
        }

        public void CompleteSkySword()
        {
            AwaitingSkySword=false;
            if(SkyBeast.SkyBeastScheduler.Instance!=null && SkyBeast.SkyBeastScheduler.Instance.Completed){Win();return;}
            State=Phase.Rest;RestRemaining=SkyBeast.HeavenSwordConfig.Load().restSeconds;phaseUntil=Time.time+RestRemaining;
            if(spirit!=null)spirit.Restore(spirit.Max*.2f);
        }
        public void Win(){if(Level!=null && State!=Phase.Lost)CheckWin();}

        void CheckWin()
        {
            if (State == Phase.Won || State == Phase.Lost) return;
            if(!bossStarted&&Level.bosses.Count>0){bossStarted=true;State=Phase.Wave;foreach(var boss in Level.bosses)SpawnNow(new Request{archetype=boss},PickPosition(Level.waves[Level.waves.Count-1]));return;}
            if (holds.Count > 0) { State = Phase.Wave; return; }
            State = Phase.Won; WinsRaised++;
            Result = MakeResult(true);
            var levels = Progression.LevelProgressService.Current;
            if (levels != null && Level.runMode==EndgameMode.Normal) { levels.RecordClear(Level, Elapsed, Result.starMask, out var clearAward); ClearAward = clearAward; }
            Progression.EndgameService.Record(Result,starEvaluator.Run);
            LevelEvents.RaiseWon(Result);
            StartCoroutine(WinRoutine());
        }

        IEnumerator WinRoutine()
        {
            if(Level.index==7){yield return new WaitForSeconds(1);yield return SkyBeast.SkyBeastEnding.Play();}
            if(Level.index==10&&Level.runMode==EndgameMode.Normal)yield return SkyBeast.P21StoryCinematic.Play(true);
            yield return new WaitForSeconds(Level.index>=8?Mathf.Max(winDelay,2.25f):winDelay);
            UIStateManager.Instance?.Win();
        }

        void OnPlayerDefeated()
        {
            if (State == Phase.Won || State == Phase.Lost || State == Phase.Idle) return;
            State = Phase.Lost; LossesRaised++;
            Result = MakeResult(false);
            LevelEvents.RaiseLost(Result);
            UIStateManager.Instance?.Defeat();
        }

        LevelResult MakeResult(bool won) => new LevelResult { level = Level, won = won, seconds = Elapsed, kills = Kills, total = TotalPlanned,starMask=starEvaluator.Finish(Level,won,Elapsed) };

        // Stops the level without a result (returning to the menu, tests).
        public void End()
        {
            RestLoadoutOpen=false;
            SkyBeast.HeavenSwordUltimate.Instance?.StopUltimate();AwaitingSkySword=CinematicPaused=false;
            Unsubscribe(); State = Phase.Idle; queue.Clear(); pending.Clear(); alive.Clear();
            StopAllCoroutines();SkyBeast.SkyBeastEnding.Cancel();SkyBeast.P21StoryCinematic.CancelActive();starEvaluator.Unhook();blackout.Restore();SkyBeast.FireBreathCycle.Instance?.StopCycle();SkyBeast.SkyBeastPresence.StopAll();EnemyTelegraph.Clear();
            EnemyPool.Instance?.ReleaseAll(); RiftPortal.CloseAll(); RestoreShaban();
        }
    }
}
