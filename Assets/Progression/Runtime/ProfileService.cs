using System;
using System.IO;
using UnityEngine;

namespace CampusRift.Progression
{
    // Owns the V2 profile: loads it at start-up, saves when something changed (coalesced to one second) and on
    // pause/quit (P06-T01). Settings stay in their own save.
    [DefaultExecutionOrder(-600)]
    public sealed class ProfileService : MonoBehaviour
    {
        public const string FileName = "campusrift-v2.json", LegacyFileName = "learning-v1.json", LegacyBackupName = "learning-v1.backup.json";
        public const float SaveDelay = 1f;
        public static ProfileService Instance { get; private set; }
        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public static string LegacyPath => Path.Combine(Application.persistentDataPath, LegacyFileName);
        public static CultivationService CultivationOrNull => Instance != null ? Instance.Cultivation : null;

        public ProfileData Data { get; private set; }
        public CultivationService Cultivation { get; private set; }
        public LevelProgressService Levels { get; private set; }
        public Wallet Wallet { get; private set; }
        public Inventory Inventory { get; private set; }
        public ArtifactService Artifacts { get; private set; }
        public Skills.SkillProgressService Skills { get; private set; }
        public string SaveError { get; private set; }
        public string RecoveryMessage { get; private set; }
        public bool Transient { get; private set; }
        public int SaveCount { get; private set; }
        public event Action Changed;
        IProfileStore store;
        float dirtySince = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { Ensure(); }

        // Creates the service on first use; Learning boots through this too, so start-up order does not matter.
        public static ProfileService Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("Profile"); DontDestroyOnLoad(go);
            var service = go.AddComponent<ProfileService>();
            service.Initialize(new JsonProfileStore(SavePath), LegacyPath);
            if (!string.IsNullOrEmpty(service.RecoveryMessage)) Debug.LogWarning(service.RecoveryMessage);
            return service;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(IProfileStore profileStore, string legacyPath)
        {
            Instance = this; store = profileStore;
            var data = store.Load();
            var json = store as JsonProfileStore;
            RecoveryMessage = json?.RecoveryMessage;
            bool fresh = json != null ? !json.FoundExisting : true;
            if (fresh && !string.IsNullOrEmpty(legacyPath) && ProfileMigration.ArchiveLegacy(legacyPath))
            { data.migration.v1Archived = true; data.migration.noticePending = true; }
            Bind(data);
            if (fresh) Flush();
            DevMode.RestorePreferences(this);
            if (Data.migration.noticePending && Application.isPlaying) gameObject.AddComponent<MigrationNotice>();
        }

        void Bind(ProfileData data)
        {
            Data = data;
            if (Cultivation == null)
            {
                Cultivation = new CultivationService(data.cultivation);
                Cultivation.Changed += MarkDirty;
                Levels = new LevelProgressService(this);
                Wallet = new Wallet(this); Inventory = new Inventory(this); Artifacts = new ArtifactService(this);
                Skills = new Skills.SkillProgressService(this);
            }
            else Cultivation.Bind(data.cultivation);
        }

        // Tests and tools: run against an in-memory profile that is never written.
        IProfileStore realStore; ProfileData realData;
        sealed class Snapshot { public IProfileStore store; public ProfileData data; public bool transient; public float dirty; }
        readonly System.Collections.Generic.Stack<Snapshot> overlays = new System.Collections.Generic.Stack<Snapshot>();
        public void PushTransient(ProfileData data)
        {
            if (!Transient && dirtySince >= 0) Flush();
            overlays.Push(new Snapshot { store = store, data = Data, transient = Transient, dirty = dirtySince });
            Transient = true; store = null; Bind(data); dirtySince = -1; Changed?.Invoke();
        }
        public void PopTransient()
        {
            if (overlays.Count == 0) return;
            var saved = overlays.Pop(); store = saved.store; Transient = saved.transient;
            Bind(saved.data); dirtySince = saved.dirty; Changed?.Invoke();
        }
        public void NotifyChanged() { Changed?.Invoke(); }
#if UNITY_EDITOR
        // Dedicated on-disk QA save above the user's untouched snapshot.
        public void PushStoredProfileForValidation(IProfileStore qaStore)
        {
            overlays.Push(new Snapshot { store = store, data = Data, transient = Transient, dirty = dirtySince });
            store = qaStore; Transient = false; Bind(qaStore.Load()); dirtySince = -1; Changed?.Invoke();
        }
#endif
        public void UseTransient(ProfileData data)
        {
            if (!Transient) { realStore = store; realData = Data; Flush(); }
            Transient = true; store = null; dirtySince = -1; Bind(data ?? new ProfileData()); Changed?.Invoke();
        }
        // Back to the real profile and store after UseTransient.
        public void EndTransient()
        {
            if (!Transient) return;
            Transient = false; store = realStore; dirtySince = -1; Bind(realData); realStore = null; realData = null; Changed?.Invoke();
        }

        public bool HasProgress
        {
            get
            {
                if (Data.cultivation.totalEarned > 0 || Data.levels.Count > 0) return true;
                foreach (var l in Data.learning.lessons) if (l.pagesRead > 0) return true;
                return false;
            }
        }

        public void MarkDirty()
        {
            if (dirtySince < 0) dirtySince = Time.unscaledTime;
            Changed?.Invoke();
        }

        void Update()
        {
            if (dirtySince >= 0 && Time.unscaledTime - dirtySince >= SaveDelay) Flush();
        }

        public void Flush()
        {
            dirtySince = -1;
            if (DevMode.Active || Transient || store == null || Data == null) return;
            try { store.Save(Data); SaveError = null; SaveCount++; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { SaveError = e.Message; }
        }

        void OnApplicationPause(bool pause) { if (pause) Flush(); }
        void OnApplicationQuit() { Flush(); }
        void OnDestroy() { if (Instance == this) { Flush(); Instance = null; } }
    }

    // The old learning save is kept as learning-v1.backup.json and left untouched (P06-T02).
    public static class ProfileMigration
    {
        public static bool ArchiveLegacy(string legacyPath)
        {
            try
            {
                if (!File.Exists(legacyPath)) return false;
                string backup = Path.Combine(Path.GetDirectoryName(legacyPath), ProfileService.LegacyBackupName);
                if (!File.Exists(backup)) File.Copy(legacyPath, backup, false);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { Debug.LogWarning("Could not archive the old save: " + e.Message); return false; }
        }
    }
}
