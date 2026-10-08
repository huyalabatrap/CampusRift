from pathlib import Path
root=Path.cwd()
def change(rel,a,b):
 p=root/rel;s=p.read_text(encoding='utf-8-sig');assert a in s,rel;p.write_text(s.replace(a,b,1),encoding='utf-8')
rel='Assets/ARRift/Runtime/ARHandMotion.cs'
change(rel,'public bool BlocksStatic {get;private set;}','public bool BlocksStatic {get;private set;}\n        public string Reason {get;private set;}="idle";')
change(rel,'State=ARHandMotionState.Released;BlocksStatic=true;Velocity=releaseVelocity=Vector3.zero;', '''// Invalid data or lifecycle cancellation is not evidence of a dynamic motion.
            // D1 retains its own held/release latch after any actual blocked frame.
            State=ARHandMotionState.Released;BlocksStatic=false;Reason="cancelled";Velocity=releaseVelocity=Vector3.zero;''')
change(rel,'if(!allowed||camera==null||f.acquireMs>0&&f.consumeMs-f.acquireMs>250){Cancel();return;}', 'if(!allowed||camera==null||f.acquireMs>0&&f.consumeMs-f.acquireMs>250){Cancel();Reason=!allowed?"paused":camera==null?"no-camera":"stale";return;}')
change(rel,'bool hand=f.landmarks!=null&&f.landmarks.Length==63&&f.worldLandmarks!=null&&f.worldLandmarks.Length==63;', 'bool hand=f.handPresent||f.landmarks!=null&&f.landmarks.Length==63;')
change(rel,'if(!hand)\n            {','if(!hand)\n            {\n                Reason="no-hand";')
change(rel,'if(!geometry.inFrame||!geometry.quality||dt>.15){Cancel();return;}','if(!geometry.inFrame||!geometry.quality||dt>.15){Cancel();Reason=dt>.15?"capture-gap":geometry.reason;return;}')
change(rel,'if(hadPoint&&(Vector3.Distance(posePosition,priorCamera)>.25f||Quaternion.Angle(poseRotation,priorRotation)>25)){Cancel();return;}', 'if(hadPoint&&(Vector3.Distance(posePosition,priorCamera)>.25f||Quaternion.Angle(poseRotation,priorRotation)>25)){Cancel();Reason="camera-jump";return;}')
change(rel,'BlocksStatic=true;\n                if(Velocity.magnitude', 'BlocksStatic=true;Reason="pinching";\n                if(Velocity.magnitude')
change(rel,'BlocksStatic=true;quietAt=-1;\n                if(pinchArmed)', 'BlocksStatic=true;Reason="pinch";quietAt=-1;\n                if(pinchArmed)')
change(rel,'BlocksStatic=true;quietAt=-1;\n                if(State!=ARHandMotionState.Swiping', 'BlocksStatic=true;Reason="swipe";quietAt=-1;\n                if(State!=ARHandMotionState.Swiping')
# There are two quiet-rearm assignments; give both their diagnostic reason.
p=root/rel;s=p.read_text(encoding='utf-8');s=s.replace('State=ARHandMotionState.Idle;BlocksStatic=false;pinchArmed=', 'State=ARHandMotionState.Idle;BlocksStatic=false;Reason="idle";pinchArmed=');p.write_text(s,encoding='utf-8')
# New identity after an observed absence must keep the release already established by D1.
rel='Assets/ARRift/Runtime/ARTwoHands.cs'
change(rel,'if(!active[s]||now-seen[s]>250||d>.25f){D1[s].Suspend(true);pending[s]=null;Identity[s]=++serial;active[s]=true;}', '''if(!active[s]||now-seen[s]>250||d>.25f)
                    {
                        // Do not undo a genuine 200ms/3-frame absence on reacquisition.
                        // A live wrist jump is ambiguous and still requires release.
                        if(!D1[s].ReleaseReady||active[s]&&now-seen[s]<=250&&d>.25f)D1[s].Suspend(true);
                        Motion[s].Cancel();pending[s]=null;Identity[s]=++serial;active[s]=true;
                    }''')
rel='Assets/ARRift/Runtime/GestureStateMachine.cs'
change(rel,'public string Decision {get;private set;}="idle";','''string decision="idle";
        public string LastRejection {get;private set;}="none";
        public string Decision {get=>decision;private set{decision=value;if(value!="idle"&&value!="charging"&&value!="fire"&&value!="held")LastRejection=value;}}''')
rel='Assets/ARRift/Runtime/GestureRecognizerBridge.cs'
change(rel,'public string Error {get;private set;}="";','''public string Error {get;private set;}="";
        public string LastNativeError {get;private set;}="";
        public long ResultCount {get;private set;}
        public float ResultsPerSecond {get;private set;}
        public float AcceptedPerSecond {get;private set;}
        public string LastDiscard {get;private set;}="none";
        double rateAt;int rawWindow,acceptedWindow;
        void NativeFailure(string message){Error=message;LastNativeError=message;Debug.LogWarning("[ARGesture] native-error="+message);}
        void CountCallback(){rawWindow++;ResultCount++;}
        void UpdateRates(){double now=Now;if(rateAt==0){rateAt=now;return;}double seconds=(now-rateAt)/1000;if(seconds<1)return;ResultsPerSecond=(float)(rawWindow/seconds);AcceptedPerSecond=(float)(acceptedWindow/seconds);rawWindow=acceptedWindow=0;rateAt=now;}''')
change(rel,'Ready=false;flightAt=Now;','Ready=false;DelegateName="pending";flightAt=Now;')
change(rel,'catch(Exception e){Error=e.Message;Ready=false;}', 'catch(Exception e){NativeFailure(e.Message);Ready=false;}')
change(rel,'catch(Exception e){Error=e.Message;Recover();return false;}', 'catch(Exception e){NativeFailure(e.Message);Recover();return false;}')
change(rel,'else {Ready=false;Error=s.error;', 'else {Ready=false;NativeFailure(s.error);')
change(rel,'var m=batch.metadata;if(m.frameId', 'CountCallback();var m=batch.metadata;if(m.frameId')
change(rel,'if(m.epoch!=Epoch||m.frameId!=submitted.frameId||!SamplingActive||HandCount!=2)continue;', 'if(m.epoch!=Epoch||m.frameId!=submitted.frameId||!SamplingActive||HandCount!=2){LastDiscard=m.epoch!=Epoch?"epoch":m.frameId!=submitted.frameId?"frame-id":!SamplingActive?"paused":"hand-count";continue;}')
change(rel,'if(f.frameId==submitted.frameId&&f.epoch==submitted.epoch)inFlight=false;', 'CountCallback();if(f.frameId==submitted.frameId&&f.epoch==submitted.epoch)inFlight=false;')
change(rel,'if(f.epoch!=Epoch||f.frameId!=submitted.frameId||!SamplingActive||HandCount!=1)continue;', 'if(f.epoch!=Epoch||f.frameId!=submitted.frameId||!SamplingActive||HandCount!=1){LastDiscard=f.epoch!=Epoch?"epoch":f.frameId!=submitted.frameId?"frame-id":!SamplingActive?"paused":"hand-count";continue;}')
change(rel,'#if UNITY_ANDROID && !UNITY_EDITOR\n            if(recoveryGate.Overdue', 'UpdateRates();\n#if UNITY_ANDROID && !UNITY_EDITOR\n            if(recoveryGate.Overdue')
change(rel,'void ReceiveMock(GestureFrame f){if(!SamplingActive)return;', 'void ReceiveMock(GestureFrame f){CountCallback();if(!SamplingActive){LastDiscard="paused";return;}')
change(rel,'void Receive(GestureFrame frame){Latest=frame;', 'void Receive(GestureFrame frame){acceptedWindow++;Latest=frame;')
# Use readable text in the new development build, preserving the original source's UTF-8.
p=root/rel;s=p.read_text(encoding='utf-8');s=s.replace('Ä\x90ang khÃ´i phá»¥c nháº\xadn dáº¡ng','Đang khôi phục nhận dạng').replace('Nháº\xadn dáº¡ng chÆ°a phá»¥c há»“i Â· chá»\x9d Ä‘Ã³ng model','Nhận dạng chưa phục hồi · chờ đóng model').replace('Nháº\xadn dáº¡ng lá»—i Â· Thá»\xad láº¡i','Nhận dạng lỗi · Thử lại');p.write_text(s,encoding='utf-8')
print('Motion and recognition diagnostics patched; D1 thresholds unchanged.')
