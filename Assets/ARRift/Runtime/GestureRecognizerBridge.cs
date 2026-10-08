using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Scripting;
using Unity.Collections;
namespace CampusRift.AR
{
    [DefaultExecutionOrder(-100)]
    public sealed class GestureRecognizerBridge : MonoBehaviour,IGestureSource
    {
        public event Action<GestureFrame> Result;public event Action<GestureHandsFrame> HandsResult;
        public int HandCount {get;private set;}=1;public int PrimaryHandId;
        readonly ConcurrentQueue<GestureHandsFrame> handResults=new ConcurrentQueue<GestureHandsFrame>();
        public event Action Invalidated;
        public string Error {get;private set;}="";
        public string LastNativeError {get;private set;}="";
        public long ResultCount {get;private set;}
        public float ResultsPerSecond {get;private set;}
        public float AcceptedPerSecond {get;private set;}
        public string LastDiscard {get;private set;}="none";
        double rateAt;int rawWindow,acceptedWindow;
        void NativeFailure(string message){Error=message;LastNativeError=message;Debug.LogWarning("[ARGesture] native-error="+message);}
        void CountCallback(){rawWindow++;ResultCount++;}
        void UpdateRates(){double now=Now;if(rateAt==0){rateAt=now;return;}double seconds=(now-rateAt)/1000;if(seconds<1)return;ResultsPerSecond=(float)(rawWindow/seconds);AcceptedPerSecond=(float)(acceptedWindow/seconds);rawWindow=acceptedWindow=0;rateAt=now;}
        public GestureFrame Latest {get;private set;}
        public float RecognitionFps {get;private set;}public float LatencyMs {get;private set;}
        public string DelegateName {get;private set;}="MOCK";
        public double? ClockErrorMs {get;private set;}double clockOffset;
        public string DevDelegate="AUTO";public int Epoch {get;private set;}
        public bool SamplingActive {get;private set;}=true;
        public bool Ready {get;private set;}=true;public bool Recovering {get;private set;}
        public bool Busy=>!Ready||inFlight||Recovering;
        readonly ConcurrentQueue<GestureFrame> results=new ConcurrentQueue<GestureFrame>();
        struct State {public int epoch;public string name,error;public bool ready;}
        readonly ConcurrentQueue<State> states=new ConcurrentQueue<State>();
        GestureFrame submitted;double previous,flightAt,recoveryAt,retryAt;bool inFlight;int retries,recoveryEpoch;readonly ARRecoveryGate recoveryGate=new ARRecoveryGate();
        public static double Now=>Time.realtimeSinceStartupAsDouble*1000;
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject native;Listener listener;IntPtr submitMethod;
        [Preserve]sealed class Listener:AndroidJavaProxy
        {
            readonly ConcurrentQueue<GestureFrame> frames;readonly ConcurrentQueue<State> states;readonly ConcurrentQueue<GestureHandsFrame> batches;public volatile bool accepting=true;
            public Listener(ConcurrentQueue<GestureFrame> f,ConcurrentQueue<State> s,ConcurrentQueue<GestureHandsFrame> b):base("com.campusrift.gesture.GestureBridge$GestureListener"){frames=f;states=s;batches=b;}
            [Preserve]public void onHands(int epoch,long frameId,string[] labels,float[] scores,string[] handed,float[] normalized,float[] world,long ts,int w,int h,long startNs,long resultNs)
            {if(!accepting)return;var meta=new GestureFrame{epoch=epoch,frameId=frameId,timestampMs=ts,width=w,height=h,inferStartMs=startNs/1e6,resultMs=resultNs/1e6};int n=Math.Min(2,labels.Length);var hands=new GestureFrame[n];for(int i=0;i<n;i++){var f=meta;f.label=labels[i];f.score=scores[i];f.handed=handed[i];f.landmarks=new float[63];f.worldLandmarks=new float[63];Array.Copy(normalized,i*63,f.landmarks,0,63);Array.Copy(world,i*63,f.worldLandmarks,0,63);f.handPresent=true;hands[i]=f;}batches.Enqueue(new GestureHandsFrame{hands=hands,metadata=meta});}
            [Preserve]public void onResult(int epoch,long frameId,string label,float score,string handed,float[] landmarks,float[] world,string[] labels,float[] scores,bool full,bool hand,long ts,int w,int h,long startNs,long resultNs)
            {if(accepting)frames.Enqueue(new GestureFrame{epoch=epoch,frameId=frameId,label=label,score=score,handed=handed,landmarks=landmarks,worldLandmarks=world,categoryLabels=labels,categoryScores=scores,fullScores=full,handPresent=hand,timestampMs=ts,width=w,height=h,inferStartMs=startNs/1e6,resultMs=resultNs/1e6});}
            [Preserve]public void onState(int epoch,string name,bool ready,string error){if(accepting)states.Enqueue(new State{epoch=epoch,name=name,ready=ready,error=error});}
        }
#else
        MockGestureSource mock;
#endif
        void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Ready=false;DelegateName="pending";flightAt=Now;
            try{using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity")){listener=new Listener(results,states,handResults);native=new AndroidJavaObject("com.campusrift.gesture.GestureBridge",activity,listener,DevDelegate);submitMethod=AndroidJNI.GetMethodID(native.GetRawClass(),"submit","(Ljava/nio/ByteBuffer;IIIJIJ)Z");}}catch(Exception e){NativeFailure(e.Message);Ready=false;}
#else
            mock=GetComponent<MockGestureSource>()??gameObject.AddComponent<MockGestureSource>();mock.Result+=ReceiveMock;
#endif
        }
        public void SetHandCount(int count)
        {
            count=count==2?2:1;if(HandCount==count||Recovering||Busy)return;HandCount=count;Epoch++;Latest=default;Invalidated?.Invoke();
#if UNITY_ANDROID && !UNITY_EDITOR
            if(native!=null){if(!recoveryGate.Begin(Now))return;Recovering=true;Ready=false;recoveryEpoch=Epoch;native.Call("setHandCount",Epoch,count);}
#endif
        }
        public void SetSamplingActive(bool active)
        {
            if(SamplingActive==active)return;SamplingActive=active;Epoch++;
            while(results.TryDequeue(out _)){}while(handResults.TryDequeue(out _)){}Latest=default;Invalidated?.Invoke();if(active)CalibrateClock();
            // Flight remains owned by Kotlin until callback or serialized recovery.
        }
        public bool Submit(NativeArray<byte> rgba,GestureFrame metadata)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(native==null||Busy||!SamplingActive||metadata.epoch!=Epoch)return false;IntPtr buffer=IntPtr.Zero;
            try{metadata.epoch=Epoch;metadata.submitMs=Now;buffer=AndroidJNI.NewDirectByteBuffer(rgba);bool ok=AndroidJNI.CallBooleanMethod(native.GetRawObject(),submitMethod,new[]{new jvalue{l=buffer},new jvalue{i=metadata.width},new jvalue{i=metadata.height},new jvalue{i=metadata.rotation},new jvalue{j=metadata.timestampMs},new jvalue{i=metadata.epoch},new jvalue{j=metadata.frameId}});if(ok){submitted=metadata;inFlight=true;flightAt=Now;}return ok;}catch(Exception e){NativeFailure(e.Message);Recover();return false;}finally{if(buffer!=IntPtr.Zero)AndroidJNI.DeleteLocalRef(buffer);}
#else
            return false;
#endif
        }
        public void SetDelegateGpu(bool enabled){SelectDelegate(enabled?"GPU":"CPU");}
        public void SelectDelegate(string name)
        {
            DevDelegate=name;retries=0;
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!recoveryGate.Begin(Now))return;Recovering=true;Ready=false;Epoch++;recoveryEpoch=Epoch;Latest=default;Invalidated?.Invoke();native?.Call("selectDelegate",Epoch,name);
#endif
        }
        public void Retry(){retries=0;Recover(true);}
        void Recover(bool cpu=true,string reason="retry")
        {
            if(!recoveryGate.Begin(Now))return;Recovering=true;Ready=false;Epoch++;recoveryEpoch=Epoch;Latest=default;Invalidated?.Invoke();recoveryAt=Now;Error="Đang khôi phục nhận dạng";
            Debug.LogWarning($"[ARGesture] recovery reason={reason} from={DelegateName} target={(cpu?"CPU":"GPU")} epoch={Epoch} nativeError={LastNativeError}");
#if UNITY_ANDROID && !UNITY_EDITOR
            native?.Call("recover",Epoch,cpu);
#endif
        }
        void Update()
        {
            while(states.TryDequeue(out var s))
            {
                if(s.epoch<recoveryEpoch)continue;DelegateName=s.name;
                if(s.ready){Ready=true;Recovering=false;recoveryGate.Complete();inFlight=false;Error="";retryAt=0;CalibrateClock();}
                else {Ready=false;NativeFailure(s.error);if(Recovering){Recovering=false;recoveryGate.Complete();retryAt=Now+(retries==0?500:1000);}else retryAt=Now+(s.name=="GPU"?0:500);}
            }
            while(handResults.TryDequeue(out var batch))
            {
                CountCallback();var m=batch.metadata;if(m.frameId==submitted.frameId&&m.epoch==submitted.epoch)inFlight=false;
                if(m.epoch!=Epoch||m.frameId!=submitted.frameId||!SamplingActive||HandCount!=2){LastDiscard=m.epoch!=Epoch?"epoch":m.frameId!=submitted.frameId?"frame-id":!SamplingActive?"paused":"hand-count";continue;}
                m=AttachMetadata(m);batch.metadata=m;for(int i=0;i<batch.hands.Length;i++)batch.hands[i]=AttachMetadata(batch.hands[i]);
                HandsResult?.Invoke(batch);var primary=m;foreach(var hand in batch.hands)if(hand.handId==PrimaryHandId){primary=hand;break;}Receive(primary);
            }
            while(results.TryDequeue(out var f))
            {
                CountCallback();if(f.frameId==submitted.frameId&&f.epoch==submitted.epoch)inFlight=false;
                if(f.epoch!=Epoch||f.frameId!=submitted.frameId||!SamplingActive||HandCount!=1){LastDiscard=f.epoch!=Epoch?"epoch":f.frameId!=submitted.frameId?"frame-id":!SamplingActive?"paused":"hand-count";continue;}
                f.rotation=submitted.rotation;f.displayMatrix=submitted.displayMatrix;f.hasDisplayMatrix=submitted.hasDisplayMatrix;f.sensorTimestamp=submitted.sensorTimestamp;
                f.hasCameraPose=submitted.hasCameraPose;f.cameraPosition=submitted.cameraPosition;f.cameraRotation=submitted.cameraRotation;f.acquireMs=submitted.acquireMs;f.convertReadyMs=submitted.convertReadyMs;f.submitMs=submitted.submitMs;f.consumeMs=Now;if(ClockErrorMs.HasValue){f.nativeToUnityMs=clockOffset;f.clockErrorMs=ClockErrorMs;}
                Receive(f);
            }
UpdateRates();
#if UNITY_ANDROID && !UNITY_EDITOR
            if(recoveryGate.Overdue(Now)){Error="Nhận dạng chưa phục hồi · chờ đóng model";return;}
            if(!Recovering&&retryAt>0&&Now>=retryAt){retryAt=0;if(retries<2){if(DelegateName!="GPU")retries++;Recover(true);}else Error="Nhận dạng lỗi · Thử lại";}
            else if(!Recovering&&retryAt==0&&retries<2&&(!Ready?ARRecoveryGate.InitializationOverdue(Now,flightAt):ARRecoveryGate.Watchdog(Now,flightAt,inFlight))){if(DelegateName!="GPU")retries++;Recover(true,!Ready?"startup-timeout":"inference-watchdog");}
#endif
        }
        GestureFrame AttachMetadata(GestureFrame f)
        {f.rotation=submitted.rotation;f.displayMatrix=submitted.displayMatrix;f.hasDisplayMatrix=submitted.hasDisplayMatrix;f.sensorTimestamp=submitted.sensorTimestamp;f.hasCameraPose=submitted.hasCameraPose;f.cameraPosition=submitted.cameraPosition;f.cameraRotation=submitted.cameraRotation;f.acquireMs=submitted.acquireMs;f.convertReadyMs=submitted.convertReadyMs;f.submitMs=submitted.submitMs;f.consumeMs=Now;if(ClockErrorMs.HasValue){f.nativeToUnityMs=clockOffset;f.clockErrorMs=ClockErrorMs;}return f;}
        void CalibrateClock()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(native==null)return;double best=double.MaxValue;
            for(int i=0;i<3;i++){double a=Now;long ns=native.Call<long>("clockNanos");double b=Now;double bound=(b-a)/2;if(bound<best){best=bound;clockOffset=(a+b)/2-ns/1e6;}}
            ClockErrorMs=best;
#endif
        }
        void ReceiveMock(GestureFrame f){CountCallback();if(!SamplingActive){LastDiscard="paused";return;}f.epoch=Epoch;f.frameId=f.timestampMs;f.handPresent=f.landmarks!=null&&f.landmarks.Length==63;f.acquireMs=f.convertReadyMs=f.submitMs=f.consumeMs=f.resultMs=Now;f.nativeToUnityMs=0;f.clockErrorMs=0;Receive(f);}
        void Receive(GestureFrame frame){acceptedWindow++;Latest=frame;retries=0;double now=Now;if(previous>0){float hz=(float)(1000/Math.Max(1,now-previous));RecognitionFps=RecognitionFps==0?hz:Mathf.Lerp(RecognitionFps,hz,.2f);}previous=now;LatencyMs=frame.acquireMs>0?(float)(now-frame.acquireMs):0;Result?.Invoke(frame);}
        void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(listener!=null)listener.accepting=false;if(native!=null){native.Call("close");native.Dispose();native=null;}
#else
            if(mock!=null)mock.Result-=ReceiveMock;
#endif
            while(results.TryDequeue(out _)){}while(handResults.TryDequeue(out _)){}while(states.TryDequeue(out _)){}
        }
    }
}
