#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Skills;
namespace CampusRift.AR
{
    public sealed class ARGoiAFocusedSmoke:MonoBehaviour
    {
        ARBattlefield field;ARSkillCaster caster;MockGestureSource mock;bool convergence;
        IEnumerator Start()
        {
            field=FindAnyObjectByType<ARBattlefield>();caster=field.GetComponent<ARSkillCaster>();mock=field.GetComponent<MockGestureSource>();mock.enabled=false;field.CheckLoad=true;
            var director=field.GetComponent<ARMonsterDirector>();float until=Time.unscaledTime+8;while(director.Actors.Count==0&&Time.unscaledTime<until)yield return null;
            var archetype=director.Actors[0].archetype;director.enabled=false;ReactionResolver.ReactionTriggered+=Reaction;
            for(int i=0;i<6;i++){var e=EnemyPool.Ensure().SpawnAR(archetype,field.Root,field.Root.TransformPoint(new Vector3((i%3-1)*1.2f,0,(i/3-.5f)*1.1f)),field);e.Vitality.SetMaxHealth(10000,true);e.GetComponent<ARMinionBrain>().enabled=false;var agent=e.GetComponent<NavMeshAgent>();if(agent.enabled&&agent.isOnNavMesh)agent.isStopped=true;}
            foreach(var skill in caster.Caster.GetComponents<SkillRuntime>())skill.ReadyOnRestEquip();caster.Caster.GetComponent<SpiritPower>().Refill();int before=caster.Fired;
            yield return Hold("None",.3f);yield return Hold("Closed_Fist",.4f);int pulled=caster.Caster.GetComponent<BlackHoleRuntime>().PulledCount;
            yield return Hold("Victory",.4f);yield return new WaitForSecondsRealtime(3.7f);
            Save("convergence-focused.json",new {convergence,pulled,casts=caster.Fired-before,aimValid=caster.AimValid,route="D1 stable switch without release; unchanged combat rules"});ReactionResolver.ReactionTriggered-=Reaction;
            var check=field.GetComponent<ARGestureCheck>();var hud=field.GetComponent<ARBattleHUD>();hud.SetHelp(false);hud.SetMenu(false);check.NewSession();check.Open();check.PreviewPhase(ARGestureCheck.Phase.Warmup);
            yield return Hold("None",.3f);check.PreviewPhase(ARGestureCheck.Phase.Trials,0);yield return Hold(GestureSkillMapper.Labels[check.RequestedGesture],.25f);
            foreach(var size in new[]{new Vector2Int(2400,1080),new Vector2Int(1600,720)}){UIValidation.SetResolution(size.x,size.y);yield return new WaitForSecondsRealtime(.15f);ScreenCapture.CaptureScreenshot("task/ar/screens/goiA/check-trial-"+size.x+"x"+size.y+".png");yield return new WaitForSecondsRealtime(.15f);}
            yield return Hold("None",.3f);check.PreviewPhase(ARGestureCheck.Phase.Trials,1);check.Skip();check.PreviewPhase(ARGestureCheck.Phase.Results);check.Export();File.WriteAllText("task/ar/goiA/mock-export-focused.json",check.LastJson);
            var parsed=Newtonsoft.Json.Linq.JObject.Parse(check.LastJson);int n=parsed["cells"].Sum(row=>(int)row[3]),skip=parsed["cells"].Sum(row=>(int)row[4]);Save("mock-roundtrip-focused.json",new {n,skip,cells=parsed["cells"].Count(),bytes=System.Text.Encoding.UTF8.GetByteCount("[ARCheck] "+check.LastJson),complete=(bool)parsed["complete"]});
            foreach(var size in new[]{new Vector2Int(2400,1080),new Vector2Int(1600,720)}){UIValidation.SetResolution(size.x,size.y);yield return new WaitForSecondsRealtime(.15f);ScreenCapture.CaptureScreenshot("task/ar/screens/goiA/check-results-"+size.x+"x"+size.y+".png");yield return new WaitForSecondsRealtime(.15f);}
            check.Close();field.placement.AutoPlacement=false;field.placement.Reposition();float wait=Time.unscaledTime+3;while(!field.placement.PreviewOnly&&Time.unscaledTime<wait)yield return null;field.placement.enabled=false;
            UIValidation.SetResolution(2400,1080);yield return new WaitForSecondsRealtime(.2f);ScreenCapture.CaptureScreenshot("task/ar/screens/goiA/placement-preview-2400x1080.png");yield return new WaitForSecondsRealtime(.2f);Save("preview-focused.json",new {field.placement.PreviewOnly,field.placement.ReticleValid,field.placement.HitType});field.placement.enabled=true;field.placement.AutoPlacement=true;
            File.WriteAllText("task/ar/goiA/focused-DONE.txt","Complete");Destroy(this);
        }
        void Reaction(ReactionType type,CampusRift.Monsters.MonsterVitality target){if(type==ReactionType.Convergence)convergence=true;}
        IEnumerator Hold(string label,float seconds){float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until){mock.Emit(label,new Vector2(.5f,.5f),1,.3f);yield return new WaitForSecondsRealtime(.05f);}}
        void Save(string name,object value){File.WriteAllText("task/ar/goiA/"+name,Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented));}
        void OnDestroy(){ReactionResolver.ReactionTriggered-=Reaction;}
    }
}
#endif
