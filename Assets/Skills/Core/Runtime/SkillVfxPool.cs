using UnityEngine;
using UnityEngine.Rendering;
using CampusRift.Controls;

namespace CampusRift.Skills
{
    public enum SkillVfxKind { Ring, Cone, Bolt, Burst, Lotus, Ice, IceShell, Bell, Vortex, Scorch, Shard, Ghost, Slash, FireField, Smoke, Sword, ChainBolt, FireRibbon, FireBloom, Portal, Accretion, FrostMist, Repulsion, Shockwave, BellShard, FireTrailSmoke, SwordDust, ReactionBolt, CopperMark, SwordImpact }
    [DefaultExecutionOrder(250)]
    public sealed class SkillVfxPool : MonoBehaviour
    {
        public const int Capacity = 112;
        public static bool ForceMobileQuality;
        public static bool MobileQuality => ForceMobileQuality || Application.isMobilePlatform || CampusInput.Mobile || QualitySettings.GetQualityLevel()==0;
        public SkillSet1VfxConfig config;
        public int CreatedCount { get; private set; }
        public int ExhaustedCount { get; private set; }
        public int PeakParticles { get; private set; }
        public int ActiveCount { get; private set; }
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P10_BENCH)
        public long MaxFrameGCBytes {get;private set;}
#endif
        public int ParticleCount { get { int n=speedForce!=null?speedForce.LiveParticleCount:0;if(nodes!=null)for(int i=0;i<nodes.Length;i++)if(nodes[i].Live)n+=nodes[i].particles.particleCount;return n; } }
        public int ObjectCount { get; private set; }
        public bool Warmed => nodes != null;
        public SkillSet2VisualBatch Shapes {get;private set;}
        Node[] nodes;
        Mesh crystal, lotus, bell, sphere,disc,sword,annulus,bellFragment,impactStar;
        SpeedForceVFX speedForce;
        static readonly AnimationCurve Taper= new AnimationCurve(new Keyframe(0,0),new Keyframe(.13f,1),new Keyframe(.85f,1),new Keyframe(1,0));
        static readonly AnimationCurve Crescent=new AnimationCurve(new Keyframe(0,0),new Keyframe(.15f,.4f),new Keyframe(.5f,1),new Keyframe(.85f,.4f),new Keyframe(1,0));
        static readonly AnimationCurve Uniform=AnimationCurve.Constant(0,1,1);
        static readonly AnimationCurve FlipbookFrames=AnimationCurve.Linear(0,0,1,1);
        CampusRift.Combat.FlyingSword[] rainSwords;
        public int RainSwordCount=>rainSwords!=null?rainSwords.Length:0;
        public int LiveRainSwordCount {get{int count=0;if(rainSwords!=null)for(int i=0;i<rainSwords.Length;i++)if(rainSwords[i].gameObject.activeSelf)count++;return count;}}
        public bool FiniteState
        {get{if(nodes!=null)for(int i=0;i<nodes.Length;i++)if(nodes[i].Live&&(!Set1SkillRuntime.Finite(nodes[i].Position)||!Set1SkillRuntime.Finite(nodes[i].End)||float.IsNaN(nodes[i].Radius)))return false;if(rainSwords!=null)for(int i=0;i<rainSwords.Length;i++)if(!Set1SkillRuntime.Finite(rainSwords[i].transform.position))return false;return true;}}
        public bool LaunchRain(SwordRainRuntime caster,Vector3 point)
        {
            if(rainSwords==null)return false;
            for(int i=0;i<rainSwords.Length;i++)if(!rainSwords[i].gameObject.activeSelf){rainSwords[i].LaunchRain(caster,point);return true;}
            ExhaustedCount++;return false;
        }
        SkinnedMeshRenderer[] rig;
        readonly Vector3[] lineBuffer=new Vector3[65],coneBuffer=new Vector3[35],boltBuffer=new Vector3[17],slashBuffer=new Vector3[9],runes12=new Vector3[12],runes24=new Vector3[24],runes56=new Vector3[56];
        Camera viewCamera;
        readonly ParticleSystem.Particle[] orbitParticles=new ParticleSystem.Particle[64];
        public sealed class Node
        {
            internal GameObject root;
            internal Transform model;
            internal MeshFilter filter;
            internal MeshRenderer surface, ink;
            internal LineRenderer line, core, detail;
            internal TrailRenderer ribbon;
            internal ParticleSystem particles;
            internal AudioSource sound, loop;
            internal MaterialPropertyBlock block=new MaterialPropertyBlock();
            internal SkillVfxKind kind;
            internal float age, duration, seed, emissionTimer;
            internal Color smokeTint;
            internal Mesh[] ghostMeshes;
            internal Mesh combinedGhost;
            internal CombineInstance[] ghostCombine;
            internal MeshRenderer[] ghostRenderers;
            internal MeshRenderer[] ghostInk;
            internal Transform[] ghostTransforms;
            public bool Live { get; internal set; }
            public Vector3 Position, End;
            public Color Color;
            public float Radius=1, Progress=1, Crack;
            public Transform Follow;
            public Vector3 Offset;
            public float Opacity=1;internal float dimUntil;
            public void Fade(float seconds=.5f){duration=age+seconds;Follow=null;}
            public void StopLoop(){loop.Stop();}
        }
        AR.ARCombatContext ar;float Scale=>ar!=null?ar.scale:1;float SessionNow=>ar!=null?ar.Now:Time.time;
        void Awake(){ar=GetComponent<AR.ARCombatContext>();WarmUp();}
        public void WarmUp()
        {
            if(nodes!=null)return;
            if(config==null)config=Resources.Load<SkillSet1VfxConfig>("SkillSet1Vfx");
            if(config==null){Debug.LogError("P10 VFX config missing: run Install Skill Set 1.");enabled=false;return;}
            if(Resources.Load<Material>("P18/geometry")!=null){var batch=new GameObject("P18 pooled geometry");batch.transform.SetParent(transform,false);Shapes=batch.AddComponent<SkillSet2VisualBatch>();}
            crystal=SkillVfxMeshes.Crystal();lotus=SkillVfxMeshes.Lotus();bell=SkillVfxMeshes.Bell();sphere=SkillVfxMeshes.Sphere();disc=SkillVfxMeshes.Disc();
            annulus=SkillVfxMeshes.Annulus();
            bellFragment=SkillVfxMeshes.BellFragment();impactStar=SkillVfxMeshes.ImpactStar();
            if(config.rainConfig!=null)sword=CampusRift.Combat.FlyingSword.BladeMesh(config.rainConfig);
            var explorer=GetComponent<CampusExplorer>();
            viewCamera=explorer!=null&&explorer.followCamera!=null?explorer.followCamera:Camera.main;
            var afterimage=GetComponent<CharacterAfterimageTrail>();
            rig=afterimage!=null&&afterimage.sources!=null&&afterimage.sources.Length>0?afterimage.sources:explorer!=null && explorer.characterAnimator!=null?explorer.characterAnimator.GetComponentsInChildren<SkinnedMeshRenderer>():new SkinnedMeshRenderer[0];
            nodes=new Node[Capacity];
            for(int i=0;i<Capacity;i++)
            {
                var n=new Node();nodes[i]=n;
                n.root=new GameObject("P10 pooled VFX "+i);n.root.transform.SetParent(transform,false);
                n.model=new GameObject("Comic model").transform;n.model.SetParent(n.root.transform,false);
                n.filter=n.model.gameObject.AddComponent<MeshFilter>();n.surface=n.model.gameObject.AddComponent<MeshRenderer>();n.surface.sharedMaterial=config.surface;NoShadow(n.surface);
                var ink=new GameObject("Ink hull");ink.transform.SetParent(n.model,false);ink.AddComponent<MeshFilter>().sharedMesh=crystal;
                n.ink=ink.AddComponent<MeshRenderer>();n.ink.sharedMaterial=config.ink;NoShadow(n.ink);
                n.line=Line(n.root.transform,"Glow",config.additive,.10f);n.core=Line(n.root.transform,"White core",config.additive,.028f);n.detail=Line(n.root.transform,"Runes and speed marks",config.additive,.045f);
                n.ribbon=n.root.AddComponent<TrailRenderer>();n.ribbon.sharedMaterial=config.fireRibbon;n.ribbon.time=.28f;n.ribbon.widthMultiplier=.42f;n.ribbon.widthCurve=Taper;n.ribbon.minVertexDistance=.12f;n.ribbon.emitting=false;n.ribbon.enabled=false;NoShadow(n.ribbon);
                var pg=new GameObject("Particles");pg.transform.SetParent(n.root.transform,false);
                n.particles=pg.AddComponent<ParticleSystem>();n.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=n.particles.main;main.playOnAwake=false;main.loop=true;main.duration=1;main.maxParticles=64;main.startLifetime=new ParticleSystem.MinMaxCurve(.25f,.65f);main.startSize=new ParticleSystem.MinMaxCurve(.06f,.18f);main.startSpeed=new ParticleSystem.MinMaxCurve(Scale,4*Scale);main.simulationSpace=ParticleSystemSimulationSpace.World;
                var em=n.particles.emission;em.enabled=false;
                var shape=n.particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.15f*Scale;
                var fade=n.particles.colorOverLifetime;fade.enabled=true;
                var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
                var size=n.particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
                var pr=pg.GetComponent<ParticleSystemRenderer>();pr.sharedMaterial=config.particles;NoShadow(pr);
                n.sound=n.root.AddComponent<AudioSource>();n.loop=n.root.AddComponent<AudioSource>();ConfigureAudio(n.sound);ConfigureAudio(n.loop);n.loop.loop=true;
                n.sound.outputAudioMixerGroup=n.loop.outputAudioMixerGroup=config.audioOutput;
                if(i<4)BuildGhost(n);
                n.root.SetActive(false);CreatedCount++;
            }
            speedForce=GetComponent<SpeedForceVFX>();if(speedForce!=null)speedForce.WarmUp();
            if(config.rainConfig!=null)
            {
                rainSwords=new CampusRift.Combat.FlyingSword[50];var combat=GetComponent<CampusRift.Combat.PlayerCombat>();
                for(int i=0;i<rainSwords.Length;i++){var go=new GameObject("P10 pooled rain sword "+i);go.transform.SetParent(transform,false);rainSwords[i]=go.AddComponent<CampusRift.Combat.FlyingSword>();rainSwords[i].PrepareRain(combat,i%3,config.rainConfig);}
            }
            ObjectCount=GetComponentsInChildren<Transform>(true).Length;
        }
        static void NoShadow(Renderer r){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;}
        void ConfigureAudio(AudioSource a){a.playOnAwake=false;a.spatialBlend=.75f;a.rolloffMode=AudioRolloffMode.Linear;a.minDistance=3*Scale;a.maxDistance=35*Scale;a.volume=.4f;}
        static LineRenderer Line(Transform parent,string name,Material material,float width)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=material;l.useWorldSpace=true;l.widthMultiplier=width;l.positionCount=65;l.numCapVertices=2;l.enabled=false;NoShadow(l);return l;
        }
        void BuildGhost(Node n)
        {
            n.ghostMeshes=new Mesh[rig.Length];n.ghostCombine=new CombineInstance[rig.Length];n.ghostRenderers=new MeshRenderer[1];n.ghostInk=new MeshRenderer[1];n.ghostTransforms=new Transform[1];
            for(int j=0;j<rig.Length;j++)
            {
                n.ghostMeshes[j]=new Mesh{name="P10 reusable afterimage source"};n.ghostMeshes[j].MarkDynamic();n.ghostCombine[j].mesh=n.ghostMeshes[j];
            }
            n.combinedGhost=new Mesh{name="P10 combined outlined afterimage"};n.combinedGhost.indexFormat=IndexFormat.UInt32;n.combinedGhost.MarkDynamic();
            var g=new GameObject("Baked afterimage");g.transform.SetParent(n.root.transform,false);n.ghostTransforms[0]=g.transform;g.AddComponent<MeshFilter>().sharedMesh=n.combinedGhost;
            var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=config.ghost;NoShadow(r);n.ghostRenderers[0]=r;
            var hull=new GameObject("Ghost ink edge");hull.transform.SetParent(g.transform,false);hull.AddComponent<MeshFilter>().sharedMesh=n.combinedGhost;n.ghostInk[0]=hull.AddComponent<MeshRenderer>();n.ghostInk[0].sharedMaterial=config.ink;NoShadow(n.ghostInk[0]);
        }
        public Node Spawn(SkillVfxKind kind,Vector3 position,Color color,float duration,float radius=1,AudioClip clip=null,AudioClip loop=null,Color? smokeTint=null)
        {
            if(nodes==null)return null;
            int start=kind==SkillVfxKind.Ghost?0:4,limit=kind==SkillVfxKind.Ghost?4:nodes.Length;
            for(int i=start;i<limit;i++)if(!nodes[i].Live)
            {
                // Fit complete circular effects before rendering; positions and radii are world units.
                float worldRadius=radius*Scale;
                if(ar!=null && kind!=SkillVfxKind.Bolt && kind!=SkillVfxKind.ChainBolt && kind!=SkillVfxKind.ReactionBolt)
                {
                    worldRadius=ar.VisualRadius(worldRadius);
                    position=ar.ConstrainVisual(position,worldRadius*(kind==SkillVfxKind.Portal?1.15f:1));
                }
                var n=nodes[i];if(ar!=null&&ar.CameraEffect==n.root.transform)ar.CameraEffect=null;n.Live=true;n.kind=kind;n.Position=position;n.End=position;n.Color=color;n.Radius=worldRadius;n.Progress=1;n.Crack=0;n.Opacity=1;n.dimUntil=0;n.age=0;n.duration=Mathf.Max(.05f,duration);n.Follow=null;n.Offset=Vector3.zero;n.seed=i*.917f;n.emissionTimer=0;n.smokeTint=smokeTint??Color.white;
                n.root.SetActive(true);n.root.transform.position=position;n.root.transform.rotation=Quaternion.identity;n.root.transform.localScale=Vector3.one/(ar!=null?Scale:1);
                n.model.localPosition=Vector3.zero;n.model.localRotation=Quaternion.identity;n.model.localScale=Vector3.one;
                Mesh mesh=kind==SkillVfxKind.Sword?sword:kind==SkillVfxKind.Scorch||kind==SkillVfxKind.CopperMark?disc:kind==SkillVfxKind.Lotus?lotus:kind==SkillVfxKind.Bell?bell:kind==SkillVfxKind.Vortex||kind==SkillVfxKind.Burst||kind==SkillVfxKind.FireBloom||kind==SkillVfxKind.IceShell?sphere:crystal;
                if(kind==SkillVfxKind.Accretion)mesh=annulus;
                if(kind==SkillVfxKind.SwordImpact)mesh=impactStar;
                if(kind==SkillVfxKind.BellShard)mesh=bellFragment;
                n.filter.sharedMesh=mesh;n.ink.GetComponent<MeshFilter>().sharedMesh=mesh;
                n.surface.sharedMaterial=kind==SkillVfxKind.Scorch?config.groundMark:config.surface;
                if(kind==SkillVfxKind.Lotus)n.surface.sharedMaterial=config.lotusSurface;
                if(kind==SkillVfxKind.Accretion)n.surface.sharedMaterial=config.accretion;
                if(kind==SkillVfxKind.CopperMark)n.surface.sharedMaterial=config.copperMark;
                if(kind==SkillVfxKind.SwordImpact)n.surface.sharedMaterial=config.impactStar;
                bool model=kind==SkillVfxKind.Sword||kind==SkillVfxKind.Scorch||kind==SkillVfxKind.Lotus||kind==SkillVfxKind.Ice||kind==SkillVfxKind.IceShell||kind==SkillVfxKind.Bell||kind==SkillVfxKind.Vortex||kind==SkillVfxKind.Shard||kind==SkillVfxKind.Burst||kind==SkillVfxKind.FireBloom;
                n.surface.enabled=model;n.ink.enabled=model && kind!=SkillVfxKind.Burst && kind!=SkillVfxKind.FireBloom && kind!=SkillVfxKind.Vortex&&kind!=SkillVfxKind.IceShell&&kind!=SkillVfxKind.Scorch;
                if(kind==SkillVfxKind.BellShard)n.surface.enabled=n.ink.enabled=true;
                if(kind==SkillVfxKind.Accretion||kind==SkillVfxKind.CopperMark||kind==SkillVfxKind.SwordImpact)n.surface.enabled=true;
                if(kind==SkillVfxKind.FireBloom)n.surface.enabled=n.ink.enabled=false;
                n.surface.sortingOrder=0;n.ink.sortingOrder=0;
                n.line.enabled=false;n.core.enabled=false;n.detail.enabled=false;
                n.detail.sharedMaterial=config.stroke;n.detail.sortingOrder=-2;n.line.sortingOrder=0;n.core.sortingOrder=1;
                n.line.sharedMaterial=n.core.sharedMaterial=kind==SkillVfxKind.Slash||kind==SkillVfxKind.ChainBolt?config.solidEmission:config.additive;
                n.line.sharedMaterial=kind==SkillVfxKind.Slash?config.layeredSlash:kind==SkillVfxKind.ChainBolt?config.layeredWide:kind==SkillVfxKind.Bolt?config.layeredSmall:config.layeredRing;
                if(kind==SkillVfxKind.ChainBolt)n.line.sharedMaterial=config.layeredLightning;
                if(kind==SkillVfxKind.Slash)n.line.sharedMaterial=config.slashStroke;
                if(kind==SkillVfxKind.ReactionBolt)n.line.sharedMaterial=config.layeredWide;
                n.line.textureMode=LineTextureMode.Stretch;
                n.line.widthCurve=n.core.widthCurve=n.detail.widthCurve=kind==SkillVfxKind.Slash||kind==SkillVfxKind.ChainBolt?Taper:Uniform;
                if(kind==SkillVfxKind.ReactionBolt)n.line.widthCurve=Taper;
                n.particles.Clear();var main=n.particles.main;main.startColor=kind==SkillVfxKind.FireBloom?Color.white:new Color(color.r*1.4f,color.g*1.4f,color.b*1.4f,1);main.startLifetime=new ParticleSystem.MinMaxCurve(.25f,kind==SkillVfxKind.FireBloom?.35f:.65f);main.startSize=new ParticleSystem.MinMaxCurve((kind==SkillVfxKind.FireBloom?.9f:.06f)*Scale,(kind==SkillVfxKind.FireBloom?1.4f:.18f)*Scale);
                var sheet=n.particles.textureSheetAnimation;sheet.enabled=kind==SkillVfxKind.FireField||kind==SkillVfxKind.Smoke||kind==SkillVfxKind.FireBloom;sheet.numTilesX=sheet.numTilesY=4;sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,FlipbookFrames);
                n.particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=kind==SkillVfxKind.FireBloom?config.fireBillow:kind==SkillVfxKind.FireField?config.fireFlipbook:kind==SkillVfxKind.Smoke?config.smokeFlipbook:config.particles;
                var particleRenderer=n.particles.GetComponent<ParticleSystemRenderer>();particleRenderer.sortingOrder=0;particleRenderer.renderMode=kind==SkillVfxKind.Vortex?ParticleSystemRenderMode.Mesh:ParticleSystemRenderMode.Billboard;
                if(kind==SkillVfxKind.Vortex){particleRenderer.mesh=crystal;particleRenderer.sharedMaterial=config.vortexDebris;}
                if(kind==SkillVfxKind.FrostMist||kind==SkillVfxKind.FireTrailSmoke){sheet.enabled=true;n.particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=kind==SkillVfxKind.FrostMist?config.frostMist:config.smokeFlipbook;}
                n.ribbon.Clear();n.ribbon.enabled=n.ribbon.emitting=kind==SkillVfxKind.FireRibbon;n.ribbon.startColor=n.ribbon.endColor=color;
                n.sound.Stop();n.loop.Stop();if(clip!=null){n.sound.pitch=kind==SkillVfxKind.Vortex?.6f:1; n.sound.PlayOneShot(clip,.7f);}if(loop!=null){n.loop.clip=loop;n.loop.pitch=.75f;n.loop.volume=.16f;n.loop.Play();}
                if(kind==SkillVfxKind.Ghost){for(int j=0;j<rig.Length;j++)
                {rig[j].BakeMesh(n.ghostMeshes[j]);n.ghostCombine[j].transform=n.root.transform.worldToLocalMatrix*rig[j].transform.localToWorldMatrix;}n.combinedGhost.CombineMeshes(n.ghostCombine,true,true);n.ghostRenderers[0].enabled=true;}
                ActiveCount++;if(ar!=null)ar.VfxStarted();return n;
            }
            ExhaustedCount++;return null;
        }
        public void Release(Node n)
        {
            if(n==null||!n.Live)return;n.Live=false;n.Follow=null;n.sound.Stop();n.loop.Stop();n.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);n.root.SetActive(false);ActiveCount--;
        }
        public void Burst(Vector3 point,Color color,float size=1,AudioClip sound=null,bool ring=true)
        {
            var n=Spawn(SkillVfxKind.Burst,point,color,.7f,size,sound);if(n!=null)Emit(n,MobileQuality?16:48);
            if(ring){var wave=Spawn(SkillVfxKind.Ring,point-Vector3.up*(.4f*Scale),color,.65f,size*2);if(wave!=null)wave.Progress=0;}
        }
        public void Emit(Node n,int count)
        {
            if(n==null||!n.Live)return;int allowed=Mathf.Min(count,(MobileQuality?400:1500)-ParticleCount);if(allowed>0){if(!n.particles.isPlaying)n.particles.Play();n.particles.Emit(allowed);}
        }
        public void Fragments(Vector3 center,Color color,int count,float size=.2f,SkillVfxKind kind=SkillVfxKind.Shard,bool priority=false)
        {
            for(int i=0;i<count;i++){var n=Spawn(kind,center,color,.65f,size);if(n!=null){n.End=(Random.onUnitSphere*Random.Range(1,3)+Vector3.up*1.2f)*Scale;if(priority)Priority(n,true);}}
        }
        void LateUpdate()
        {
            if(nodes==null||ar!=null&&ar.Paused)return;
            bool reduced=UI.SettingsManager.Instance?.Current.ReduceSkillFlashes??false;
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P10_BENCH)
            long gcBefore=System.GC.GetAllocatedBytesForCurrentThread();
#endif
            for(int i=0;i<nodes.Length;i++)
            {
                var n=nodes[i];if(!n.Live)continue;n.age+=ar!=null?ar.battlefield.CombatDelta:Time.deltaTime;if(n.age>=n.duration){Release(n);continue;}
                if(n.Follow!=null)n.Position=n.Follow.position+n.Offset;
                n.root.transform.position=n.Position;
                float fade=Mathf.Clamp01((n.duration-n.age)/.5f)*n.Opacity,f=n.age/n.duration;
                if(n.kind==SkillVfxKind.Ice&&n.dimUntil>SessionNow)fade*=.16f;
                if(n.kind==SkillVfxKind.Scorch||n.kind==SkillVfxKind.CopperMark){n.block.Clear();n.block.SetColor("_MarkColor",n.kind==SkillVfxKind.CopperMark?n.Color:n.Color.maxColorComponent>.5f?Dark(n.Color):n.Color);n.block.SetFloat("_MarkAlpha",fade*.85f);n.surface.SetPropertyBlock(n.block);}
                else if(n.surface.enabled||n.ink.enabled||n.kind==SkillVfxKind.Ghost){
                n.block.Clear();n.block.SetColor("_BaseColor",n.kind==SkillVfxKind.Vortex?new Color(.006f,.002f,.012f,1):n.kind==SkillVfxKind.Scorch?Dark(n.Color):n.Color);
                n.block.SetColor("_Emission",n.Color*(n.kind==SkillVfxKind.Lotus?1+4*n.Progress:1.6f)*(reduced?.25f:1));n.block.SetFloat("_Alpha",fade*(n.kind==SkillVfxKind.Bell?.32f:n.kind==SkillVfxKind.IceShell?.38f:.85f));n.block.SetFloat("_Crack",n.Crack);n.block.SetFloat("_Progress",n.Progress);n.block.SetFloat("_Distortion",n.kind==SkillVfxKind.Vortex&&!MobileQuality?.008f:0);
                n.block.SetFloat("_LotusGlow",n.kind==SkillVfxKind.Lotus?1:0);
                if(n.loop.isPlaying)n.loop.volume=.16f*fade;
                n.block.SetFloat("_FlatGlow",n.kind==SkillVfxKind.Sword?1:0);n.block.SetFloat("_ImpactRim",n.kind==SkillVfxKind.Burst||n.kind==SkillVfxKind.FireBloom?1:0);n.block.SetFloat("_BellMetal",n.kind==SkillVfxKind.Bell?1:0);n.block.SetFloat("_OpaqueCore",n.kind==SkillVfxKind.Vortex?1:0);if(n.kind==SkillVfxKind.Vortex)n.block.SetFloat("_Alpha",fade);
                if(n.surface.enabled)n.surface.SetPropertyBlock(n.block);if(n.ink.enabled)n.ink.SetPropertyBlock(n.block);
                }
                switch(n.kind)
                {
                    case SkillVfxKind.Ghost:
                        n.block.SetColor("_GhostColor",Color.Lerp(n.Color,new Color(.6f,.13f,1),f)*1.2f);n.block.SetFloat("_GhostAlpha",.5f*fade);for(int j=0;j<n.ghostRenderers.Length;j++){n.ghostRenderers[j].SetPropertyBlock(n.block);n.ghostInk[j].SetPropertyBlock(n.block);}break;
                    case SkillVfxKind.FireRibbon:var ribbonColor=n.Color;ribbonColor.a=fade;n.ribbon.startColor=n.ribbon.endColor=ribbonColor;Stream(n,MobileQuality?8:20);break;
                    case SkillVfxKind.FireBloom:FireBillows(n,fade);break;
                    case SkillVfxKind.Portal:Orbit(n,n.Radius,fade,12);n.line.startColor=n.line.endColor=n.Color*n.Progress;n.core.startColor=n.core.endColor=n.Color*n.Progress;n.line.widthMultiplier*=Mathf.Sqrt(n.Progress);break;
                    case SkillVfxKind.Ring: Circle(n,n.Position,n.Radius*(n.Progress==0?Mathf.Clamp01(n.age/.25f):1),fade);break;
                    case SkillVfxKind.Shockwave:Circle(n,n.Position,n.Radius*Mathf.Clamp01(n.age/.55f),Mathf.Clamp01((n.duration-n.age)/.3f));break;
                    case SkillVfxKind.Cone: Cone(n,fade);break;
                    case SkillVfxKind.Bolt: Bolt(n,fade);break;
                    case SkillVfxKind.ChainBolt: Bolt(n,n.age<.15f?1:Mathf.Clamp01((n.duration-n.age)/.15f));break;
                    case SkillVfxKind.ReactionBolt:Bolt(n,n.age<.10f?1:Mathf.Clamp01((n.duration-n.age)/.14f));break;
                    case SkillVfxKind.Sword: n.model.localScale=Vector3.one*n.Radius*fade;n.model.localRotation=Quaternion.Euler(-80,90,n.age*20);break;
                    case SkillVfxKind.Slash: Slash(n);break;
                    case SkillVfxKind.Lotus:
                        n.model.localRotation=Quaternion.Euler(0,n.age*140,0);n.model.localScale=new Vector3(.5f+.5f*n.Progress,1.4f-.5f*n.Progress,.5f+.5f*n.Progress)*n.Radius;
                        Orbit(n,.75f*n.Radius,fade,6);LotusParticles(n);break;
                    case SkillVfxKind.Ice:
                        n.model.localScale=new Vector3(n.Radius,Mathf.SmoothStep(0,n.Radius,Mathf.Clamp01(n.age/.16f)),n.Radius)*fade;
                        n.model.localRotation=Quaternion.Euler(-10,n.seed*120,15);break;
                    case SkillVfxKind.IceShell:
                        n.model.localScale=new Vector3(.62f,1.05f,.58f)*Scale;n.model.localPosition=Vector3.up*(1.02f*Scale);Circle(n,n.Position,.65f*Scale,fade*.6f);Stream(n,MobileQuality?5:12);break;
                    case SkillVfxKind.Bell:
                        n.model.localScale=Vector3.one*n.Radius;n.model.localPosition=Vector3.up*Mathf.Lerp(.8f,0,Mathf.Clamp01(n.age/.25f))*Scale;Orbit(n,n.Radius*1.03f,fade,12);break;
                    case SkillVfxKind.Accretion:
                        n.model.localScale=Vector3.one*n.Radius*Mathf.SmoothStep(0,1,Mathf.Clamp01(n.age/.25f));n.model.localRotation=Quaternion.Euler(18,n.age*45,24);break;
                    case SkillVfxKind.Vortex:
                        n.model.localScale=Vector3.one*n.Radius*Mathf.SmoothStep(0,1,Mathf.Clamp01(n.age/.25f))*(n.Progress==0?Mathf.Clamp01((n.duration-n.age)/.45f):1);VortexSpirals(n,fade);VortexParticles(n);break;
                    case SkillVfxKind.Repulsion:RepulsionParticles(n);break;
                    case SkillVfxKind.CopperMark:
                        n.model.localScale=Vector3.one*n.Radius;n.model.localPosition=Vector3.up*(.025f*Scale);break;
                    case SkillVfxKind.Scorch:
                        n.model.localScale=Vector3.one*n.Radius;n.model.localPosition=Vector3.up*(.02f*Scale);
                        Circle(n,n.Position,n.Radius,fade*.4f);break;
                    case SkillVfxKind.FireField: FieldParticles(n,fade,false);break;
                    case SkillVfxKind.Smoke: FieldParticles(n,fade,true);break;
                    case SkillVfxKind.FrostMist: FieldParticles(n,fade,true);break;
                    case SkillVfxKind.FireTrailSmoke:TrailSmoke(n,fade);break;
                    case SkillVfxKind.Shard:
                    case SkillVfxKind.BellShard:
                        n.model.localScale=Vector3.one*n.Radius*fade;n.model.localPosition=n.End*n.age+Vector3.down*(n.age*n.age*4*Scale);n.model.localRotation=Quaternion.Euler(n.age*260,n.seed*80,n.age*190);break;
                    case SkillVfxKind.SwordImpact:
                        n.surface.enabled=!reduced&&n.age<.09f;n.model.localRotation=viewCamera!=null?viewCamera.transform.rotation:Quaternion.identity;n.model.localScale=Vector3.one*n.Radius*(.6f+Mathf.Clamp01(n.age/.06f)*.8f);
                        Circle(n,n.Position-Vector3.up*(.17f*Scale),Mathf.Clamp01(n.age/.24f)*1.15f*Scale,fade*.85f);n.line.sharedMaterial=config.layeredWide;n.line.widthMultiplier=.14f*Scale;break;
                    case SkillVfxKind.Burst:
                        n.model.localScale=Vector3.one*n.Radius*(.6f+Mathf.Clamp01(n.age/.08f)*.4f);n.surface.enabled=!reduced&&n.age<.066f;
                        n.ink.enabled=false;n.block.SetColor("_BaseColor",Color.white);n.block.SetColor("_Emission",Color.white*4);if(n.surface.enabled)n.surface.SetPropertyBlock(n.block);break;
                }
            }
            PeakParticles=Mathf.Max(PeakParticles,ParticleCount);
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P10_BENCH)
            MaxFrameGCBytes=System.Math.Max(MaxFrameGCBytes,System.GC.GetAllocatedBytesForCurrentThread()-gcBefore);
#endif
        }
        // Reaction passes draw after the triggering skill; reset all render state on reuse.
        public Node Priority(Node n,bool debris=false)
        {
            if(n==null)return null;n.surface.sortingOrder=n.line.sortingOrder=n.core.sortingOrder=20;
            if(n.kind==SkillVfxKind.ReactionBolt||n.kind==SkillVfxKind.ChainBolt||n.kind==SkillVfxKind.Bolt)n.line.sharedMaterial=config.reactionStroke;
            else if(n.surface.enabled)n.surface.sharedMaterial=debris?config.reactionIce:config.reactionSurface;
            n.particles.GetComponent<ParticleSystemRenderer>().sortingOrder=20;return n;
        }
        public void DimIce(Vector3 center,float radius,float seconds)
        {for(int i=0;i<nodes.Length;i++){var n=nodes[i];if(n.Live&&n.kind==SkillVfxKind.Ice&&(n.Position-center).sqrMagnitude<radius*radius)n.dimUntil=Mathf.Max(n.dimUntil,SessionNow+seconds);}}
        void FireBillows(Node n,float fade)
        {
            // Rising lobes, narrow stem then spreading crown: never a shield/sphere mesh.
            n.surface.enabled=n.ink.enabled=false;n.emissionTimer+=Time.deltaTime*(MobileQuality?24:52);int count=(int)n.emissionTimer;n.emissionTimer-=count;
            if(count>0&&!n.particles.isPlaying)n.particles.Play();
            for(int i=0;i<count&&ParticleCount<(MobileQuality?400:1500);i++)
            {
                float a=Random.value*Mathf.PI*2,spread=n.Radius*Mathf.Lerp(.18f,.65f,Mathf.Clamp01(n.age/.35f));
                var e=new ParticleSystem.EmitParams();e.position=n.Position+new Vector3(Mathf.Cos(a)*spread,n.age*n.Radius*2,Mathf.Sin(a)*spread);
                e.velocity=new Vector3(Mathf.Cos(a)*.7f,2.8f,Mathf.Sin(a)*.7f);e.startSize=n.Radius*Random.Range(.55f,.9f);e.startLifetime=.45f;e.startColor=new Color(1,1,1,fade*.68f);n.particles.Emit(e,1);
            }
        }
        void Stream(Node n,float rate){n.emissionTimer+=Time.deltaTime*rate;int count=(int)n.emissionTimer;if(count>0){n.emissionTimer-=count;Emit(n,count);}}
        void FieldParticles(Node n,float fade,bool smoke)
        {
            bool frost=n.kind==SkillVfxKind.FrostMist;
            n.emissionTimer+=Time.deltaTime*(frost?(MobileQuality?8:18):(MobileQuality?32:72))*fade;int count=(int)n.emissionTimer;n.emissionTimer-=count;
            count=Mathf.Min(count,(MobileQuality?400:1500)-ParticleCount);
            if(count>0&&!n.particles.isPlaying)n.particles.Play();
            for(int i=0;i<count;i++)
            {
                float a=Random.value*Mathf.PI*2,r=n.Radius*(smoke?Random.value:i%3==0?Random.value:1);
                var e=new ParticleSystem.EmitParams();e.position=n.Position+new Vector3(Mathf.Cos(a)*r,.15f*Scale,Mathf.Sin(a)*r);e.velocity=Vector3.up*((smoke?.75f:1.1f)*Scale);
                e.startSize=(frost?Random.Range(.7f,1.2f):smoke?Random.Range(.9f,1.6f):Random.Range(.8f,1.6f))*Scale;e.startLifetime=frost?.8f:smoke?1.2f:1;e.startColor=frost?new Color(.65f,.9f,1,fade*.3f):new Color(n.smokeTint.r,n.smokeTint.g,n.smokeTint.b,fade*(smoke?.45f:1));n.particles.Emit(e,1);
            }
        }
        void Circle(Node n,Vector3 center,float radius,float alpha)
        {
            var tilt=n.kind==SkillVfxKind.Vortex?Quaternion.Euler(18,0,24):Quaternion.identity;
            for(int i=0;i<65;i++){float a=i/64f*Mathf.PI*2;lineBuffer[i]=center+tilt*new Vector3(Mathf.Cos(a)*radius,.04f*Scale,Mathf.Sin(a)*radius);}Draw(n.line,65,n.Color,alpha,n.kind==SkillVfxKind.Shockwave?.75f:n.kind==SkillVfxKind.Vortex?.38f:n.kind==SkillVfxKind.Portal?.24f:.15f);if(n.kind==SkillVfxKind.Shockwave)n.line.sharedMaterial=config.layeredWide;n.line.SetPositions(lineBuffer);
        }
        void Cone(Node n,float fade)
        {
            Vector3 dir=(n.End-n.Position).normalized;if(dir.sqrMagnitude<.01f)dir=Vector3.forward;coneBuffer[0]=n.Position+Vector3.up*(.04f*Scale);
            for(int i=0;i<=32;i++)coneBuffer[i+1]=n.Position+Quaternion.AngleAxis(-45+i*90f/32,Vector3.up)*dir*n.Radius+Vector3.up*(.04f*Scale);
            coneBuffer[34]=coneBuffer[0];Draw(n.line,35,n.Color,fade,.19f);n.line.SetPositions(coneBuffer);
        }
        void Bolt(Node n,float fade)
        {
            Vector3 dir=n.End-n.Position;Vector3 side=Vector3.Cross(dir.normalized,Vector3.up);const int count=17;
            bool reaction=n.kind==SkillVfxKind.ReactionBolt;
            float phase=(Time.frameCount/2)*1.913f+n.seed;
            for(int i=0;i<count;i++){float t=i/(count-1f),jitter=n.Progress<0?Mathf.Sin(t*Mathf.PI*2-n.age*8)*.2f:Mathf.Sin(i*9.4f+phase);boltBuffer[i]=Vector3.Lerp(n.Position,n.End,t)+side*jitter*Mathf.Sin(t*Mathf.PI)*((n.kind==SkillVfxKind.ChainBolt?.65f:.23f)*Scale)+Vector3.up*(n.kind==SkillVfxKind.ChainBolt?Mathf.Sin(t*Mathf.PI)*(.5f+.4f*Mathf.Sin(i*13.4f+phase))*Scale:0);}
            float coreWidth=.055f*n.Radius;
            if(n.kind==SkillVfxKind.ChainBolt||reaction){var cam=viewCamera;coreWidth=cam!=null?Vector3.Distance(cam.transform.position,(n.Position+n.End)*.5f)*Mathf.Tan(cam.fieldOfView*Mathf.Deg2Rad*.5f)*2*.038f:.55f;coreWidth*=n.Radius/Scale;}
            Draw(n.line,count,n.Color,fade,reaction?coreWidth*1.85f:n.kind==SkillVfxKind.ChainBolt?coreWidth*1.65f:.42f*n.Radius/Scale);n.line.SetPositions(boltBuffer);
        }
        void Orbit(Node n,float radius,float fade,int marks)
        {
            Circle(n,n.Position+Vector3.up*((n.kind==SkillVfxKind.Bell?.55f:0)*Scale),radius,fade*.6f);
            int count=Mathf.Min(64,marks*2);Draw(n.core,count,n.Color,fade,.065f);
            var buffer=marks==6?runes12:marks==12?runes24:runes56;
            for(int i=0;i<count;i++){float a=i/(float)count*Mathf.PI*2+n.age*(n.kind==SkillVfxKind.Vortex?3:1);float r=radius*(i%2==0?.8f:1.1f);buffer[i]=n.Position+new Vector3(Mathf.Cos(a)*r,((n.kind==SkillVfxKind.Bell?.85f:.15f)+Mathf.Sin(a*3)*.16f)*Scale,Mathf.Sin(a)*r);}n.core.SetPositions(buffer);
        }
        public static Color Dark(Color c)
        {if(c.r>.8f&&c.g>.5f&&c.b>.16f)return new Color(.12f,.055f,.012f);if(c.b>c.r*.8f)return c.r>c.g?new Color(.055f,.007f,.1f):new Color(.008f,.035f,.09f);return c.g<c.r*.5f?new Color(.16f,.015f,.006f):new Color(.09f,.012f,.12f);}
        void Slash(Node n)
        {
            float grow=n.Progress,fade=n.age<.23f?1:n.age<.3f?Mathf.Lerp(1,0,(n.age-.23f)/.07f):.18f*Mathf.Clamp01((n.duration-n.age)/(n.duration-.3f));
            Vector3 end=Vector3.Lerp(n.Position,n.End,grow);
            var cam=viewCamera;float width=.75f;
            if(cam!=null)width=Mathf.Max(width,Vector3.Distance(cam.transform.position,(n.Position+n.End)*.5f)*Mathf.Tan(cam.fieldOfView*Mathf.Deg2Rad*.5f)*2*.038f);
            if(n.age>=.3f){n.line.enabled=n.core.enabled=n.detail.enabled=false;Stream(n,MobileQuality?15:40);n.particles.transform.position=Vector3.Lerp(n.Position,n.End,Random.value);return;}
            Draw(n.line,9,n.Color,fade,width*1.85f);
            Vector3 side=Vector3.Cross((end-n.Position).normalized,Vector3.up);
            n.line.widthCurve=Crescent;
            for(int i=0;i<9;i++){float t=i/8f;slashBuffer[i]=Vector3.Lerp(n.Position,end,t)+side*Mathf.Sin(t*Mathf.PI)*2.2f;}n.line.SetPositions(slashBuffer);
            // Short electric teeth along each edge, drawn by the two already warmed lines.
            for(int edge=0;edge<2;edge++){
                var line=edge==0?n.core:n.detail;line.sharedMaterial=config.layeredSmall;Draw(line,12,new Color(.7f,.15f,1),fade,.12f);
                for(int i=0;i<12;i++){float t=(i/2+1)/7f;var p=Vector3.Lerp(n.Position,end,t)+side*(Mathf.Sin(t*Mathf.PI)*2.2f+(edge==0?-1:1)*width*.65f);runes12[i]=p+(i%2==0?Vector3.zero:Vector3.up*(.18f+.35f*Mathf.Sin(i*2.6f+n.age*15)));}line.SetPositions(runes12);
            }
            if(n.age>.23f){Stream(n,MobileQuality?15:40);n.particles.transform.position=Vector3.Lerp(n.Position,n.End,Random.value);}
        }
        float VortexScale=>ar!=null?Mathf.Min(Scale,ar.battlefield.placement.Radius*.85f/9):Scale;
        void VortexParticles(Node n)
        {
            n.emissionTimer+=Time.deltaTime*(MobileQuality?18:36);int count=(int)n.emissionTimer;n.emissionTimer-=count;
            int cap=MobileQuality?400:1500;count=Mathf.Min(count,cap-ParticleCount);
            if(count>0&&!n.particles.isPlaying)n.particles.Play();
            for(int i=0;i<count;i++)
            {
                float a=Random.value*Mathf.PI*2;Vector3 radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var e=new ParticleSystem.EmitParams();e.position=n.Position+(Vector3.down*1.65f+radial*Random.Range(6,9))*VortexScale;e.velocity=(-radial*3+Vector3.Cross(Vector3.up,radial)*8+Vector3.up*.35f)*VortexScale;e.startLifetime=2.5f;e.startSize=Random.Range(.3f,.55f)*VortexScale;e.startColor=Random.value<.35f?new Color(.2f,.08f,.3f,1):new Color(1,.2f,.8f,1);n.particles.Emit(e,1);
            }
            int live=n.particles.GetParticles(orbitParticles);for(int i=0;i<live;i++){Vector3 delta=Vector3.ProjectOnPlane(orbitParticles[i].position-n.Position,Vector3.up);float distance=delta.magnitude;var radial=distance>.01f?delta/distance:Vector3.right;orbitParticles[i].velocity=-radial*(2*VortexScale+distance*.65f)+(Vector3.Cross(Vector3.up,radial)*8+Vector3.up*.35f)*VortexScale;}n.particles.SetParticles(orbitParticles,live);
        }
        void LotusParticles(Node n)
        {
            n.emissionTimer+=Time.deltaTime*(MobileQuality?12:30);int count=(int)n.emissionTimer;n.emissionTimer-=count;
            if(count>0&&!n.particles.isPlaying)n.particles.Play();
            for(int i=0;i<count&&ParticleCount<(MobileQuality?400:1500);i++)
            {float a=Random.value*Mathf.PI*2;var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var e=new ParticleSystem.EmitParams();e.position=n.Position+radial*1.2f;e.velocity=-radial*1.7f+Vector3.Cross(Vector3.up,radial)*3;e.startLifetime=.65f;e.startSize=.08f;e.startColor=new Color(1,.5f,.05f,1);n.particles.Emit(e,1);}
            int live=n.particles.GetParticles(orbitParticles);for(int i=0;i<live;i++){Vector3 d=Vector3.ProjectOnPlane(orbitParticles[i].position-n.Position,Vector3.up);var radial=d.sqrMagnitude>.001f?d.normalized:Vector3.right;orbitParticles[i].velocity=-radial*1.7f+Vector3.Cross(Vector3.up,radial)*3;}n.particles.SetParticles(orbitParticles,live);
        }
        void TrailSmoke(Node n,float fade)
        {
            n.emissionTimer+=Time.deltaTime*(MobileQuality?8:18);int count=(int)n.emissionTimer;n.emissionTimer-=count;
            if(count>0&&!n.particles.isPlaying)n.particles.Play();
            for(int i=0;i<count&&ParticleCount<(MobileQuality?400:1500);i++){var e=new ParticleSystem.EmitParams();e.position=n.Position;e.velocity=Vector3.up*.35f;e.startLifetime=.45f;e.startSize=Random.Range(.3f,.6f);e.startColor=new Color(.12f,.07f,.09f,fade*.22f);n.particles.Emit(e,1);}
        }
        void VortexSpirals(Node n,float fade)
        {
            for(int arm=0;arm<3;arm++)
            {
                var line=arm==0?n.line:arm==1?n.core:n.detail;line.sharedMaterial=config.layeredSmall;
                float head=Mathf.Repeat(n.age*.65f+arm/3f,1),tail=Mathf.Max(0,head-.18f);
                for(int i=0;i<21;i++){float t=Mathf.Lerp(tail,head,i/20f),r=Mathf.Lerp(8.5f,1.4f,t)*VortexScale,a=t*Mathf.PI*3+arm*Mathf.PI*2/3;lineBuffer[i]=n.Position+new Vector3(Mathf.Cos(a)*r,(-1.65f+t*1.3f)*VortexScale,Mathf.Sin(a)*r);}
                Draw(line,21,new Color(.85f,.1f,1),fade*.65f,.16f);line.SetPositions(lineBuffer);
            }
        }
        void RepulsionParticles(Node n)
        {
            if(n.Progress<0)return;n.Progress=-1;
            int count=MobileQuality?24:60;
            if(!n.particles.isPlaying)n.particles.Play();
            for(int i=0;i<count&&ParticleCount<(MobileQuality?400:1500);i++)
            {float a=i/(float)count*Mathf.PI*2;var e=new ParticleSystem.EmitParams();e.position=n.Position+Vector3.up*(.3f*Scale);e.velocity=new Vector3(Mathf.Cos(a),.1f,Mathf.Sin(a))*15*Scale;e.startLifetime=.6f;e.startSize=.35f*Scale;e.startColor=new Color(1,.2f,.85f,1);n.particles.Emit(e,1);}
        }
        void Draw(LineRenderer l,int count,Color color,float alpha,float width){l.enabled=true;l.positionCount=count;l.widthMultiplier=width*Scale;color.a=alpha;l.startColor=l.endColor=color;}
        public void ResetMetrics(){PeakParticles=ParticleCount;ExhaustedCount=0;
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P10_BENCH)
            MaxFrameGCBytes=0;
#endif
        }
        public void Clear(){if(nodes!=null)for(int i=0;i<nodes.Length;i++)Release(nodes[i]);if(rainSwords!=null)for(int i=0;i<rainSwords.Length;i++)if(rainSwords[i]!=null)rainSwords[i].gameObject.SetActive(false);if(Shapes!=null)Shapes.Clear();}
        void OnDisable(){Clear();}
        void OnDestroy(){if(nodes!=null)for(int i=0;i<nodes.Length;i++)if(nodes[i].ghostMeshes!=null){for(int j=0;j<nodes[i].ghostMeshes.Length;j++)Destroy(nodes[i].ghostMeshes[j]);Destroy(nodes[i].combinedGhost);}Destroy(crystal);Destroy(lotus);Destroy(bell);Destroy(sphere);Destroy(disc);Destroy(sword);Destroy(annulus);Destroy(bellFragment);Destroy(impactStar);}
    }
}
