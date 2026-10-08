using UnityEngine;
using CampusRift.Combat;

namespace CampusRift.Monsters
{
    // Health, suppression and hit/status tint for every monster: Shaban and the V2 minions alike.
    [DisallowMultipleComponent]
    public sealed class MonsterVitality : MonoBehaviour, IDamageable
    {
        CampusRift.AR.ARCombatContext arContext;
        float SessionNow => arContext!=null?arContext.Now:Time.time;

        [Min(1)] public float maxHealth = 500;
        [SerializeField] Element element = Element.None;
        [Range(0, 0.8f)] public float defense;
        // Bosses: freeze becomes a slow, stuns are capped (plan §3.2).
        public bool resistHardControl;
        public CombatFaction Faction {get;private set;}
        public void SetFaction(CombatFaction value){Faction=value;if(value==CombatFaction.Ally)Active.Remove(this);else if(isActiveAndEnabled&&!Active.Contains(this))Active.Add(this);}
        public void Heal(float amount){if(!Defeated&&amount>0)Health=Mathf.Min(maxHealth,Health+amount);}
        public event System.Action DefeatedOnce;
        public event System.Action<DamageInfo> Damaged;
        // Shared feed for damage numbers, health bars and stars; cleared on domain reload.
        public static event System.Action<MonsterVitality, DamageInfo> AnyDamaged;
        // Living monsters (enabled ones), for targeting without scene-wide searches.
        public static readonly System.Collections.Generic.List<MonsterVitality> Active = new System.Collections.Generic.List<MonsterVitality>(32);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { AnyDamaged = null; Active.Clear(); }
        void OnEnable() { if (Faction==CombatFaction.Hostile&&!Active.Contains(this)) Active.Add(this); }
        public float Health { get; private set; }
        public int ReceivedHits { get; private set; }
        public bool Defeated => Health <= 0;
        public bool Suppressed => Defeated || SessionNow < releaseAt;
        public float SuppressionRemaining => Mathf.Max(0, releaseAt-SessionNow);
        public void ClearSuppression(){releaseAt=0;}
        public Element Element { get => element; set => element = value; }
        public bool IsDead => Defeated;
        public Transform Anchor => transform;
        IMotionHold hold;
        MonsterCombat combat;
        StatusEffectHost status;
        ReactionResolver reactions;
        // Looked up lazily: spawners may add the host after this component.
        StatusEffectHost Status => status != null ? status : (status = GetComponent<StatusEffectHost>());
        Animator animator;
        Renderer[] renderers;
        bool cameraHidden;
        bool[] cameraOriginalOff;
        MaterialPropertyBlock[] originals;
        MaterialPropertyBlock flash;
        float releaseAt, flashUntil;
        bool comicSkillFlash,softComicFlash;
        bool telegraph;
        bool wasSuppressed, painted, hasSealState;
        Color? tint;
        static readonly int Sealed = Animator.StringToHash("Base Layer.Sealed");
        static readonly int Locomotion = Animator.StringToHash("Base Layer.OriginalAnimation");

        void Awake()
        {
            arContext=GetComponent<CampusRift.AR.ARCombatContext>();
            Health=maxHealth; hold=GetComponent<IMotionHold>(); combat=GetComponent<MonsterCombat>();
            reactions=GetComponent<ReactionResolver>() ?? gameObject.AddComponent<ReactionResolver>();
            animator=GetComponentInChildren<Animator>();
            hasSealState=animator!=null && animator.HasState(0,Sealed);
            renderers=GetComponentsInChildren<Renderer>(); originals=new MaterialPropertyBlock[renderers.Length];
            cameraOriginalOff=new bool[renderers.Length];
            flash=new MaterialPropertyBlock();
            for(int i=0;i<renderers.Length;i++){originals[i]=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(originals[i]);}
        }

        // Level scaling. refill=true for a fresh spawn; false keeps the current health fraction.
        public void SetMaxHealth(float value, bool refill)
        {
            value=Mathf.Max(1,value);
            float fraction=maxHealth>0?Health/maxHealth:1;
            maxHealth=value; Health=refill?maxHealth:Mathf.Clamp01(fraction)*maxHealth;
        }

        // Pool reuse: full health, no suppression, no tint.
        public void ResetVitality()
        {
            Health=maxHealth; ReceivedHits=0; releaseAt=0; wasSuppressed=false; flashUntil=0; tint=null;softComicFlash=false;
            reactions?.ResetLife();
            if(renderers!=null) RestoreBlocks();
        }

        public bool ApplyDamage(DamageInfo info)
        {
            if (Defeated || info.amount <= 0 || !isActiveAndEnabled) return false;
            var arElite=GetComponent<CampusRift.AR.AREliteAffix>();
            if(arElite!=null&&!arElite.Accept(ref info))return false;
            if(Faction==CombatFaction.Ally&&info.attacker!=null&&(info.attacker.GetComponent<CampusRift.Skills.SoulAlly>()!=null||info.attacker.GetComponent<PlayerStats>()!=null))return false;
            var sight=info.attacker!=null?info.attacker.GetComponent<CampusRift.Skills.SpiritSightRuntime>():null;
            if(sight!=null)info.amount*=sight.DamageBonus(this,info.critical);
            if(info.attacker!=null && (info.source==DamageSource.Skill||info.source==DamageSource.Melee||info.source==DamageSource.Projectile))
            {
                float extra=info.attacker.GetComponent<CampusRift.Progression.BuffSystem>()?.ElementBonus(info.element)??0;
                if(info.attacker.GetComponent<PlayerStats>()!=null && ElementChart.Multiplier(info.element,Element)>1)extra+=CampusRift.Progression.ProfileService.Instance?.Artifacts.CounterBonus??0;
                info.amount*=1+extra;
            }
            var original=info;
            int reactionMask=reactions!=null?reactions.Before(this,ref info):0;
            float armor = defense - (Status != null && Status.Has(StatusType.ArmorBreak) ? Status.Magnitude(StatusType.ArmorBreak) : 0f);
            float amount = CampusRift.SkyBeast.FireBreathCycle.IsFireHazard(info)?info.amount:DamageCalculator.AfterDefense(info.amount, Mathf.Max(0, armor));
            amount=CampusRift.Enemies.EliteAffix.Reduce(this,amount);
            amount=Mathf.Min(Health,amount);Health=Mathf.Max(0,Health-amount); ReceivedHits++;softComicFlash=info.skillId=="than-kiem-ngu-loi";comicSkillFlash=info.source==DamageSource.Skill&&ComicSkill(info.skillId);flashUntil=SessionNow+(comicSkillFlash?.065f:.18f);
            info.amount = amount;
            Damaged?.Invoke(info);
            AnyDamaged?.Invoke(this, info);
            if(Defeated)DefeatedOnce?.Invoke();
            reactions?.After(this,original,reactionMask);
            return true;
        }
        static bool ComicSkill(string id){switch(id){case "tich-lich-nhat-thiem":case "phat-no-hoa-lien":case "han-bang-phong-an":case "than-kiem-ngu-loi":case "kim-chung-trao":case "hac-dong-than-la":case "van-kiem-quyet":case "hang-long-thap-bat-chuong":case "tam-muoi-chan-hoa":case "bac-minh-than-cong":case "con-bang-cuc-toc":case "thien-loi-dan":case "tru-tien-kiem-tran":case "vo-hon-chan-than":return true;default:return false;}}

        // Giant Hand path: raw damage (element already resolved by the caster) plus a stagger.
        public bool ReceiveSeal(float damage, float stagger)
        {
            if (Defeated || damage <= 0 || !isActiveAndEnabled) return false;
            if (!ApplyDamage(DamageInfo.Create(damage, Element.Tho, DamageSource.Skill, transform.position + Vector3.up, Vector3.down))) return false;
            Suppress(stagger);
            Status?.Apply(StatusType.Stun,stagger);
            if(hasSealState) animator.CrossFadeInFixedTime(Sealed,0.045f,0,0);
            return true;
        }

        // V2 path: fully resolved damage (attack × percent × element × crit) plus the stagger.
        public bool ReceiveSeal(DamageInfo info, float stagger)
        {
            if (Defeated || !isActiveAndEnabled || !ApplyDamage(info)) return false;
            Suppress(stagger);
            Status?.Apply(StatusType.Stun,stagger,0,info.attacker);
            if(hasSealState) animator.CrossFadeInFixedTime(Sealed,0.045f,0,0);
            return true;
        }
        // The summon binds without dealing damage; impact starts the full stun duration.
        public void Suppress(float duration)
        {
            if(!isActiveAndEnabled || duration<=0)return;
            if(resistHardControl) duration=Mathf.Min(duration,1f);
            releaseAt=Mathf.Max(releaseAt,SessionNow+duration);
            combat?.Interrupt(); wasSuppressed=true;
            HoldPosition();
        }

        // Status tint (burn, freeze, …); null restores the authored materials. Hit flash takes priority.
        public void SetTint(Color? value) { tint = value; }
        // Red pulse shown while the monster winds up an attack (P03-T06).
        public void SetTelegraph(bool on) { telegraph = on; }

        // A dash can take the third-person camera through an enemy. Hide only the intersecting
        // model for that brief crossing; world colliders still resolve camera distance normally.
        public void AvoidCameraClipping(Vector3 cameraPosition)
        {
            bool inside=false;for(int i=0;i<renderers.Length;i++)if(renderers[i] is SkinnedMeshRenderer){var bounds=renderers[i].bounds;bounds.Expand(.2f);if(bounds.Contains(cameraPosition)){inside=true;break;}}
            if(inside==cameraHidden)return;
            for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null){if(inside){cameraOriginalOff[i]=renderers[i].forceRenderingOff;renderers[i].forceRenderingOff=true;}else renderers[i].forceRenderingOff=cameraOriginalOff[i];}
            cameraHidden=inside;
        }

        void Update()
        {
            if(arContext!=null&&arContext.Paused)return;
            if(Suppressed) HoldPosition();
            if(wasSuppressed && !Suppressed)
            {
                wasSuppressed=false;
                if(hasSealState) animator.CrossFadeInFixedTime(Locomotion,0.18f);
            }
            if(SessionNow<flashUntil && !(UI.SettingsManager.Instance?.Current.ReduceSkillFlashes??false))
            {
                float t=(flashUntil-SessionNow)/0.18f;
                if(comicSkillFlash)Paint(softComicFlash?new Color(.78f,.69f,.86f):new Color(.025f,.01f,.04f),softComicFlash?new Color(.78f,.69f,.86f):new Color(.04f,.015f,.06f));else Paint(Color.Lerp(new Color(0.4f,0.1f,0.8f),new Color(3,1.9f,0.4f),t),new Color(1,0.7f,0.25f));
            }
            else if(telegraph){float pulse=(UI.SettingsManager.Instance?.Current.ReduceSkillFlashes??false)?.5f:.5f+.5f*Mathf.Sin(SessionNow*22f);var red=Color.Lerp(Color.white,new Color(1.25f,.78f,.72f),pulse);Paint(red,red);}
            else if(tint.HasValue)
            {
                // The Shock status outlives the 65 ms impact. Keep the lightning victim's
                // atlas/face readable during that status instead of prolonging a black silhouette.
                var visibleTint=softComicFlash&&Status!=null&&Status.Has(StatusType.Shock)?new Color(.92f,.84f,1.05f):tint.Value;
                Paint(visibleTint,visibleTint);
            }
            else if(painted) RestoreBlocks();
        }
        void Paint(Color baseColor, Color color)
        {
            flash.SetColor("_BaseColor",baseColor); flash.SetColor("_Color",color);
            foreach(var r in renderers) if(r!=null) r.SetPropertyBlock(flash);
            painted=true;
        }
        void RestoreBlocks()
        {for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].SetPropertyBlock(originals[i]);painted=false;}
        void HoldPosition()
        {
            hold?.HoldForSeal();
        }
        void OnDisable(){Active.Remove(this);telegraph=false;if(renderers!=null){if(cameraHidden){for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].forceRenderingOff=cameraOriginalOff[i];cameraHidden=false;}RestoreBlocks();}}
    }
}
