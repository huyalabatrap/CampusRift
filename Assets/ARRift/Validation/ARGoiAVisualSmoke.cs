#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
namespace CampusRift.AR
{
    public sealed class ARGoiAVisualSmoke:MonoBehaviour
    {
        ARBattleHUD hud;ARBattlefield field;RiftPlacementService placement;ARGestureCheck check;MockGestureSource mock;
        readonly List<object> audits=new List<object>();
        IEnumerator Start()
        {
            hud=FindAnyObjectByType<ARBattleHUD>();field=hud.GetComponent<ARBattlefield>();placement=field.placement;check=hud.GetComponent<ARGestureCheck>();mock=hud.GetComponent<MockGestureSource>();mock.enabled=false;
            placement.AutoPlacement=false;hud.SetMenu(false);hud.SetHelp(false);
            foreach(var size in new[]{new Vector2Int(2400,1080),new Vector2Int(1600,720)})
            {
                UIValidation.SetResolution(size.x,size.y);yield return new WaitForSecondsRealtime(.25f);
                placement.Reposition();yield return null;
                float until=Time.unscaledTime+6;while(!placement.PreviewOnly&&Time.unscaledTime<until)yield return null;
                Save("preview-state-"+size.x+".json",new {placement.PreviewOnly,placement.ReticleValid,placement.HitType});
                placement.enabled=false;yield return Capture("placement-preview",size);placement.enabled=true;
                until=Time.unscaledTime+6;while(!placement.ReticleValid&&Time.unscaledTime<until)yield return null;
                yield return Capture("placement-valid",size);placement.Confirm();
                until=Time.unscaledTime+5;while(placement.Root==null&&Time.unscaledTime<until)yield return null;
                if(placement.Root==null)throw new Exception("Polygon confirm smoke failed");placement.StartBattlefield();
                field.CheckLoad=true;field.Shrine.SetProgressionMaxHealth(10000);field.Shrine.Revive(1,0);yield return new WaitForSecondsRealtime(1.3f);
                yield return Capture("battle-center",size);
                var caster=field.GetComponent<ARSkillCaster>();caster.InjectedRejection=CastOutcome.Cooldown;
                yield return Hold("None",.3f);yield return Hold("Open_Palm",.3f);caster.InjectedRejection=null;
                yield return Capture("reject-feedback",size);
                check.NewSession();check.Open();yield return Capture("check-prepare",size);
                check.PreviewPhase(ARGestureCheck.Phase.Warmup);yield return Capture("check-warmup",size);
                check.PreviewPhase(ARGestureCheck.Phase.Trials,0);yield return Hold("None",.3f);yield return Hold(GestureSkillMapper.Labels[check.GetComponentInChildren<ARHandGraphic>().Gesture],.35f);
                yield return Capture("check-trial",size);
                check.PreviewPhase(ARGestureCheck.Phase.Trials,1);yield return Hold("None",.3f);check.Skip();yield return Capture("check-release",size);
                check.PreviewPhase(ARGestureCheck.Phase.Negative);yield return Capture("check-negative",size);
                check.PreviewPhase(ARGestureCheck.Phase.Results);yield return Capture("check-results",size);
                if(size.x==1600){check.Export();File.WriteAllText("task/ar/goiA/mock-export.json",check.LastJson);var parsed=Newtonsoft.Json.Linq.JObject.Parse(check.LastJson);Save("mock-export-check.json",new {cells=parsed["cells"].Count(),complete=(bool)parsed["complete"],bytes=System.Text.Encoding.UTF8.GetByteCount("[ARCheck] "+check.LastJson)});}
                check.Close();field.CheckLoad=true;
            }
            // A single audit pass over the visible states collected above.
            Save("text-audit.json",audits);yield return NavigationCases();placement.AutoPlacement=true;
            field.CheckLoad=false;File.WriteAllText("task/ar/goiA/visual-DONE.txt","Complete");Destroy(this);
        }
        IEnumerator Hold(string label,float seconds)
        {float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until){mock.Emit(label,new Vector2(.5f,.5f),1,.3f);yield return new WaitForSecondsRealtime(.05f);}}
        IEnumerator Capture(string kind,Vector2Int size)
        {yield return new WaitForSecondsRealtime(.15f);ScreenCapture.CaptureScreenshot("task/ar/screens/goiA/"+kind+"-"+size.x+"x"+size.y+".png");yield return new WaitForSecondsRealtime(.15f);if(size.x==1600)audits.Add(CampusRift.UI.ComicTextAudit.Scan(kind));}
        IEnumerator NavigationCases()
        {
            var results=new List<object>();
            foreach(bool floor in new[]{false,true})foreach(bool max in new[]{false,true})
            {
                float scale=max?(floor?.78f:.195f):(floor?.0504f:.0378f),radius=max?(floor?1.95f:.65f):.126f;int type=floor?field.floorAgentType:field.tableAgentType;
                var root=new GameObject("Goi A NavMesh fixture");root.transform.position=new Vector3(-10,2,-10);
                var filter=root.AddComponent<MeshFilter>();var v=new Vector3[65];var tris=new int[192];for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;v[i+1]=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);tris[i*3]=0;tris[i*3+1]=(i+1)%64+1;tris[i*3+2]=i+1;}
                var mesh=new Mesh{vertices=v,triangles=tris};mesh.RecalculateNormals();filter.sharedMesh=mesh;root.AddComponent<MeshRenderer>();var surface=root.AddComponent<NavMeshSurface>();surface.agentTypeID=type;surface.collectObjects=CollectObjects.Children;surface.overrideVoxelSize=true;surface.voxelSize=.04f*scale;surface.BuildNavMesh();
                var actor=new GameObject("Goi A scaled nav actor");actor.transform.position=root.transform.position;var agent=actor.AddComponent<NavMeshAgent>();agent.enabled=false;agent.agentTypeID=type;agent.radius=.28f*scale;agent.height=1.8f*scale;agent.speed=scale*3;agent.stoppingDistance=.01f;
                bool sampled=NavMesh.SamplePosition(root.transform.position,out var hit,radius,new NavMeshQueryFilter{agentTypeID=type,areaMask=NavMesh.AllAreas});if(sampled){actor.transform.position=hit.position;agent.enabled=true;agent.Warp(hit.position);agent.SetDestination(root.transform.position+Vector3.right*radius*.3f);}
                yield return new WaitForSecondsRealtime(.2f);var profile=NavMesh.GetSettingsByID(type);results.Add(new {floor,max,scale,radius,sampled,onMesh=agent.enabled&&agent.isOnNavMesh,path=agent.enabled&&agent.isOnNavMesh?agent.pathStatus.ToString():"steering",fitsProfile=agent.radius<=profile.agentRadius+1e-5f&&agent.height<=profile.agentHeight+1e-5f});
                surface.RemoveData();Destroy(actor);Destroy(root);Destroy(mesh);yield return null;
            }
            Save("navigation-min-max.json",results);
        }
        void Save(string name,object value){File.WriteAllText("task/ar/goiA/"+name,Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));}
    }
}
#endif
