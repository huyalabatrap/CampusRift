from pathlib import Path
p=Path('Assets/Plugins/Android/GestureBridge.kt');s=p.read_text(encoding='utf-8-sig').replace('    fun recover(epoch:', '    fun clockNanos(): Long = SystemClock.elapsedRealtimeNanos()\n    fun recover(epoch:');p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/IGestureSource.cs');s=p.read_text(encoding='utf-8-sig').replace('public double sensorTimestamp,', 'public double? nativeToUnityMs,clockErrorMs;\n        public double sensorTimestamp,');p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/GestureRecognizerBridge.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('public string DevDelegate=', 'public double? ClockErrorMs {get;private set;}double clockOffset;\n        public string DevDelegate=')
s=s.replace('while(results.TryDequeue(out _)){}Latest=default;Invalidated?.Invoke();', 'while(results.TryDequeue(out _)){}Latest=default;Invalidated?.Invoke();if(active)CalibrateClock();')
s=s.replace('Ready=true;Recovering=false;recoveryGate.Complete();inFlight=false;Error="";retryAt=0;', 'Ready=true;Recovering=false;recoveryGate.Complete();inFlight=false;Error="";retryAt=0;CalibrateClock();')
s=s.replace('f.submitMs=submitted.submitMs;f.consumeMs=Now;', 'f.submitMs=submitted.submitMs;f.consumeMs=Now;if(ClockErrorMs.HasValue){f.nativeToUnityMs=clockOffset;f.clockErrorMs=ClockErrorMs;}')
s=s.replace('f.acquireMs=f.convertReadyMs=f.submitMs=f.consumeMs=Now;', 'f.acquireMs=f.convertReadyMs=f.submitMs=f.consumeMs=f.resultMs=Now;f.nativeToUnityMs=0;f.clockErrorMs=0;')
s=s.replace('        void ReceiveMock(', '''        void CalibrateClock()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(native==null)return;double best=double.MaxValue;
            for(int i=0;i<3;i++){double a=Now;long ns=native.Call<long>("clockNanos");double b=Now;double bound=(b-a)/2;if(bound<best){best=bound;clockOffset=(a+b)/2-ns/1e6;}}
            ClockErrorMs=best;
#endif
        }
        void ReceiveMock(''')
p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/GestureStateMachine.cs');s=p.read_text(encoding='utf-8-sig').replace('public double firstValidMs,', 'public double? triggerResultMs;public double firstValidMs,').replace('firstValidMs=firstValid,triggerConsumeMs=', 'firstValidMs=firstValid,triggerResultMs=f.nativeToUnityMs.HasValue?(double?)(f.resultMs+f.nativeToUnityMs.Value):null,triggerConsumeMs=');p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARReviewMetrics.cs');s=p.read_text(encoding='utf-8-sig').replace('acquireConsume=new ARMetricHistogram();', 'acquireConsume=new ARMetricHistogram(),submitInfer=new ARMetricHistogram(),resultConsume=new ARMetricHistogram();public double? clockError;')
s=s.replace('["clockErrorMs"]=null', '["clockErrorMs"]=clockError').replace('["submitInfer"]=null,["resultConsume"]=null', '["submitInfer"]=submitInfer.Summary(),["resultConsume"]=resultConsume.Summary()')
p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARGestureCheck.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('active=Stage>=Phase.Trials&&Stage<=Phase.Play&&!field.Paused&&dt<.25', 'active=Stage>=Phase.Trials&&Stage<=Phase.Play&&!field.Paused')
s=s.replace('Metrics.frames.Add(Time.unscaledDeltaTime*1000);Metrics.below27=Time.unscaledDeltaTime>1f/27?', 'Metrics.frames.Add(dt*1000);Metrics.below27=dt>1f/27?')
s=s.replace('if(f.acquireMs>0)Metrics.acquireConsume.Add(f.consumeMs-f.acquireMs);', 'if(f.acquireMs>0)Metrics.acquireConsume.Add(f.consumeMs-f.acquireMs);if(f.nativeToUnityMs.HasValue){Metrics.clockError=Math.Max(Metrics.clockError??0,f.clockErrorMs??0);if(f.inferStartMs>0)Metrics.submitInfer.Add(Math.Max(0,f.inferStartMs+f.nativeToUnityMs.Value-f.submitMs));Metrics.resultConsume.Add(Math.Max(0,f.consumeMs-f.resultMs-f.nativeToUnityMs.Value));}')
s=s.replace('trialVfx=now-intent.triggerConsumeMs;', 'trialVfx=intent.triggerResultMs.HasValue?(double?)(now-intent.triggerResultMs.Value):null;')
p.write_text(s,encoding='utf-8')
