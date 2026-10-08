using System.Collections.Generic;
using CampusRift.Monsters;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.Rendering;

namespace CampusRift.Skills
{
    // One reusable actor: only the player's visual rig is copied, never movement, health or skills.
    [DisallowMultipleComponent]
    public sealed class PhantomDecoy : MonoBehaviour
    {
        public static PhantomDecoy Active { get; private set; }
        static readonly List<PhantomDecoy> live=new List<PhantomDecoy>(2);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){Active=null;live.Clear();}
        public bool Live { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float SpawnedAt { get; private set; }
        public float Lifetime { get; private set; }
        public float Remaining => Live ? Mathf.Max(0, SpawnedAt + Lifetime - Time.time) : 0;
        public int RendererCount => renderers == null ? 0 : renderers.Length;
        public int AudioEvents { get; private set; }
        public event System.Action<Vector3> Dissolved;

        Animator animator;
        SkinnedMeshRenderer[] renderers;
        Material[] ownedMaterials;
        MaterialPropertyBlock block;
        CharacterController controller;
        AudioSource audioSource;
        AudioClip footstep;
        AudioClip spawnSound, expireSound;
        ParticleSystem particles;
        NavMeshPath route;
        Vector3[] corners;
        CampusAutomaticDoor[] doors;
        readonly Collider[] nearby = new Collider[24];
        int corner;
        float nextStep, nextDoor, blockedFor, navGapDistance, fadeStarted = -1;
        float speed, opacity;
        Vector3 lastPosition;
        PlayerMonsterHealth ownerHealth;
        static readonly int GhostColor = Shader.PropertyToID("_GhostColor");
        static readonly int GhostAlpha = Shader.PropertyToID("_GhostAlpha");
        static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        static readonly int RunRate = Animator.StringToHash("RunRate");

        public void Initialize(Animator source, Material ghost, Material shardMaterial, Mesh shardMesh,
            AudioClip appear, AudioClip vanish,
            AudioMixerGroup mixer, PlayerMonsterHealth health)
        {
            ownerHealth = health;
            var visual = Instantiate(source.gameObject, transform);
            visual.name = "Phantom visual (player rig only)";
            visual.transform.localPosition = source.transform.localPosition;
            visual.transform.localRotation = source.transform.localRotation;
            visual.transform.localScale = source.transform.localScale;
            foreach (var collider in visual.GetComponentsInChildren<Collider>()) Destroy(collider);
            animator = visual.GetComponent<Animator>();
            animator.applyRootMotion = false;
            // Reuse the curated nine visible parts from the player's afterimage setup.
            // The other four are duplicate black outline shells and only add overdraw.
            var trail = source.GetComponentInParent<CharacterAfterimageTrail>();
            HashSet<string> visibleNames = null;
            if (trail != null && trail.sources != null && trail.sources.Length > 0)
            {
                visibleNames = new HashSet<string>();
                foreach (var part in trail.sources) if (part != null) visibleNames.Add(part.name);
                if (visibleNames.Count == 0) visibleNames = null;
            }
            var selected = new List<SkinnedMeshRenderer>();
            foreach (var part in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (visibleNames != null && !visibleNames.Contains(part.name)) { part.enabled = false; continue; }
                selected.Add(part);
            }
            renderers = selected.ToArray();
            var materials = new List<Material>(renderers.Length * 2);
            foreach (var renderer in renderers)
            {
                var originals = renderer.sharedMaterials;
                var spectral = new Material[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                {
                    var copy = new Material(ghost) { name = "Phantom / " + renderer.name, hideFlags = HideFlags.DontSave };
                    var original = originals[i];
                    if (original != null)
                    {
                        if (original.HasProperty("_BaseMap")) copy.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
                        if (original.HasProperty("_AlphaClip") && original.GetFloat("_AlphaClip") > 0.5f && original.HasProperty("_Cutoff"))
                            copy.SetFloat("_Cutoff", original.GetFloat("_Cutoff"));
                        if (original.HasProperty("_Cull")) copy.SetFloat("_Cull", original.GetFloat("_Cull"));
                    }
                    spectral[i] = copy; materials.Add(copy);
                }
                renderer.sharedMaterials = spectral;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.updateWhenOffscreen = false;
            }
            ownedMaterials = materials.ToArray();
            block = new MaterialPropertyBlock();
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 1.7f; controller.radius = 0.3f; controller.center = Vector3.up * 0.85f;
            controller.stepOffset = 0.36f; controller.slopeLimit = 48f; controller.skinWidth = 0.04f;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.spatialBlend = 1; audioSource.minDistance = 2;
            audioSource.maxDistance = 20; audioSource.outputAudioMixerGroup = mixer;
            Skills.SkillAudio.Apply(audioSource);
            spawnSound = appear; expireSound = vanish;
            footstep = MakeFootstep();
            var fx = new GameObject("Dimensional fragments"); fx.transform.SetParent(transform, false);
            particles = fx.AddComponent<ParticleSystem>();
            var main = particles.main; main.playOnAwake = false; main.loop = false;
            main.maxParticles = 28; main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(.3f,.7f,1), new Color(.62f,.24f,1));
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.55f;
            var emission = particles.emission; emission.enabled = false;
            var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            particleRenderer.mesh = shardMesh;
            particleRenderer.sharedMaterial = shardMaterial;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            route = new NavMeshPath();
            doors = FindObjectsByType<CampusAutomaticDoor>();
            gameObject.SetActive(false);
        }

        static AudioClip MakeFootstep()
        {
            const int count = 3300;
            var clip = AudioClip.Create("Phantom soft footfall", count, 1, 22050, false);
            var data = new float[count]; uint seed = 712345u;
            for (int i = 0; i < count; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                float noise = (seed / 4294967295f) * 2f - 1f;
                float envelope = Mathf.Exp(-i / 390f) * (1f - Mathf.Clamp01(i / 3300f));
                data[i] = noise * envelope * .16f;
            }
            clip.SetData(data, 0); return clip;
        }

        public bool Launch(Vector3 start, Vector3 direction, float travelSpeed, float duration)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.5f) return false;
            NavMeshHit sampled;
            bool found = false;
            Vector3[] offsets = { direction * .75f, Quaternion.Euler(0,30,0)*direction*.65f,
                Quaternion.Euler(0,-30,0)*direction*.65f, Vector3.zero };
            foreach (var offset in offsets)
            {
                if (!NavMesh.SamplePosition(start + offset, out sampled, 1.1f, NavMesh.AllAreas) ||
                    Mathf.Abs(sampled.position.y - start.y) > 1.3f) continue;
                start = sampled.position; found = true; break;
            }
            if (!found) return false;
            bool pathFound = false;
            for (float distance = 15; distance >= 5; distance -= 2.5f)
            {
                if (!NavMesh.SamplePosition(start + direction * distance, out sampled, 2f, NavMesh.AllAreas) ||
                    Mathf.Abs(sampled.position.y - start.y) > 2f) continue;
                if (NavMesh.CalculatePath(start, sampled.position, NavMesh.AllAreas, route) &&
                    route.status == NavMeshPathStatus.PathComplete && route.corners.Length >= 2)
                { pathFound = true; break; }
            }
            if (!pathFound) return false;
            corners = route.corners;
            gameObject.SetActive(true);
            controller.enabled = false;
            transform.position = start + Vector3.up * .05f;
            transform.rotation = Quaternion.LookRotation(direction);
            controller.enabled = true;
            corner = 1; speed = travelSpeed; opacity = 0; blockedFor = navGapDistance = 0;
            SpawnedAt = Time.time; Lifetime = duration; fadeStarted = -1;
            lastPosition = transform.position; Velocity = direction * speed;
            Live = true; Active = this;if(!live.Contains(this))live.Add(this); AudioEvents = 0;
            animator.Rebind(); animator.Update(0);
            ApplyAppearance(.5f);
            if (spawnSound != null) audioSource.PlayOneShot(spawnSound, .35f);
            particles.Emit(18);
            SoundEventBus.Publish(new SoundEvent(transform.position, 1.1f, PlayerSoundType.Landing, Time.time, transform));
            return true;
        }

        void Update()
        {
            if (!Live)
            {
                if (fadeStarted < 0) return;
                float t = Mathf.Clamp01((Time.time - fadeStarted) / .42f);
                ApplyAppearance(1 - t);
                if (t >= 1) gameObject.SetActive(false);
                return;
            }
            if (ownerHealth == null || ownerHealth.CurrentHealth <= 0 || Time.time - SpawnedAt >= Lifetime)
            { Dissolve(false); return; }
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (corner >= corners.Length) { Dissolve(false); return; }
            Vector3 target = corners[corner];
            Vector3 horizontal = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            if (horizontal.sqrMagnitude < .22f * .22f)
            {
                corner++;
                if (corner >= corners.Length) { Dissolve(false); return; }
                horizontal = Vector3.ProjectOnPlane(corners[corner] - transform.position, Vector3.up);
            }
            Vector3 heading = horizontal.normalized;
            if (heading.sqrMagnitude > 0.1f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(heading), 540f * dt);
            if (Time.time >= nextDoor)
            {
                nextDoor = Time.time + .15f;
                foreach (var door in doors)
                {
                    if (door == null || Vector3.Dot(door.doorway.center-transform.position,heading)<-.5f) continue;
                    Vector3 chest=transform.position+Vector3.up;
                    if ((door.doorway.ClosestPoint(chest)-chest).sqrMagnitude<=25f) door.RequestOpenFrom(transform.position);
                }
            }
            // A route may contain an off-mesh link or become carved by a new barrier.
            // Never step into empty space just because its next corner is reachable on paper.
            if (dt > 0)
            {
                NavMeshHit onMesh, ahead, edge;
                Vector3 step = transform.position + heading * speed * dt;
                if (!NavMesh.SamplePosition(transform.position, out onMesh, .9f, NavMesh.AllAreas) ||
                    !NavMesh.SamplePosition(step, out ahead, .65f, NavMesh.AllAreas) ||
                    Mathf.Abs(ahead.position.y - onMesh.position.y) > .65f)
                { Dissolve(false); return; }
                if (NavMesh.Raycast(onMesh.position,ahead.position,out edge,NavMesh.AllAreas))
                {
                    // The campus has short NavMesh seams on physically connected entrance ramps.
                    // Cross only a small, grounded seam with the real capsule; walls still collide.
                    navGapDistance+=speed*dt;
                    int groundMask=~((1<<6)|(1<<7)|(1<<8)|(1<<9)|(1<<10));
                    if(navGapDistance>.8f || !Physics.Raycast(step+Vector3.up*.7f,Vector3.down,out var floor,1.3f,groundMask,QueryTriggerInteraction.Ignore)
                        || floor.normal.y<.67f || Mathf.Abs(floor.point.y-onMesh.position.y)>.65f)
                    {Dissolve(false);return;}
                }
                else navGapDistance=0;
            }
            Vector3 before = transform.position;
            controller.Move(heading * speed * dt + Vector3.down * (controller.isGrounded ? 3f : 12f) * dt);
            Velocity = (transform.position - before) / Mathf.Max(.001f, dt);
            blockedFor = Vector3.ProjectOnPlane(transform.position - lastPosition, Vector3.up).sqrMagnitude < .015f * .015f
                ? blockedFor + dt : 0;
            lastPosition = transform.position;
            if (blockedFor > 1.15f) { Dissolve(false); return; }
            animator.SetFloat(MoveSpeed, speed, .12f, dt);
            animator.SetFloat(RunRate, Mathf.Clamp(speed / 4.6f, .7f, 2.1f));
            if (Time.time >= nextStep)
            {
                nextStep = Time.time + .34f;
                if (footstep != null) audioSource.PlayOneShot(footstep, .55f);
                SoundEventBus.Publish(new SoundEvent(transform.position, 1.35f, PlayerSoundType.Run, Time.time, transform));
                AudioEvents++;
                if (AudioEvents % 2 == 0) particles.Emit(1);
            }
            ApplyAppearance(.5f + .5f * Mathf.Clamp01((Time.time - SpawnedAt) / .2f));
        }

        void ApplyAppearance(float value)
        {
            opacity = value;
            block.SetColor(GhostColor, Color.Lerp(new Color(.13f,.39f,.88f), new Color(.48f,.22f,.88f),
                .5f + .5f * Mathf.Sin(Time.time * 7f)));
            block.SetFloat(GhostAlpha, .72f * value);
            foreach (var renderer in renderers) renderer.SetPropertyBlock(block);
        }

        public void Dissolve(bool exposed)
        {
            if (!Live) return;
            Live = false;live.Remove(this); if (Active == this) Active = live.Count>0?live[live.Count-1]:null;
            Dissolved?.Invoke(transform.position);
            fadeStarted = Time.time;
            Velocity = Vector3.zero;
            animator.SetFloat(MoveSpeed, 0);
            particles.Emit(exposed ? 24 : 10);
            if (expireSound != null) audioSource.PlayOneShot(expireSound, exposed ? .5f : .3f);
        }

        void OnDisable() { live.Remove(this);if (Active == this) Active = live.Count>0?live[live.Count-1]:null; Live = false; }
        void OnDestroy()
        {
            if (ownedMaterials != null) foreach (var material in ownedMaterials) if (material != null) Destroy(material);
            if (footstep != null) Destroy(footstep);
        }
    }
}
