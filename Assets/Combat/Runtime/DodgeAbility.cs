using UnityEngine;
using CampusRift.Controls;
using CampusRift.Monsters;

namespace CampusRift.Combat
{
    // Né: a 5 m burst with 0.3 s of invulnerability (plan §6). Costs stamina, so it competes with sprinting.
    [DisallowMultipleComponent, DefaultExecutionOrder(-10)]
    public sealed class DodgeAbility : MonoBehaviour
    {
        [Min(1)] public float distance = 5f;
        [Min(0.05f)] public float duration = 0.18f;
        [Min(0)] public float energyCost = 25f;
        [Min(0)] public float cooldown = 0.6f;
        [Min(0)] public float invulnerableSeconds = 0.3f;
        public int DodgeCount { get; private set; }
        public float CooldownRemaining => CampusRift.Progression.DevMode.NoCooldown ? 0 : Mathf.Max(0, readyAt - Time.time);
        public string Feedback { get; private set; } = "";
        CampusExplorer explorer; CampusInput input; PlayerMonsterHealth health; CharacterAfterimageTrail trail; PlayerSoundEmitter sound;
        float readyAt;

        void Awake()
        {
            explorer = GetComponent<CampusExplorer>(); input = GetComponent<CampusInput>(); health = GetComponent<PlayerMonsterHealth>();
            trail = GetComponent<CharacterAfterimageTrail>(); sound = GetComponent<PlayerSoundEmitter>();
        }

        void Update()
        {
            if (input == null || !input.Allowed) return;
            if (input.Pressed(CampusAction.Dash)) TryDodge();
        }

        public bool CanDodge => CooldownRemaining <= 0 && (health == null || !health.IsDead) && explorer.RidingElevator == null && !explorer.IsDashing;

        public bool TryDodge()
        {
            if (!CanDodge) return false;
            var speed=GetComponent<CampusRift.Skills.KunpengSpeedRuntime>();
            if (!(speed!=null&&speed.Active) && !explorer.TrySpendEnergy(energyCost)) { Feedback = "NOT ENOUGH STAMINA"; return false; }
            Vector2 stick = input.Move;
            // With no input the dodge slips backwards, away from what the player is facing.
            Vector3 direction = stick.sqrMagnitude > 0.04f ? explorer.InputToWorld(stick).normalized : -transform.forward;
            explorer.Dash(direction, distance, duration);
            if (health != null) health.GrantInvulnerability(invulnerableSeconds);
            if (trail != null) trail.TriggerDash(invulnerableSeconds);
            if (sound != null) sound.AirDash();
            readyAt = Time.time + cooldown; DodgeCount++; Feedback = "";
            return true;
        }
    }
}
