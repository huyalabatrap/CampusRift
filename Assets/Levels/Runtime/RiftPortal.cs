using System.Collections.Generic;
using UnityEngine;

namespace CampusRift.Levels
{
    // The glowing violet crack a monster steps out of (P05-T02). Built from code so it needs no art: a jagged
    // LineRenderer, a few rising sparks and a short synthesized whoosh. Portals are pooled and reused.
    public sealed class RiftPortal : MonoBehaviour
    {
        public const float OpenSeconds = 0.45f, HoldSeconds = 1.0f, CloseSeconds = 0.45f;
        static readonly Color Violet = new Color(0.62f, 0.25f, 1f, 1f), Core = new Color(0.95f, 0.8f, 1f, 1f);
        static readonly List<RiftPortal> idle = new List<RiftPortal>();
        static readonly List<RiftPortal> live = new List<RiftPortal>();
        static AudioClip whoosh;

        LineRenderer outer, inner,ink; ParticleSystem sparks; AudioSource audioSource;
        float age, height = 2.6f;
        Vector3[] shape;
        public static int LiveCount => live.Count;
        public bool IsOpen => age >= OpenSeconds * 0.6f && age < OpenSeconds + HoldSeconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { idle.Clear(); live.Clear(); whoosh = null; }

        public static RiftPortal Open(Vector3 position, Vector3 facing)
        {
            RiftPortal portal = null;
            while (idle.Count > 0 && portal == null) { portal = idle[idle.Count - 1]; idle.RemoveAt(idle.Count - 1); }
            if (portal == null) portal = Create();
            portal.transform.position = position;
            Vector3 flat = Vector3.ProjectOnPlane(facing, Vector3.up);
            portal.transform.rotation = flat.sqrMagnitude > 0.01f ? Quaternion.LookRotation(flat) : Quaternion.identity;
            portal.Begin();
            return portal;
        }

        public static void CloseAll() { foreach (var p in live.ToArray()) p.End(); }

        static RiftPortal Create()
        {
            var go = new GameObject("Rift Portal"); var portal = go.AddComponent<RiftPortal>(); portal.Build(); return portal;
        }

        void Build()
        {
            var trail = Resources.Load<Material>("EnemyVfx/EnemyTrail"); var burst = Resources.Load<Material>("EnemyVfx/EnemyBurst");
            outer = MakeLine("Glow", trail, 0.55f, new Color(Violet.r, Violet.g, Violet.b, 0.55f));
            inner = MakeLine("Core", trail, 0.14f, Core);
            ink=MakeLine("Comic ink",Resources.Load<Material>("EnemyVfx/P12Telegraph"),.7f,new Color(.025f,.008f,.04f,.98f));
            var sparkObject = new GameObject("Sparks"); sparkObject.transform.SetParent(transform, false);
            sparks = sparkObject.AddComponent<ParticleSystem>();
            var main = sparks.main; main.playOnAwake = false; main.loop = false; main.startLifetime = 0.9f; main.startSpeed = 0.9f;
            main.startSize = 0.16f; main.startColor = Violet; main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = -0.25f; main.maxParticles = 40;
            var emission = sparks.emission; emission.enabled = false;
            var shapeModule = sparks.shape; shapeModule.shapeType = ParticleSystemShapeType.Box; shapeModule.scale = new Vector3(0.25f, 2.2f, 0.25f); shapeModule.position = new Vector3(0, 1.3f, 0);
            var fade = sparks.colorOverLifetime; fade.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            fade.color = g;
            var renderer = sparkObject.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = burst;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.spatialBlend = 1f; audioSource.minDistance = 4; audioSource.maxDistance = 40; audioSource.volume = 0.55f;
            shape = new Vector3[9];
        }

        LineRenderer MakeLine(string name, Material material, float width, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.useWorldSpace = false; line.positionCount = 9;
            line.widthMultiplier = width; line.startColor = line.endColor = color; line.numCapVertices = 4;
            var curve = new AnimationCurve(new Keyframe(0, 0.05f), new Keyframe(0.5f, 1f), new Keyframe(1, 0.05f)); line.widthCurve = curve;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }

        void Begin()
        {
            age = 0; height = 2.6f;
            for (int i = 0; i < shape.Length; i++)
            {
                float t = i / (float)(shape.Length - 1);
                float sway = i == 0 || i == shape.Length - 1 ? 0 : Random.Range(-0.22f, 0.22f);
                shape[i] = new Vector3(sway, t * height, 0);
            }
            Apply(0);
            gameObject.SetActive(true);
            if (!live.Contains(this)) live.Add(this);
            sparks.Clear(); sparks.Emit(18);
            if (whoosh == null) whoosh = MakeWhoosh();
            audioSource.pitch = Random.Range(0.9f, 1.1f); audioSource.PlayOneShot(whoosh);
        }

        void Apply(float openness)
        {
            for (int i = 0; i < shape.Length; i++)
            {
                var p = new Vector3(shape[i].x * openness, shape[i].y, 0);
                outer.SetPosition(i, p); inner.SetPosition(i, p);
                ink.SetPosition(i,p+Vector3.forward*.012f);
            }
            outer.widthMultiplier = 0.55f * openness; inner.widthMultiplier = 0.14f * openness;
            ink.widthMultiplier=.7f*openness;
        }

        void Update()
        {
            var ar=GetComponent<CampusRift.AR.ARCombatContext>();if(ar!=null&&ar.Paused)return;
            age += Time.deltaTime;
            float open = Mathf.Clamp01(age / OpenSeconds);
            float close = age > OpenSeconds + HoldSeconds ? 1 - Mathf.Clamp01((age - OpenSeconds - HoldSeconds) / CloseSeconds) : 1;
            float flicker = 1 + Mathf.Sin(age * 38f) * 0.08f;
            Apply(open * close * flicker);
            if (age >= OpenSeconds + HoldSeconds + CloseSeconds) End();
        }

        void End()
        {
            live.Remove(this); gameObject.SetActive(false);
            if (!idle.Contains(this)) idle.Add(this);
        }

        // A low rising hum with a noisy tail: enough to tell the player that something just came through.
        static AudioClip MakeWhoosh()
        {
            const int rate = 22050; int length = (int)(rate * 0.7f); var data = new float[length];
            var random = new System.Random(7); float phase = 0;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)length;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) * (1f - t * 0.4f);
                float freq = Mathf.Lerp(70f, 190f, t * t);
                phase += 2 * Mathf.PI * freq / rate;
                float tone = Mathf.Sin(phase) + 0.4f * Mathf.Sin(phase * 2.01f);
                float noise = (float)(random.NextDouble() * 2 - 1) * 0.45f * t;
                data[i] = (tone * 0.5f + noise) * envelope * 0.6f;
            }
            var clip = AudioClip.Create("RiftWhoosh", length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
    }
}
