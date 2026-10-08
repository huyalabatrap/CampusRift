using System.Collections.Generic;
using UnityEngine;
using CampusRift.Controls;
using CampusRift.Monsters;

namespace CampusRift.Combat
{
    // Lock-on (P03-T05). Tab / the mobile lock button picks the monster nearest the screen centre and cycles
    // through the rest; holding it releases the lock. A lock that dies hands over to the closest valid monster.
    [DisallowMultipleComponent, DefaultExecutionOrder(-30)]
    public sealed class TargetLock : MonoBehaviour
    {
        [Min(1)] public float range = 25f;
        [Min(0.1f)] public float releaseHold = 0.5f;
        // Additive glow material (assigned by setup); a runtime lookup would be stripped from player builds.
        public Material reticleMaterial;
        public MonsterVitality Current { get; private set; }
        public event System.Action<MonsterVitality> Changed;
        CampusExplorer explorer; CampusInput input; Combat.PlayerStats stats;
        readonly List<MonsterVitality> candidates = new List<MonsterVitality>(16);
        float holdStart = -1, blockedSince = -1;
        bool released;
        SpriteRenderer reticle; Sprite ring;

        void Awake() { explorer = GetComponent<CampusExplorer>(); input = GetComponent<CampusInput>(); }

        public Vector3 AimForward
        {
            get
            {
                var cam = explorer != null ? explorer.followCamera : null;
                Vector3 f = Vector3.ProjectOnPlane(cam != null ? cam.transform.forward : transform.forward, Vector3.up);
                return f.sqrMagnitude > 0.001f ? f.normalized : transform.forward;
            }
        }

        public bool IsValid(MonsterVitality m, bool needLineOfSight = true)
        {
            if (m == null || m.Defeated || !m.isActiveAndEnabled) return false;
            if ((m.transform.position - transform.position).sqrMagnitude > range * range) return false;
            return !needLineOfSight || CombatLine.Clear(transform.position + Vector3.up * 1.2f, CombatLine.Chest(m), transform);
        }

        // Candidates in front of the camera, sorted left → right by angle.
        List<MonsterVitality> Gather()
        {
            candidates.Clear();
            Vector3 origin = transform.position, forward = AimForward;
            foreach (var m in MonsterVitality.Active)
            {
                if (!IsValid(m)) continue;
                Vector3 flat = Vector3.ProjectOnPlane(m.transform.position - origin, Vector3.up);
                if (flat.sqrMagnitude > 0.01f && Vector3.Angle(forward, flat) > 100f) continue;
                candidates.Add(m);
            }
            candidates.Sort((a, b) => SignedAngle(a).CompareTo(SignedAngle(b)));
            return candidates;
        }
        float SignedAngle(MonsterVitality m) => Vector3.SignedAngle(AimForward, Vector3.ProjectOnPlane(m.transform.position - transform.position, Vector3.up), Vector3.up);

        MonsterVitality NearestCentre(List<MonsterVitality> list)
        {
            MonsterVitality best = null; float score = float.MaxValue;
            foreach (var m in list)
            {
                float s = Mathf.Abs(SignedAngle(m)) + (m.transform.position - transform.position).magnitude * 0.6f;
                if (s < score) { score = s; best = m; }
            }
            return best;
        }

        public void Set(MonsterVitality target)
        {
            if (Current == target) return;
            Current = target;
            var bars = EnemyHealthBars.Instance; if (bars != null) bars.SetLocked(target);
            Changed?.Invoke(target);
        }

        // Tab: lock the centre-most monster, or step to the next one to the right (wrapping).
        public void CycleOrLock()
        {
            var list = Gather();
            if (list.Count == 0) { Set(null); return; }
            if (Current == null || !list.Contains(Current)) { Set(NearestCentre(list)); return; }
            int index = list.IndexOf(Current);
            Set(list[(index + 1) % list.Count]);
        }

        public void Release() { released = true; Set(null); }

        void Update()
        {
            if (input == null || !input.Allowed) { holdStart = -1; return; }
            if (input.Pressed(CampusAction.LockOn)) { holdStart = Time.unscaledTime; released = false; CycleOrLock(); }
            if (holdStart >= 0)
            {
                if (!input.IsHeld(CampusAction.LockOn)) holdStart = -1;
                else if (!released && Time.unscaledTime - holdStart >= releaseHold) Release();
            }
            Validate();
            UpdateReticle();
        }

        void Validate()
        {
            if (Current == null) return;
            if (Current.Defeated || !Current.isActiveAndEnabled)
            {
                // Hand over to the nearest valid monster, unless the player released the lock.
                var list = Gather();
                Set(list.Count > 0 ? NearestCentre(list) : null); return;
            }
            if ((Current.transform.position - transform.position).sqrMagnitude > range * range) { Set(null); return; }
            // Lose the lock after two seconds behind walls.
            bool visible = CombatLine.Clear(transform.position + Vector3.up * 1.2f, CombatLine.Chest(Current), transform);
            if (visible) blockedSince = -1;
            else { if (blockedSince < 0) blockedSince = Time.time; if (Time.time - blockedSince > 2f) { blockedSince = -1; Set(null); } }
        }

        void UpdateReticle()
        {
            if (Current == null) { if (reticle != null && reticle.gameObject.activeSelf) reticle.gameObject.SetActive(false); return; }
            if (reticle == null) BuildReticle();
            reticle.gameObject.SetActive(true);
            var cam = Camera.main;
            reticle.transform.position = CombatLine.Chest(Current);
            if (cam != null)
            {
                reticle.transform.rotation = cam.transform.rotation;
                float distance = Vector3.Distance(cam.transform.position, reticle.transform.position);
                reticle.transform.localScale = Vector3.one * Mathf.Clamp(distance * 0.055f, 0.5f, 3f) * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f));
            }
            reticle.color = ElementChart.ColorOf(Current.Element == Element.None ? Element.Kim : Current.Element);
        }

        void BuildReticle()
        {
            const int size = 64; var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Lock Ring" };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f + .5f, dy = y - size / 2f + .5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 26f) / 2.2f);
                    // Four gaps make it read as brackets rather than a plain circle.
                    float angle = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, 90f);
                    float gap = angle > 35f && angle < 55f ? 0f : 1f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, ring * gap));
                }
            tex.Apply(); ring = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size / 1.2f);
            var go = new GameObject("Lock-on Reticle"); reticle = go.AddComponent<SpriteRenderer>();
            reticle.sprite = ring; reticle.sortingOrder = 50;
            if (reticleMaterial != null) reticle.sharedMaterial = reticleMaterial;
        }

        void OnDisable() { if (reticle != null) reticle.gameObject.SetActive(false); }
        void OnDestroy() { if (reticle != null) Destroy(reticle.gameObject); }
    }
}
