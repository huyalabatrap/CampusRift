#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditorInternal;
using CampusRift.Skills;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    public sealed class FireCpuProfile:MonoBehaviour
    {
        [Serializable]public sealed class Row{public string name;public double inclusiveMs,selfMs;public int calls;}
        [Serializable]public sealed class Data{public string state;public int frames;public List<Row> rows=new List<Row>();}
        public string Label="before";
        IEnumerator Start()
        {
            var world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();world.player.enabled=false;UIValidation.SetResolution(1920,1080);Application.targetFrameRate=-1;
            yield return new WaitForSecondsRealtime(1);foreach(var v in world.victims)v.gameObject.SetActive(false);world.PlacePlayer(new Vector3(10,.13f,-5));
            world.camera.transform.position=new Vector3(8.563098f,2.03f,-8.081447f);world.camera.transform.rotation=Quaternion.Euler(8,25,0);world.camera.fieldOfView=60;
            SettingsManager.Instance.Sky.SetPreset(SkyPreset.Default);SettingsManager.Instance.Sky.SetBrightness(1);
            var cycle=FireBreathCycle.Ensure();cycle.AutoAdvance=false;
            bool old=ProfilerDriver.enabled,oldEditor=ProfilerDriver.profileEditor;ProfilerDriver.profileEditor=true;ProfilerDriver.enabled=true;
            foreach(bool breath in new[]{false,true})
            {
                cycle.StartDev(8);cycle.Advance(breath?6:20);cycle.Ground.Clear();yield return new WaitForSecondsRealtime(2);
                var data=new Data{state=breath?"Breath":"Rest"};var rows=new Dictionary<string,Row>();int last=-1;
                for(int frame=0;frame<40;frame++)
                {
                    yield return null;int id=ProfilerDriver.lastFrameIndex;if(id==last)continue;last=id;
                    using(var view=ProfilerDriver.GetRawFrameDataView(id,0))
                    {
                        if(!view.valid)continue;data.frames++;
                        for(int i=0;i<view.sampleCount;i++)
                        {
                            string name=view.GetSampleName(i);double total=view.GetSampleTimeMs(i),self=total;
                            int end=i+view.GetSampleChildrenCountRecursive(i);
                            for(int child=i+1;child<=end;child+=1+view.GetSampleChildrenCountRecursive(child))self-=view.GetSampleTimeMs(child);
                            if(!rows.TryGetValue(name,out var row)){row=new Row{name=name};rows.Add(name,row);}row.inclusiveMs+=total;row.selfMs+=Math.Max(0,self);row.calls++;
                        }
                    }
                }
                foreach(var row in rows.Values){row.inclusiveMs/=Math.Max(1,data.frames);row.selfMs/=Math.Max(1,data.frames);data.rows.Add(row);}
                data.rows.Sort((a,b)=>b.selfMs.CompareTo(a.selfMs));File.WriteAllText("task/perf/"+Label+"-CPU-"+data.state+".json",JsonUtility.ToJson(data,true));
            }
            ProfilerDriver.enabled=old;ProfilerDriver.profileEditor=oldEditor;cycle.StopCycle();world.player.enabled=true;world.End();File.WriteAllText("task/perf/"+Label+"-CPU-DONE.txt","CPU profile captured, 40 frames/state; diagnostic overhead excluded from FPS samples");Destroy(gameObject);
        }
    }
}
#endif
