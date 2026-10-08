using UnityEngine;
using CampusRift.Monsters;

namespace CampusRift.Combat
{
    // Red pulse on a monster that is winding up an attack (P03-T06; used by minions from P04).
    // TelegraphOutline.Show(monster, seconds) starts it; it switches itself off.
    [DisallowMultipleComponent]
    public sealed class TelegraphOutline : MonoBehaviour
    {
        MonsterVitality vitality;
        float until;
        public bool Active => Time.time < until;

        public static void Show(GameObject monster, float seconds)
        {
            if (monster == null) return;
            var outline = monster.GetComponent<TelegraphOutline>();
            if (outline == null) outline = monster.AddComponent<TelegraphOutline>();
            outline.Begin(seconds);
        }

        void Begin(float seconds)
        {
            if (vitality == null) vitality = GetComponent<MonsterVitality>();
            until = Mathf.Max(until, Time.time + seconds);
            if (vitality != null) vitality.SetTelegraph(true);
            enabled = true;
        }
        public void Hide() { until = 0; if (vitality != null) vitality.SetTelegraph(false); }
        void Update() { if (!Active) { if (vitality != null) vitality.SetTelegraph(false); enabled = false; } }
        void OnDisable() { if (vitality != null) vitality.SetTelegraph(false); }
    }
}
