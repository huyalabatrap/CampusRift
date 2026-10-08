using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CampusRift
{
    /// <summary>
    /// "Speed force" boost look: lightning crawling over the body, jagged bolts dragged along the running path,
    /// crackle/fork/spark particles, an ignition burst, a flickering light and faint screen speed lines.
    /// Pure presentation: it reads CampusExplorer.IsSprinting and never changes movement.
    /// </summary>
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CampusExplorer))]
    public sealed class SpeedForceVFX : MonoBehaviour
    {
        [Header("Assets")]
        public Material boltMaterial;
        public Material crackleMaterial;
        public Material forkMaterial;
        public Material sparkMaterial;
        public Material flareMaterial;
        public Texture2D speedLinesTexture;
        public AudioClip[] ignitionClips;

        [Header("Look")]
        [ColorUsage(false)] public Color primaryColor = new Color(1f, .74f, .2f);
        [ColorUsage(false)] public Color secondaryColor = new Color(.62f, .42f, 1f);
        [Range(0, 1)] public float secondaryChance = .25f;
        [Range(0, 12)] public int bodyArcs = 6;
        [Range(0, 5)] public int trailBolts = 3;
        [Min(.05f)] public float trailDuration = .3f;
        [Min(.01f)] public float boltRefresh = .045f;
        [Min(.01f)] public float rampUp = .1f;
        [Min(.01f)] public float rampDown = .35f;
        [Range(0, 1)] public float screenLines = .22f;
        [Range(0, 1)] public float ignitionVolume = .35f;
        public bool flickerLight = true;
        [Min(1f)] public float teleportDistance = 4f;

        public float Power { get; private set; }
        public int ActiveArcs { get; private set; }
        public int TrailSamples => history.Count;
        public int Ignitions { get; private set; }
        public int LiveParticleCount => (crackle!=null?crackle.particleCount:0)+(forks!=null?forks.particleCount:0)+(sparks!=null?sparks.particleCount:0)+(flare!=null?flare.particleCount:0);

        struct Sample { public Vector3 position; public float time; }
        sealed class Bolt
        {
            public LineRenderer glow, core;
            public float life;
            public bool secondary;
        }

        CampusExplorer explorer;
        Transform hips, chest;
        readonly List<Transform> anchors = new List<Transform>();
        readonly List<Sample> history = new List<Sample>(64);
        Renderer[] bodyRenderers;
        Transform arcRoot, trailRoot;
        Bolt[] arcs, trails;
        ParticleSystem crackle, forks, sparks, flare;
        Light glowLight;
        AudioSource audioSource;
        RawImage linesImage;
        Canvas linesCanvas;
        Vector3[] buffer = new Vector3[32];
        float[] trailNoise;
        float refreshTimer, lightFlicker = 1, linesTimer;
        Vector3 lastPosition;
        bool hasLast, wasBoosting, built;
        float burstUntil;
        public void Burst(float duration) { burstUntil = Mathf.Max(burstUntil, Time.time + Mathf.Max(0, duration)); Build(); }
        public void WarmUp() { Build(); }
        static readonly float[] TrailHeights = { .05f, .38f, -.42f, .2f, -.2f };
        static readonly float[] TrailSides = { 0, .12f, -.1f, -.16f, .16f };

        void Awake() => explorer = GetComponent<CampusExplorer>();

        void OnDisable() => Clear();

        void OnDestroy()
        {
            if (linesCanvas != null) Destroy(linesCanvas.gameObject);
            if (trailRoot != null) Destroy(trailRoot.gameObject);
        }

        /// <summary>Hide everything immediately (respawn, teleport, cutscene).</summary>
        public void Clear()
        {
            Power = 0; history.Clear(); hasLast = false; wasBoosting = false; ActiveArcs = 0;
            if (!built) return;
            foreach (var bolt in arcs) Show(bolt, false);
            foreach (var bolt in trails) Show(bolt, false);
            foreach (var ps in new[] { crackle, forks, sparks, flare }) if (ps != null) ps.Clear();
            if (glowLight != null) glowLight.enabled = false;
            if (linesImage != null) linesImage.enabled = false;
        }

        void LateUpdate()
        {
            if (!Build()) return;
            float dt = Time.deltaTime;
            Vector3 position = transform.position;
            if (hasLast && (position - lastPosition).sqrMagnitude > teleportDistance * teleportDistance) Clear();
            lastPosition = position; hasLast = true;
            if (dt <= 0) { UpdateScreenLines(0); return; }

            bool boosting = explorer != null && (explorer.IsSprinting || Time.time < burstUntil);
            Power = Mathf.MoveTowards(Power, boosting ? 1 : 0, dt / (boosting ? rampUp : rampDown));
            if (boosting && !wasBoosting) Ignite();
            wasBoosting = boosting;

            float now = Time.time;
            if (boosting) history.Add(new Sample { position = TrailAnchor(), time = now });
            while (history.Count > 0 && now - history[0].time > trailDuration) history.RemoveAt(0);
            if (history.Count > 48) history.RemoveAt(0);

            bool bodyVisible = BodyVisible();
            refreshTimer -= dt;
            bool refresh = refreshTimer <= 0;
            if (refresh) { refreshTimer = boltRefresh; lightFlicker = Random.Range(.55f, 1.25f); RollTrailNoise(); }

            UpdateArcs(dt, bodyVisible ? Power : 0);
            UpdateTrail();
            Emit(crackle, bodyVisible ? 42 * Power : 0);
            Emit(forks, bodyVisible ? 14 * Power : 0);
            Emit(sparks, 55 * Power * (explorer.IsGrounded ? 1 : .3f));
            if (glowLight != null)
            {
                glowLight.enabled = flickerLight && Power > .02f;
                glowLight.intensity = 1.6f * Power * lightFlicker;
                glowLight.color = Color.Lerp(primaryColor, Color.white, .25f);
            }
            UpdateScreenLines(Power);
        }

        Vector3 TrailAnchor() => hips != null ? hips.position : transform.position + Vector3.up * .85f;

        bool BodyVisible()
        {
            foreach (var r in bodyRenderers) if (r != null && r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy) return true;
            return false;
        }

        // ------------------------------------------------------------ setup
        bool Build()
        {
            if (built) return true;
            if (boltMaterial == null) return false;
            if (explorer == null) explorer = GetComponent<CampusExplorer>();
            var animator = GetComponentInChildren<Animator>();
            var root = animator != null ? animator.transform : transform;
            bodyRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();
            hips = FindBone(root, "hips");
            chest = FindBone(root, "chest") ?? FindBone(root, "spine");
            foreach (var key in new[] { "hips", "chest", "head_", "l_hand", "r_hand", "l_foot", "r_foot", "l_lowerleg", "r_lowerleg", "l_upperarm", "r_upperarm", "l_lowerarm", "r_rowerarm" })
            {
                var bone = FindBone(root, key);
                if (bone != null && !anchors.Contains(bone)) anchors.Add(bone);
            }
            if (anchors.Count == 0) anchors.Add(transform);

            arcRoot = new GameObject("Speed Force Arcs").transform;
            arcRoot.SetParent(transform, false);
            trailRoot = new GameObject("Speed Force Trail") { hideFlags = HideFlags.DontSave }.transform;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(trailRoot.gameObject, gameObject.scene);
            arcs = new Bolt[bodyArcs];
            for (int i = 0; i < arcs.Length; i++) arcs[i] = MakeBolt(arcRoot, "Arc " + i, false, .07f, .022f);
            trails = new Bolt[trailBolts];
            for (int i = 0; i < trails.Length; i++) trails[i] = MakeBolt(trailRoot, "Trail Bolt " + i, true, .2f, .055f);
            trailNoise = new float[trailBolts * buffer.Length * 2];

            crackle = MakeParticles("Crackle", crackleMaterial, true, 2, 2, .06f, .13f, .22f, .55f, 0, 0);
            if (crackle != null)
            {
                var shape = crackle.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(.45f, 1.45f, .4f); shape.position = new Vector3(0, .85f, 0);
            }
            forks = MakeParticles("Forks", forkMaterial, false, 2, 1, .05f, .1f, .35f, .75f, 2.5f, 6f);
            if (forks != null)
            {
                var shape = forks.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .35f; shape.position = new Vector3(0, .9f, 0);
                var r = forks.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 1.6f; r.velocityScale = .03f;
            }
            sparks = MakeParticles("Sparks", sparkMaterial, false, 1, 1, .18f, .34f, .05f, .12f, 1.2f, 3.2f);
            if (sparks != null)
            {
                var main = sparks.main; main.gravityModifier = 1.2f;
                var shape = sparks.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 55; shape.radius = .25f; shape.position = new Vector3(0, .05f, 0); shape.rotation = new Vector3(-90, 0, 0);
                var r = sparks.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 2.5f; r.velocityScale = .06f;
            }
            flare = MakeParticles("Ignition Flare", flareMaterial, true, 1, 1, .12f, .18f, 1f, 1.4f, 0, 0);
            if (flare != null) { var shape = flare.shape; shape.enabled = false; flare.transform.localPosition = new Vector3(0, .9f, 0); }

            var lightGo = new GameObject("Speed Force Light");
            lightGo.transform.SetParent(transform, false); lightGo.transform.localPosition = new Vector3(0, .9f, 0);
            glowLight = lightGo.AddComponent<Light>();
            glowLight.type = LightType.Point; glowLight.range = 3.5f; glowLight.shadows = LightShadows.None; glowLight.enabled = false;

            if (ignitionClips != null && ignitionClips.Length > 0)
            {
                audioSource = lightGo.AddComponent<AudioSource>();
                audioSource.playOnAwake = false; audioSource.spatialBlend = .6f; audioSource.dopplerLevel = 0;
                var reference = GetComponent<AudioSource>();
                if (reference != null) audioSource.outputAudioMixerGroup = reference.outputAudioMixerGroup;
            }
            BuildScreenLines();
            built = true;
            return true;
        }

        static Transform FindBone(Transform root, string key)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.ToLowerInvariant().StartsWith(key)) return t;
            return null;
        }

        Bolt MakeBolt(Transform parent, string name, bool world, float glowWidth, float coreWidth)
        {
            var bolt = new Bolt { glow = Line(parent, name + " Glow", world, glowWidth), core = Line(parent, name + " Core", world, coreWidth) };
            if (world)
            {
                // Thick at the runner, thinning out toward the tail.
                bolt.glow.widthCurve = AnimationCurve.Linear(0, 1, 1, .15f);
                bolt.core.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            }
            Show(bolt, false);
            return bolt;
        }

        LineRenderer Line(Transform parent, string name, bool world, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = boltMaterial;
            line.useWorldSpace = world;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 0; line.numCornerVertices = 0;
            line.widthMultiplier = width;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off; line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.allowOcclusionWhenDynamic = false;
            return line;
        }

        ParticleSystem MakeParticles(string name, Material material, bool local, int tilesX, int tilesY, float lifeMin, float lifeMax, float sizeMin, float sizeMax, float speedMin, float speedMax)
        {
            if (material == null) return null;
            var go = new GameObject("Speed Force " + name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false; main.loop = true; main.duration = 1;
            main.simulationSpace = local ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startColor = new ParticleSystem.MinMaxGradient(primaryColor, Color.Lerp(primaryColor, secondaryColor, .8f));
            main.maxParticles = 80;
            var emission = ps.emission; emission.rateOverTime = 0;
            var fade = ps.colorOverLifetime; fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.8f, .5f), new GradientAlphaKey(0, 1) });
            fade.color = g;
            if (tilesX * tilesY > 1)
            {
                var sheet = ps.textureSheetAnimation; sheet.enabled = true;
                sheet.numTilesX = tilesX; sheet.numTilesY = tilesY;
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0);
                sheet.startFrame = new ParticleSystem.MinMaxCurve(0, tilesX * tilesY - .01f);
            }
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            ps.Play();
            return ps;
        }

        void BuildScreenLines()
        {
            if (speedLinesTexture == null || screenLines <= 0) return;
            var go = new GameObject("Speed Force Screen Lines", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.hideFlags = HideFlags.DontSave; go.layer = 5;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            linesCanvas = go.GetComponent<Canvas>();
            linesCanvas.renderMode = RenderMode.ScreenSpaceOverlay; linesCanvas.sortingOrder = -20;
            var image = new GameObject("Lines", typeof(RectTransform), typeof(RawImage));
            image.transform.SetParent(go.transform, false);
            var r = (RectTransform)image.transform;
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            linesImage = image.GetComponent<RawImage>();
            linesImage.texture = speedLinesTexture; linesImage.raycastTarget = false; linesImage.enabled = false;
        }

        // ------------------------------------------------------------ per frame
        void Ignite()
        {
            Ignitions++;
            refreshTimer = 0;
            if (flare != null) flare.Emit(1);
            if (forks != null) forks.Emit(10);
            if (crackle != null) crackle.Emit(8);
            if (audioSource != null && ignitionClips.Length > 0)
            {
                audioSource.pitch = Random.Range(.9f, 1.15f);
                audioSource.PlayOneShot(ignitionClips[Random.Range(0, ignitionClips.Length)], ignitionVolume);
            }
        }

        static void Emit(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var emission = ps.emission; emission.rateOverTime = rate;
        }

        void UpdateArcs(float dt, float power)
        {
            int wanted = Mathf.CeilToInt(arcs.Length * power);
            ActiveArcs = 0;
            for (int i = 0; i < arcs.Length; i++)
            {
                var bolt = arcs[i];
                bolt.life -= dt;
                if (i >= wanted) { Show(bolt, false); continue; }
                if (bolt.life <= 0)
                {
                    // Arcs strike, vanish for a beat, then re-strike somewhere else.
                    bool rest = bolt.glow.enabled && Random.value < .35f;
                    bolt.life = rest ? Random.Range(.02f, .06f) : Random.Range(.05f, .11f);
                    if (rest) { Show(bolt, false); continue; }
                    StrikeArc(bolt);
                }
                if (bolt.glow.enabled) ActiveArcs++;
            }
        }

        void StrikeArc(Bolt bolt)
        {
            var a = anchors[Random.Range(0, anchors.Count)];
            Vector3 start = a.position, end;
            Transform b = null;
            if (Random.value < .6f)
            {
                for (int tries = 0; tries < 4 && b == null; tries++)
                {
                    var candidate = anchors[Random.Range(0, anchors.Count)];
                    float d = (candidate.position - start).sqrMagnitude;
                    if (candidate != a && d < .9f * .9f && d > .15f * .15f) b = candidate;
                }
            }
            if (b != null) end = b.position;
            else
            {
                var outward = Vector3.ProjectOnPlane(Random.onUnitSphere, Vector3.up).normalized + Random.Range(-.5f, .8f) * Vector3.up;
                end = start + outward.normalized * Random.Range(.25f, .6f);
            }
            int count = Random.Range(7, 11);
            Jag(transform.InverseTransformPoint(start), transform.InverseTransformPoint(end), count, .22f);
            bolt.secondary = Random.value < secondaryChance;
            Apply(bolt, count, 1);
        }

        void RollTrailNoise()
        {
            for (int i = 0; i < trailNoise.Length; i++) trailNoise[i] = Random.Range(-1f, 1f);
            if (trails == null) return;
            foreach (var bolt in trails) bolt.secondary = Random.value < secondaryChance * .6f;
        }

        void UpdateTrail()
        {
            int n = history.Count;
            if (n < 2) { foreach (var bolt in trails) Show(bolt, false); return; }
            float total = 0;
            for (int i = n - 1; i > 0; i--) total += Vector3.Distance(history[i].position, history[i - 1].position);
            if (total < .2f) { foreach (var bolt in trails) Show(bolt, false); return; }
            int count = Mathf.Clamp(Mathf.CeilToInt(total / .16f) + 2, 6, buffer.Length);
            var heights = TrailHeights;
            var sides = TrailSides;
            for (int k = 0; k < trails.Length; k++)
            {
                for (int p = 0; p < count; p++)
                {
                    float t = p / (count - 1f);
                    Vector3 point = AlongHistory(t * total, out Vector3 dir);
                    Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
                    float amp = Mathf.Lerp(.06f, .42f, t);
                    int noise = (k * buffer.Length + p) * 2;
                    point += Vector3.up * heights[k % heights.Length] * (1 - t * .4f) + right * sides[k % sides.Length];
                    point += (right * trailNoise[noise] + Vector3.up * trailNoise[noise + 1] * .7f) * amp;
                    buffer[p] = point;
                }
                // Fresh trail is solid, older segments burn out toward the tail.
                Apply(trails[k], count, Mathf.Clamp01(Power * 1.4f + .15f) * Mathf.Clamp01(n / 4f));
            }
        }

        Vector3 AlongHistory(float distance, out Vector3 direction)
        {
            direction = transform.forward;
            for (int i = history.Count - 1; i > 0; i--)
            {
                Vector3 a = history[i].position, b = history[i - 1].position;
                float seg = Vector3.Distance(a, b);
                if (seg > 1e-4f) direction = (a - b) / seg;
                if (distance <= seg || i == 1) return Vector3.Lerp(a, b, seg > 1e-4f ? Mathf.Clamp01(distance / seg) : 0);
                distance -= seg;
            }
            return history[history.Count - 1].position;
        }

        void Jag(Vector3 a, Vector3 b, int count, float jag)
        {
            Vector3 dir = b - a; float length = dir.magnitude;
            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                Vector3 p = Vector3.Lerp(a, b, t);
                if (i > 0 && i < count - 1)
                    p += Vector3.ProjectOnPlane(Random.insideUnitSphere, dir).normalized * Random.Range(0, jag) * length * Mathf.Sin(t * Mathf.PI);
                buffer[i] = p;
            }
        }

        void Apply(Bolt bolt, int count, float alpha)
        {
            var hue = bolt.secondary ? secondaryColor : primaryColor;
            var head = new Color(hue.r, hue.g, hue.b, alpha);
            var tail = new Color(hue.r, hue.g, hue.b, 0);
            for (int l = 0; l < 2; l++)
            {
                var line = l == 0 ? bolt.glow : bolt.core;
                line.positionCount = count;
                line.SetPositions(buffer);
                line.startColor = line == bolt.core ? Color.Lerp(head, new Color(1, 1, 1, alpha), .6f) : head;
                line.endColor = line.useWorldSpace ? tail : head * new Color(1, 1, 1, .6f);
                line.enabled = true;
            }
        }

        static void Show(Bolt bolt, bool visible)
        {
            if (bolt == null) return;
            bolt.glow.enabled = visible; bolt.core.enabled = visible;
        }

        void UpdateScreenLines(float power)
        {
            if (linesImage == null) return;
            var ui = UI.UIStateManager.Instance;
            bool gameplay = ui == null || ui.State == UI.UIState.Gameplay;
            float alpha = gameplay ? screenLines * power : 0;
            linesImage.enabled = alpha > .005f;
            if (!linesImage.enabled) return;
            linesTimer -= Time.unscaledDeltaTime;
            var r = linesImage.rectTransform;
            if (linesTimer <= 0)
            {
                linesTimer = .05f;
                float size = Mathf.Max(Screen.width, Screen.height) / Mathf.Max(.01f, linesCanvas.scaleFactor) * 1.25f;
                r.sizeDelta = new Vector2(size, size);
                r.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));
                linesImage.uvRect = Random.value < .5f ? new Rect(0, 0, 1, 1) : new Rect(1, 0, -1, 1);
            }
            linesImage.color = new Color(1, .93f, .8f, alpha * Random.Range(.7f, 1f));
        }
    }
}
