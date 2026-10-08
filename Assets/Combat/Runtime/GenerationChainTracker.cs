using System;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.Monsters;
namespace CampusRift.Combat
{
    [DisallowMultipleComponent]
    public sealed class GenerationChainTracker : MonoBehaviour
    {
        AR.ARCombatContext ar;float SessionNow=>ar!=null?ar.Now:Time.time;float Scale=>ar!=null?ar.scale:1;

        public ReactionConfig config;
        readonly Element[] elements = new Element[3];
        float startedAt, completedAt;
        SpiritPower spirit;
        PlayerMonsterHealth health;
        public int Count { get; private set; }
        public int Completions { get; private set; }
        public Element Used(int index) => index >= 0 && index < Count ? elements[index] : Element.None;
        public Element Next => Count > 0 && Count < 3 ? ElementChart.Generates(elements[Count - 1]) : Element.None;
        public float Remaining => Count > 0 && Count < 3 ? Mathf.Max(0, Window - (SessionNow - startedAt)) : 0;
        public float Window => config != null ? config.chainWindow : 6;
        public event Action Changed, ChainCompleted;
        void Awake() { ar=GetComponent<AR.ARCombatContext>(); config = config != null ? config : ReactionConfig.Current; spirit = GetComponent<SpiritPower>(); health = GetComponent<PlayerMonsterHealth>(); }
        // Called once after validation/resource payment, before scheduling skill hits.
        public float Commit(SkillRuntime skill)
        {
            if (skill == null || skill.Definition == null || config == null || (health != null && health.IsDead)) return 1;
            return Record(skill.Definition.element);
        }
        public float Record(Element element)
        {
            if (config == null || (health != null && health.IsDead)) { ResetChain(); return 1; }
            if (Count == 3 || (Count > 0 && SessionNow - startedAt > Window)) ResetChain();
            if (ElementChart.Generates(element) == Element.None) { ResetChain(); return 1; }
            if (Count > 0 && !ElementChart.Generates(elements[Count - 1], element)) ResetChain();
            if (Count == 0) startedAt = SessionNow;
            elements[Count++] = element;
            if (Count == 3)
            {
                completedAt = SessionNow; Completions++;
                spirit?.Restore(config.chainSpirit);
                Changed?.Invoke(); ChainCompleted?.Invoke();
                return 1 + config.chainDamageBonus;
            }
            Changed?.Invoke(); return 1;
        }
        public void ResetChain() { if (Count == 0) return; Count = 0; Array.Clear(elements, 0, elements.Length); Changed?.Invoke(); }
        void Update()
        {
            if(ar!=null&&ar.Paused)return;
            if ((health != null && health.IsDead) || (Count == 3 ? SessionNow - completedAt > 1.2f : Count > 0 && Remaining <= 0)) ResetChain();
        }
        void OnDisable() { ResetChain(); }
    }
}
