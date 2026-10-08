using UnityEngine;
using UnityEngine.AI;
using CampusRift.Skills;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    public enum SkyStrikeKind{Feather,Meteor}
    // Six gameplay slots. Ring/body renderers toggle only at stage boundaries; emission uses shared pools.
    public sealed class SkyStrikePool:MonoBehaviour
    {
        sealed class Strike
        {
            public Transform body;public MeshRenderer rock,feather;public LineRenderer ink,ring,trailInk,trail;
            public SpriteRenderer scorch;public Vector3 target,start;public SkyStrikeKind kind;public float age,warn,radius,damage,expires;public int stage;
            public FireBreathProfile profile;public MaterialPropertyBlock tint=new MaterialPropertyBlock();
        }
        readonly Strike[] pool=new Strike[6];
        ParticleSystem sparks,smoke,flash,flame;AudioSource voice;AudioClip falling,impact,flap;
        Mesh featherMesh,rockMesh,craterMesh;float trailClock;FireEffectQuality appliedQuality;SkyStrikeRibbonBatch ribbon;
        public int ActiveCount{get;private set;}
        public int Impacts{get;private set;}
        public int CreatedCount=>pool.Length;
        public float LastPlayerDamage{get;private set;}
        public Vector3[] Targets{get{var result=new Vector3[ActiveCount];int i=0;foreach(var s in pool)if(s.stage>0)result[i++]=s.target;return result;}}
        void Awake()
        {
            featherMesh=FeatherMesh();rockMesh=RockMesh();craterMesh=CraterMesh();
            var batch=new GameObject("Six sky strike meteor trails");batch.transform.SetParent(transform,false);ribbon=batch.AddComponent<SkyStrikeRibbonBatch>();
            var rockMaterial=Resources.Load<Material>("P14/meteor");var featherMaterial=Resources.Load<Material>("P14/feather");
            for(int i=0;i<pool.Length;i++)
            {
                var root=new GameObject("Pooled sky strike "+i);root.transform.SetParent(transform,false);
                var s=new Strike();var body=new GameObject("Spinning fire body");body.transform.SetParent(root.transform,false);s.body=body.transform;
                s.rock=MeshObject(s.body,"Molten rock",rockMesh,rockMaterial);s.feather=MeshObject(s.body,"Flaming feather",featherMesh,featherMaterial);
                s.ink=FireVisualFactory.Line(root.transform,"Telegraph dark rim",.38f,new Color(.055f,.005f,.003f,1),false);
                s.ring=FireVisualFactory.Line(root.transform,"Telegraph hot core",.11f,new Color(4,1.8f,.1f,1),true);
                foreach(var line in new[]{s.ink,s.ring}){line.loop=true;line.positionCount=48;line.widthCurve=AnimationCurve.Constant(0,1,1);line.numCapVertices=0;}
                s.trailInk=FireVisualFactory.Line(root.transform,"Falling dark trail",.9f,new Color(.12f,.015f,.005f,.85f),false);
                s.trail=FireVisualFactory.Line(root.transform,"Falling hot trail",.45f,new Color(4,1,.04f,1),true);
                foreach(var line in new[]{s.trailInk,s.trail}){line.positionCount=5;line.numCapVertices=0;line.widthCurve=new AnimationCurve(new Keyframe(0,.7f),new Keyframe(.12f,1),new Keyframe(.5f,.4f),new Keyframe(1,0));}
                var mark=new GameObject("Impact scorch",typeof(SpriteRenderer));mark.transform.SetParent(root.transform,false);s.scorch=mark.GetComponent<SpriteRenderer>();s.scorch.sprite=Resources.Load<Sprite>("P13/scorch");s.scorch.sharedMaterial=Resources.Load<Material>("P13/scorch-alpha");s.scorch.transform.rotation=Quaternion.Euler(90,0,0);
                Hide(s);pool[i]=s;
            }
            sparks=FireVisualFactory.Particles(transform,"Sky strike molten fragments","spark",true,0,.3f,.75f,Color.white);
            var debris=sparks.GetComponent<ParticleSystemRenderer>();debris.renderMode=ParticleSystemRenderMode.Mesh;debris.mesh=rockMesh;debris.sharedMaterial=rockMaterial;
            var debrisMain=sparks.main;debrisMain.gravityModifier=1.3f;debrisMain.startRotation3D=true;debrisMain.startRotationX=new ParticleSystem.MinMaxCurve(0,6.28f);debrisMain.startRotationY=new ParticleSystem.MinMaxCurve(0,6.28f);
            var spin=sparks.rotationOverLifetime;spin.enabled=true;spin.x=3;spin.y=5;spin.z=2;
            smoke=FireVisualFactory.Particles(transform,"Rock and feather short charcoal smoke","smoke",false,0,1.5f,.9f,new Color(.075f,.045f,.03f,.5f));
            var growth=smoke.sizeOverLifetime;growth.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.2f,1),new Keyframe(1,1.15f)));
            flash=FireVisualFactory.Particles(transform,"Compact comic sky impact","flame",true,0,3,.16f,new Color(5,2,.2f));
            flame=FireVisualFactory.Particles(transform,"Falling fire","flame",true,0,.9f,.45f,new Color(4,.8f,.02f));
            // Limit screen coverage even when a strike lands at the player's feet.
            smoke.GetComponent<ParticleSystemRenderer>().maxParticleSize=.085f;flash.GetComponent<ParticleSystemRenderer>().maxParticleSize=.11f;flame.GetComponent<ParticleSystemRenderer>().maxParticleSize=.065f;
            ApplyBudget();foreach(var p in new[]{sparks,smoke,flash,flame})p.Play();
            voice=gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=0;voice.volume=.4f;
            var mixer=SettingsManager.Instance?.Mixer;if(mixer!=null){var groups=mixer.FindMatchingGroups("SFX");if(groups.Length>0)voice.outputAudioMixerGroup=groups[0];}
            falling=Resources.Load<AudioClip>("P14/feather-fall");impact=Resources.Load<AudioClip>("P14/meteor-impact");flap=Resources.Load<AudioClip>("P14/wing-flap");
        }
        static MeshRenderer MeshObject(Transform parent,string name,Mesh mesh,Material material)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return r;}
        static void Hide(Strike s){s.rock.enabled=s.feather.enabled=s.ink.enabled=s.ring.enabled=s.trail.enabled=s.trailInk.enabled=s.scorch.enabled=false;s.stage=0;}
        public static bool ResolveSurface(Vector3 candidate,SkyStrikeKind kind,out Vector3 surface)
        {
            surface=candidate;
            // The topmost solid environment surface catches the meteor. It can never fall through a roof.
            if(kind==SkyStrikeKind.Meteor&&Physics.Raycast(candidate+Vector3.up*170,Vector3.down,out var hit,220,ShelterDetector.EnvironmentMask,QueryTriggerInteraction.Ignore)&&hit.normal.y>.35f){surface=hit.point+Vector3.up*.08f;return ShelterDetector.Evaluate(surface+Vector3.up*.3f)==Shelter.Outdoor;}
            if(!NavMesh.SamplePosition(candidate,out var nav,4,NavMesh.AllAreas)||ShelterDetector.AtFeet(nav.position)!=Shelter.Outdoor)return false;
            surface=nav.position+Vector3.up*.08f;return true;
        }
        public bool Launch(Vector3 candidate,SkyStrikeKind kind,SkyBeastController source,FireBreathProfile profile)
        {
            if(profile==null||!ResolveSurface(candidate,kind,out var point))return false;
            foreach(var s in pool)if(s.stage==0)
            {
                s.target=point;s.kind=kind;s.profile=profile;s.warn=kind==SkyStrikeKind.Feather?1.2f:1.5f;s.radius=kind==SkyStrikeKind.Feather?1.6f:3;
                s.damage=profile.recommendedHealth*(kind==SkyStrikeKind.Feather?.12f:.15f);s.age=0;s.stage=1;ActiveCount++;
                s.start=point+Vector3.up*(kind==SkyStrikeKind.Feather?28:42)+new Vector3(-3,0,-4);
                s.scorch.enabled=false;
                s.rock.GetComponent<MeshFilter>().sharedMesh=rockMesh;
                s.tint.Clear();s.tint.SetFloat("_Opacity",1);s.tint.SetFloat("_Charred",0);s.rock.SetPropertyBlock(s.tint);s.feather.SetPropertyBlock(s.tint);
                for(int n=0;n<48;n++){float a=n*Mathf.PI*2/48;var p=point+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*s.radius;s.ink.SetPosition(n,p);s.ring.SetPosition(n,p+Vector3.up*.015f);}
                s.ink.enabled=s.ring.enabled=true;
                voice.PlayOneShot(kind==SkyStrikeKind.Feather?flap:falling,.55f);return true;
            }
            return false;
        }
        public static bool CanHitPlayer(Vector3 feet,Vector3 surface,float radius)
        {
            if(ShelterDetector.AtFeet(feet)!=Shelter.Outdoor||Mathf.Abs(feet.y-surface.y)>2)return false;
            if(Vector3.ProjectOnPlane(feet-surface,Vector3.up).sqrMagnitude>radius*radius)return false;
            return !Physics.Linecast(surface+Vector3.up*.4f,feet+Vector3.up, ShelterDetector.EnvironmentMask,QueryTriggerInteraction.Ignore);
        }
        void Impact(Strike s)
        {
            s.stage=3;s.rock.enabled=s.feather.enabled=s.trail.enabled=s.trailInk.enabled=s.ink.enabled=s.ring.enabled=false;s.expires=Time.time+3;Impacts++;
            s.scorch.transform.position=s.target+Vector3.up*.015f;s.scorch.transform.localScale=Vector3.one*(s.kind==SkyStrikeKind.Meteor?6:2);s.scorch.color=Color.white;s.scorch.enabled=true;
            if(s.kind==SkyStrikeKind.Meteor){s.rock.GetComponent<MeshFilter>().sharedMesh=craterMesh;s.body.position=s.target+Vector3.up*.025f;s.body.rotation=Quaternion.Euler(0,Random.Range(0,360),0);s.body.localScale=Vector3.one*1.4f;s.tint.SetFloat("_Charred",1);s.rock.SetPropertyBlock(s.tint);s.rock.enabled=true;}
            for(int i=0;i<FireVisualQuality.Choose(16,12,8);i++){float a=i*2.399963f;float speed=Random.Range(2,4);sparks.Emit(new ParticleSystem.EmitParams{position=s.target+Vector3.up*.2f,velocity=new Vector3(Mathf.Cos(a)*speed,Random.Range(2,4.5f),Mathf.Sin(a)*speed),startSize=Random.Range(.09f,.19f),startLifetime=Random.Range(.5f,.9f)},1);}
            for(int i=0;i<FireVisualQuality.Choose(8,6,4);i++){var spread=Random.insideUnitCircle*1.1f;smoke.Emit(new ParticleSystem.EmitParams{position=s.target+new Vector3(spread.x,.2f,spread.y),velocity=Vector3.up*.9f,startSize=s.kind==SkyStrikeKind.Meteor?1.5f:.8f,startLifetime=.9f},1);}
            if(!(UI.SettingsManager.Instance?.Current.ReduceSkillFlashes??false)) flash.Emit(new ParticleSystem.EmitParams{position=s.target+Vector3.up*.4f,startSize=s.kind==SkyStrikeKind.Meteor?3.2f:1.6f,startLifetime=.16f},FireVisualQuality.Choose(4,3,2));
            voice.PlayOneShot(s.kind==SkyStrikeKind.Meteor?impact:falling,s.kind==SkyStrikeKind.Meteor?.8f:.35f);
            var player=FireBreathCycle.Instance?.Player;LastPlayerDamage=0;
            if(player!=null&&!player.IsDead&&CanHitPlayer(player.transform.position,s.target,s.radius))
            {float hp=player.CurrentHealth;FireBreathCycle.DamagePlayer(player,s.damage,s.kind==SkyStrikeKind.Feather?"long-vu-hoa":"mua-thien-thach");LastPlayerDamage=hp-player.CurrentHealth;}
            if(player!=null&&Vector3.Distance(player.transform.position,s.target)<20)
            {float strength=s.kind==SkyStrikeKind.Meteor?.10f:.045f;var comic=player.GetComponent<SkillImpact>();if(comic!=null)comic.Pulse(strength);else player.GetComponent<GiantHandCameraImpulse>()?.Pulse(strength);}
            if(s.kind==SkyStrikeKind.Feather)FireBreathCycle.Instance?.Ground.SpawnSmallAt(s.target,s.profile);
        }
        public void Advance(float dt)
        {
            trailClock+=dt;bool emit=trailClock>=.05f;if(emit)trailClock=0;
            bool ribbonVisible=false;var camera=Camera.main;
            for(int slot=0;slot<pool.Length;slot++)
            {
                var s=pool[slot];ribbon.Hide(slot);
                if(s.stage==0)continue;s.age+=dt;
                if(s.stage==1)
                {
                    float pulse=1+Mathf.Sin(s.age*12)*.12f;s.ring.widthMultiplier=.11f*pulse;
                    // Falling bodies share the warning window, reaching the mark exactly when it ends.
                    float t=Mathf.Clamp01(s.age/s.warn);var position=Vector3.Lerp(s.start,s.target,t*t);
                    var incoming=(s.target-s.start).normalized;float near=camera!=null?Mathf.Clamp(Vector3.Distance(camera.transform.position,position)/8,.2f,1):1;
                    s.body.position=position;
                    s.body.rotation=s.kind==SkyStrikeKind.Feather?Quaternion.LookRotation(incoming,camera!=null?camera.transform.position-position:Vector3.forward)*Quaternion.Euler(0,0,Mathf.Sin(s.age*11)*22):Quaternion.Euler(s.age*130,s.age*260,s.age*170);
                    s.body.localScale=Vector3.one*(s.kind==SkyStrikeKind.Feather?.9f:.95f)*near;
                    s.rock.enabled=s.kind==SkyStrikeKind.Meteor;s.feather.enabled=s.kind==SkyStrikeKind.Feather;
                    s.trail.enabled=s.trailInk.enabled=s.kind==SkyStrikeKind.Feather;
                    var tail=position-incoming*(s.kind==SkyStrikeKind.Meteor?8:3)*near;
                    if(s.kind==SkyStrikeKind.Meteor){ribbon.Set(slot,position,tail,camera!=null?camera.transform.position:position+Vector3.forward);ribbonVisible=true;}
                    else for(int n=0;n<5;n++){float f=n/4f;var p=Vector3.Lerp(position,tail,f)+s.body.right*Mathf.Sin(s.age*14-f*5)*f*.35f*near;s.trail.SetPosition(n,p);s.trailInk.SetPosition(n,p);s.trail.widthMultiplier=.25f*near;s.trailInk.widthMultiplier=.45f*near;}
                    if(emit){flame.Emit(new ParticleSystem.EmitParams{position=position,startSize=(s.kind==SkyStrikeKind.Meteor?1.2f:.65f)*near,velocity=-incoming*2,startLifetime=.45f},FireVisualQuality.Mobile?1:2);if(s.kind==SkyStrikeKind.Meteor)smoke.Emit(new ParticleSystem.EmitParams{position=position+Vector3.up*2,velocity=Vector3.up,startSize=.8f*near,startLifetime=.7f},FireVisualQuality.Mobile?1:2);}
                    if(s.age>=s.warn){Impact(s);ribbon.Hide(slot);}
                }
                else if(s.stage==3){float left=s.expires-Time.time;float fade=Mathf.Clamp01(left/1.5f);s.scorch.color=new Color(1,1,1,fade);s.tint.SetFloat("_Opacity",fade);s.rock.SetPropertyBlock(s.tint);if(left<=0){Hide(s);ActiveCount--;}}
            }
            ribbon.Upload(ribbonVisible);
        }
        void ApplyBudget(){appliedQuality=FireVisualQuality.Current;foreach(var p in new[]{sparks,smoke,flash,flame}){var m=p.main;m.maxParticles=FireVisualQuality.Choose(128,96,64);}}
        void Update(){if(FireBreathCycle.Instance!=null&&FireBreathCycle.Instance.CinematicPaused)return;if(appliedQuality!=FireVisualQuality.Current)ApplyBudget();Advance(Time.deltaTime);}
        public void Clear(){foreach(var s in pool)if(s!=null)Hide(s);ActiveCount=0;if(ribbon!=null)ribbon.Upload(false);foreach(var p in new[]{sparks,smoke,flash,flame})if(p!=null)p.Clear();}
        void OnDisable()=>Clear();
        void OnDestroy(){Destroy(featherMesh);Destroy(rockMesh);Destroy(craterMesh);}
        static Mesh CraterMesh()
        {
            var mesh=new Mesh{name="Shallow charred impact crater"};var v=new Vector3[25];var uv=new Vector2[25];var tri=new System.Collections.Generic.List<int>(120);
            for(int row=0;row<3;row++)for(int c=0;c<8;c++)
            {
                int i=row*8+c;float a=c*Mathf.PI/4,r=(row==0?1:row==1?.7f:.4f)*(1+.07f*Mathf.Sin(c*3.7f));
                v[i]=new Vector3(Mathf.Cos(a)*r,row==1?.12f:row==2?-.02f:0,Mathf.Sin(a)*r);uv[i]=new Vector2(c/8f,row*.5f);
                int j=row*8+(c+1)%8;if(row<2)tri.AddRange(new[]{i,i+8,j,j,i+8,j+8});else tri.AddRange(new[]{i,24,j});
            }
            v[24]=Vector3.down*.03f;mesh.vertices=v;mesh.uv=uv;mesh.triangles=tri.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh FeatherMesh()
        {
            var mesh=new Mesh{name="Curved barbed flame feather"};var v=new Vector3[34];var uv=new Vector2[34];var tri=new int[96];
            for(int i=0;i<17;i++){float t=i/16f,w=Mathf.Sin(t*Mathf.PI)*.23f*(i%2==0?1:.79f),curve=Mathf.Sin(t*Mathf.PI)*.22f;v[i*2]=new Vector3(curve-w,.15f*Mathf.Sin(t*Mathf.PI),t*2.4f-1.2f);v[i*2+1]=new Vector3(curve+w,.15f*Mathf.Sin(t*Mathf.PI)+w*.2f,t*2.4f-1.2f);uv[i*2]=new Vector2(0,t);uv[i*2+1]=new Vector2(1,t);if(i<16){int n=i*6,a=i*2;tri[n]=a;tri[n+1]=a+2;tri[n+2]=a+1;tri[n+3]=a+1;tri[n+4]=a+2;tri[n+5]=a+3;}}
            mesh.vertices=v;mesh.uv=uv;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh RockMesh()
        {
            var mesh=new Mesh{name="Original faceted molten rock"};var v=new Vector3[26];var uv=new Vector2[26];var tri=new System.Collections.Generic.List<int>(144);
            v[24]=Vector3.up;v[25]=Vector3.down;
            for(int row=0;row<3;row++)for(int c=0;c<8;c++){int i=row*8+c;float a=c*Mathf.PI/4;v[i]=new Vector3(Mathf.Cos(a),row*.65f-.65f,Mathf.Sin(a))*(.7f+.15f*Mathf.Sin(i*3.7f));uv[i]=new Vector2(c/8f,row*.5f);if(row<2){int j=row*8+(c+1)%8;tri.AddRange(new[]{i,j,i+8,j,j+8,i+8});}if(row==0)tri.AddRange(new[]{25,(c+1)%8,c});if(row==2)tri.AddRange(new[]{24,i,row*8+(c+1)%8});}
            mesh.vertices=v;mesh.uv=uv;mesh.triangles=tri.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
