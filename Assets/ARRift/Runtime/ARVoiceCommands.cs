using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Scripting;
namespace CampusRift.AR
{
    public sealed class ARVoiceCommands : MonoBehaviour
    {
        ARTechSettings settings;ARSkillCaster caster;ARBattlefield field;bool listening,permissionPending;int generation;
        readonly ConcurrentQueue<string> messages=new ConcurrentQueue<string>();int keyword=-1;double keywordAt;
        public string Status {get;private set;}="";public bool Listening=>listening;
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject native;
        [Preserve]sealed class Listener:AndroidJavaProxy
        {readonly ConcurrentQueue<string> queue;public Listener(ConcurrentQueue<string> q):base("com.campusrift.tech.VoiceBridge$Listener"){queue=q;}[Preserve]public void onKeyword(int generation,int keyword){if(queue.Count<16)queue.Enqueue(generation+":"+keyword);}[Preserve]public void onState(string state){if(queue.Count<16)queue.Enqueue(state);}}
#endif
        void Start(){settings=GetComponent<ARTechSettings>();caster=GetComponent<ARSkillCaster>();field=GetComponent<ARBattlefield>();}
        public void Toggle()
        {
            if(settings.Voice){settings.Voice=false;StopListening();return;}
#if UNITY_ANDROID && !UNITY_EDITOR
            if(UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone)){settings.Voice=true;return;}
            if(permissionPending)return;permissionPending=true;var callbacks=new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted+=_=>{permissionPending=false;settings.Voice=true;};callbacks.PermissionDenied+=_=>{permissionPending=false;settings.Voice=false;Status="Microphone denied";};
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone,callbacks);
#else
            settings.Voice=true;Status="Android · Vosk offline";
#endif
        }
        void Drain()
        {
            while(messages.TryDequeue(out var value))
            {var pieces=value.Split(':');if(pieces.Length==2&&int.TryParse(pieces[0],out int g)&&int.TryParse(pieces[1],out int k)){if(g==generation&&listening){keyword=k;keywordAt=GestureRecognizerBridge.Now;}}else Status=value;}
        }
        void Update()
        {
            Drain();
#if UNITY_ANDROID && !UNITY_EDITOR
            if(settings.VoiceAllowed&&native==null)
            {try{using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))native=new AndroidJavaObject("com.campusrift.tech.VoiceBridge",activity,new Listener(messages));native.Call("prepare");}catch{settings.Voice=false;Status="voice unavailable";}}
            if(!settings.VoiceAllowed&&native!=null){StopListening();native.Call("close");native.Dispose();native=null;}
#endif
            bool want=settings.VoiceAllowed&&field.Root!=null&&!field.Paused&&!field.CheckLoad&&!caster.Practice&&caster.Seal>=100&&caster.sequences.Buffering;
            if(want==listening)return;
            if(!want){StopListening();return;}
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone)){settings.Voice=false;return;}
            try{if(native==null){using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))native=new AndroidJavaObject("com.campusrift.tech.VoiceBridge",activity,new Listener(messages));}generation++;keyword=-1;listening=true;native.Call("start",generation);}catch(Exception e){Status=e.GetType().Name;settings.Voice=false;listening=false;}
#else
            listening=true;generation++;keyword=-1;
#endif
        }
        public float Multiplier(ARUltimate kind){Drain();return settings!=null&&settings.VoiceAllowed&&listening&&keyword==(int)kind&&GestureRecognizerBridge.Now-keywordAt<=1500?1.3f:1;}
        public void Consume(){keyword=-1;}
        void StopListening(){if(!listening)return;listening=false;generation++;keyword=-1;
#if UNITY_ANDROID && !UNITY_EDITOR
            native?.Call("stop");
#endif
        }
        void OnApplicationPause(bool pause){if(pause)StopListening();}
        void OnDisable(){StopListening();}
        void OnDestroy(){StopListening();
#if UNITY_ANDROID && !UNITY_EDITOR
            native?.Call("close");native?.Dispose();native=null;
#endif
        }
    }
}
