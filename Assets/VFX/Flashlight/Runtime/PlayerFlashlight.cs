using UnityEngine;
using UnityEngine.Rendering;

namespace CampusRift
{
    /// <summary>
    /// Hand-held flashlight for the dark: the model sits in the right fist (grip solved from the finger bones),
    /// a shadowed spot lights the path ahead and a faint fill keeps the character readable.
    /// It fades in as the sky gets dark and is put away in daylight.
    /// </summary>
    // Before the afterimage (100) and speed-force (110) samplers, so they see the raised arm.
    [DefaultExecutionOrder(90)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CampusExplorer))]
    public sealed class PlayerFlashlight : MonoBehaviour
    {
        [Header("Assets")]
        public GameObject model;
        public Material lensMaterial;
        public Texture cookie;

        [Header("Beam")]
        [ColorUsage(false)] public Color color = new Color(1f, .93f, .8f);
        [Min(0)] public float intensity = 9f;
        [Min(1)] public float range = 18f;
        [Range(10, 120)] public float spotAngle = 70f;
        [Range(1, 120)] public float innerSpotAngle = 34f;
        [Range(0, 45)] public float downPitch = 8f;
        [Range(0, 1)] public float handSway = .3f;
        public bool shadows = true;
        [Header("Fill (keeps the player readable)")]
        [Min(0)] public float fillIntensity = .35f;
        [Min(.5f)] public float fillRange = 3.2f;

        [Header("Holding pose (two-bone IK on the right arm)")]
        public bool raiseArm = true;
        [Tooltip("Wrist target relative to the shoulder: right, up, forward (character space, metres).")]
        public Vector3 handOffset = new Vector3(.1f, -.28f, .26f);

        [Header("When")]
        [Tooltip("Sky brightness at or below which the flashlight is fully on.")]
        [Range(0, 1)] public float fullyOnBelow = .25f;
        [Tooltip("Sky brightness above which the flashlight is put away.")]
        [Range(0, 1)] public float offAbove = .5f;
        public bool alwaysOn;

        public float Level { get; private set; }
        public bool Equipped => Level > .01f;
        public Light Beam => beam;

        CampusExplorer explorer;
        UI.SkyLightingController sky;
        Transform hand, forearm, upperArm, grip, visual;
        GameObject instance;
        Renderer[] modelRenderers, bodyRenderers;
        Light beam, fill;
        Transform lens;
        MaterialPropertyBlock block;
        int lensSlot = -1;
        Vector3 lensLocal = Vector3.forward * .12f, gripScale = Vector3.one, palmLocal = Vector3.zero;
        Transform[][] fingerChains = new Transform[0][];
        Vector3[] fingerDirs = new Vector3[0];
        [Tooltip("Curl per finger joint (root, middle, tip) in degrees; the thumb uses half.")]
        public Vector3 fingerCurl = new Vector3(75, 85, 60);
        float skyScan;
        Quaternion aim;
        bool built, aimReady;
        // Bones the pose writes. The run/idle clips do not key the fingers (and may skip others), so an
        // un-animated bone would keep last frame's result and the curl would pile up every frame. Each frame
        // we first put back the animated value if the Animator did not write one since our last edit.
        sealed class PosedBone { public Transform bone; public Quaternion animated, written; public bool dirty; }
        readonly System.Collections.Generic.List<PosedBone> posed = new System.Collections.Generic.List<PosedBone>();
        PosedBone Track(Transform t) { var p = new PosedBone { bone = t, animated = t.localRotation }; posed.Add(p); return p; }
        void RestoreUnanimated()
        {
            foreach (var p in posed)
            {
                if (p.dirty && Quaternion.Angle(p.bone.localRotation, p.written) < .01f) p.bone.localRotation = p.animated;
                p.animated = p.bone.localRotation; p.dirty = false;
            }
        }
        void MarkWritten() { foreach (var p in posed) { p.written = p.bone.localRotation; p.dirty = true; } }

        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        void Awake() => explorer = GetComponent<CampusExplorer>();

        void OnDisable()
        {
            Level = 0;
            if (built) { Apply(0); RestoreUnanimated(); }
        }

        void LateUpdate()
        {
            if (!Build()) return;
            float target = alwaysOn ? 1 : Mathf.InverseLerp(offAbove, fullyOnBelow, SkyBrightness());
            target = Mathf.SmoothStep(0, 1, target);
            // Equip is quick, putting away a touch slower; unscaled so pause menus do not freeze it midway.
            Level = Mathf.MoveTowards(Level, target, Time.unscaledDeltaTime / (target > Level ? .35f : .6f));
            Apply(Level);
            RestoreUnanimated();   // always, so putting the light away also returns the hand to its animated pose
            if (Level <= .001f) return;
            if (raiseArm && upperArm != null) { PoseArm(Mathf.SmoothStep(0, 1, Level)); MarkWritten(); }

            // Beam: from the lens, pointing where the character faces, pitched to the ground ahead,
            // with a little of the hand's swing so it feels held rather than mounted.
            var facing = visual != null ? visual.forward : transform.forward;
            facing = Vector3.ProjectOnPlane(facing, Vector3.up).normalized;
            if (facing.sqrMagnitude < .01f) facing = transform.forward;
            var wanted = Quaternion.AngleAxis(downPitch, Vector3.Cross(Vector3.up, facing)) * facing;
            wanted = Vector3.Slerp(wanted, lens.forward, handSway).normalized;
            var goal = Quaternion.LookRotation(wanted, Vector3.up);
            aim = aimReady ? Quaternion.Slerp(aim, goal, 1 - Mathf.Exp(-14 * Time.deltaTime)) : goal;
            aimReady = true;
            beam.transform.SetPositionAndRotation(lens.position + wanted * .02f, aim);
            // Fill from above and behind the head (the camera side): readable silhouette, no hot spot on the clothes.
            fill.transform.position = transform.position + Vector3.up * 2.1f - facing * .7f;
        }

        // Analytic two-bone IK (after D. Holden): keep the animated elbow plane, bring the wrist to a point in front of
        // the chest and turn the fist so the flashlight points where the character faces. Blended by the equip level.
        void PoseArm(float weight)
        {
            var facing = Vector3.ProjectOnPlane(visual.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, facing);
            Vector3 target = upperArm.position + right * handOffset.x + Vector3.up * handOffset.y + facing * handOffset.z;
            Quaternion upperAnim = upperArm.localRotation, foreAnim = forearm.localRotation, handAnim = hand.localRotation;

            Vector3 a = upperArm.position, b = forearm.position, c = hand.position;
            float lab = (b - a).magnitude, lcb = (c - b).magnitude;
            float lat = Mathf.Clamp((target - a).magnitude, .01f, lab + lcb - .01f);
            float acab0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((c - a).normalized, (b - a).normalized), -1, 1));
            float babc0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((a - b).normalized, (c - b).normalized), -1, 1));
            float acat0 = Mathf.Acos(Mathf.Clamp(Vector3.Dot((c - a).normalized, (target - a).normalized), -1, 1));
            float acab1 = Mathf.Acos(Mathf.Clamp((lcb * lcb - lab * lab - lat * lat) / (-2 * lab * lat), -1, 1));
            float babc1 = Mathf.Acos(Mathf.Clamp((lat * lat - lab * lab - lcb * lcb) / (-2 * lab * lcb), -1, 1));
            Vector3 axis0 = Vector3.Cross(c - a, b - a);
            // A straight arm has no bend plane: bend the elbow down and back, like a natural hold.
            if (axis0.sqrMagnitude < 1e-6f) axis0 = Vector3.Cross(c - a, -facing - Vector3.up);
            axis0.Normalize();
            Vector3 axis1 = Vector3.Cross(c - a, target - a);
            Quaternion aG = upperArm.rotation, bG = forearm.rotation;
            var r0 = Quaternion.AngleAxis((acab1 - acab0) * Mathf.Rad2Deg, Quaternion.Inverse(aG) * axis0);
            var r1 = Quaternion.AngleAxis((babc1 - babc0) * Mathf.Rad2Deg, Quaternion.Inverse(bG) * axis0);
            var r2 = axis1.sqrMagnitude > 1e-8f ? Quaternion.AngleAxis(acat0 * Mathf.Rad2Deg, Quaternion.Inverse(aG) * axis1.normalized) : Quaternion.identity;
            upperArm.localRotation = Quaternion.Slerp(upperAnim, upperAnim * r0 * r2, weight);
            forearm.localRotation = Quaternion.Slerp(foreAnim, foreAnim * r1, weight);

            // Fist orientation: grip forward = facing, pitched like the beam.
            var aimDir = Quaternion.AngleAxis(downPitch, right) * facing;
            // Roll the fist so it continues the forearm instead of snapping the wrist.
            var up = Vector3.ProjectOnPlane(forearm.position - hand.position, aimDir);
            var wantedGrip = Quaternion.LookRotation(aimDir, up.sqrMagnitude > 1e-6f ? up : Vector3.up);
            var handWorld = wantedGrip * Quaternion.Inverse(grip.localRotation);
            var handLocal = Quaternion.Inverse(forearm.rotation) * handWorld;
            hand.localRotation = Quaternion.Slerp(handAnim, handLocal, weight);
            CurlFingers(weight);
        }

        void CurlFingers(float weight)
        {
            if (palmLocal == Vector3.zero) return;
            for (int f = 0; f < fingerChains.Length; f++)
            {
                var chain = fingerChains[f];
                bool thumb = chain[0].name.ToLowerInvariant().Contains("thumb");
                // Rotating the finger direction about (finger x palm) turns it toward the palm: a closing fist.
                var axis = hand.TransformDirection(Vector3.Cross(fingerDirs[f], palmLocal).normalized);
                for (int j = 0; j < chain.Length && j < 3; j++)
                {
                    float angle = (j == 0 ? fingerCurl.x : j == 1 ? fingerCurl.y : fingerCurl.z) * (thumb ? .5f : 1) * weight;
                    chain[j].rotation = Quaternion.AngleAxis(angle, axis) * chain[j].rotation;
                }
            }
        }

        float SkyBrightness()
        {
            if (sky == null && Time.unscaledTime >= skyScan) { skyScan = Time.unscaledTime + 1; sky = FindAnyObjectByType<UI.SkyLightingController>(); }
            return sky != null ? sky.Brightness : 1;
        }

        void Apply(float level)
        {
            bool hidden = BodyHidden();
            if (instance != null)
            {
                bool show = level > .01f;
                if (instance.activeSelf != show) instance.SetActive(show);
                // Brought up into the hand rather than popping in.
                grip.localScale = gripScale * Mathf.SmoothStep(.2f, 1, Mathf.Clamp01(level * 2.5f));
                foreach (var r in modelRenderers) if (r != null) r.forceRenderingOff = hidden;
                if (lensSlot >= 0)
                {
                    block.SetColor(EmissionColor, color * (level * 3.5f));
                    modelRenderers[0].SetPropertyBlock(block, lensSlot);
                }
            }
            beam.enabled = level > .01f; fill.enabled = level > .01f && !hidden;
            beam.intensity = intensity * level; fill.intensity = fillIntensity * level;
        }

        bool BodyHidden()
        {
            if (bodyRenderers == null || bodyRenderers.Length == 0) return false;
            foreach (var r in bodyRenderers) if (r != null && !r.forceRenderingOff) return false;
            return true;
        }

        // ------------------------------------------------------------ setup
        bool Build()
        {
            if (built) return true;
            if (explorer == null) explorer = GetComponent<CampusExplorer>();
            var animator = GetComponentInChildren<Animator>();
            visual = animator != null ? animator.transform : transform;
            bodyRenderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            hand = Find(visual, "r_hand") ?? Find(visual, "righthand") ?? Find(visual, "hand_r");
            if (hand != null && hand.parent != null && hand.parent.parent != null) { forearm = hand.parent; upperArm = forearm.parent; Track(upperArm); Track(forearm); Track(hand); }
            block = new MaterialPropertyBlock();

            grip = new GameObject("Flashlight Grip").transform;
            if (hand != null) { grip.SetParent(hand, false); SolveGrip(); }
            else { grip.SetParent(visual, false); grip.localPosition = new Vector3(.22f, .85f, .2f); }

            if (model != null)
            {
                instance = Instantiate(model, grip, false);
                instance.name = "Flashlight";
                modelRenderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var r in modelRenderers) { r.shadowCastingMode = ShadowCastingMode.Off; r.lightProbeUsage = LightProbeUsage.BlendProbes; }
                OrientModel();
            }
            lens = new GameObject("Lens").transform;
            lens.SetParent(instance != null ? instance.transform : grip, false);
            lens.localPosition = lensLocal;
            lens.rotation = grip.rotation;

            beam = new GameObject("Flashlight Beam").AddComponent<Light>();
            beam.transform.SetParent(transform, false);
            beam.type = LightType.Spot; beam.color = color; beam.range = range;
            beam.spotAngle = spotAngle; beam.innerSpotAngle = Mathf.Min(innerSpotAngle, spotAngle - 1);
            beam.shadows = shadows ? LightShadows.Soft : LightShadows.None; beam.shadowStrength = .9f; beam.shadowNearPlane = .15f;
            beam.renderMode = LightRenderMode.ForcePixel;
            if (cookie != null) beam.cookie = cookie;
            fill = new GameObject("Flashlight Fill").AddComponent<Light>();
            fill.transform.SetParent(transform, false);
            fill.type = LightType.Point; fill.color = Color.Lerp(color, Color.white, .3f); fill.range = fillRange; fill.shadows = LightShadows.None;
            built = true;
            Apply(0);
            return true;
        }

        static Transform Find(Transform root, string key)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.ToLowerInvariant().Replace("_", "").StartsWith(key.Replace("_", ""))) return t;
            return null;
        }

        static Transform Child(Transform hand, params string[] keys)
        {
            foreach (Transform c in hand)
            {
                var n = c.name.ToLowerInvariant();
                foreach (var k in keys) if (n.Contains(k)) return c;
            }
            return null;
        }

        // The finger roots are children of the hand, so their offsets are pose independent: the flashlight
        // runs along the knuckle line (little finger -> index, lens on the thumb side), through the fist.
        void SolveGrip()
        {
            var index = Child(hand, "index");
            var little = Child(hand, "pinky", "little", "ritt") ?? Child(hand, "ring");
            var thumb = Child(hand, "thumb");
            if (index == null || little == null) { grip.localPosition = Vector3.zero; return; }
            Vector3 i = hand.InverseTransformPoint(index.position), l = hand.InverseTransformPoint(little.position);
            Vector3 knuckles = (i + l) * .5f;
            Vector3 along = (i - l).normalized;
            Vector3 fingers = knuckles.normalized;
            Vector3 palm = thumb != null ? Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(hand.InverseTransformPoint(thumb.position) - knuckles, along), fingers).normalized : Vector3.zero;
            // The thumb root sits toward the back of the hand on this rig family, so the palm is the opposite side.
            palm = -palm;
            // Fist centre: inside the curled fingers, on the palm side, just short of the knuckles.
            grip.localPosition = knuckles * .8f + palm * .022f;
            palmLocal = palm;
            // Finger chains (root -> tip) for the fist; the curl axis is fixed per finger in hand space.
            var chains = new System.Collections.Generic.List<Transform[]>(); var dirs = new System.Collections.Generic.List<Vector3>();
            foreach (Transform root in hand)
            {
                var chain = new System.Collections.Generic.List<Transform>();
                for (var t = root; t != null; t = t.childCount > 0 ? t.GetChild(0) : null) chain.Add(t);
                if (chain.Count < 2) continue;
                chains.Add(chain.ToArray());
                dirs.Add((hand.InverseTransformPoint(chain[1].position) - hand.InverseTransformPoint(chain[0].position)).normalized);
            }
            fingerChains = chains.ToArray(); fingerDirs = dirs.ToArray();
            foreach (var chain in fingerChains) for (int j = 0; j < chain.Length && j < 3; j++) Track(chain[j]);
            grip.localRotation = Quaternion.LookRotation(along, -fingers);
            gripScale = Vector3.one / Mathf.Max(.001f, hand.lossyScale.x);
            grip.localScale = gripScale;
        }

        void OrientModel()
        {
            // Find the lens (emissive slot) and turn the model so it points down the grip's forward axis.
            var renderer = modelRenderers.Length > 0 ? modelRenderers[0] : null;
            var filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null) return;
            var mats = renderer.sharedMaterials;
            for (int m = 0; m < mats.Length; m++) if (mats[m] == lensMaterial) lensSlot = m;
            var mesh = filter.sharedMesh;
            Vector3 lensCentre = mesh.bounds.center + Vector3.forward * mesh.bounds.extents.z;
            if (lensSlot >= 0 && lensSlot < mesh.subMeshCount && mesh.isReadable)
            {
                var verts = mesh.vertices; var tris = mesh.GetTriangles(lensSlot);
                Vector3 sum = Vector3.zero; foreach (int t in tris) sum += verts[t];
                if (tris.Length > 0) lensCentre = sum / tris.Length;
            }
            var root = instance.transform; var local = renderer.transform;
            Vector3 lensPoint = root.InverseTransformPoint(local.TransformPoint(lensCentre));
            Vector3 centre = root.InverseTransformPoint(local.TransformPoint(mesh.bounds.center));
            root.localRotation = Quaternion.FromToRotation((lensPoint - centre).normalized, Vector3.forward);
            // Centre of the body in the fist, nudged back so the head clears the knuckles.
            root.localPosition = -(root.localRotation * Vector3.Scale(root.localScale, centre)) - Vector3.forward * .015f;
            lensLocal = lensPoint + (lensPoint - centre).normalized * .005f;
        }

    }
}
