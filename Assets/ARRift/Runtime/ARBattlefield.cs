using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using CampusRift.Monsters;
using CampusRift.Enemies;
namespace CampusRift.AR
{
    [DefaultExecutionOrder(-900)]
    public sealed class ARBattlefield : MonoBehaviour
    {
        public RiftPlacementService placement;public Light estimatedLight;public ARCameraManager cameraManager;public AROcclusionManager occlusion;
        public int tableAgentType, floorAgentType;public Material shadowMaterial;
        public Transform Root=>placement.Root;public float Scale=>placement.ActualScale;
        public PlayerMonsterHealth Shrine {get;private set;}public Transform Rift {get;private set;}
        public bool CheckLoad;
        public ARModeSession ModeSession {get;private set;}
        public ARGameMode Mode=>ModeSession.Mode;
        public float CombatDelta=>Time.deltaTime*(GetComponent<ARPlayerCombat>()?.ClockRate??1);
        public float Clock {get;private set;}public bool Paused {get;private set;}=true;public bool PracticeActive;public bool UserPaused; public bool MenuPaused; public bool AnchorLost {get;private set;} float anchorNoneSince=-1;
        public bool NavigationReady {get;private set;}public NavMeshSurface Surface {get;private set;}
        public int LightValueFlags {get;private set;}=-1; // 1 brightness, 2 direction, 4 intensity, 8 SH; -1 no frame.
        public int AgentType=>placement.settings.Floor?floorAgentType:tableAgentType;
        public event Action Built,Removing;
        float trackingSince=-1;Mesh mesh;bool lastPause=true;UnityEngine.Rendering.SphericalHarmonicsL2 oldAmbient;
        bool applicationPaused;
        void OnApplicationPause(bool value){applicationPaused=value;if(!value)trackingSince=-1;}
        void Awake(){if(placement==null)placement=GetComponent<RiftPlacementService>();ModeSession=GetComponent<ARModeSession>()??gameObject.AddComponent<ARModeSession>();oldAmbient=RenderSettings.ambientProbe;if(cameraManager!=null)cameraManager.requestedLightEstimation=LightEstimation.AmbientIntensity|LightEstimation.AmbientColor|LightEstimation.MainLightDirection|LightEstimation.MainLightIntensity|LightEstimation.AmbientSphericalHarmonics;}
        void OnEnable(){placement.Placed+=Build;placement.Removing+=Clear;if(cameraManager!=null)cameraManager.frameReceived+=LightFrame;}
        void OnDisable(){placement.Placed-=Build;placement.Removing-=Clear;if(cameraManager!=null)cameraManager.frameReceived-=LightFrame;Clear();RenderSettings.ambientProbe=oldAmbient;}
        void LightFrame(ARCameraFrameEventArgs frame)
        {if(estimatedLight==null)return;var l=frame.lightEstimation;LightValueFlags=(l.averageBrightness.HasValue?1:0)|(l.mainLightDirection.HasValue?2:0)|(l.mainLightIntensityLumens.HasValue?4:0)|(l.ambientSphericalHarmonics.HasValue?8:0);float t=1-Mathf.Exp(-Time.unscaledDeltaTime/.2f);
            if(l.mainLightDirection.HasValue)estimatedLight.transform.rotation=Quaternion.Slerp(estimatedLight.transform.rotation,Quaternion.LookRotation(l.mainLightDirection.Value),t);
            float target=l.mainLightIntensityLumens.HasValue?Mathf.Clamp(l.mainLightIntensityLumens.Value/1000,.2f,2):l.averageBrightness.HasValue?Mathf.Clamp(l.averageBrightness.Value,.2f,2):1;estimatedLight.intensity=Mathf.Lerp(estimatedLight.intensity,target,t);
            if(l.ambientSphericalHarmonics.HasValue){var current=RenderSettings.ambientProbe;var next=l.ambientSphericalHarmonics.Value;for(int i=0;i<3;i++)for(int j=0;j<9;j++)current[i,j]=Mathf.Lerp(current[i,j],next[i,j],t);RenderSettings.ambientProbe=current;}}
        void Build(Transform root,ARPlane plane)
        {
            anchorNoneSince=-1;trackingSince=-1;Clock=Time.time;float extent=placement.Radius/Scale;
            var ground=new GameObject("AR shadow and navigation",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));ground.transform.SetParent(root,false);
            const int segments=64;
            var v=new Vector3[segments+1];var triangles=new int[segments*3];v[0]=new Vector3(0,.005f,0);
            for(int i=0;i<segments;i++){float angle=i*Mathf.PI*2/segments;v[i+1]=new Vector3(Mathf.Cos(angle)*extent,.005f,Mathf.Sin(angle)*extent);triangles[i*3]=0;triangles[i*3+1]=(i+1)%segments+1;triangles[i*3+2]=i+1;}
            mesh=new Mesh{name="AR fixed navigation disc",vertices=v,triangles=triangles};mesh.RecalculateNormals();
            ground.GetComponent<MeshFilter>().sharedMesh=mesh;ground.GetComponent<MeshCollider>().sharedMesh=mesh;ground.GetComponent<MeshRenderer>().sharedMaterial=shadowMaterial;
            Surface=ground.AddComponent<NavMeshSurface>();Surface.agentTypeID=AgentType;Surface.collectObjects=CollectObjects.Children;Surface.useGeometry=NavMeshCollectGeometry.RenderMeshes;Surface.overrideVoxelSize=true;Surface.voxelSize=.04f*Scale;Surface.overrideTileSize=true;Surface.tileSize=64;
            try{Surface.BuildNavMesh();NavigationReady=NavMesh.SamplePosition(root.position,out var hit,.3f*Scale,new NavMeshQueryFilter{agentTypeID=AgentType,areaMask=NavMesh.AllAreas});}catch(Exception e){NavigationReady=false;Debug.LogWarning("AR navigation uses steering: "+e.Message);}
            if(!NavigationReady)Debug.LogWarning("[ARNav] Disc nhỏ hơn profile bake · quái dùng steering trên disc.");
            var shrine=new GameObject("Linh Tran");shrine.transform.SetParent(root,false);shrine.transform.localPosition=Vector3.forward*extent*.65f;
            Shrine=shrine.AddComponent<PlayerMonsterHealth>();Shrine.respawnOnDefeat=false;Shrine.maxHealth=Mode.shrineHealth;Shrine.Revive(1,0);
            var context=shrine.AddComponent<ARCombatContext>();context.battlefield=this;context.scale=Scale;
            shrine.AddComponent<ARShrineVisual>().Initialize(Shrine,this);
            Rift=new GameObject("Rift spawn").transform;Rift.SetParent(root,false);Rift.localPosition=Vector3.back*extent*.6f;ARRune.Create(Rift,.6f);
            GetComponent<ARCombatAudio>()?.Rift(Rift,Scale);EnemyDirector.EnsureAR();Built?.Invoke();
        }
        void Update()
        {
            bool anchorNone=placement.Anchor==null||placement.Anchor.trackingState==TrackingState.None;
            if(anchorNone){if(anchorNoneSince<0)anchorNoneSince=Time.unscaledTime;}else anchorNoneSince=-1;
            AnchorLost=Root!=null&&anchorNoneSince>=0&&Time.unscaledTime-anchorNoneSince>2;
            bool tracking=ARSession.state==ARSessionState.SessionTracking&&!AnchorLost;
            if(!tracking)trackingSince=-1;else if(trackingSince<0)trackingSince=Time.unscaledTime;
            Paused=Root==null||placement.Adjusting||Shrine==null||UserPaused||MenuPaused||applicationPaused||!tracking||Time.unscaledTime-trackingSince<1;
            var sampler=GetComponent<FrameSampler>();if(sampler!=null)sampler.SetSamplingActive(!applicationPaused&&!MenuPaused&&!UserPaused&&(PracticeActive||!Paused));
            if(!Paused)Clock+=CombatDelta;

            if(lastPause!=Paused&&Root!=null){foreach(var p in Root.GetComponentsInChildren<ParticleSystem>()){if(Paused)p.Pause();else p.Play();}lastPause=Paused;}
        }
        void Clear(){anchorNoneSince=-1;trackingSince=-1;Removing?.Invoke();if(EnemyPool.Instance!=null)EnemyPool.Instance.ReleaseAll();if(Surface!=null)Surface.RemoveData();Surface=null;NavigationReady=false;Shrine=null;Rift=null;if(mesh!=null)Destroy(mesh);}
    }
}
