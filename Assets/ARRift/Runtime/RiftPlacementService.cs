using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
namespace CampusRift.AR
{
    public sealed class RiftPlacementService : MonoBehaviour
    {
        public ARModeSettings settings; public ARPlaneManager planes; public ARAnchorManager anchors; public ARRaycastManager raycasts; public Camera view;
        public Transform Root {get;private set;} public ARAnchor Anchor {get;private set;}
        float radius;
        public ARPlane SelectedPlane {get;private set;} public float Radius {get=>radius*adjustScale;private set=>radius=value;}
        public float ActualScale=>settings.Scale*radius/(settings.Floor?1.5f:.5f)*adjustScale;
        public bool ReticleValid {get;private set;} public bool Adjusting {get;private set;}
        public bool PreviewOnly {get;private set;}public event Action Anchored;
        public string AnchorMethod {get;private set;}="none"; public string HitType {get;private set;}="none";
        public float EnteredAt {get;private set;} public float FirstPlaneAt {get;private set;}=-1;
        public float ValidAt {get;private set;}=-1; public float AnchoredAt {get;private set;}=-1;
        public bool English,InputBlocked;public bool AutoPlacement=true;
        public string Message {get;private set;}="Lia chậm quanh mặt bàn/sàn";
        public event Action<Transform,ARPlane> Placed; public event Action Removing;
        sealed class Observation {public float height,time;}
        readonly Dictionary<TrackableId,Observation> observations=new Dictionary<TrackableId,Observation>();
        sealed class Boundary {public ARPlane plane;public Vector2[] points;public float area,convexity;}
        readonly Dictionary<TrackableId,Boundary> boundaries=new Dictionary<TrackableId,Boundary>();Vector2[] anchorBoundary;
        Boundary BoundaryFor(ARPlane p){if(!boundaries.TryGetValue(p.trackableId,out var b)){b=new Boundary{plane=p};boundaries[p.trackableId]=b;p.boundaryChanged+=BoundaryChanged;RefreshBoundary(b);}return b;}
        void RefreshBoundary(Boundary b){b.points=b.plane.boundary.ToArray();b.area=ARPlaneScoring.Area(b.points);b.convexity=ARPlaneScoring.Convexity(b.points);}
        void BoundaryChanged(ARPlaneBoundaryChangedEventArgs e){if(boundaries.TryGetValue(e.plane.trackableId,out var b))RefreshBoundary(b);}
        readonly List<ARRaycastHit> hits=new List<ARRaycastHit>();readonly List<RaycastResult> uiHits=new List<RaycastResult>();
        ARRune reticle; ARPlane candidate; Pose pose; Vector3 stableHit,offset,lastAnchor,smoothFrom; Quaternion rotationOffset=Quaternion.identity,smoothRotation;
        float stableSince=-1,nextScan,area,adjustScale=1,adjustUntil,smoothAt=-1; bool attaching; int generation;
        Vector2 lastFinger,lastSecond; bool dragging,two;
        ARCameraManager cameraManager; AROcclusionManager occlusion; ARPointCloudManager points;float brightness=1;bool configured;
        readonly List<Transform> dots=new List<Transform>();Material dotMaterial;bool pointMaterialUnavailable;
        void Awake(){EnteredAt=Time.unscaledTime;Log("scene");}
        void OnEnable(){if(planes!=null)planes.trackablesChanged.AddListener(Changed);}
        void OnDisable(){if(planes!=null)planes.trackablesChanged.RemoveListener(Changed);}
        void Start(){cameraManager=view.GetComponent<ARCameraManager>();occlusion=view.GetComponent<AROcclusionManager>();points=planes.GetComponent<ARPointCloudManager>();if(cameraManager!=null){cameraManager.autoFocusRequested=true;cameraManager.frameReceived+=CameraFrame;}}
        void CameraFrame(ARCameraFrameEventArgs e){if(e.lightEstimation.averageBrightness.HasValue)brightness=e.lightEstimation.averageBrightness.Value;}
        void Changed(ARTrackablesChangedEventArgs<ARPlane> c){foreach(var pair in c.removed){observations.Remove(pair.Key);if(boundaries.TryGetValue(pair.Key,out var b)){if(b.plane!=null)b.plane.boundaryChanged-=BoundaryChanged;boundaries.Remove(pair.Key);}}}
        string L(string vi,string en)=>English?en:vi;
        void Update()
        {
            if(settings==null||view==null)return;
            if(cameraManager!=null&&cameraManager.subsystem!=null&&!configured)ConfigureCamera();UpdateDepth();
            if(Root!=null){FollowAnchor();if(Adjusting){Adjust();if(!InputBlocked&&Time.unscaledTime>=adjustUntil)StartBattlefield();}return;}
            if(reticle==null){reticle=ARRune.Create(null,1);reticle.name="AR center reticle";}
            if(ARSession.state!=ARSessionState.SessionTracking||attaching){ReticleValid=false;stableSince=-1;observations.Clear();reticle.SetValid(false);return;}
            if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+.05f;Choose();UpdatePoints();}
            if(ReticleValid&&!InputBlocked){if(Pressed(out var point)&&!OverUI(point))Confirm();else if(AutoPlacement&&stableSince>=0&&Time.unscaledTime-stableSince>=.8f)Confirm();}
        }
        void ConfigureCamera()
        {
            cameraManager.autoFocusRequested=true;
            using(var configs=cameraManager.GetConfigurations(Unity.Collections.Allocator.Temp))
            {int best=-1;float score=float.MinValue;for(int i=0;i<configs.Length;i++){var c=configs[i];if(c.resolution.x<640||c.resolution.y<480)continue;float s=(c.framerate.HasValue&&c.framerate.Value==30?10000:0)-Mathf.Abs(c.resolution.x*c.resolution.y-640*480)/1000f;if(s>score){score=s;best=i;}}if(best>=0)cameraManager.currentConfiguration=configs[best];}configured=true;
        }
        void UpdateDepth()
        {if(occlusion==null)return;var tech=GetComponent<ARTechSettings>();bool collision=tech!=null&&tech.DepthCollision&&tech.Quality!=null&&tech.Quality.DepthCollisionAllowed;bool support=occlusion.descriptor!=null&&occlusion.descriptor.environmentDepthImageSupported==Supported.Supported;bool want=support&&(Root==null||settings.Occlusion||collision);if(occlusion.enabled!=want)occlusion.enabled=want;if(want)occlusion.requestedEnvironmentDepthMode=EnvironmentDepthMode.Fastest;}
        void Choose()
        {
            foreach(var p in planes.trackables)
            {if(FirstPlaneAt<0){FirstPlaneAt=Time.unscaledTime;Log("first-plane");}if(p.trackingState!=TrackingState.Tracking){observations.Remove(p.trackableId);continue;}float h=p.transform.position.y;if(!observations.TryGetValue(p.trackableId,out var o))observations[p.trackableId]=new Observation{height=h,time=Time.unscaledTime};else if(Mathf.Abs(h-o.height)>.02f){o.height=h;o.time=Time.unscaledTime;}}
            ReticleValid=false;PreviewOnly=false;candidate=null;area=0;HitType="none";Message=L("Lia chậm quanh mặt bàn/sàn","Slowly scan a table or floor");
            Vector2 center=new Vector2(Screen.width*.5f,Screen.height*.5f);
            foreach(var type in new[]{TrackableType.PlaneWithinPolygon,TrackableType.PlaneEstimated,TrackableType.Depth,TrackableType.FeaturePoint})
            {
                if(raycasts.subsystem==null||(raycasts.subsystem.subsystemDescriptor.supportedTrackableTypes&type)==0)continue;
                if(!raycasts.Raycast(center,hits,type))continue;foreach(var hit in hits){if(Evaluate(hit,type)){ReticleValid=true;break;}}if(ReticleValid||PreviewOnly)break;
            }
            if(ReticleValid)
            {if(stableSince<0||Vector3.Distance(stableHit,pose.position)>=.04f){stableHit=pose.position;stableSince=Time.unscaledTime;}if(ValidAt<0){ValidAt=Time.unscaledTime;Log("reticle-valid");}Message=L("Chạm để đặt trận","Tap to place");}
            else
            {
                stableSince=-1;if(HitType=="none"){pose=new Pose(view.transform.position+view.transform.forward*.7f,Quaternion.Euler(0,view.transform.eulerAngles.y,0));Radius=settings.Floor?1.5f:.5f;}
                if(Time.unscaledTime-EnteredAt>4){if(brightness<.35f||ARSession.notTrackingReason==NotTrackingReason.InsufficientLight)Message=L("Thêm ánh sáng","Add more light");else if(HitType=="none")Message=ARSession.notTrackingReason==NotTrackingReason.ExcessiveMotion?L("Lia điện thoại chậm lại","Move your phone slowly"):L("Mặt bàn trơn quá: đặt vài đồ vật lên bàn","Plain surface: add a few objects");else if(Vector3.Distance(view.transform.position,pose.position)<.25f)Message=L("Lùi xa hơn một chút","Move back a little");}
            }
            reticle.transform.SetPositionAndRotation(pose.position,pose.rotation);reticle.transform.localScale=Vector3.one*Mathf.Max(.05f,Radius);reticle.SetValid(ReticleValid);reticle.SetPreview(PreviewOnly);
        }
        bool Evaluate(ARRaycastHit hit,TrackableType type)
        {
            HitType=type.ToString();var p=planes.GetPlane(hit.trackableId);var delta=hit.pose.position-view.transform.position;
            var forward=Vector3.ProjectOnPlane(view.transform.position-hit.pose.position,Vector3.up);if(forward.sqrMagnitude<.00001f)forward=Vector3.ProjectOnPlane(-view.transform.forward,Vector3.up);
            pose=new Pose(hit.pose.position,Quaternion.LookRotation(forward.normalized,Vector3.up));float angle=Mathf.Atan2(-delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg;
            if(p!=null&&type==TrackableType.PlaneWithinPolygon)
            {
                if(p.trackingState!=TrackingState.Tracking||p.alignment!=PlaneAlignment.HorizontalUp||p.subsumedBy!=null||!observations.TryGetValue(p.trackableId,out var o)||Time.unscaledTime-o.time<.3f)return false;
                var cached=BoundaryFor(p);var boundary=cached.points;area=cached.area;var local=p.transform.InverseTransformPoint(hit.pose.position);var q=new Vector2(local.x,local.z);Radius=settings.Floor?1.5f:.5f;
                // Preserve the chosen hit, reducing the radius further near a boundary.
                if(!ARPlaneScoring.Inside(boundary,q))return false;Radius=Mathf.Min(Radius,ARPlaneScoring.EdgeDistance(boundary,q)-.01f);
                if(Radius<.18f){Message=L("Chỗ hơi nhỏ","Surface too small");return false;}
                if(ARPlaneScoring.Score(area,settings.MinimumArea,delta.magnitude,angle,cached.convexity,Vector3.Dot(view.transform.forward,delta.normalized))<0)return false;candidate=p;return true;
            }
            if(type!=TrackableType.Depth&&type!=TrackableType.FeaturePoint&&type!=TrackableType.PlaneEstimated)return false;
            if(Vector3.Angle(hit.pose.rotation*Vector3.up,Vector3.up)>=15||delta.magnitude<.25f||delta.magnitude>3||angle<5||angle>85)return false;
            Radius=settings.Floor?1.5f:.5f;PreviewOnly=true;Message=L("Đang tìm mặt · vòng mờ chưa thể đặt","Finding surface · preview only");return false;
        }
        public async void Confirm()
        {
            if(attaching||Root!=null||ARSession.state!=ARSessionState.SessionTracking)return;Choose();if(!ReticleValid||candidate==null)return;attaching=true;int token=generation;var selected=candidate;var placementPose=pose;
            try
            {
                ARAnchor anchor=null;AnchorMethod="world";
                try{var result=await anchors.TryAddAnchorAsync(placementPose);if(result.status.IsSuccess())anchor=result.value;}catch(Exception e){Log("world-failed:"+e.Message);}
                if(anchor==null&&selected!=null&&anchors.descriptor!=null&&anchors.descriptor.supportsTrackableAttachments){anchor=anchors.AttachAnchor(selected,placementPose);AnchorMethod="attached-fallback";}
                if(this==null||token!=generation){if(anchor!=null)Destroy(anchor.gameObject);return;}
                if(anchor!=null&&(ARSession.state!=ARSessionState.SessionTracking||selected==null||!FitsPlane(selected,placementPose.position,Radius))){Destroy(anchor.gameObject);anchor=null;}
                if(anchor==null){Message=L("Chưa neo được · quét lại","Anchor failed · scan again");stableSince=Time.unscaledTime;return;}
                Anchor=anchor;SelectedPlane=selected;var boundary=BoundaryFor(selected).points;anchorBoundary=new Vector2[boundary.Length];for(int i=0;i<boundary.Length;i++){var local=anchor.transform.InverseTransformPoint(selected.transform.TransformPoint(new Vector3(boundary[i].x,0,boundary[i].y)));anchorBoundary[i]=new Vector2(local.x,local.z);}Root=new GameObject("ARBattlefieldRoot").transform;Root.SetPositionAndRotation(anchor.transform.position,anchor.transform.rotation);offset=Vector3.zero;rotationOffset=Quaternion.identity;adjustScale=1;lastAnchor=anchor.transform.position;Root.localScale=Vector3.one*ActualScale;ARRune.Create(Root,Radius/ActualScale);
                planes.requestedDetectionMode=PlaneDetectionMode.None;SetPlaneVisuals(false);planes.enabled=false;
                if(points!=null)points.enabled=false;foreach(var d in dots)d.gameObject.SetActive(false);reticle.gameObject.SetActive(false);AnchoredAt=Time.unscaledTime;Adjusting=true;adjustUntil=Time.unscaledTime+3;Message=L("Kéo để dời · hai ngón xoay / thu phóng","Drag to move · two fingers rotate / scale");Log("anchored");Anchored?.Invoke();
            }
            finally{attaching=false;}
        }
        bool FitsPlane(ARPlane plane,Vector3 center,float disc){var local=plane.transform.InverseTransformPoint(center);return ARPlaneScoring.DiscFits(BoundaryFor(plane).points,new Vector2(local.x,local.z),disc,.01f);}
        bool FitsAdjustment()=>anchorBoundary!=null&&ARPlaneScoring.DiscFits(anchorBoundary,new Vector2(offset.x,offset.z),Radius,.01f);
        public bool TryAdjustment(Vector3 localOffset,float scale,Quaternion rotation)
        {var prior=offset;float oldScale=adjustScale;var oldRotation=rotationOffset;offset=localOffset;adjustScale=Mathf.Clamp(scale,.7f,1.3f);rotationOffset=rotation;if(FitsAdjustment())return true;offset=prior;adjustScale=oldScale;rotationOffset=oldRotation;Message=L("Vòng trận phải nằm trọn trên mặt bàn","Keep the whole disc on the surface");return false;}
        void FollowAnchor()
        {if(Anchor==null)return;var target=Anchor.transform.TransformPoint(offset);var rot=Anchor.transform.rotation*rotationOffset;float t=1-Mathf.Exp(-Time.unscaledDeltaTime/.15f);Root.SetPositionAndRotation(Vector3.Lerp(Root.position,target,t),Quaternion.Slerp(Root.rotation,rot,t));Root.localScale=Vector3.one*ActualScale;}
        public void StartBattlefield(){if(!Adjusting||Root==null||!FitsAdjustment())return;Adjusting=false;Message=L("Đã neo chiến trường","Battlefield anchored");Placed?.Invoke(Root,SelectedPlane);}
        bool Pressed(out Vector2 p){var t=Touchscreen.current;if(t!=null&&t.primaryTouch.press.wasPressedThisFrame){p=t.primaryTouch.position.ReadValue();return true;}p=Mouse.current!=null?Mouse.current.position.ReadValue():default;return Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame;}
        bool OverUI(Vector2 p){if(EventSystem.current==null)return false;uiHits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},uiHits);return uiHits.Count>0;}
        void Adjust()
        {
            if(InputBlocked)return;var t=Touchscreen.current;int count=0;Vector2 a=default,b=default;if(t!=null){foreach(var finger in t.touches)if(finger.press.isPressed){if(count==0)a=finger.position.ReadValue();else if(count==1)b=finger.position.ReadValue();count++;}}else if(Mouse.current!=null&&Mouse.current.leftButton.isPressed){count=1;a=Mouse.current.position.ReadValue();}
            if(count==0||OverUI(a)){dragging=two=false;return;}adjustUntil=Time.unscaledTime+3;
            if(count>=2){if(two){var previous=lastSecond-lastFinger;var current=b-a;TryAdjustment(offset,adjustScale*current.magnitude/Mathf.Max(1,previous.magnitude),Quaternion.AngleAxis(Vector2.SignedAngle(previous,current),Vector3.up)*rotationOffset);}two=true;lastSecond=b;dragging=false;}
            else {if(dragging){var plane=new Plane(Vector3.up,Root.position);if(plane.Raycast(view.ScreenPointToRay(a),out float d)&&plane.Raycast(view.ScreenPointToRay(lastFinger),out float old))TryAdjustment(offset+Anchor.transform.InverseTransformVector(view.ScreenPointToRay(a).GetPoint(d)-view.ScreenPointToRay(lastFinger).GetPoint(old)),adjustScale,rotationOffset);}dragging=true;two=false;}lastFinger=a;
        }
        void UpdatePoints()
        {
            if(points==null)return;int index=0;
            if(dotMaterial==null)
            {
                // A Resources material retains the shader and its transparent variant in APKs.
                var template=Resources.Load<Material>("ARModes/FeaturePoints");
                if(template==null){if(!pointMaterialUnavailable){pointMaterialUnavailable=true;Debug.LogWarning("[ARDiag] Feature-point material missing; placement remains available");}return;}
                dotMaterial=new Material(template);
            }
            foreach(var cloud in points.trackables){if(!cloud.positions.HasValue)continue;var data=cloud.positions.Value;for(int i=0;i<data.Length&&index<48;i+=12){if(index>=dots.Count){var d=GameObject.CreatePrimitive(PrimitiveType.Sphere);d.name="AR faint feature point";Destroy(d.GetComponent<Collider>());d.GetComponent<Renderer>().sharedMaterial=dotMaterial;d.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;d.transform.localScale=Vector3.one*.004f;dots.Add(d.transform);}dots[index].gameObject.SetActive(true);dots[index++].position=cloud.transform.TransformPoint(data[i]);}}for(int i=index;i<dots.Count;i++)dots[i].gameObject.SetActive(false);
        }
        void Log(string milestone)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ARPlace] {milestone} t={Time.unscaledTime-EnteredAt:0.000} hit={HitType} area={area:0.000} radius={Radius:0.000} anchor={AnchorMethod}");
#endif
        }
        void SetPlaneVisuals(bool visible)
        {
            foreach(var plane in planes.trackables)
            {
                // The visualizer updates renderer.enabled every frame independently of its manager.
                foreach(var visualizer in plane.GetComponentsInChildren<ARPlaneMeshVisualizer>(true))visualizer.enabled=visible;
                if(!visible)foreach(var renderer in plane.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            }
        }
        public void Reposition(){generation++;Removing?.Invoke();if(Root!=null)Destroy(Root.gameObject);if(Anchor!=null)Destroy(Anchor.gameObject);Root=null;Anchor=null;SelectedPlane=null;candidate=null;Adjusting=ReticleValid=false;adjustScale=1;smoothAt=-1;observations.Clear();stableSince=-1;EnteredAt=Time.unscaledTime;FirstPlaneAt=ValidAt=AnchoredAt=-1;planes.requestedDetectionMode=PlaneDetectionMode.Horizontal;planes.enabled=true;SetPlaneVisuals(true);if(points!=null)points.enabled=true;if(reticle!=null)reticle.gameObject.SetActive(true);}
        public void SetFloor(bool value){settings.Floor=value;Reposition();}
        void OnDestroy(){foreach(var b in boundaries.Values)if(b.plane!=null)b.plane.boundaryChanged-=BoundaryChanged;if(cameraManager!=null)cameraManager.frameReceived-=CameraFrame;if(reticle!=null)Destroy(reticle.gameObject);foreach(var d in dots)if(d!=null)Destroy(d.gameObject);if(dotMaterial!=null)Destroy(dotMaterial);if(Root!=null)Destroy(Root.gameObject);if(Anchor!=null)Destroy(Anchor.gameObject);}
    }
}
