using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Levels;
using CampusRift.Monsters;
using CampusRift.UI;

namespace CampusRift.Progression
{
    // Explicit consent, local JSONL only. No profile identifier, answer text or network transport.
    public sealed class LocalTelemetry : MonoBehaviour
    {
        [Serializable] public sealed class Count { public string id; public int count; }
        [Serializable] public sealed class Answer { public string id; public bool correct; }
        [Serializable] public sealed class Row
        {
            public int schema = 1, level, deaths, fireHits, stars;
            public string kind, utc, outcome, deathCause, mode;
            public int towerFloor;
            public float seconds;
            public List<Count> skills = new List<Count>(), items = new List<Count>(), reactions = new List<Count>();
            public List<Answer> answers = new List<Answer>();
        }
        public static LocalTelemetry Instance { get; private set; }
        public static string Folder => Path.Combine(Application.persistentDataPath, "telemetry");
#if UNITY_EDITOR
        public static string TestFolder;
#endif
        static string ActiveFolder
        {
            get {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(TestFolder)) return TestFolder;
#endif
                return Folder;
            }
        }
        public string LastError { get; private set; }
        public int WrittenRows { get; private set; }
        Row run;
        PlayerMonsterHealth health;
        string cause;
        static bool Consent => SettingsManager.Instance != null && SettingsManager.Instance.Current.LocalTelemetryEnabled;
        static bool Allowed
        {
            get
            {
                if (DevMode.Active || !Consent) return false;
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(TestFolder)) return true;
#endif
                return ProfileService.Instance == null || !ProfileService.Instance.Transient;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null;
#if UNITY_EDITOR
            TestFolder = null;
#endif
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("Local telemetry"); DontDestroyOnLoad(go); go.AddComponent<LocalTelemetry>();
        }
        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            LevelEvents.LevelStarted += Begin;
            LevelEvents.LevelWon += Won; LevelEvents.LevelLost += Lost;
            ReactionResolver.ReactionTriggered += Reaction;
        }
        void OnDestroy()
        {
            LevelEvents.LevelStarted -= Begin; LevelEvents.LevelWon -= Won; LevelEvents.LevelLost -= Lost;
            ReactionResolver.ReactionTriggered -= Reaction; Unhook();
            if (Instance == this) Instance = null;
        }
        void Unhook() { if (health != null) { health.DamageReceived -= Damage; health.Defeated.RemoveListener(Death); } health = null; }
        void Begin(LevelDefinition d)
        {
            Finish("abandoned", 0); Unhook(); cause = null;
            if (!Allowed) return;
            run = new Row { kind = "run", level = d.index, mode=d.runMode.ToString(), towerFloor=d.towerFloor, utc = DateTime.UtcNow.ToString("o") };
            health = FindAnyObjectByType<PlayerMonsterHealth>();
            if (health != null) { health.DamageReceived += Damage; health.Defeated.AddListener(Death); }
        }
        void Damage(DamageInfo d)
        {
            if (!Allowed || run == null) return;
            cause = string.IsNullOrEmpty(d.skillId) ? d.source + "/" + d.element : d.skillId;
            if (SkyBeast.FireBreathCycle.IsFireHazard(d)) run.fireHits++;
        }
        void Death() { if (Allowed && run != null) { run.deaths++; run.deathCause = cause; } }
        void Reaction(ReactionType type, MonsterVitality victim) { if (Allowed && run != null) Add(run.reactions, type.ToString()); }
        public static void Skill(string id) { if (Allowed && Instance?.run != null) Add(Instance.run.skills, id); }
        // AR passes aggregate label counters only; no images, positions, or landmark data.
        public static void ARGestures(List<Count> counts)
        {if(!Allowed||Instance==null)return;Instance.Write(new Row{kind="ar-gestures",utc=DateTime.UtcNow.ToString("o"),skills=new List<Count>(counts)});}
        public static void Item(string id) { if (Allowed && Instance?.run != null) Add(Instance.run.items, id); }
        static void Add(List<Count> list, string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var value = list.Find(x => x.id == id); if (value == null) { value = new Count { id = id }; list.Add(value); } value.count++;
        }
        public static void Quiz(Learning.QuizResult result)
        {
            if (!Allowed || Instance == null) return;
            var row = new Row { kind = "quiz", utc = DateTime.UtcNow.ToString("o") };
            foreach (var a in result.answers) row.answers.Add(new Answer { id = a.questionId, correct = a.correct });
            Instance.Write(row);
        }
        void Won(LevelResult r) { Finish("won", r.starMask, r.seconds); }
        void Lost(LevelResult r) { if(Allowed&&run!=null&&run.deaths==0){run.deaths=1;run.deathCause=cause;} Finish("lost", r.starMask, r.seconds); }
        public void Finish(string outcome, int stars, float seconds = -1)
        {
            if (run == null) return;
            run.outcome = outcome; run.stars = stars;
            run.seconds = seconds >= 0 ? seconds : run.seconds;
            if (Allowed) Write(run); run = null; Unhook();
        }
        void Update()
        {
            if (!Allowed) { run = null; Unhook(); return; }
            if(run!=null&&LevelDirector.Instance!=null&&LevelDirector.Instance.State!=LevelDirector.Phase.Idle)run.seconds=LevelDirector.Instance.Elapsed;
            if (run != null && (LevelDirector.Instance == null || LevelDirector.Instance.State == LevelDirector.Phase.Idle)) Finish("abandoned", 0);
        }
        void Write(Row row)
        {
            if (!Allowed) return;
            try
            {
                string folder = ActiveFolder;
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "sessions-" + DateTime.UtcNow.ToString("yyyy-MM-dd") + ".jsonl"), JsonUtility.ToJson(row) + "\n", new System.Text.UTF8Encoding(false));
                LastError = null; WrittenRows++;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { LastError = e.Message; }
        }
        public bool DeleteLocalFiles()
        {
            run = null; Unhook();
            try { if (Directory.Exists(ActiveFolder)) foreach (var p in Directory.GetFiles(ActiveFolder, "*.jsonl", SearchOption.TopDirectoryOnly)) File.Delete(p); LastError = null; return true; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { LastError = e.Message; return false; }
        }
        // Keep a run open across background/resume; one row is committed at outcome or quit.
        void OnApplicationQuit() { Finish("quit", 0); }
    }
}
