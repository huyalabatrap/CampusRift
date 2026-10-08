using System.Collections.Generic;
using UnityEngine;
namespace CampusRift.AR
{
    public sealed class ARAdaptiveQuality:MonoBehaviour
    {
        public bool Reduced {get;private set;}public bool ThermalLimited=>ThermalSeverity.HasValue&&ThermalSeverity.Value>=3;
        public int? ThermalSeverity {get;private set;}
        public bool TwoHandsAllowed=>RoomProbeAllowed&&SystemInfo.processorCount>=8;
        public bool DepthCollisionAllowed=>!Reduced&&!ThermalLimited;
        public bool VoiceAllowed=>!Reduced&&!ThermalLimited&&SystemInfo.systemMemorySize>=4000;
        public bool RecordingAllowed=>!Reduced&&!ThermalLimited&&SystemInfo.systemMemorySize>=4000;
        public int SecondaryRiftLimit=>Reduced?1:3;
        public int RoomVfxBudget=>Reduced?8:24;
        public bool RoomProbeAllowed=>!Reduced&&QualitySettings.GetQualityLevel()>0&&SystemInfo.systemMemorySize>=6000&&SystemInfo.graphicsShaderLevel>=45;
        ARXRLoaderControl loader;float nextThermal;readonly Queue<Vector2> frames=new Queue<Vector2>();int slow;
        void Awake(){loader=FindAnyObjectByType<ARXRLoaderControl>();}
        void Update()
        {
            if(loader==null||!loader.Ready)return;
            if(Time.unscaledTime>=nextThermal){nextThermal=Time.unscaledTime+2;ThermalSeverity=ReadThermal();}
            bool late=Time.unscaledDeltaTime>.040f;frames.Enqueue(new Vector2(Time.unscaledTime,late?1:0));if(late)slow++;
            while(frames.Count>0&&frames.Peek().x<Time.unscaledTime-5){if(frames.Dequeue().y>0)slow--;}
            if(!Reduced&&(ThermalLimited||frames.Count>0&&Time.unscaledTime-frames.Peek().x>=4.9f&&(float)slow/frames.Count>.2f)){Reduced=true;if(loader.SessionPipeline!=null)loader.SessionPipeline.renderScale=.85f;}
        }
        static int? ReadThermal()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try{using(var version=new AndroidJavaClass("android.os.Build$VERSION")){if(version.GetStatic<int>("SDK_INT")<29)return null;}using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))using(var power=activity.Call<AndroidJavaObject>("getSystemService","power"))return power.Call<int>("getCurrentThermalStatus");}catch{return null;}
#else
            return null;
#endif
        }
    }
}
