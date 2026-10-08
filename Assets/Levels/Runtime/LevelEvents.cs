using System;
using CampusRift.Enemies;

namespace CampusRift.Levels
{
    public struct LevelResult
    {
        public LevelDefinition level;
        public bool won;
        public float seconds;
        public int kills, total, starMask;
    }

    // Static feed for the HUD, the star evaluator (P12) and the Thiên Kiếm meter (P15).
    public static class LevelEvents
    {
        public static event Action<LevelDefinition> LevelStarted;
        public static event Action<int, int> WaveStarted;       // wave number (1-based), wave count
        public static event Action<int, int> WaveCleared;
        public static event Action<EnemyInstance> EnemyKilled;
        public static event Action<LevelResult> LevelWon;
        public static event Action<LevelResult> LevelLost;

        public static void RaiseStarted(LevelDefinition d) => LevelStarted?.Invoke(d);
        public static void RaiseWaveStarted(int wave, int count) => WaveStarted?.Invoke(wave, count);
        public static void RaiseWaveCleared(int wave, int count) => WaveCleared?.Invoke(wave, count);
        public static void RaiseEnemyKilled(EnemyInstance e) => EnemyKilled?.Invoke(e);
        public static void RaiseWon(LevelResult r) => LevelWon?.Invoke(r);
        public static void RaiseLost(LevelResult r) => LevelLost?.Invoke(r);
        public static void ResetAll() { LevelStarted = null; WaveStarted = null; WaveCleared = null; EnemyKilled = null; LevelWon = null; LevelLost = null; }
    }
}
