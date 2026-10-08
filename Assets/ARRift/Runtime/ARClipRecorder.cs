using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Scripting;
namespace CampusRift.AR
{
    public sealed class ARClipRecorder : MonoBehaviour
    {
        readonly ConcurrentQueue<string> messages=new ConcurrentQueue<string>();ARTechSettings settings;
        public bool Recording {get;private set;}public bool Pending {get;private set;}public string Status {get;private set;}="";
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject native;
        [Preserve]sealed class Listener:AndroidJavaProxy
        {readonly ConcurrentQueue<string> queue;public Listener(ConcurrentQueue<string> q):base("com.campusrift.tech.ClipBridge$Listener"){queue=q;}[Preserve]public void onState(string state){if(queue.Count<16)queue.Enqueue(state);}}
#endif
        void Start(){settings=GetComponent<ARTechSettings>();}
        // Called only by the affirmative action in the room-image warning dialog.
        public void StartConfirmed()
        {
            if(settings==null||!settings.RecordingAllowed||Pending||Recording)return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try{using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity")){if(native==null)native=new AndroidJavaObject("com.campusrift.tech.ClipBridge",activity,new Listener(messages));Pending=true;native.Call("request");}}catch(Exception e){Pending=false;Status=e.GetType().Name;}
#else
            Status="MediaProjection · Android 10+";
#endif
        }
        public void Stop(){
#if UNITY_ANDROID && !UNITY_EDITOR
            native?.Call("stop");
#endif
        }
        void Update(){while(messages.TryDequeue(out var state)){Status=state;Pending=state=="consent";Recording=state=="recording";}if((Recording||Pending)&&!settings.RecordingAllowed)Stop();}
        void OnApplicationPause(bool pause){if(pause&&Recording)Stop();}
        void OnDisable(){Stop();}
        void OnDestroy(){Stop();
#if UNITY_ANDROID && !UNITY_EDITOR
            native?.Call("close");native?.Dispose();native=null;
#endif
        }
    }
}
