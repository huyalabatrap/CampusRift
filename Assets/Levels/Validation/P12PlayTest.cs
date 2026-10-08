#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CampusRift.Skills;
namespace CampusRift.Validation
{
    public abstract class P12PlayTest : MonoBehaviour
    {
        [Serializable] public sealed class Report {public string capturedAt,test;public List<string> passed=new List<string>(),failed=new List<string>(),measurements=new List<string>();}
        protected readonly Report report=new Report();protected SkillSet1TestWorld world;
        protected void Check(bool ok,string label){(ok?report.passed:report.failed).Add(label);Save();Debug.Log("P12 "+GetType().Name+" "+(ok?"PASS ":"FAIL ")+label);}
        protected void Measure(string line){report.measurements.Add(line);Save();}
        void Save(){Directory.CreateDirectory("Artifacts/P12/tests");File.WriteAllText("Artifacts/P12/tests/"+GetType().Name+".json",JsonUtility.ToJson(report,true));}
        protected abstract IEnumerator Run();
        IEnumerator Start(){report.test=GetType().Name;report.capturedAt=DateTime.UtcNow.ToString("o");world=new SkillSet1TestWorld{GameplayCamera=true};
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0){var routine=stack.Peek();bool more=false;object value=null;try{more=routine.MoveNext();if(more)value=routine.Current;}catch(Exception e){Check(false,"Exception "+e);break;}if(!more){stack.Pop();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;}
            try{Cleanup();}catch(Exception e){Check(false,"Cleanup "+e);}Save();File.WriteAllText("Artifacts/P12/tests/"+GetType().Name+"-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);}
        protected virtual void Cleanup(){Enemies.EnemyPool.Instance?.ReleaseAll();SkyBeast.SkyBeastPresence.StopAll();world.End();}
        protected void Begin(){world.Begin();foreach(var victim in world.victims)if(victim!=null)Enemies.EnemyPool.Instance.Release(victim.GetComponent<Enemies.EnemyInstance>());}
        void Update(){if(UI.UIStateManager.Instance!=null&&UI.UIStateManager.Instance.State==UI.UIState.Paused)UI.UIStateManager.Instance.EnterScene(true);}
    }
}
#endif
