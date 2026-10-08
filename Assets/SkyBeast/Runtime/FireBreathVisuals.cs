using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    // Cosmetic pools only. FireBreathCycle and BurningGround own all damage and timing.
    public sealed class FireBreathVisuals:MonoBehaviour
    {
        sealed class Meteor{public Vector3 impact,start;public float age,duration,nextSmoke;public bool active;}
        sealed class Mark{public SpriteRenderer sprite;public float expires;}
        sealed class MouthJet{public GameObject root;public Mesh cone;public Vector3[] vertices;public ParticleSystem flame,smoke;public float clock;}
        readonly Meteor[] meteors=new Meteor[72];
        readonly Mark[] marks=new Mark[24];
        readonly Vector3[] plumePoints=new Vector3[6];
        readonly List<Vector3> targets=new List<Vector3>(64);
        readonly List<AudioLowPassFilter> dragonFilters=new List<AudioLowPassFilter>();
        readonly Dictionary<SkyBeastController,MouthJet> jets=new Dictionary<SkyBeastController,MouthJet>();
        SkyBeastController[] dragons=System.Array.Empty<SkyBeastController>();
        FireBreathCycle cycle;ParticleSystem sparks,smoke,flash,billows,embers,plumes;
        AudioSource alarm,fire,roar;AudioLowPassFilter[] filters;
        float spawnClock,shakeClock,emberClock,plumeClock,nextTargets,nextDragons;
        FireEffectQuality appliedQuality;FireMeteorBatch meteorBatch;
        Vector3 targetCenter;int markCursor,plumeCount;
        public int LiveMeteors{get;private set;}
        public int SpawnedMeteors{get;private set;}
        public int ImpactCount{get;private set;}
        public int ActiveScorches{get;private set;}
        public int PlumeCount=>Mathf.Min(plumeCount,FireVisualQuality.Choose(6,4,3));
        public int Budget=>FireVisualQuality.Choose(72,48,36);
        public FireEffectQuality Quality=>FireVisualQuality.Current;
        public int ParticleCount=>sparks.particleCount+smoke.particleCount+flash.particleCount+billows.particleCount+embers.particleCount+plumes.particleCount;
        public float LowPassHz=>filters!=null&&filters.Length>0?filters[0].cutoffFrequency:0;
        void Awake()
        {
            cycle=GetComponent<FireBreathCycle>();
            var batchRoot=new GameObject("Pooled meteor ribbon batch");batchRoot.transform.SetParent(transform,false);
            meteorBatch=batchRoot.AddComponent<FireMeteorBatch>();
            for(int i=0;i<meteors.Length;i++)meteors[i]=new Meteor();
            for(int i=0;i<marks.Length;i++)
            {
                var go=new GameObject("Pooled meteor scorch "+i,typeof(SpriteRenderer));go.transform.SetParent(transform,false);
                go.transform.rotation=Quaternion.Euler(90,0,0);go.transform.localScale=Vector3.one*3.2f;
                var sr=go.GetComponent<SpriteRenderer>();sr.sprite=Resources.Load<Sprite>("P13/scorch");sr.sharedMaterial=Resources.Load<Material>("P13/scorch-alpha");sr.enabled=false;
                marks[i]=new Mark{sprite=sr};
            }
            sparks=FireVisualFactory.Particles(transform,"Impact sparks","spark",true,0,.32f,.8f,new Color(4,1,.07f));
            smoke=FireVisualFactory.Particles(transform,"Black impact and trail smoke","smoke",false,0,4,3,new Color(.055f,.035f,.025f,.72f));
            flash=FireVisualFactory.Particles(transform,"Gold impact flash","flame",true,0,3,.35f,new Color(5,3,1));
            billows=FireVisualFactory.RollingFire(transform,"Rolling meteor fire",2,1);
            embers=FireVisualFactory.Particles(transform,"Airborne embers","spark",true,0,.12f,2.5f,new Color(3,.6f,.04f));
            plumes=FireVisualFactory.Particles(transform,"Distant black smoke columns","smoke",false,0,7,7,new Color(.035f,.028f,.023f,.88f));
            sparks.Play();smoke.Play();flash.Play();billows.Play();embers.Play();plumes.Play();
            alarm=Voice("Warning siren",.38f);alarm.clip=Resources.Load<AudioClip>("P13/alarm");alarm.loop=true;
            fire=Voice("Campus fire roar",.7f);fire.clip=Resources.Load<AudioClip>("P13/fire");fire.loop=true;
            roar=Voice("Distant warning roar",.42f);
            filters=new[]{alarm.gameObject.AddComponent<AudioLowPassFilter>(),fire.gameObject.AddComponent<AudioLowPassFilter>(),roar.gameObject.AddComponent<AudioLowPassFilter>()};
            cycle.WarningStarted+=Warning;cycle.BreathStarted+=Breath;cycle.BreathEnded+=Ended;
            appliedQuality=FireVisualQuality.Current;ApplyParticleBudget();
        }
        AudioSource Voice(string name,float volume)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var a=go.AddComponent<AudioSource>();a.playOnAwake=false;a.volume=volume;a.dopplerLevel=0;
            var mixer=SettingsManager.Instance?.Mixer;if(mixer!=null){var group=mixer.FindMatchingGroups("Ambient");if(group.Length==0)group=mixer.FindMatchingGroups("SFX");if(group.Length>0)a.outputAudioMixerGroup=group[0];}return a;
        }
        void Warning()
        {
            alarm.Play();CacheDragons();
            if(dragons.Length==0){var d=Resources.Load<SkyBeastDefinition>("P12/Dragon026");if(d!=null&&d.roars.Length>0)roar.PlayOneShot(d.roars[0]);}
        }
        void Breath()
        {
            alarm.Stop();fire.Play();spawnClock=0;shakeClock=0;emberClock=0;plumeClock=0;
            SpawnedMeteors=ImpactCount=0;CacheTargets();CacheDragons();
            for(int i=0;i<(FireVisualQuality.Choose(12,8,6));i++)SpawnMeteor();
            // Smoke already climbing behind the roofs at onset, without distant emitters outside 60m.
            for(int j=0;j<Mathf.Min(plumeCount,FireVisualQuality.Choose(6,4,3));j++)for(int i=0;i<8;i++)
                plumes.Emit(new ParticleSystem.EmitParams{position=plumePoints[j]+Vector3.up*(10+i*4),velocity=new Vector3(.3f,6,.1f),startSize=6+i*.45f,startLifetime=5},1);
        }
        void Ended(){}
        void CacheTargets()
        {
            var camera=Camera.main;if(camera==null)return;
            targets.Clear();targetCenter=camera.transform.position;nextTargets=Time.time+2;
            var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
            var right=Vector3.Cross(Vector3.up,forward);
            for(int i=0;i<160&&targets.Count<64;i++)
            {
                float angle=Random.Range(-95,95)*Mathf.Deg2Rad,radius=Random.Range(6,58);
                Vector3 point=targetCenter+(forward*Mathf.Cos(angle)+right*Mathf.Sin(angle))*radius;
                point.y=cycle.Player!=null?cycle.Player.transform.position.y:0;
                if(NavMesh.SamplePosition(point,out var hit,4,NavMesh.AllAreas)&&Vector3.Distance(targetCenter,hit.position)<59&&ShelterDetector.AtFeet(hit.position)==Shelter.Outdoor)targets.Add(hit.position+Vector3.up*.08f);
            }
            plumeCount=0;
            foreach(var point in targets)
            {
                if(plumeCount>=plumePoints.Length)break;
                if(Vector3.Distance(targetCenter,point)<30)continue;
                bool spaced=true;for(int j=0;j<plumeCount;j++)if((point-plumePoints[j]).sqrMagnitude<100){spaced=false;break;}
                if(spaced)plumePoints[plumeCount++]=point;
            }
        }
        bool SpawnMeteor()
        {
            if(targets.Count==0)return false;
            var camera=Camera.main;if(camera==null)return false;
            Vector3 point=targets[Random.Range(0,targets.Count)];if(Vector3.Distance(camera.transform.position,point)>60)return false;
            for(int i=0;i<Budget;i++)
            {
                var m=meteors[i];if(m.active)continue;
                m.impact=point;Vector3 incoming=new Vector3(9,30,5);
                foreach(var dragon in dragons)if(dragon!=null&&dragon.Mouth!=null){incoming=(dragon.Mouth.position-point).normalized*32;if(incoming.y<18)incoming.y=24;break;}
                m.start=point+incoming*Random.Range(.85f,1.15f);m.age=0;m.nextSmoke=0;m.duration=Random.Range(.9f,1.35f);m.active=true;LiveMeteors++;SpawnedMeteors++;return true;
            }
            return false;
        }
        void Impact(Vector3 position)
        {
            ImpactCount++;int count=FireVisualQuality.Choose(16,12,8);
            for(int i=0;i<count;i++)sparks.Emit(new ParticleSystem.EmitParams{position=position,velocity=new Vector3(Random.Range(-5,5),Random.Range(3,8),Random.Range(-5,5)),startLifetime=Random.Range(.4f,.9f)},1);
            if(!(UI.SettingsManager.Instance?.Current.ReduceSkillFlashes ?? false)) flash.Emit(new ParticleSystem.EmitParams{position=position+Vector3.up*.6f,velocity=Vector3.up*.5f,startSize=3.6f,startLifetime=.3f},1);
            billows.Emit(new ParticleSystem.EmitParams{position=position+Vector3.up*.5f,velocity=Vector3.up*2,startSize=2.1f,startLifetime=.65f},1);
            smoke.Emit(new ParticleSystem.EmitParams{position=position+Vector3.up*.4f,velocity=Vector3.up*3,startSize=4.5f,startLifetime=3},FireVisualQuality.Choose(2,1,1));
            int markBudget=FireVisualQuality.Choose(24,16,12);markCursor%=markBudget;
            var mark=marks[markCursor];markCursor=(markCursor+1)%markBudget;
            mark.sprite.transform.position=position+Vector3.up*.015f;mark.sprite.transform.rotation=Quaternion.Euler(90,Random.Range(0,360),0);mark.sprite.enabled=true;mark.expires=Time.time+3;
        }
        void CacheDragons()
        {
            nextDragons=Time.time+.5f;dragons=FindObjectsByType<SkyBeastController>(FindObjectsSortMode.None);
            foreach(var dragon in dragons)
            {
                foreach(var a in dragon.GetComponentsInChildren<AudioSource>())
                {var f=a.GetComponent<AudioLowPassFilter>();if(f==null){f=a.gameObject.AddComponent<AudioLowPassFilter>();dragonFilters.Add(f);}}
                if(dragon.Mouth==null||jets.ContainsKey(dragon))continue;
                var root=new GameObject("Rolling flame cone from Socket_Mouth");root.transform.SetParent(transform,false);
                var cone=root.AddComponent<MeshFilter>();var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>("P13/mouth-cone");renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                var mesh=new Mesh{name="Pooled rolling mouth cone"};mesh.MarkDynamic();var vertices=new Vector3[65];var uv=new Vector2[65];var triangles=new int[288];int tri=0;
                for(int row=0;row<13;row++)for(int col=0;col<5;col++)
                {int index=row*5+col;uv[index]=new Vector2(col/4f,row/12f);if(row<12&&col<4){triangles[tri++]=index;triangles[tri++]=index+5;triangles[tri++]=index+1;triangles[tri++]=index+1;triangles[tri++]=index+5;triangles[tri++]=index+6;}}
                mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;cone.sharedMesh=mesh;
                var flame=FireVisualFactory.RollingFire(root.transform,"Mouth curling fire lobes",7,1.1f);
                var dark=FireVisualFactory.Particles(root.transform,"Mouth trailing charcoal smoke","smoke",false,0,12,1.4f,new Color(.055f,.035f,.02f,.7f));
                var darkMain=dark.main;darkMain.maxParticles=FireVisualQuality.Choose(64,44,32);
                flame.Play();dark.Play();jets.Add(dragon,new MouthJet{root=root,cone=mesh,vertices=vertices,flame=flame,smoke=dark});root.SetActive(false);
            }
        }
        void LateUpdate()
        {
            // Socket transforms have followed the animated jaw and P12 flight for this frame.
            foreach(var pair in jets)
            {
                var dragon=pair.Key;var jet=pair.Value;
                bool active=cycle.IsBreathing&&dragon!=null&&dragon.isActiveAndEnabled&&dragon.Mouth!=null&&cycle.IsBreathSource(dragon);
                if(jet.root.activeSelf!=active){jet.root.SetActive(active);if(!active){jet.flame.Clear();jet.smoke.Clear();}}if(!active)continue;
                Vector3 start=dragon.Mouth.position,dir=(Vector3.down+Vector3.ProjectOnPlane(dragon.transform.forward,Vector3.up)*.4f).normalized;
                var camera=Camera.main;Vector3 toward=camera!=null?camera.transform.position-start:Vector3.right;
                // Long Nộ reaches the actual outdoor court rather than stopping 54m below a ~110m dragon.
                // Sweep/ground fire remain cosmetic; FireBreathCycle alone owns the unchanged damage.
                bool fury=cycle.IsFury;Vector3 landing=Vector3.zero,groundForward=Vector3.forward;
                if(fury)
                {
                    Vector3 feet=cycle.Player!=null?cycle.Player.transform.position:targetCenter;
                    groundForward=Vector3.ProjectOnPlane(camera!=null?camera.transform.forward:dragon.transform.forward,Vector3.up).normalized;
                    if(groundForward.sqrMagnitude<.01f)groundForward=Vector3.forward;
                    var sweep=Quaternion.Euler(0,Mathf.Sin(Time.time*1.1f)*42,0)*groundForward;
                    var aim=feet+sweep*22;
                    if(NavMesh.SamplePosition(aim,out var hit,4,NavMesh.AllAreas)&&ShelterDetector.AtFeet(hit.position)==Shelter.Outdoor)landing=hit.position;
                    else
                    {
                        landing=feet;float closest=float.MaxValue;
                        foreach(var target in targets){float d=(target-aim).sqrMagnitude;if(d<closest){closest=d;landing=target;}}
                    }
                    landing+=Vector3.up*.45f;dir=(landing-start).normalized;
                }
                Vector3 side=Vector3.Cross(dir,toward).normalized;if(side.sqrMagnitude<.01f)side=Vector3.right;
                for(int row=0;row<13;row++)
                {
                    float t=row/12f;
                    Vector3 point=fury?(t<=.8f?Vector3.Lerp(start,landing,t/.8f):landing+groundForward*((t-.8f)*60)):start+dir*(54*t);
                    point+=side*Mathf.Sin(Time.time*9-t*15)*t*(fury?.65f:1.1f);
                    float width=Mathf.Lerp(.5f,fury?19:24,Mathf.Pow(t,.8f))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.86f,1,t)));
                    for(int col=0;col<5;col++)jet.vertices[row*5+col]=jet.root.transform.InverseTransformPoint(point+side*((col/4f-.5f)*width));
                }
                jet.cone.vertices=jet.vertices;jet.cone.RecalculateBounds();
                jet.clock+=Time.deltaTime*(FireVisualQuality.Choose(64,44,32));int count=Mathf.FloorToInt(jet.clock);jet.clock-=count;
                for(int i=0;i<count;i++)
                {
                    float t=Random.Range(.04f,.9f);Vector3 point=fury?Vector3.Lerp(start,landing,t):start+dir*(54*t);
                    point+=Random.insideUnitSphere*(t*(fury?1.5f:3));
                    bool ground=fury&&i%2==0;if(ground)point=landing+side*Random.Range(-6,6)+groundForward*Random.Range(0,9);
                    jet.flame.Emit(new ParticleSystem.EmitParams{position=point,velocity=ground?Vector3.up*2:dir*12+Random.insideUnitSphere*3,startSize=fury?(ground?Random.Range(2,3.8f):Mathf.Lerp(1.4f,7,t)):Mathf.Lerp(1.4f,18,t),startLifetime=.7f,startColor=new Color(1,1,1,.9f)},1);
                    if(i%2==0)jet.smoke.Emit(new ParticleSystem.EmitParams{position=point+side*(t*(fury?2:5)),velocity=fury?Vector3.up*2:dir*8+Vector3.up*2,startSize=Mathf.Lerp(2,fury?4:14,t),startLifetime=fury?.9f:1.2f},1);
                }
            }
        }
        void Update()
        {
            if(appliedQuality!=FireVisualQuality.Current){appliedQuality=FireVisualQuality.Current;ApplyParticleBudget();}
            float storm=cycle.State==FireBreathCycle.Phase.Warning?cycle.WarningProgress:cycle.IsBreathing?1:cycle.State==FireBreathCycle.Phase.Afterfire?cycle.Remaining/10:0;
            var fury=SkyBeastScheduler.Instance?.Fury;if(fury!=null&&fury.Warning)storm=1-fury.Remaining/5;
            SettingsManager.Instance?.Sky?.SetFireStorm(storm,cycle.IsFury||(fury!=null&&fury.Warning));
            bool indoor=cycle.Player!=null&&ShelterDetector.AtFeet(cycle.Player.transform.position)==Shelter.Indoor;
            float cutoff=indoor?650:22000;foreach(var f in filters)f.cutoffFrequency=cutoff;
            foreach(var f in dragonFilters)if(f!=null)f.cutoffFrequency=storm>0?cutoff:22000;
            if(Time.time>=nextDragons)CacheDragons();
            if(cycle.State==FireBreathCycle.Phase.Disabled||cycle.State==FireBreathCycle.Phase.Rest){alarm.Stop();fire.Stop();}
            else fire.volume=.7f*(cycle.State==FireBreathCycle.Phase.Afterfire?Mathf.Clamp01(cycle.Remaining/4):1);
            var camera=Camera.main;
            if(camera!=null&&storm>0&&(Time.time>=nextTargets||(camera.transform.position-targetCenter).sqrMagnitude>36))CacheTargets();
            if(cycle.IsBreathing)
            {
                spawnClock+=Time.deltaTime*(FireVisualQuality.Choose(44,30,22));
                int spawn=Mathf.Min(12,Mathf.FloorToInt(spawnClock));spawnClock-=spawn;for(int i=0;i<spawn;i++)SpawnMeteor();
                shakeClock-=Time.deltaTime;if(shakeClock<=0){shakeClock=cycle.IsFury?.18f:.35f;cycle.Player?.GetComponent<Skills.GiantHandCameraImpulse>()?.Pulse(indoor?.025f:.07f);}
            }
            if(cycle.IsBreathing||cycle.State==FireBreathCycle.Phase.Afterfire)
            {
                emberClock+=Time.deltaTime*(FireVisualQuality.Choose(24,16,12));int count=Mathf.FloorToInt(emberClock);emberClock-=count;
                for(int i=0;i<count&&targets.Count>0;i++)
                {
                    var point=targets[Random.Range(0,targets.Count)];if(camera==null||Vector3.Distance(camera.transform.position,point)>=60)continue;
                    if(cycle.IsFury)
                    {
                        // Reuse the existing ember emission allowance for small, sustained court flames.
                        billows.Emit(new ParticleSystem.EmitParams{position=point+Vector3.up*.35f,velocity=Vector3.up*1.4f,startSize=Random.Range(2,3.5f),startLifetime=.9f},1);
                        int markBudget=FireVisualQuality.Choose(24,16,12);markCursor%=markBudget;var mark=marks[markCursor];markCursor=(markCursor+1)%markBudget;
                        mark.sprite.transform.position=point+Vector3.up*.015f;mark.sprite.enabled=true;mark.expires=Time.time+3;
                    }
                    else embers.Emit(new ParticleSystem.EmitParams{position=point+Vector3.up*Random.Range(1,9),velocity=new Vector3(.6f,1.4f,.3f),startLifetime=2.5f},1);
                }
                plumeClock+=Time.deltaTime*(FireVisualQuality.Choose(10,7,5)*.5f);int puff=Mathf.FloorToInt(plumeClock);plumeClock-=puff;
                for(int i=0;i<puff;i++)for(int j=0;j<Mathf.Min(plumeCount,FireVisualQuality.Choose(6,4,3));j++)
                    if(camera!=null&&Vector3.Distance(camera.transform.position,plumePoints[j])<60)plumes.Emit(new ParticleSystem.EmitParams{position=plumePoints[j]+Vector3.up*.7f,velocity=new Vector3(.3f,6,.1f),startSize=Random.Range(6,8),startLifetime=7},1);
            }
            ActiveScorches=0;
            foreach(var mark in marks)if(mark.sprite.enabled){float left=mark.expires-Time.time;mark.sprite.enabled=left>0;mark.sprite.color=new Color(.20f,.12f,.08f,Mathf.Clamp01(left));if(left>0)ActiveScorches++;}
            for(int i=0;i<meteors.Length;i++)
            {
                var m=meteors[i];if(!m.active)continue;m.age+=Time.deltaTime;
                if(m.age>=m.duration){Impact(m.impact);m.active=false;meteorBatch.Hide(i);LiveMeteors--;continue;}
                if(camera!=null&&Vector3.Distance(camera.transform.position,m.impact)>60){m.active=false;meteorBatch.Hide(i);LiveMeteors--;continue;}
                float t=m.age/m.duration;Vector3 head=Vector3.Lerp(m.start,m.impact,t),tail=head+(m.start-m.impact).normalized*Mathf.Lerp(6,11,t);
                meteorBatch.Set(i,head,tail,camera!=null?camera.transform.position:head+Vector3.forward);
                if(m.age>=m.nextSmoke)
                {
                    m.nextSmoke=m.age+(FireVisualQuality.Mobile?.24f:FireVisualQuality.Current==FireEffectQuality.PcLow?.18f:.12f);
                    smoke.Emit(new ParticleSystem.EmitParams{position=tail,velocity=Vector3.up*1.2f,startSize=2.6f,startLifetime=1.8f},1);
                    billows.Emit(new ParticleSystem.EmitParams{position=head,velocity=(m.impact-m.start).normalized*3,startSize=1.7f,startLifetime=.25f},1);
                }
            }
            meteorBatch.Upload(LiveMeteors>0);
        }
        void ApplyParticleBudget()
        {
            foreach(var p in new[]{sparks,smoke,flash,billows,embers,plumes})
            {var main=p.main;main.maxParticles=FireVisualQuality.Choose(256,192,128);}
            var billowMain=billows.main;billowMain.maxParticles=FireVisualQuality.Choose(96,64,48);
            foreach(var jet in jets.Values){var main=jet.flame.main;main.maxParticles=FireVisualQuality.Choose(128,96,64);var dark=jet.smoke.main;dark.maxParticles=FireVisualQuality.Choose(64,44,32);}
            for(int i=Budget;i<meteors.Length;i++)if(meteors[i].active){meteors[i].active=false;meteorBatch.Hide(i);LiveMeteors--;}
            int marksBudget=FireVisualQuality.Choose(24,16,12);markCursor%=marksBudget;for(int i=marksBudget;i<marks.Length;i++)marks[i].sprite.enabled=false;
        }
        void OnDestroy()
        {
            if(cycle!=null){cycle.WarningStarted-=Warning;cycle.BreathStarted-=Breath;cycle.BreathEnded-=Ended;}
            SettingsManager.Instance?.Sky?.SetFireStorm(0);
            foreach(var f in dragonFilters)if(f!=null)Destroy(f);
            foreach(var jet in jets.Values)if(jet.cone!=null)Destroy(jet.cone);
        }
    }
}
