using System;
using UnityEngine;

namespace CampusRift.Monsters
{
    public enum PlayerSoundType { Walk, Run, Sprint, Jump, Landing, HeavyLanding, Grapple, AirDash, Combat, GiantHandSkill }
    public readonly struct SoundEvent
    {
        public readonly Vector3 Position;
        public readonly float Loudness;
        public readonly PlayerSoundType SoundType;
        public readonly float Timestamp;
        // Used only to ignore the emitting collider during an occlusion ray. It is not
        // a player identity and is never supplied to the monster's target selector.
        public readonly Transform Emitter;
        public SoundEvent(Vector3 position, float loudness, PlayerSoundType type, float timestamp)
            : this(position, loudness, type, timestamp, null) { }
        public SoundEvent(Vector3 position, float loudness, PlayerSoundType type, float timestamp, Transform emitter)
        { Position = position; Loudness = loudness; SoundType = type; Timestamp = timestamp; Emitter = emitter; }
    }

    // Environmental tools and character emitters share an anonymous evidence channel.
    public static class SoundEventBus
    {
        public static event Action<SoundEvent> Emitted;
        public static void Publish(SoundEvent sound) { Emitted?.Invoke(sound); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Emitted = null; }
    }

    [RequireComponent(typeof(CampusExplorer))]
    public sealed class PlayerSoundEmitter : MonoBehaviour
    {
        public event Action<SoundEvent> SoundEmitted;
        CampusExplorer movement;
        CharacterController controller;
        bool wasGrounded, initialized;
        float previousVerticalSpeed, nextStep;
        void Awake() { movement = GetComponent<CampusExplorer>(); controller = GetComponent<CharacterController>(); }
        void LateUpdate()
        {
            if (!movement.enabled || !controller.enabled) { initialized = false; return; }
            bool grounded = movement.IsGrounded;
            float vertical = controller.velocity.y;
            if (initialized)
            {
                if (wasGrounded && !grounded && vertical > 1) Emit(PlayerSoundType.Jump);
                if (!wasGrounded && grounded) Emit(previousVerticalSpeed < -12 ? PlayerSoundType.HeavyLanding : PlayerSoundType.Landing);
            }
            if (grounded && movement.CurrentSpeed > 0.5f && Time.time >= nextStep)
            {
                var type = movement.IsSprinting ? PlayerSoundType.Sprint : movement.CurrentSpeed > movement.walkSpeed + 0.3f ? PlayerSoundType.Run : PlayerSoundType.Walk;
                Emit(type); nextStep = Time.time + (movement.IsSprinting ? 0.28f : 0.48f);
            }
            initialized = true; wasGrounded = grounded; previousVerticalSpeed = vertical;
        }
        // Call these at the actual skill activation, not every airborne frame.
        public void Grapple() => Emit(PlayerSoundType.Grapple);
        public void AirDash() => Emit(PlayerSoundType.AirDash);
        public void Combat() => Emit(PlayerSoundType.Combat);
        public void GiantHandSkill() => Emit(PlayerSoundType.GiantHandSkill);
        public void Emit(PlayerSoundType type)
        {
            float loudness;
            switch (type)
            {
                case PlayerSoundType.Walk: loudness = 0.35f; break;
                case PlayerSoundType.Run: loudness = 0.8f; break;
                case PlayerSoundType.Sprint: loudness = 1.5f; break;
                case PlayerSoundType.Jump: loudness = 0.8f; break;
                case PlayerSoundType.Landing: loudness = 1.1f; break;
                case PlayerSoundType.HeavyLanding: loudness = 3; break;
                case PlayerSoundType.GiantHandSkill: loudness = 5; break;
                case PlayerSoundType.Combat: loudness = 3; break;
                default: loudness = 2; break;
            }
            var sound = new SoundEvent(transform.position, loudness, type, Time.time, transform);
            SoundEmitted?.Invoke(sound);
            SoundEventBus.Publish(sound);
        }
    }
}
