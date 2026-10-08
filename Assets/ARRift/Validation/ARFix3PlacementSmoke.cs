#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR.Simulation;
using UnityEngine.XR.ARFoundation;
namespace CampusRift.AR
{
    public sealed class ARFix3PlacementSmoke:MonoBehaviour
    {
        IEnumerator Start()
        {
            Application.runInBackground=true;var p=FindAnyObjectByType<RiftPlacementService>();
            yield return null;
            var consent=FindAnyObjectByType<ARSessionBootstrap>();consent.Continue();consent.Continue();
            float start=Time.unscaledTime;SimulationCameraPoseProvider provider=null;
            while((provider=FindAnyObjectByType<SimulationCameraPoseProvider>())==null&&Time.unscaledTime-start<15)yield return null;
            if(provider==null){Save(new {failed="no provider"});yield break;}
            provider.transform.position=new Vector3(.75f,1.5f,.1f);
            foreach(float yaw in new[]{155f,170f,180f,195f,210f,180f}){provider.transform.rotation=Quaternion.Euler(40,yaw,0);yield return new WaitForSecondsRealtime(.2f);}
            float ready=Time.unscaledTime;bool captured=false;
            while(p.Root==null&&Time.unscaledTime-start<25){if(p.ReticleValid&&!captured){ScreenCapture.CaptureScreenshot("task/ar/screens/fix3/placement-first-1600x720.png");captured=true;}yield return null;}
            if(p.Root==null){var details=new List<string>();foreach(var x in p.planes.trackables)details.Add(x.transform.position+" area="+ARPlaneScoring.Area(x.boundary.ToArray()));Save(new {failed="no anchor",p.Message,p.HitType,p.FirstPlaneAt,p.ValidAt,details});yield break;}
            Save(new {sceneToYellow=p.ValidAt-p.EnteredAt,yellowToAnchor=p.AnchoredAt-p.ValidAt,p.EnteredAt,p.FirstPlaneAt,p.ValidAt,p.AnchoredAt,p.AnchorMethod,p.HitType,p.Radius,p.ActualScale,scanCompleteAt=ready,start,rootPosition=p.Root.position.ToString()});
            // No synthetic plane or anchor: both came from the XR Simulation provider.
            while(p.Adjusting)yield return null;
            File.WriteAllText("task/ar/fix3/placement-DONE.txt","Complete");
            Destroy(this);
        }
        void Save(object data){File.WriteAllText("task/ar/fix3/placement-timing.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented,new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore}));}
    }
}
#endif
