from pathlib import Path
p=Path('Assets/ARRift/Runtime/RiftPlacementService.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('public string AnchorMethod', 'public bool PreviewOnly {get;private set;}public event Action Anchored;\n        public string AnchorMethod')
s=s.replace('readonly List<ARRaycastHit> hits=', '''sealed class Boundary {public ARPlane plane;public Vector2[] points;public float area,convexity;}
        readonly Dictionary<TrackableId,Boundary> boundaries=new Dictionary<TrackableId,Boundary>();Vector2[] anchorBoundary;
        Boundary BoundaryFor(ARPlane p){if(!boundaries.TryGetValue(p.trackableId,out var b)){b=new Boundary{plane=p};boundaries[p.trackableId]=b;p.boundaryChanged+=BoundaryChanged;RefreshBoundary(b);}return b;}
        void RefreshBoundary(Boundary b){b.points=b.plane.boundary.ToArray();b.area=ARPlaneScoring.Area(b.points);b.convexity=ARPlaneScoring.Convexity(b.points);}
        void BoundaryChanged(ARPlaneBoundaryChangedEventArgs e){if(boundaries.TryGetValue(e.plane.trackableId,out var b))RefreshBoundary(b);}
        readonly List<ARRaycastHit> hits=''')
s=s.replace('observations.Remove(pair.Key);}', 'observations.Remove(pair.Key);}') # explicit replaced below
s=s.replace('void Changed(ARTrackablesChangedEventArgs<ARPlane> c){foreach(var pair in c.removed)observations.Remove(pair.Key);}', 'void Changed(ARTrackablesChangedEventArgs<ARPlane> c){foreach(var pair in c.removed){observations.Remove(pair.Key);if(boundaries.TryGetValue(pair.Key,out var b)){if(b.plane!=null)b.plane.boundaryChanged-=BoundaryChanged;boundaries.Remove(pair.Key);}}}')
s=s.replace('ReticleValid=false;reticle.SetValid(false);return;', 'ReticleValid=false;stableSince=-1;observations.Clear();reticle.SetValid(false);return;')
s=s.replace('ReticleValid=false;candidate=null;area=0;', 'ReticleValid=false;PreviewOnly=false;candidate=null;area=0;')
s=s.replace('if(ReticleValid)break;', 'if(ReticleValid||PreviewOnly)break;')
s=s.replace('reticle.SetValid(ReticleValid);', 'reticle.SetValid(ReticleValid);reticle.SetPreview(PreviewOnly);')
s=s.replace('if(p!=null)\n', 'if(p!=null&&type==TrackableType.PlaneWithinPolygon)\n')
s=s.replace('var boundary=p.boundary.ToArray();area=ARPlaneScoring.Area(boundary);', 'var cached=BoundaryFor(p);var boundary=cached.points;area=cached.area;')
s=s.replace('ARPlaneScoring.Incenter(boundary,out float inscribed);Radius=Mathf.Min(inscribed,settings.Floor?1.5f:.5f);', 'Radius=settings.Floor?1.5f:.5f;')
s=s.replace('ARPlaneScoring.EdgeDistance(boundary,q));','ARPlaneScoring.EdgeDistance(boundary,q)-.01f);')
s=s.replace('ARPlaneScoring.Convexity(boundary),', 'cached.convexity,')
s=s.replace('if(type!=TrackableType.Depth&&type!=TrackableType.FeaturePoint)return false;', 'if(type!=TrackableType.Depth&&type!=TrackableType.FeaturePoint&&type!=TrackableType.PlaneEstimated)return false;')
s=s.replace('Radius=settings.Floor?1.5f:.5f;return true;', 'Radius=settings.Floor?1.5f:.5f;PreviewOnly=true;Message=L("Đang tìm mặt · vòng mờ chưa thể đặt","Finding surface · preview only");return false;')
s=s.replace('if(!ReticleValid||attaching||Root!=null)return;attaching=true;', 'if(attaching||Root!=null||ARSession.state!=ARSessionState.SessionTracking)return;Choose();if(!ReticleValid||candidate==null)return;attaching=true;')
s=s.replace('if(anchor==null){Message=', 'if(anchor!=null&&(ARSession.state!=ARSessionState.SessionTracking||selected==null||!FitsPlane(selected,placementPose.position,Radius))){Destroy(anchor.gameObject);anchor=null;}\n                if(anchor==null){Message=')
s=s.replace('Anchor=anchor;SelectedPlane=selected;', 'Anchor=anchor;SelectedPlane=selected;var boundary=BoundaryFor(selected).points;anchorBoundary=new Vector2[boundary.Length];for(int i=0;i<boundary.Length;i++){var local=anchor.transform.InverseTransformPoint(selected.transform.TransformPoint(new Vector3(boundary[i].x,0,boundary[i].y)));anchorBoundary[i]=new Vector2(local.x,local.z);}')
s=s.replace('Log("anchored");', 'Log("anchored");Anchored?.Invoke();')
start=s.index('        void FollowAnchor()');end=s.index('        bool Pressed(',start)
s=s[:start]+'''        bool FitsPlane(ARPlane plane,Vector3 center,float disc){var local=plane.transform.InverseTransformPoint(center);return ARPlaneScoring.DiscFits(BoundaryFor(plane).points,new Vector2(local.x,local.z),disc,.01f);}
        bool FitsAdjustment()=>anchorBoundary!=null&&ARPlaneScoring.DiscFits(anchorBoundary,new Vector2(offset.x,offset.z),Radius,.01f);
        public bool TryAdjustment(Vector3 localOffset,float scale,Quaternion rotation)
        {var prior=offset;float oldScale=adjustScale;var oldRotation=rotationOffset;offset=localOffset;adjustScale=Mathf.Clamp(scale,.7f,1.3f);rotationOffset=rotation;if(FitsAdjustment())return true;offset=prior;adjustScale=oldScale;rotationOffset=oldRotation;Message=L("Vòng trận phải nằm trọn trên mặt bàn","Keep the whole disc on the surface");return false;}
        void FollowAnchor()
        {if(Anchor==null)return;var target=Anchor.transform.TransformPoint(offset);var rot=Anchor.transform.rotation*rotationOffset;float t=1-Mathf.Exp(-Time.unscaledDeltaTime/.15f);Root.SetPositionAndRotation(Vector3.Lerp(Root.position,target,t),Quaternion.Slerp(Root.rotation,rot,t));Root.localScale=Vector3.one*ActualScale;}
        public void StartBattlefield(){if(!Adjusting||Root==null||!FitsAdjustment())return;Adjusting=false;Message=L("Đã neo chiến trường","Battlefield anchored");Placed?.Invoke(Root,SelectedPlane);}
'''+s[end:]
s=s.replace('rotationOffset=Quaternion.AngleAxis(Vector2.SignedAngle(previous,current),Vector3.up)*rotationOffset;adjustScale=Mathf.Clamp(adjustScale*current.magnitude/Mathf.Max(1,previous.magnitude),.7f,1.3f);', 'TryAdjustment(offset,adjustScale*current.magnitude/Mathf.Max(1,previous.magnitude),Quaternion.AngleAxis(Vector2.SignedAngle(previous,current),Vector3.up)*rotationOffset);')
s=s.replace('offset+=view.ScreenPointToRay(a).GetPoint(d)-view.ScreenPointToRay(lastFinger).GetPoint(old);', 'TryAdjustment(offset+Anchor.transform.InverseTransformVector(view.ScreenPointToRay(a).GetPoint(d)-view.ScreenPointToRay(lastFinger).GetPoint(old)),adjustScale,rotationOffset);')
s=s.replace('void OnDestroy(){', 'void OnDestroy(){foreach(var b in boundaries.Values)if(b.plane!=null)b.plane.boundaryChanged-=BoundaryChanged;')
p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARPlaneScoring.cs');s=p.read_text(encoding='utf-8-sig').replace('public static float Area(', 'public static bool DiscFits(IList<Vector2> polygon,Vector2 center,float radius,float margin=.01f)=>polygon!=null&&polygon.Count>=3&&Inside(polygon,center)&&EdgeDistance(polygon,center)+1e-5f>=radius+margin;\n        public static float Area(');p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARRune.cs');s=p.read_text(encoding='utf-8-sig').replace('public void SetValid', 'public void SetPreview(bool preview){if(material!=null)material.SetFloat("_Silhouette",preview?1:0);}\n        public void SetValid');p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Shaders/ARRune.shader');s=p.read_text(encoding='utf-8-sig').replace('if(_Silhouette>.5) return half4(.09,.025,.16,.22);', 'if(_Silhouette>.5) return half4(.35,.3,.48,(1-smoothstep(.85,1,length(i.uv*2-1)))*.16);');p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARBattlefield.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('oldAmbient=RenderSettings.ambientProbe;', 'oldAmbient=RenderSettings.ambientProbe;if(cameraManager!=null)cameraManager.requestedLightEstimation=LightEstimation.AmbientIntensity|LightEstimation.AmbientColor|LightEstimation.MainLightDirection|LightEstimation.MainLightIntensity|LightEstimation.AmbientSphericalHarmonics;')
start=s.index('        {if(estimatedLight==null)return;');end=s.index('        void Build(',start)
s=s[:start]+'''        {if(estimatedLight==null)return;var l=frame.lightEstimation;float t=1-Mathf.Exp(-Time.unscaledDeltaTime/.2f);
            if(l.mainLightDirection.HasValue)estimatedLight.transform.rotation=Quaternion.Slerp(estimatedLight.transform.rotation,Quaternion.LookRotation(l.mainLightDirection.Value),t);
            float target=l.mainLightIntensityLumens.HasValue?Mathf.Clamp(l.mainLightIntensityLumens.Value/1000,.2f,2):l.averageBrightness.HasValue?Mathf.Clamp(l.averageBrightness.Value,.2f,2):1;estimatedLight.intensity=Mathf.Lerp(estimatedLight.intensity,target,t);
            if(l.ambientSphericalHarmonics.HasValue){var current=RenderSettings.ambientProbe;var next=l.ambientSphericalHarmonics.Value;for(int i=0;i<3;i++)for(int j=0;j<9;j++)current[i,j]=Mathf.Lerp(current[i,j],next[i,j],t);RenderSettings.ambientProbe=current;}}
'''+s[end:]
s=s.replace('Clock=Time.time;', 'anchorNoneSince=-1;trackingSince=-1;Clock=Time.time;')
s=s.replace('void Clear(){Removing?.Invoke();', 'void Clear(){anchorNoneSince=-1;trackingSince=-1;Removing?.Invoke();')
p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Scenes/ARRiftBattle.unity');s=p.read_text(encoding='utf-8-sig').replace('m_LightEstimation: 0','m_LightEstimation: 31');p.write_text(s,encoding='utf-8')
p=Path('ProjectSettings/NavMeshAreas.asset');s=p.read_text(encoding='utf-8-sig').replace('agentRadius: 0.05\n    agentHeight: 0.27','agentRadius: 0.0546\n    agentHeight: 0.351').replace('agentRadius: 0.16800001\n    agentHeight: 1.08','agentRadius: 0.2184\n    agentHeight: 1.404');p.write_text(s,encoding='utf-8')
