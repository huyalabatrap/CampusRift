using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
namespace CampusRift.AR
{
    // Room geometry stays ephemeral. Every portal owns a separate local AR anchor.
    public sealed class ARSecondaryRifts : MonoBehaviour
    {
        public sealed class Portal
        {
            public ARAnchor anchor; public ARPlane plane; public Transform visual; public AudioSource audio;
            public bool wall; public float radius, expires,lostSince=-1; public int sequence;
            public Vector3 Position=>visual.position;
        }
        public readonly List<Portal> Portals=new List<Portal>(3);
        ARBattlefield field; ARAdaptiveQuality quality; System.Random random;
        int generation; bool attaching, scanning; float nextScan;
        PlaneDetectionMode previousDetection; bool previousEnabled;
        public int Limit=>quality!=null?quality.SecondaryRiftLimit:3;
        void Awake(){field=GetComponent<ARBattlefield>();quality=FindAnyObjectByType<ARAdaptiveQuality>();}
        public void Begin(int seed){Clear();random=new System.Random(seed^360);}
        public void SetScanning(bool want)
        {
            if(field==null||field.placement==null)return;
            var manager=field.placement.planes;if(manager==null||scanning==want)return;
            if(want){previousDetection=manager.requestedDetectionMode;previousEnabled=manager.enabled;manager.enabled=true;manager.requestedDetectionMode=PlaneDetectionMode.Horizontal|PlaneDetectionMode.Vertical;}
            else {manager.requestedDetectionMode=previousDetection;manager.enabled=previousEnabled;}
            scanning=want;
        }
        public void Request(bool hunt)
        {
            if(attaching||Portals.Count>=Limit||field.Paused||field.Root==null||Time.unscaledTime<nextScan)return;
            nextScan=Time.unscaledTime+2;SetScanning(true);
            var candidates=new List<ARPlane>();
            foreach(var plane in field.placement.planes.trackables)
                if(plane.trackingState==TrackingState.Tracking&&plane.subsumedBy==null&&(plane.alignment==PlaneAlignment.Vertical||plane.alignment==PlaneAlignment.HorizontalUp)&&(hunt||plane!=field.placement.SelectedPlane))candidates.Add(plane);
            candidates.Sort((a,b)=>string.CompareOrdinal(a.trackableId.ToString(),b.trackableId.ToString()));
            if(candidates.Count==0)return;
            int offset=random.Next(candidates.Count);
            for(int i=0;i<candidates.Count;i++)
            {
                var plane=candidates[(offset+i)%candidates.Count];bool occupied=false;
                foreach(var p in Portals)if(p.plane==plane)occupied=true;
                if(occupied)continue;
                var boundary=plane.boundary.ToArray();if(boundary.Length<3)continue;
                // Sample polygon interiors with bounded work; never use extents/estimated hits.
                var lo=boundary[0];var hi=lo;foreach(var q in boundary){lo=Vector2.Min(lo,q);hi=Vector2.Max(hi,q);}
                float radius=Mathf.Clamp(.45f*field.Scale,.07f,.28f);
                for(int n=0;n<24;n++)
                {
                    var q=new Vector2(Mathf.Lerp(lo.x,hi.x,(float)random.NextDouble()),Mathf.Lerp(lo.y,hi.y,(float)random.NextDouble()));
                    if(!ARPlaneScoring.DiscFits(boundary,q,radius))continue;
                    var position=plane.transform.TransformPoint(new Vector3(q.x,0,q.y));
                    if(Vector3.Distance(position,field.placement.view.transform.position)>4||Vector3.Distance(position,field.placement.view.transform.position)<.35f)continue;
                    if(plane==field.placement.SelectedPlane&&Vector3.Distance(position,field.Root.position)<field.placement.Radius*.55f)continue;
                    Attach(plane,new Pose(position,plane.transform.rotation),radius,hunt);return;
                }
            }
        }
        bool Fits(ARPlane plane,Vector3 position,float radius)
        {if(plane==null||plane.trackingState!=TrackingState.Tracking||plane.subsumedBy!=null)return false;var p=plane.transform.InverseTransformPoint(position);return ARPlaneScoring.DiscFits(plane.boundary.ToArray(),new Vector2(p.x,p.z),radius);}
        async void Attach(ARPlane plane,Pose pose,float radius,bool hunt)
        {
            attaching=true;int token=generation;ARAnchor anchor=null;
            try
            {
                var manager=field.placement.anchors;
                try{var result=await manager.TryAddAnchorAsync(pose);if(result.status.IsSuccess())anchor=result.value;}catch(Exception){/* Attachment fallback below. */}
                if(anchor==null&&manager.descriptor!=null&&manager.descriptor.supportsTrackableAttachments&&Fits(plane,pose.position,radius))anchor=manager.AttachAnchor(plane,pose);
                if(this==null||token!=generation||field.Root==null||field.Paused||Portals.Count>=Limit||!Fits(plane,pose.position,radius))
                {if(anchor!=null)Destroy(anchor.gameObject);return;}
                if(anchor==null)return;
                var rune=ARRune.Create(anchor.transform,radius);rune.SetValid(true);
                var audio=GetComponent<ARCombatAudio>().SecondaryRift(rune.transform,field.Scale);
                Portals.Add(new Portal{anchor=anchor,plane=plane,visual=rune.transform,audio=audio,wall=plane.alignment==PlaneAlignment.Vertical,radius=radius,expires=hunt?field.Clock+20:float.PositiveInfinity,sequence=random.Next(GestureSequenceMatcher.Seals.Length)});
            }
            finally{if(this!=null&&token==generation)attaching=false;}
        }
        public void Remove(Portal p){Portals.Remove(p);if(p.anchor!=null)Destroy(p.anchor.gameObject);}
        public bool SpawnOrigin(int index,out Vector3 position,out bool wall)
        {position=Vector3.zero;wall=false;if(Portals.Count==0)return false;var p=Portals[index%Portals.Count];if(p.anchor==null||p.anchor.trackingState!=TrackingState.Tracking)return false;position=p.Position+p.visual.up*.025f;wall=p.wall;return true;}
        void Update()
        {
            for(int i=Portals.Count-1;i>=0;i--){var p=Portals[i];if(p.anchor==null||p.visual==null||p.plane==null||p.plane.subsumedBy!=null){Remove(p);continue;}if(p.anchor.trackingState==TrackingState.None){if(p.lostSince<0)p.lostSince=Time.unscaledTime;if(Time.unscaledTime-p.lostSince>2){Remove(p);continue;}}else p.lostSince=-1;bool visible=!field.Paused&&p.anchor.trackingState==TrackingState.Tracking;p.visual.gameObject.SetActive(visible);if(p.audio!=null){p.audio.volume=ARCombatAudio.Volume*.3f;if(visible&&!p.audio.isPlaying)p.audio.UnPause();else if(!visible&&p.audio.isPlaying)p.audio.Pause();}}
            while(Portals.Count>Limit)Remove(Portals[Portals.Count-1]);
            if(scanning)foreach(var p in field.placement.planes.trackables){foreach(var v in p.GetComponentsInChildren<ARPlaneMeshVisualizer>())v.enabled=false;foreach(var r in p.GetComponentsInChildren<Renderer>())r.enabled=false;}
        }
        public void Clear(){generation++;attaching=false;while(Portals.Count>0)Remove(Portals[Portals.Count-1]);SetScanning(false);nextScan=0;}
        void OnDestroy(){Clear();}
    }
}
