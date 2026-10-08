using UnityEngine;

namespace CampusRift.Audio
{
    public enum SfxGroup { SmallMonster, BigMonster, Defeat, Victory }

    // Plays the user-supplied sound effects (Resources/Sfx). Each group picks a random clip and never the same one twice
    // in a row; monster groups are rate limited so a horde does not turn into noise.
    public static class GameSfx
    {
        static readonly string[][] Names =
        {
            new[] { "quai-nho-2", "quai-nho-3" },
            new[] { "quai-lon-1" },
            new[] { "that-bai-1", "that-bai-2" },
            new[] { "win" },
        };
        static readonly float[] MinGap = { 0.18f, 1.5f, 0f, 0f };
        static readonly float[] Volume = { 0.7f, 0.9f, 0.9f, 0.9f };
        // The supplied monster clips run 5–12 s; a voice is cut after this many seconds so attacks do not pile up.
        static readonly float[] MaxSeconds = { 3f, 4f, 0f, 0f };
        static readonly int[] MaxAtOnce = { 3, 1, 1, 1 };
        static AudioClip[][] clips; static int[] last; static float[] nextAt;
        static AudioSource ui; static AudioSource[] pool; static int poolIndex;
        public static int PlayCount { get; private set; }
        public static string LastClip { get; private set; }
        public static bool Enabled = true;
        // Counts a sound played by a monster's own source.
        public static void NotePlayed(AudioClip clip) { PlayCount++; LastClip = clip != null ? clip.name : null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { clips = null; ui = null; pool = null; PlayCount = 0; LastClip = null; Enabled = true; }

        static void Load()
        {
            if (clips != null) return;
            clips = new AudioClip[Names.Length][]; last = new int[Names.Length]; nextAt = new float[Names.Length];
            for (int g = 0; g < Names.Length; g++)
            {
                clips[g] = new AudioClip[Names[g].Length]; last[g] = -1;
                for (int i = 0; i < Names[g].Length; i++) clips[g][i] = Resources.Load<AudioClip>("Sfx/" + Names[g][i]);
            }
        }

        public static AudioClip Pick(SfxGroup group)
        {
            Load();
            var list = clips[(int)group]; if (list.Length == 0) return null;
            int index = Random.Range(0, list.Length);
            if (list.Length > 1 && index == last[(int)group]) index = (index + 1 + Random.Range(0, list.Length - 1)) % list.Length;
            last[(int)group] = index; return list[index];
        }

        // One stable growl per source level; no random-state changes to other SFX groups.
        public static AudioClip SmallMonsterForLevel(int sourceLevel)
        {
            Load();
            var list = clips[(int)SfxGroup.SmallMonster];
            return list.Length == 0 ? null : list[(Mathf.Max(1, sourceLevel) - 1) % list.Length];
        }

        // Positional sound for monsters.
        public static bool PlayAt(SfxGroup group, Vector3 position)
        {
            if (!Enabled) return false;
            Load(); int g = (int)group;
            if (Time.unscaledTime < nextAt[g]) return false;
            var clip = Pick(group); if (clip == null) return false;
            var source = NextSource(g); if (source == null) return false;
            nextAt[g] = Time.unscaledTime + MinGap[g];
            source.transform.position = position; source.clip = clip; source.volume = Volume[g]; source.pitch = Random.Range(0.95f, 1.05f); source.Play();
            if (MaxSeconds[g] > 0 && clip.length > MaxSeconds[g]) source.SetScheduledEndTime(AudioSettings.dspTime + MaxSeconds[g]);
            PlayCount++; LastClip = clip.name; return true;
        }

        // Flat sound that keeps playing while the game is paused (victory and defeat jingles).
        public static bool PlayUI(SfxGroup group)
        {
            if (!Enabled) return false;
            var clip = Pick(group); if (clip == null) return false;
            if (ui == null)
            {
                var go = new GameObject("UI Sfx"); Object.DontDestroyOnLoad(go);
                ui = go.AddComponent<AudioSource>(); ui.playOnAwake = false; ui.spatialBlend = 0; ui.ignoreListenerPause = true;
            }
            ui.PlayOneShot(clip, Volume[(int)group]); PlayCount++; LastClip = clip.name; return true;
        }

        static AudioSource NextSource(int group)
        {
            if (pool == null || pool[0] == null)
            {
                var root = new GameObject("Monster Sfx"); Object.DontDestroyOnLoad(root);
                pool = new AudioSource[6];
                for (int i = 0; i < pool.Length; i++)
                {
                    var go = new GameObject("Voice " + i); go.transform.SetParent(root.transform, false);
                    var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 1; s.minDistance = 4; s.maxDistance = 45; s.rolloffMode = AudioRolloffMode.Logarithmic; s.dopplerLevel = 0;
                    pool[i] = s;
                }
            }
            // A group may only use MaxAtOnce voices; the oldest free voice is preferred.
            int playing = 0; foreach (var s in pool) if (s.isPlaying && s.clip != null && GroupOf(s.clip) == group) playing++;
            if (playing >= MaxAtOnce[group]) return null;
            for (int i = 0; i < pool.Length; i++) { var s = pool[(poolIndex + i) % pool.Length]; if (!s.isPlaying) { poolIndex = (poolIndex + i + 1) % pool.Length; return s; } }
            return null;
        }

        static int GroupOf(AudioClip clip)
        {
            for (int g = 0; g < Names.Length; g++) foreach (var n in Names[g]) if (n == clip.name) return g;
            return -1;
        }
    }
}
