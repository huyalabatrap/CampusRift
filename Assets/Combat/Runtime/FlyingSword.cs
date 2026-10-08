using System.Collections.Generic;
using UnityEngine;
using CampusRift.Monsters;

namespace CampusRift.Combat
{
    // One of the jade flying swords. It idles behind the player's shoulders, darts to a target on a swing,
    // then flies home. The piercing variant runs straight and cuts every monster on the way.
    [DisallowMultipleComponent]
    public sealed class FlyingSword : MonoBehaviour
    {
        public enum Mode { Orbit, Flying, Returning, Piercing, Rain }
        public Mode State { get; private set; } = Mode.Orbit;
        public bool Available => State == Mode.Orbit;
        public int Slot { get; private set; }
        PlayerCombat owner; NguKiemConfig config;
        TrailRenderer trail; Transform visual;
        Vector3 orbitVelocity;
        // Flight
        MonsterVitality target; Vector3 goal; float percent; bool heavy; float giveUpAt;
        // Pierce
        Vector3 pierceDirection; float travelled;
        readonly HashSet<MonsterVitality> pierced = new HashSet<MonsterVitality>();
        CampusRift.Skills.SwordRainRuntime rainOwner;
        Vector3 rainStart,rainPoint;
        float rainAge;
        bool rainImpacted;
        MeshRenderer rainRenderer;
        MeshRenderer rainInk;
        TrailRenderer rainInkTrail;
        MaterialPropertyBlock rainBlock;
        void Awake(){rainBlock=new MaterialPropertyBlock();}
        static readonly Collider[] overlaps = new Collider[32];

        public void Initialize(PlayerCombat combat, int slot, NguKiemConfig cfg)
        {
            owner = combat; Slot = slot; config = cfg; name = "Ngu Kiem Sword " + (slot + 1);
            visual = new GameObject("Blade").transform; visual.SetParent(transform, false);
            var filter = visual.gameObject.AddComponent<MeshFilter>(); filter.sharedMesh = BladeMesh(cfg);
            var renderer = visual.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = cfg.bladeMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            visual.localScale = Vector3.one * 0.5f;
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = cfg.trailMaterial; trail.time = 0.2f; trail.widthMultiplier = 0.16f; trail.minVertexDistance = 0.08f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trail.receiveShadows = false;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(cfg.tipColor, 0), new GradientColorKey(cfg.bladeColor, 1) },
                             new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(0, 1) });
            trail.colorGradient = gradient; trail.emitting = false;
            if(owner!=null)transform.position = owner.SlotPoint(slot);
        }

        public static Mesh BladeMesh(NguKiemConfig cfg)
        {
            // Flat double-sided blade in the XZ plane, tip on +Z; vertex colour fades from jade to a bright tip.
            var m = new Mesh { name = "Ngu Kiem Blade" };
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Color ca, Color cb, Color ccc, Color cd)
            { int i = v.Count; v.AddRange(new[] { a, b, cc, d }); c.AddRange(new[] { ca, cb, ccc, cd }); t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 }); }
            Color root = cfg.bladeColor, tip = cfg.tipColor;
            // Blade: two quads meeting at the shoulder, then a tapered point.
            Quad(new Vector3(-.045f, 0, -.15f), new Vector3(.045f, 0, -.15f), new Vector3(.07f, 0, .35f), new Vector3(-.07f, 0, .35f), root, root, root, root);
            int p = v.Count; v.AddRange(new[] { new Vector3(-.07f, 0, .35f), new Vector3(.07f, 0, .35f), new Vector3(0, 0, .95f) });
            c.AddRange(new[] { root, root, tip }); t.AddRange(new[] { p, p + 2, p + 1 });
            // Guard and grip.
            Quad(new Vector3(-.11f, 0, -.2f), new Vector3(.11f, 0, -.2f), new Vector3(.11f, 0, -.15f), new Vector3(-.11f, 0, -.15f), tip, tip, tip, tip);
            Quad(new Vector3(-.02f, 0, -.5f), new Vector3(.02f, 0, -.5f), new Vector3(.02f, 0, -.2f), new Vector3(-.02f, 0, -.2f), root * .6f, root * .6f, root * .6f, root * .6f);
            m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0);
            var uv = new Vector2[v.Count]; m.uv = uv;
            m.RecalculateNormals();m.RecalculateBounds(); return m;
        }

        public void LaunchAt(MonsterVitality victim, float damagePercent, bool heavyHit)
        {
            target = victim; percent = damagePercent; heavy = heavyHit; goal = CombatLine.Chest(victim);
            Begin(Mode.Flying, 1.3f);
        }

        // A dedicated warm pool uses the same blade, trail and projectile component as P03.
        public void PrepareRain(PlayerCombat combat,int slot,NguKiemConfig cfg)
        {
            Initialize(combat,slot,cfg);rainRenderer=visual.GetComponent<MeshRenderer>();
            // Warm a broad readable blade/guard; passive P03 swords keep their mesh.
            var rainMesh=visual.GetComponent<MeshFilter>().sharedMesh;var rainVertices=rainMesh.vertices;
            for(int i=0;i<rainVertices.Length;i++)rainVertices[i].x*=rainVertices[i].z<-.14f?2.2f:1.8f;
            rainMesh.vertices=rainVertices;rainMesh.RecalculateBounds();
            var poolConfig=Resources.Load<CampusRift.Skills.SkillSet1VfxConfig>("SkillSet1Vfx");
            var hull=new GameObject("Rain sword ink");hull.transform.SetParent(visual,false);hull.AddComponent<MeshFilter>().sharedMesh=visual.GetComponent<MeshFilter>().sharedMesh;rainInk=hull.AddComponent<MeshRenderer>();rainInk.sharedMaterial=poolConfig.ink;rainInk.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var tail=new GameObject("Rain dark trail");tail.transform.SetParent(transform,false);rainInkTrail=tail.AddComponent<TrailRenderer>();rainInkTrail.sharedMaterial=poolConfig.stroke;rainInkTrail.time=.2f;rainInkTrail.widthMultiplier=.3f;rainInkTrail.startColor=rainInkTrail.endColor=new Color(.12f,.055f,.012f);rainInkTrail.emitting=false;
            trail.sharedMaterial=poolConfig.layeredWide;rainInkTrail.enabled=false;gameObject.SetActive(false);
            trail.time=.15f;trail.startColor=new Color(1,.78f,.22f,1);trail.endColor=new Color(1,.78f,.22f,0);trail.widthCurve=AnimationCurve.EaseInOut(0,1,1,0);
        }
        public void LaunchRain(CampusRift.Skills.SwordRainRuntime caster,Vector3 point)
        {
            rainOwner=caster;rainPoint=point;rainStart=point+Vector3.up*((caster.ARContext!=null?3.2f:6.5f)*caster.WorldScale);rainAge=0;rainImpacted=false;State=Mode.Rain;
            gameObject.SetActive(true);transform.position=rainStart;transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);
            visual.localScale=Vector3.one*1.95f;trail.Clear();trail.emitting=true;rainInkTrail.Clear();rainInkTrail.emitting=false;trail.widthMultiplier=(CampusRift.Skills.SkillVfxPool.MobileQuality?.14f:.22f)*caster.WorldScale;
            rainBlock.SetFloat("_Alpha",1);rainBlock.SetFloat("_FlatGlow",1);rainBlock.SetColor("_BaseColor",config.bladeColor);rainBlock.SetColor("_Emission",config.tipColor*2);rainRenderer.SetPropertyBlock(rainBlock);rainBlock.SetFloat("_Width",.03f);rainInk.SetPropertyBlock(rainBlock);
        }
        void UpdateRain(float dt)
        {
            if(rainOwner!=null&&rainOwner.SessionPaused)return;
            if(rainOwner!=null&&rainOwner.ARContext!=null)visual.localPosition=Vector3.zero;
            rainAge+=dt;
            if(!rainImpacted)
            {
                var next=Vector3.Lerp(rainStart,rainPoint,Mathf.Clamp01(rainAge/.24f));var depth=rainOwner!=null&&rainOwner.ARContext!=null?rainOwner.ARContext.battlefield.GetComponent<CampusRift.AR.ARDepthCollision>():null;
                if(depth!=null&&depth.Sweep(transform.position,next,out var contact)){rainPoint=contact;rainAge=.24f;next=contact;}
                transform.position=next;
                if(rainAge>=.24f){rainImpacted=true;trail.emitting=rainInkTrail.emitting=false;if(rainOwner!=null)rainOwner.SwordImpact(rainPoint);}
            }
            else
            {
                float alpha=1-Mathf.Clamp01((rainAge-.74f)/.5f);rainBlock.SetFloat("_Alpha",alpha);rainRenderer.SetPropertyBlock(rainBlock);rainBlock.SetFloat("_Width",.03f);rainInk.SetPropertyBlock(rainBlock);
                visual.localScale=Vector3.one*1.95f;
                if(rainAge>=1.24f){if(rainOwner!=null)rainOwner.SwordDissolve(rainPoint);gameObject.SetActive(false);rainOwner=null;}
            }
        }

        public void LaunchMiss(Vector3 direction)
        {
            target = null; percent = 0; heavy = false;
            goal = transform.position + direction.normalized * config.missDistance;
            Begin(Mode.Flying, 1f);
        }

        public void LaunchPierce(Vector3 origin, Vector3 direction, float damagePercent)
        {
            target = null; percent = damagePercent; heavy = true; pierced.Clear(); travelled = 0;
            pierceDirection = direction.normalized;
            transform.position = origin;
            Begin(Mode.Piercing, 2f);
        }

        void Begin(Mode mode, float timeout)
        {
            State = mode; giveUpAt = Time.time + timeout; orbitVelocity = Vector3.zero;
            trail.Clear(); trail.emitting = true;
        }

        void Update()
        {
            if(State==Mode.Rain){UpdateRain(Time.deltaTime);return;}
            if (owner == null) return;
            float dt = Time.deltaTime;
            switch (State)
            {
                case Mode.Orbit: UpdateOrbit(dt); break;
                case Mode.Flying: UpdateFlying(dt); break;
                case Mode.Returning: UpdateReturning(dt); break;
                case Mode.Piercing: UpdatePiercing(dt); break;
            }
        }

        void UpdateOrbit(float dt)
        {
            Vector3 slot = owner.SlotPoint(Slot);
            transform.position = Vector3.SmoothDamp(transform.position, slot, ref orbitVelocity, 0.08f, 60f, dt);
            Quaternion rest = Quaternion.LookRotation(Vector3.up + owner.transform.right * (Slot - 1) * 0.45f - owner.transform.forward * 0.25f, owner.transform.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, rest, 1f - Mathf.Exp(-12f * dt));
            if (trail.emitting) trail.emitting = false;
        }

        void UpdateFlying(float dt)
        {
            if (target != null && !target.Defeated) goal = CombatLine.Chest(target);
            Vector3 to = goal - transform.position; float step = config.flightSpeed * dt;
            if (to.magnitude <= Mathf.Max(step, config.hitRadius))
            {
                if (target != null && !target.Defeated) owner.ResolveHit(target, percent, heavy);
                ReturnHome(); return;
            }
            Vector3 next = transform.position + to.normalized * step;
            if (CombatLine.Block(transform.position, next, out _)) { ReturnHome(); return; }
            transform.position = next;
            transform.rotation = Quaternion.LookRotation(to.normalized);
            if (Time.time > giveUpAt) ReturnHome();
        }

        void UpdateReturning(float dt)
        {
            Vector3 slot = owner.SlotPoint(Slot); Vector3 to = slot - transform.position;
            float step = config.returnSpeed * dt;
            if (to.magnitude <= Mathf.Max(step, 0.3f) || Time.time > giveUpAt) { State = Mode.Orbit; orbitVelocity = Vector3.zero; return; }
            transform.position += to.normalized * step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), 1f - Mathf.Exp(-16f * dt));
        }

        void UpdatePiercing(float dt)
        {
            float step = Mathf.Min(config.pierceSpeed * dt, config.pierceLength - travelled);
            Vector3 from = transform.position, next = from + pierceDirection * step;
            bool blocked = CombatLine.Block(from, next, out Vector3 wall);
            if (blocked) next = wall - pierceDirection * 0.05f;
            int count = Physics.OverlapCapsuleNonAlloc(from, next, config.pierceRadius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var victim = overlaps[i].GetComponentInParent<MonsterVitality>();
                if (victim == null || victim.Defeated || !pierced.Add(victim)) continue;
                owner.ResolveHit(victim, percent, true);
            }
            travelled += step; transform.position = next;
            transform.rotation = Quaternion.LookRotation(pierceDirection);
            if (blocked || travelled >= config.pierceLength - 0.01f || Time.time > giveUpAt) ReturnHome();
        }

        void ReturnHome() { State = Mode.Returning; giveUpAt = Time.time + 2f; target = null; }

        // Immediate reset (scene start, respawn).
        public void SnapHome() { State = Mode.Orbit; target = null; trail.Clear(); trail.emitting = false; transform.position = owner.SlotPoint(Slot); }
    }
}
