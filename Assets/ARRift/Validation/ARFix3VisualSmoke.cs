#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
namespace CampusRift.AR
{
    public sealed class ARFix3VisualSmoke:MonoBehaviour
    {
        ARBattleHUD hud;RiftPlacementService placement;ARBattlefield field;
        IEnumerator Start()
        {
            hud=FindAnyObjectByType<ARBattleHUD>();placement=hud.GetComponent<RiftPlacementService>();field=hud.GetComponent<ARBattlefield>();
            // Narrow plane-change test. The world anchor and fixed ground mesh stay untouched.
            var plane=placement.SelectedPlane;Vector3 before=field.Root.position;Vector3 old=plane.transform.position;var other=new GameObject("Simulated subsumption target").AddComponent<ARPlane>();
            var property=typeof(ARPlane).GetProperty("subsumedBy");plane.transform.position+=new Vector3(.7f,.25f,-.4f);property.SetValue(plane,other);yield return new WaitForSecondsRealtime(.3f);
            float drift=Vector3.Distance(before,field.Root.position);Save("plane-drift.json",new {driftMeters=drift,pass=drift<=.01f,subsumption=plane.subsumedBy!=null,anchor=placement.AnchorMethod,detection=placement.planes.requestedDetectionMode.ToString(),mesh=field.Surface.GetComponent<MeshFilter>().sharedMesh.name});plane.transform.position=old;property.SetValue(plane,null);Destroy(other.gameObject);
            foreach(var size in new[]{new Vector2Int(2400,1080),new Vector2Int(1600,720)})
            {
                UIValidation.SetResolution(size.x,size.y);yield return new WaitForSecondsRealtime(.3f);
                placement.Reposition();yield return null;hud.enabled=false;placement.InputBlocked=true;
                float until=Time.unscaledTime+5;while(!placement.ReticleValid&&Time.unscaledTime<until)yield return null;
                hud.enabled=true;yield return null;hud.enabled=false;placement.InputBlocked=true;
                yield return Capture("placement",size);hud.enabled=true;placement.InputBlocked=false;placement.Confirm();
                while(field.Shrine==null)yield return null;field.Shrine.SetProgressionMaxHealth(10000);field.Shrine.Revive(1,0);yield return new WaitForSecondsRealtime(1.5f);
                Save("coverage-"+size.x+".json",hud.Coverage());yield return Capture("battle",size);
                bool beforePause=field.UserPaused;hud.SetMenu(true);yield return new WaitForSecondsRealtime(.2f);float clock=field.Clock;yield return new WaitForSecondsRealtime(.2f);bool menuPause=field.Paused&&Mathf.Approximately(clock,field.Clock);yield return Capture("menu",size);
                hud.SetMenu(false);yield return new WaitForSecondsRealtime(.2f);bool resume=!field.Paused;field.UserPaused=true;hud.SetMenu(true);yield return null;hud.SetMenu(false);yield return null;bool explicitPause=field.Paused&&field.UserPaused;field.UserPaused=beforePause;
                Save("menu-behavior-"+size.x+".json",new {menuPause,resume,explicitPause,globalTimeScale=Time.timeScale});
                hud.SetHelp(true);yield return Capture("guide",size);hud.SetHelp(false);hud.ShowCombo(0);yield return Capture("combo",size);
                Save("coverage-combo-"+size.x+".json",hud.Coverage());
            }
            var audit=CampusRift.UI.ComicTextAudit.Scan("AR fix3 1600x720");CampusRift.UI.ComicTextAudit.Save(audit,"task/ar/fix3/text-audit.json");
            Save("gesture-editor-metrics.json",new {source="Editor MOCK (no MediaPipe inference)",hz=hud.GetComponent<GestureRecognizerBridge>().RecognitionFps,latencyMs=hud.GetComponent<GestureRecognizerBridge>().LatencyMs});
            File.WriteAllText("task/ar/fix3/visual-DONE.txt","Complete");Destroy(this);
        }
        IEnumerator Capture(string kind,Vector2Int size){yield return new WaitForSecondsRealtime(.25f);ScreenCapture.CaptureScreenshot("task/ar/screens/fix3/"+kind+"-"+size.x+"x"+size.y+".png");yield return new WaitForSecondsRealtime(.3f);}
        void Save(string path,object result){File.WriteAllText("task/ar/fix3/"+path,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));}
    }
}
#endif
