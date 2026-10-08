using System;
using UnityEngine;
using CampusRift.Monsters;

namespace CampusRift.Combat
{
    public struct ReactionEvent
    {
        public ReactionType type;
        public MonsterVitality target;
        public GameObject attacker;
        public Vector3 point;
        public int affected;
    }
    // Receiver-side, per-life cooldowns. Secondary hits cannot enter this resolver.
    [DisallowMultipleComponent]
    public sealed class ReactionResolver : MonoBehaviour
    {
        AR.ARCombatContext ar;float SessionNow=>ar!=null?ar.Now:Time.time;float Scale=>ar!=null?ar.scale:1;

        public ReactionConfig config;
        readonly float[] readyAt = new float[9];
        readonly MonsterVitality[] nearby = new MonsterVitality[128];
        StatusEffectHost status;
        public int TriggerCount { get; private set; }
        public static event Action<ReactionType, MonsterVitality> ReactionTriggered;
        public static event Action<ReactionEvent> Feedback;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { ReactionTriggered = null; Feedback = null; }
        void Awake() { ar=GetComponent<AR.ARCombatContext>(); config = config != null ? config : ReactionConfig.Current; }
        public void ResetLife() { Array.Clear(readyAt, 0, readyAt.Length); TriggerCount = 0; }
        void OnDisable() { ResetLife(); }
        bool Ready(ReactionType type) => SessionNow >= readyAt[(int)type];
        public static bool PublishExtended(ReactionType type,MonsterVitality target,GameObject attacker,Vector3 point,int affected=1)
        {
            if((int)type<5||attacker==null)return false;var receiver=target!=null?target.GetComponent<ReactionResolver>():attacker.GetComponent<ReactionResolver>();
            if(receiver==null||receiver.config==null||!receiver.Ready(type))return false;receiver.readyAt[(int)type]=receiver.SessionNow+receiver.config.internalCooldown;receiver.TriggerCount++;
            ReactionTriggered?.Invoke(type,target);Feedback?.Invoke(new ReactionEvent{type=type,target=target,attacker=attacker,point=point,affected=affected});return true;
        }
        public void SpreadBurn(MonsterVitality target,GameObject caster)
        {
            if(status==null)status=GetComponent<StatusEffectHost>();if(status==null||!status.Has(StatusType.Burn)||!Ready(ReactionType.Wildfire))return;
            float duration=status.Remaining(StatusType.Burn),amount=status.Magnitude(StatusType.Burn),radius=config.Rule(ReactionType.Wildfire).radius;int affected=0;
            foreach(var m in MonsterVitality.Active)if(m!=null&&m!=target&&!m.Defeated&&Mathf.Abs(m.transform.position.y-target.transform.position.y)<3&&Vector3.ProjectOnPlane(m.transform.position-target.transform.position,Vector3.up).sqrMagnitude<=radius*radius&&CombatLine.Clear(target.transform.position+Vector3.up,m.transform.position+Vector3.up,target.transform))
            {m.GetComponent<StatusEffectHost>()?.Apply(StatusType.Burn,duration,amount,caster);affected++;}
            if(affected>0)PublishExtended(ReactionType.Wildfire,target,caster,target.transform.position,affected);
        }

        // Return a bitmask so multiple independent conditions on one hit remain possible.
        public int Before(MonsterVitality target, ref DamageInfo info)
        {
            if (config == null || info.source == DamageSource.Reaction || info.source == DamageSource.Environment || info.amount <= 0) return 0;
            if (status == null) status = GetComponent<StatusEffectHost>();
            if (status == null) return 0;
            int mask = 0;
            if (info.element == Element.Loi)
            {
                if (status.Has(StatusType.Freeze))
                {
                    if (Ready(ReactionType.IceLightning)) { mask |= 1 << (int)ReactionType.IceLightning; status.Consume(StatusType.Freeze); }
                }
                else if ((status.Has(StatusType.Chill) || status.Has(StatusType.Wet)) && Ready(ReactionType.ElectricFlow)) mask |= 1 << (int)ReactionType.ElectricFlow;
            }
            if (info.element == Element.Hoa && status.Has(StatusType.Burn) && Ready(ReactionType.FireExplosion)) mask |= 1 << (int)ReactionType.FireExplosion;
            if (info.isArea && status.Has(StatusType.Pulled) && Ready(ReactionType.Convergence)) mask |= 1 << (int)ReactionType.Convergence;
            if (info.element == Element.Kim && status.Has(StatusType.Stun) && Ready(ReactionType.ArmorShatter))
            {
                mask |= 1 << (int)ReactionType.ArmorShatter;
                var rule = config.Rule(ReactionType.ArmorShatter);
                status.Apply(StatusType.ArmorBreak, rule.statusDuration, rule.statusMagnitude, info.attacker);
                GetComponent<IArmorShatterable>()?.BreakMetalBody();
            }
            for (int i = 0; i < 5; i++) if ((mask & (1 << i)) != 0) readyAt[i] = SessionNow + config.internalCooldown;
            return mask;
        }

        public void After(MonsterVitality target, DamageInfo original, int mask)
        {
            for (int i = 0; i < 5; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                var type = (ReactionType)i; var rule = config.Rule(type);
                int affected = 0;
                Vector3 center = target.transform.position;
                int count = 0;
                // Snapshot before hits: lethal hits can remove pooled enemies from Active.
                for (int n = 0; n < MonsterVitality.Active.Count && count < nearby.Length; n++)
                {
                    var victim = MonsterVitality.Active[n];
                    if (victim == null || victim.Defeated || !victim.isActiveAndEnabled) continue;
                    Vector3 delta = victim.transform.position - center;
                    if (victim != target && (rule.radius <= 0 || Mathf.Abs(delta.y) > 3*Scale || Vector3.ProjectOnPlane(delta, Vector3.up).sqrMagnitude > rule.radius * rule.radius * Scale * Scale)) continue;
                    if (victim != target && !CombatLine.Clear(center + Vector3.up*Scale, victim.transform.position + Vector3.up*Scale, target.transform)) continue;
                    nearby[count++] = victim;
                }
                for (int n = 0; n < count; n++)
                {
                    var victim = nearby[n]; nearby[n] = null;
                    if (victim == null || !victim.isActiveAndEnabled || victim.Defeated) continue;
                    if (type == ReactionType.ElectricFlow)
                    {
                        victim.GetComponent<StatusEffectHost>()?.Apply(StatusType.Shock, rule.statusDuration, 0, original.attacker); affected++; continue;
                    }
                    float amount = 0;
                    if (type == ReactionType.IceLightning)
                        amount = original.amount * rule.bonus / ElementChart.Multiplier(original.element, target.Element) * ElementChart.Multiplier(original.element, victim.Element);
                    else if (type == ReactionType.FireExplosion)
                    {
                        float attack = original.attackPower;
                        if (attack <= 0 && original.attacker != null) { var stats = original.attacker.GetComponent<PlayerStats>(); attack = stats != null ? stats.Attack * stats.DamageDealt : 0; }
                        amount = attack * rule.bonus * ElementChart.Multiplier(Element.Hoa, victim.Element);
                    }
                    else if (type == ReactionType.Convergence && victim == target) amount = original.amount * rule.bonus;
                    if (amount <= 0) continue;
                    var hit = DamageInfo.Create(amount, original.element, DamageSource.Reaction, victim.transform.position + Vector3.up*Scale, original.direction, original.attacker);
                    hit.skillId = rule.id; hit.attackPower = original.attackPower;
                    if (victim.ApplyDamage(hit)) affected++;
                }
                TriggerCount++;
                ReactionTriggered?.Invoke(type, target);
                Feedback?.Invoke(new ReactionEvent { type = type, target = target, attacker = original.attacker, point = center, affected = affected });
            }
        }
        // Every area caster marks its hit; ordinary projectiles and chain bounces remain single-target.
        public static bool AreaSkill(string id)
        {
            switch (id)
            {
                case "hang-long-thap-bat-chuong":case "tam-muoi-chan-hoa":case "thien-loi-dan":case "tru-tien-kiem-tran":case "banh-truong-lanh-dia":case "vo-hon-chan-than":
                case "dai-thu-an": case "anh-phan-than": case "phat-no-hoa-lien": case "han-bang-phong-an": case "hac-dong-than-la": case "van-kiem-quyet": case "tich-lich-nhat-thiem": return true;
                default: return false;
            }
        }
    }
}
