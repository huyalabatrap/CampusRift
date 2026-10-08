using CampusRift.Controls;
using CampusRift.Monsters;
using CampusRift.UI;
using UnityEngine;
using UnityEngine.Audio;

namespace CampusRift.Skills
{
    [DefaultExecutionOrder(170), DisallowMultipleComponent, RequireComponent(typeof(CampusExplorer))]
    public sealed class PhantomDecoySkill : MonoBehaviour
    {
        public Material ghostMaterial;
        public Material previewMaterial;
        public Material shardMaterial;
        public Mesh shardMesh;
        public AudioClip spawnSound, expireSound;
        public AudioMixerGroup audioOutput;
        [Range(2, 12)] public float duration = 5.5f;
        [Range(2, 30)] public float cooldown = 18f;
        [Range(2, 12)] public float runSpeed = 7.4f;
        public bool IsPreviewing { get; private set; }
        public float CooldownRemaining => CampusRift.Progression.DevMode.NoCooldown ? 0 : Mathf.Max(0, readyAt - Time.time);
        public void AdvanceCooldown(float seconds){readyAt=Mathf.Max(Time.time,readyAt-Mathf.Max(0,seconds));}
        public void ReadyOnRestEquip(){CancelPreview();if(decoy!=null)decoy.Dissolve(false);if(second!=null)second.Dissolve(false);readyAt=0;Changed?.Invoke();}
        public bool Ready => CooldownRemaining <= 0 && (decoy == null || !decoy.Live) && (second==null||!second.Live);
        public int LiveDecoys=>(decoy!=null&&decoy.Live?1:0)+(second!=null&&second.Live?1:0);
        public PhantomDecoy Decoy => decoy;
        public string Feedback { get; private set; } = "";
        public event System.Action Changed;
        CampusExplorer explorer;
        CampusInput input;
        PlayerMonsterHealth health;
        VoidWallSkill wall;
        GiantHandSkill hand;
        PhantomDecoy decoy;
        PhantomDecoy second;
        LineRenderer preview;
        float readyAt;
        PhantomRuntime runtime;
        public float EffectiveCooldown=>cooldown*(runtime!=null?runtime.CooldownMultiplier:1);
        public float EffectiveDuration=>duration*(runtime!=null?runtime.EffectMultiplier:1);
        public int LastDissolveHits {get;private set;}
        readonly MonsterVitality[] dissolveVictims=new MonsterVitality[64];
        readonly System.Random rng=new System.Random(10009);
        public bool IsUnlocked => Learning.LearningSkillGate.Allows(this);
        bool Allowed => IsUnlocked && health != null && health.CurrentHealth > 0 && input != null && input.Allowed;

        void Awake()
        {
            explorer = GetComponent<CampusExplorer>(); input = GetComponent<CampusInput>();
            runtime=GetComponent<PhantomRuntime>();
            health = GetComponent<PlayerMonsterHealth>(); wall = GetComponent<VoidWallSkill>(); hand = GetComponent<GiantHandSkill>();
            if (explorer.characterAnimator == null || ghostMaterial == null) { enabled = false; return; }
            var actor = new GameObject("Phantom Decoy (pooled)");
            decoy = actor.AddComponent<PhantomDecoy>();
            decoy.Initialize(explorer.characterAnimator, ghostMaterial, shardMaterial, shardMesh,
                spawnSound, expireSound, audioOutput, health);
            decoy.Dissolved+=DissolveExplosion;
            second=new GameObject("Phantom second decoy (pooled)").AddComponent<PhantomDecoy>();second.Initialize(explorer.characterAnimator,ghostMaterial,shardMaterial,shardMesh,spawnSound,expireSound,audioOutput,health);second.Dissolved+=DissolveExplosion;
            var guide = new GameObject("Phantom aim guide");
            preview = guide.AddComponent<LineRenderer>();
            preview.useWorldSpace = true; preview.positionCount = 2; preview.startWidth = .045f; preview.endWidth = .005f;
            preview.numCapVertices = 4; preview.sharedMaterial = previewMaterial;
            preview.startColor = new Color(.25f,.85f,1,.65f); preview.endColor = new Color(.7f,.26f,1,0);
            guide.SetActive(false);
        }

        void Update()
        {
            if (!Allowed) { CancelPreview(); if (health != null && health.CurrentHealth <= 0 && decoy != null) decoy.Dissolve(false); return; }
            if (input.Pressed(CampusAction.Phantom))
            { if (IsPreviewing) Confirm(); else BeginPreview(); }
            else if (IsPreviewing && input.Pressed(CampusAction.Confirm)) Confirm();
            if (IsPreviewing && input.Pressed(CampusAction.Cancel)) CancelPreview();
            if (IsPreviewing)
            {
                Vector3 direction = AimDirection;
                preview.SetPosition(0, transform.position + Vector3.up * .12f);
                preview.SetPosition(1, transform.position + direction * 9f + Vector3.up * .12f);
            }
        }

        Vector3 AimDirection => input != null ? input.PhantomDirection(explorer.followCamera) : transform.forward;
        public bool BeginPreview()
        {
            if (!Allowed || !Ready) { Feedback = "PHANTOM RECHARGING"; Changed?.Invoke(); return false; }
            wall?.CancelPreview(); hand?.CancelPreview();
            IsPreviewing = true; preview.gameObject.SetActive(true); Changed?.Invoke(); return true;
        }
        public void CancelPreview()
        {
            if (!IsPreviewing) return;
            IsPreviewing = false; if (preview != null) preview.gameObject.SetActive(false); Changed?.Invoke();
        }
        public bool Confirm() => IsPreviewing && Cast(AimDirection);
        public bool Cast(Vector3 direction)
        {
            if (!Allowed || !Ready || decoy == null) return false;
            if (!SkillSpirit.CanPay(this, "anh-phan-than")) { CancelPreview(); Feedback = "NOT ENOUGH SPIRIT"; Changed?.Invoke(); return false; }
            if (!decoy.Launch(transform.position, direction, runSpeed, EffectiveDuration))
            { Feedback = "NO CLEAR ROUTE FOR PHANTOM"; Changed?.Invoke(); return false; }
            SkillSpirit.TryPay(this, "anh-phan-than");
            if(runtime!=null&&runtime.Mastered){Vector3 side=Vector3.Cross(Vector3.up,direction.normalized);second.Launch(transform.position+side*.9f,Quaternion.Euler(0,20,0)*direction,runSpeed,EffectiveDuration);}
            readyAt = Time.time + EffectiveCooldown;
            CancelPreview();
            Feedback = "PHANTOM RELEASED"; Changed?.Invoke(); return true;
        }
        void OnDisable() { CancelPreview(); if (decoy != null) decoy.Dissolve(false);if(second!=null)second.Dissolve(false); }
        void DissolveExplosion(Vector3 center)
        {
            LastDissolveHits=0;if(!isActiveAndEnabled||health==null||health.IsDead||runtime==null||runtime.rank<3)return;
            int count=0;for(int i=0;i<MonsterVitality.Active.Count&&count<dissolveVictims.Length;i++)
            {var m=MonsterVitality.Active[i];if(m==null||m.Defeated||Vector3.ProjectOnPlane(m.transform.position-center,Vector3.up).sqrMagnitude>9||Mathf.Abs(m.transform.position.y-center.y)>3||!Combat.CombatLine.Clear(center+Vector3.up,m.transform.position+Vector3.up,transform))continue;dissolveVictims[count++]=m;}
            var stats=GetComponent<Combat.PlayerStats>();var impact=GetComponent<SkillImpact>();
            for(int i=0;i<count;i++){var m=dissolveVictims[i];dissolveVictims[i]=null;var hit=Combat.DamageCalculator.Compute(stats!=null?stats.Attack*stats.DamageDealt:20,1.5f*runtime.EffectMultiplier*runtime.CastDamageMultiplier,Combat.Element.Am,m,stats!=null?stats.EffectiveCritChance:0,stats!=null?stats.CritDamage:1.5f,rng,Combat.DamageSource.Skill);hit.attacker=gameObject;hit.skillId="anh-phan-than";hit.isArea=true;if(m.ApplyDamage(hit)){LastDissolveHits++;impact?.HoldVictim(m,.065f);}}
            var pool=GetComponent<SkillVfxPool>();if(pool!=null){pool.Burst(center+Vector3.up,ComicTheme.Purple,1.6f,pool.config.voidHit);pool.Spawn(SkillVfxKind.Scorch,center,ComicTheme.Purple,2,3);}if(LastDissolveHits>0)impact?.Pulse(.45f,.065f);
        }
        void OnDestroy()
        {
            if (decoy != null) Destroy(decoy.gameObject);
            if(second!=null)Destroy(second.gameObject);
            if (preview != null) Destroy(preview.gameObject);
        }
    }
}
