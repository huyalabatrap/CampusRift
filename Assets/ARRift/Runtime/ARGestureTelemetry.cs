using System.Collections.Generic;
using UnityEngine;
using CampusRift.Progression;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARGestureTelemetry:MonoBehaviour
    {
        readonly List<LocalTelemetry.Count> counts=new List<LocalTelemetry.Count>();GestureRecognizerBridge bridge;ARSkillCaster caster;bool flushed;
        bool Consent=>SettingsManager.Instance!=null&&SettingsManager.Instance.Current.LocalTelemetryEnabled;
        void Start(){bridge=GetComponent<GestureRecognizerBridge>();caster=GetComponent<ARSkillCaster>();if(bridge!=null)bridge.Result+=Frame;if(caster!=null)caster.CastAttempted+=Fired;}
        void Frame(GestureFrame frame){if(System.Array.IndexOf(GestureSkillMapper.Labels,frame.label)>=0)Count("recognized:"+frame.label);}
        void Fired(string label,bool success){if(success)Count("fired:"+label);}
        void Count(string key){if(!Consent)return;var row=counts.Find(x=>x.id==key);if(row==null){row=new LocalTelemetry.Count{id=key};counts.Add(row);}row.count++;}
        void Update(){if(!Consent)counts.Clear();}
        void Flush(){if(flushed)return;flushed=true;if(Consent&&counts.Count>0)LocalTelemetry.ARGestures(counts);counts.Clear();}
        void OnApplicationQuit(){Flush();}
        void OnDestroy(){if(bridge!=null)bridge.Result-=Frame;if(caster!=null)caster.CastAttempted-=Fired;Flush();}
    }
}
