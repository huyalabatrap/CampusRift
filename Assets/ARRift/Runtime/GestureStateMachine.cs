using System;
using UnityEngine;
namespace CampusRift.AR
{
    public struct GestureIntent {public long id,frameId;public int epoch,handId;public string label;public double? triggerResultMs;public double firstValidMs,triggerConsumeMs,captureSpanMs;public Vector2 palm;}
    public sealed class GestureStateMachine
    {
        public ARModeSettings Settings;public readonly float[] Evidence=new float[GestureSkillMapper.Labels.Length];
        public GeometryResult Geometry {get;private set;}=new GeometryResult();
        string decision="idle";
        public string LastRejection {get;private set;}="none";
        public string Decision {get=>decision;private set{decision=value;if(value!="idle"&&value!="charging"&&value!="fire"&&value!="held")LastRejection=value;}}public string TopLabel {get;private set;}="None";public string SecondLabel {get;private set;}="None";
        public float TopEvidence {get;private set;}public float SecondEvidence=>0;
        public bool ReleaseReady=>!requireRelease&&held==null;
        public bool HasProcessed=>lastCapture>=0;
        public string RequireReleaseReason {get;private set;}="none";
        public string HeldLabel=>held??"None";
        public event Action<GestureIntent> Intent;
        public event Action<string,Vector2> GestureFired;public event Action<string,float> GestureProgress;
        string held,candidate;int count,absenceCount,epoch=-1;long lastId=-1,intentId;double lastCapture=-1,start,absence=-1,lockedUntil,firstValid;
        Vector2 lastPalm;bool hadPalm,requireRelease;
        public static bool Mapped(string label)=>Array.IndexOf(GestureSkillMapper.Labels,label)>=0;
        void ResetCandidate(){candidate=null;count=0;TopLabel="None";TopEvidence=0;Array.Clear(Evidence,0,Evidence.Length);GestureProgress?.Invoke("None",0);}
        // Input gates reset evidence. Lifecycle changes only lock an already consumed pose.
        public void Suspend(bool resetHeld=false,string reason="pause",bool latch=true)
        {ResetCandidate();if(latch&&(held!=null||requireRelease)){requireRelease=true;RequireReleaseReason=reason;}if(resetHeld)held=null;absence=-1;absenceCount=0;hadPalm=false;Decision="suspended";}
        void Released(){held=null;requireRelease=false;RequireReleaseReason="none";}
        void Relaxed(double now){if(absence<0)absence=now;absenceCount++;if(now-absence+1e-6>=(Settings!=null?Settings.handRelease:.2f)&&absenceCount>=3)Released();}
        public void Process(GestureFrame f,bool allowed=true,bool dynamicBlocked=false,bool dynamicCompleted=false)
        {
            double now=f.sensorTimestamp>0?f.sensorTimestamp:f.timestampMs*.001;
            if(epoch<0)epoch=f.epoch;else if(f.epoch!=epoch){if(f.epoch<epoch)return;epoch=f.epoch;lastId=-1;lastCapture=-1;Suspend(reason:"epoch");}
            long id=f.frameId>0?f.frameId:f.timestampMs;
            if(id<=lastId||now<=lastCapture){Decision="duplicate";return;}
            double dt=lastCapture<0?0:now-lastCapture;lastId=id;lastCapture=now;
            if(dt>.150001){ResetCandidate();absence=-1;absenceCount=0;hadPalm=false;}
            if(!allowed){Suspend(reason:"!allowed",latch:false);return;}
            if(f.acquireMs>0&&f.consumeMs-f.acquireMs>250){ResetCandidate();absence=-1;absenceCount=0;hadPalm=false;Decision="stale";return;}
            bool hand=f.handPresent||f.landmarks!=null&&f.landmarks.Length==63;
            if(!hand){Geometry=GestureGeometry.Evaluate(f,Settings);ResetCandidate();hadPalm=false;Relaxed(now);Decision="absent";return;}
            Geometry=GestureGeometry.Evaluate(f,Settings);
            float speed=hadPalm&&dt>0?(float)(Vector2.Distance(lastPalm*new Vector2(Screen.width,Screen.height),Geometry.palm*new Vector2(Screen.width,Screen.height))/Mathf.Max(1,Mathf.Min(Screen.width,Screen.height))/dt):0;
            lastPalm=Geometry.palm;hadPalm=true;string label=f.label??"None";
            if(dynamicCompleted)RequireRelease("dynamic");
            if(dynamicBlocked||dynamicCompleted){ResetCandidate();absence=-1;absenceCount=0;Decision="dynamic-motion";return;}
            // A fresh, finite, in-frame unrecognized pose is a real relaxed hand.
            // Invalid/stale/clipped frames and brief classifier flicker do not rearm.
            if(!Mapped(label)&&Geometry.inFrame&&float.IsFinite(f.score))
            {ResetCandidate();Relaxed(now);Decision="neutral";return;}
            absence=-1;absenceCount=0;
            bool switching=requireRelease&&held!=null&&label!=held;
            if(requireRelease&&!switching||!Geometry.inFrame||speed>(Settings!=null?Settings.maxPalmSpeed:1.5f)||!Mapped(label)||!float.IsFinite(f.score)||f.score<(Settings!=null?Settings.modelThreshold:.6f)||Geometry.Contradicts(label))
            {ResetCandidate();Decision=requireRelease?"release-required":!Geometry.inFrame?Geometry.reason:speed>1.5f?"moving":Geometry.Contradicts(label)?"geometry-reject":"model-reject";return;}
            if(label==held){ResetCandidate();Decision="held";return;}
            if(candidate!=label){ResetCandidate();candidate=label;start=now;firstValid=f.consumeMs>0?f.consumeMs:now*1000;}
            count++;double dwell=held!=null?(Settings!=null?Settings.stableSwitch:.2f):(Settings!=null?Settings.gestureDwell:.1f);
            TopLabel=label;TopEvidence=Mathf.Min((float)((now-start)/dwell),count/3f);Evidence[Array.IndexOf(GestureSkillMapper.Labels,label)]=TopEvidence;GestureProgress?.Invoke(label,TopEvidence);Decision="charging";
            if(count>=3&&now-start+1e-6>=dwell&&now+1e-6>=lockedUntil)
            {
                requireRelease=false;RequireReleaseReason="none";
                held=label;lockedUntil=now+(Settings!=null?Settings.gestureLockout:.35f);
                var intent=new GestureIntent{id=++intentId,handId=f.handId,epoch=f.epoch,frameId=id,label=label,firstValidMs=firstValid,triggerResultMs=f.nativeToUnityMs.HasValue?(double?)(f.resultMs+f.nativeToUnityMs.Value):null,triggerConsumeMs=f.consumeMs>0?f.consumeMs:now*1000,captureSpanMs=(now-start)*1000,palm=Geometry.palm*new Vector2(Screen.width,Screen.height)};
                ResetCandidate();Decision="fire";Intent?.Invoke(intent);GestureFired?.Invoke(label,intent.palm);
            }
        }
        public void RequireRelease(string reason="reject"){Suspend(latch:false);requireRelease=true;RequireReleaseReason=reason;Decision="release-required";}
    }
}
