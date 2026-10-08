using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Enemies;
using CampusRift.Levels;
namespace CampusRift.SkyBeast
{
    // The denominator includes queued/pending enemies, not just enemies already visible.
    public sealed class SwordIntent : MonoBehaviour
    {
        public static SwordIntent Instance { get; private set; }
        public int TotalWeight { get; private set; }
        public int DefeatedWeight { get; private set; }
        public int PlannedCount { get; private set; }
        public int DefeatedCount { get; private set; }
        public bool WaveCleared { get; private set; }
        public bool Full => Progression.DevMode.Active || WaveCleared && PlannedCount > 0 && DefeatedCount >= PlannedCount;
        public float Fraction => Full ? 1 : Mathf.Min(.99f, TotalWeight > 0 ? (float)DefeatedWeight / TotalWeight : 0);
        public event Action Changed;
        public float ReadyAt { get; private set; }
        public static event Action Ready;
        readonly Dictionary<EnemyInstance,int> tracked = new Dictionary<EnemyInstance,int>();
        readonly List<EnemyInstance> summons = new List<EnemyInstance>();
        void Awake() { Instance = this; EnemyDirector.EnemyDied += Died; }
        public void BeginWave(int weight, int count)
        {
            tracked.Clear(); TotalWeight = weight; PlannedCount = count; DefeatedWeight = DefeatedCount = 0; WaveCleared = false; Changed?.Invoke();
        }
        public void Track(EnemyInstance enemy) { if(enemy != null && enemy.countsForSwordIntent) tracked[enemy] = enemy.SwordIntentWeight; }
        public void SpawnFailed(int weight) { PlannedCount = Mathf.Max(0, PlannedCount - 1); TotalWeight = Mathf.Max(0, TotalWeight - weight); Changed?.Invoke(); }
        void Died(EnemyInstance enemy)
        {
            if(!tracked.TryGetValue(enemy, out int weight)) return;
            tracked.Remove(enemy); DefeatedCount++; DefeatedWeight += weight; Changed?.Invoke();
        }
        public void MarkWaveCleared()
        {
            if(WaveCleared) return; WaveCleared = true;
            if(Full)
            {
                ReadyAt=Time.time;Ready?.Invoke();
                summons.Clear();
                if(EnemyDirector.Instance != null) foreach(var e in EnemyDirector.Instance.Active) if(e != null && !e.countsForSwordIntent) summons.Add(e);
                foreach(var e in summons) StartCoroutine(Dissolve(e));
                HeavenSwordAudio.Instance?.Ready();
            }
            Changed?.Invoke();
        }
        IEnumerator Dissolve(EnemyInstance e)
        {
            // Do not inflict a hit: summoned bombers must not explode or award kills.
            if(e.Brain != null) { e.Brain.StopAllCoroutines(); e.Brain.enabled = false; }
            e.GetComponent<EnemyAbilityRunner>()?.Cancel(); e.GetComponent<ExpandedEnemyRuntime>()?.Cancel();var flight=e.GetComponent<FlyingMotor>();if(flight!=null)flight.enabled=false;e.Motor?.Stop();
            var colliders = e.GetComponentsInChildren<Collider>(); foreach(var c in colliders)c.enabled=false;
            float t=0; while(t<.65f && e!=null && e.gameObject.activeSelf){t+=Time.deltaTime;e.Animation?.Dissolve(Mathf.Clamp01(t/.65f));yield return null;}
            if(e!=null){LevelDirector.Instance?.ForgetSummoned(e);EnemyPool.Instance?.Release(e);foreach(var c in colliders)if(c!=null)c.enabled=true;}
        }
        public void Consume() { BeginWave(0,0); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetEvents(){Instance=null;Ready=null;}
        void OnDestroy() { EnemyDirector.EnemyDied -= Died; if(Instance==this)Instance=null; }
    }
}
