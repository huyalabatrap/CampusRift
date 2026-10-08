using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CampusRift
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CampusExplorer))]
    public sealed class CharacterAfterimageTrail : MonoBehaviour
    {
        [Header("Pose sources")]
        public SkinnedMeshRenderer[] sources;
        public Material ghostMaterial;
        [Header("Trail")]
        [Range(2, 12)] public int capacity = 6;
        [Min(0.025f)] public float sampleInterval = 0.065f;
        [Min(0.05f)] public float lifetime = 0.36f;
        [Min(0.05f)] public float minimumSpacing = 0.24f;
        [Range(0, 1)] public float opacity = 0.48f;
        [ColorUsage(false, true)] public Color freshColor = new Color(0.10f, 1.4f, 2f, 1f);
        [ColorUsage(false, true)] public Color fadedColor = new Color(0.55f, 0.2f, 1.2f, 1f);
        [Min(1f)] public float teleportDistance = 4f;

        public int ActiveGhostCount { get; private set; }
        public int TotalCaptured { get; private set; }
        public int PoolCapacity => slots == null ? 0 : slots.Length;

        sealed class Part
        {
            public Mesh mesh;
            public MeshRenderer renderer;
            public Transform transform;
        }

        sealed class Ghost
        {
            public GameObject root;
            public Part[] parts;
            public bool active;
            public float age;
            public float duration;
        }

        static readonly int GhostColor = Shader.PropertyToID("_GhostColor");
        static readonly int GhostAlpha = Shader.PropertyToID("_GhostAlpha");
        CampusExplorer explorer;
        SkinnedMeshRenderer[] renderers;
        Ghost[] slots;
        GameObject poolRoot;
        readonly List<Material> ownedMaterials = new List<Material>();
        MaterialPropertyBlock properties;
        Vector3 previousPosition;
        Vector3 lastSamplePosition;
        bool hasPreviousPosition;
        bool hasSample;
        float untilSample;
        float dashRemaining;
        int nextSlot;

        void Awake() => explorer = GetComponent<CampusExplorer>();

        // LateUpdate runs after the Animator has evaluated this frame's original running pose.
        void LateUpdate() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (dt <= 0 || !EnsurePool()) return;
            Vector3 position = transform.position;
            Vector3 displacement = hasPreviousPosition ? position - previousPosition : Vector3.zero;
            if (hasPreviousPosition && displacement.sqrMagnitude > teleportDistance * teleportDistance)
            {
                ClearTrail();
                previousPosition = position;
                return;
            }
            previousPosition = position;
            hasPreviousPosition = true;

            foreach (var ghost in slots)
            {
                if (!ghost.active) continue;
                ghost.age += dt;
                if (ghost.age >= ghost.duration)
                {
                    ghost.active = false;
                    ghost.root.SetActive(false);
                    ActiveGhostCount--;
                }
                else UpdateAppearance(ghost);
            }

            displacement.y = 0;
            bool wantsTrail = (explorer != null && explorer.IsSprinting) || dashRemaining > 0;
            dashRemaining = Mathf.Max(0, dashRemaining - dt);
            untilSample -= dt;
            if (!wantsTrail || displacement.sqrMagnitude < 0.000001f || !HasVisibleSource())
            {
                untilSample = 0;
                hasSample = false;
                return;
            }
            if (untilSample > 0 || (hasSample && (position - lastSamplePosition).sqrMagnitude < minimumSpacing * minimumSpacing)) return;
            CapturePose(position);
            untilSample = Mathf.Max(0.025f, sampleInterval);
            lastSamplePosition = position;
            hasSample = true;
        }

        /// <summary>Call at the start of a future dash. This triggers VFX only; it does not move the character.</summary>
        public void TriggerDash(float duration = 0.25f)
        {
            dashRemaining = Mathf.Max(dashRemaining, Mathf.Clamp(duration, 0, 5f));
            untilSample = 0;
        }

        public void ClearTrail()
        {
            if (slots != null)
                foreach (var ghost in slots) { ghost.active = false; ghost.root.SetActive(false); }
            ActiveGhostCount = 0;
            dashRemaining = 0;
            untilSample = 0;
            hasSample = false;
            hasPreviousPosition = false;
        }

        bool HasVisibleSource()
        {
            foreach (var source in renderers)
                if (source != null && source.enabled && source.gameObject.activeInHierarchy && !source.forceRenderingOff) return true;
            return false;
        }

        bool EnsurePool()
        {
            if (slots != null) return true;
            if (ghostMaterial == null) return false;
            if (explorer == null) explorer = GetComponent<CampusExplorer>();
            var valid = new List<SkinnedMeshRenderer>();
            var candidates = sources != null && sources.Length > 0 ? sources : GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var source in candidates)
                if (source != null && source.sharedMesh != null) valid.Add(source);
            if (valid.Count == 0) return false;
            renderers = valid.ToArray();
            properties = new MaterialPropertyBlock();

            var materials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                var originals = renderers[i].sharedMaterials;
                materials[i] = new Material[originals.Length];
                for (int m = 0; m < originals.Length; m++)
                {
                    var original = originals[m];
                    var material = new Material(ghostMaterial) { name = "Afterimage " + renderers[i].name, hideFlags = HideFlags.DontSave };
                    if (original != null)
                    {
                        if (original.HasProperty("_BaseMap"))
                        {
                            material.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
                            material.SetTextureScale("_BaseMap", original.GetTextureScale("_BaseMap"));
                            material.SetTextureOffset("_BaseMap", original.GetTextureOffset("_BaseMap"));
                        }
                        bool cutout = original.HasProperty("_AlphaClip") && original.GetFloat("_AlphaClip") > 0.5f;
                        material.SetFloat("_Cutoff", cutout && original.HasProperty("_Cutoff") ? original.GetFloat("_Cutoff") : 0f);
                        if (original.HasProperty("_Cull")) material.SetFloat("_Cull", original.GetFloat("_Cull"));
                    }
                    ownedMaterials.Add(material);
                    materials[i][m] = material;
                }
            }

            poolRoot = new GameObject(name + " Afterimage Pool") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(poolRoot, gameObject.scene);
            slots = new Ghost[Mathf.Clamp(capacity, 2, 12)];
            for (int slot = 0; slot < slots.Length; slot++)
            {
                var ghost = new Ghost { root = new GameObject("Frozen Pose " + slot), parts = new Part[renderers.Length] };
                ghost.root.transform.SetParent(poolRoot.transform, false);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var go = new GameObject(renderers[i].name + " Ghost");
                    go.layer = renderers[i].gameObject.layer;
                    go.transform.SetParent(ghost.root.transform, false);
                    var mesh = new Mesh { name = "Afterimage pose " + slot + " part " + i, hideFlags = HideFlags.DontSave };
                    mesh.MarkDynamic();
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = materials[i];
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                    ghost.parts[i] = new Part { mesh = mesh, renderer = renderer, transform = go.transform };
                }
                ghost.root.SetActive(false);
                slots[slot] = ghost;
            }
            return true;
        }

        void CapturePose(Vector3 position)
        {
            var ghost = slots[nextSlot];
            nextSlot = (nextSlot + 1) % slots.Length;
            if (!ghost.active) ActiveGhostCount++;
            ghost.root.transform.position = position;
            ghost.age = 0;
            ghost.duration = Mathf.Max(0.05f, lifetime);
            ghost.active = true;
            for (int i = 0; i < renderers.Length; i++)
            {
                var source = renderers[i];
                var part = ghost.parts[i];
                bool visible = source != null && source.enabled && source.gameObject.activeInHierarchy && !source.forceRenderingOff;
                part.renderer.enabled = visible;
                if (!visible) continue;
                source.BakeMesh(part.mesh, false);
                part.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                part.transform.localScale = source.transform.lossyScale;
            }
            UpdateAppearance(ghost);
            ghost.root.SetActive(true);
            TotalCaptured++;
        }

        void UpdateAppearance(Ghost ghost)
        {
            float progress = Mathf.Clamp01(ghost.age / ghost.duration);
            properties.SetColor(GhostColor, Color.Lerp(freshColor, fadedColor, progress));
            properties.SetFloat(GhostAlpha, opacity * Mathf.Pow(1 - progress, 1.5f));
            foreach (var part in ghost.parts) part.renderer.SetPropertyBlock(properties);
        }

        void OnDisable() => ClearTrail();

        void OnDestroy()
        {
            if (slots != null)
                foreach (var ghost in slots)
                    foreach (var part in ghost.parts)
                        if (part.mesh != null) Destroy(part.mesh);
            foreach (var material in ownedMaterials) if (material != null) Destroy(material);
            if (poolRoot != null) Destroy(poolRoot);
        }
    }
}
