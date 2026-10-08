using System;
using UnityEngine;

namespace CampusRift.Combat
{
    public enum ReactionType { IceLightning, ElectricFlow, FireExplosion, Convergence, ArmorShatter, Wildfire, SwordSoul, GuardDrain, DomainResonance }
    [Serializable] public sealed class ReactionRule
    {
        public ReactionType type;
        public string id, nameEN, nameVI, hintEN, hintVI;
        public Element first, second;
        [Min(0)] public float radius, bonus, statusDuration, statusMagnitude;
        public float hitStop = .08f, impulse = .55f;
        public AudioClip transient, body, tail;
    }
    [CreateAssetMenu(menuName="Campus Rift/Combat/Reactions")]
    public sealed class ReactionConfig : ScriptableObject
    {
        public float internalCooldown = 1, chainWindow = 6, chainDamageBonus = .3f, chainSpirit = 20;
        public ReactionRule[] rules;
        public ReactionRule Rule(ReactionType type) => rules[(int)type];
        public static ReactionConfig Current
        {
            get
            {
                var link = Resources.Load<ReactionConfigReference>("ReactionConfigReference");
                return link != null ? link.config : null;
            }
        }
    }
}
