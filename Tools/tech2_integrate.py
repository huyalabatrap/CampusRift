from pathlib import Path
import shutil,uuid
backup=Path('Backups/AR-Tech2-pre-20261006')
def edit(name,old,new):
    p=Path(name)
    if not (backup/name).exists():
        (backup/name).parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,backup/name)
    s=p.read_text(encoding='utf-8-sig')
    if old not in s:raise RuntimeError('Missing edit anchor: '+name+' '+old[:60])
    p.write_text(s.replace(old,new),encoding='utf-8')
edit('Assets/ARRift/Runtime/ARAdaptiveQuality.cs','public int SecondaryRiftLimit', 'public bool TwoHandsAllowed=>RoomProbeAllowed&&SystemInfo.processorCount>=8;\n        public bool DepthCollisionAllowed=>!Reduced&&!ThermalLimited;\n        public bool VoiceAllowed=>!Reduced&&!ThermalLimited&&SystemInfo.systemMemorySize>=4000;\n        public bool RecordingAllowed=>!Reduced&&!ThermalLimited&&SystemInfo.systemMemorySize>=4000;\n        public int SecondaryRiftLimit')
edit('Assets/ARRift/Runtime/IGestureSource.cs','public string label,handed;', 'public int handId;public string label,handed;')
edit('Assets/ARRift/Runtime/ARBattleHUD.cs','gameObject.AddComponent<ARCombatHUD>();','gameObject.AddComponent<ARTechSettings>();gameObject.AddComponent<ARDepthCollision>();gameObject.AddComponent<ARVoiceCommands>();gameObject.AddComponent<ARClipRecorder>();gameObject.AddComponent<ARTechHUD>();\n            gameObject.AddComponent<ARCombatHUD>();')
edit('Assets/ARRift/Runtime/ARBattleHUD.cs','field.MenuPaused=MenuOpen||HelpOpen||exitOpen||','field.MenuPaused=(GetComponent<ARTechHUD>()?.Blocking??false)||MenuOpen||HelpOpen||exitOpen||')
edit('Assets/ARRift/Runtime/ARRoomProbe.cs','quality.RoomProbeAllowed&&field.Root', 'quality.RoomProbeAllowed&&(GetComponent<ARTechSettings>()?.RoomProbe??true)&&field.Root')
edit('Assets/ARRift/Runtime/GestureRecognizerBridge.cs','public event Action<GestureFrame> Result;', 'public event Action<GestureFrame> Result;public event Action<GestureHandsFrame> HandsResult;\n        public int HandCount {get;private set;}=1;\n        readonly ConcurrentQueue<GestureHandsFrame> handResults=new ConcurrentQueue<GestureHandsFrame>();')
edit('Assets/ARRift/Runtime/GestureRecognizerBridge.cs','readonly ConcurrentQueue<GestureFrame> frames;readonly ConcurrentQueue<State> states;','readonly ConcurrentQueue<GestureFrame> frames;readonly ConcurrentQueue<State> states;readonly ConcurrentQueue<GestureHandsFrame> batches;')
edit('Assets/ARRift/Runtime/GestureRecognizerBridge.cs','public Listener(ConcurrentQueue<GestureFrame> f,ConcurrentQueue<State> s):','public Listener(ConcurrentQueue<GestureFrame> f,ConcurrentQueue<State> s,ConcurrentQueue<GestureHandsFrame> b):')
edit('Assets/ARRift/Runtime/GestureRecognizerBridge.cs','{frames=f;states=s;}','{frames=f;states=s;batches=b;}\n            [Preserve]public void onHands(int epoch,long frameId,string[] labels,float[] scores,string[] handed,float[] normalized,float[] world,long ts,int w,int h,long startNs,long resultNs)\n            {if(!accepting)return;var meta=new GestureFrame{epoch=epoch,frameId=frameId,timestampMs=ts,width=w,height=h,inferStartMs=startNs/1e6,resultMs=resultNs/1e6};int n=Math.Min(2,labels.Length);var hands=new GestureFrame[n];for(int i=0;i<n;i++){var f=meta;f.label=labels[i];f.score=scores[i];f.handed=handed[i];f.landmarks=new float[63];f.worldLandmarks=new float[63];Array.Copy(normalized,i*63,f.landmarks,0,63);Array.Copy(world,i*63,f.worldLandmarks,0,63);f.handPresent=true;hands[i]=f;}batches.Enqueue(new GestureHandsFrame{hands=hands,metadata=meta});}')
edit('Assets/ARRift/Runtime/GestureRecognizerBridge.cs','new Listener(results,states)','new Listener(results,states,handResults)')
edit('Assets/ARRift/Runtime/GestureRecognizerBridge.cs','public void SetSamplingActive(bool active)', '''public void SetHandCount(int count)
        {
            count=count==2?2:1;if(HandCount==count||Recovering||Busy)return;HandCount=count;Epoch++;Latest=default;Invalidated?.Invoke();
#if UNITY_ANDROID && !UNITY_EDITOR
            if(native!=null){if(!recoveryGate.Begin(Now))return;Recovering=true;Ready=false;recoveryEpoch=Epoch;native.Call("setHandCount",Epoch,count);}
#endif
        }
        public void SetSamplingActive(bool active)''')
edit('Assets/ARRift/Runtime/GestureRecognizerBridge.cs','while(results.TryDequeue(out var f))','''while(handResults.TryDequeue(out var batch))
            {
                var m=batch.metadata;if(m.frameId==submitted.frameId&&m.epoch==submitted.epoch)inFlight=false;
                if(m.epoch!=Epoch||m.frameId!=submitted.frameId||!SamplingActive||HandCount!=2)continue;
                m=AttachMetadata(m);batch.metadata=m;for(int i=0;i<batch.hands.Length;i++)batch.hands[i]=AttachMetadata(batch.hands[i]);
                HandsResult?.Invoke(batch);var primary=batch.hands.Length>0?batch.hands[0]:m;Receive(primary);
            }
            while(results.TryDequeue(out var f))''')
edit('Assets/ARRift/Runtime/GestureRecognizerBridge.cs','if(f.epoch!=Epoch||f.frameId!=submitted.frameId||!SamplingActive)continue;','if(f.epoch!=Epoch||f.frameId!=submitted.frameId||!SamplingActive||HandCount!=1)continue;')
edit('Assets/ARRift/Runtime/GestureRecognizerBridge.cs','void CalibrateClock()', '''GestureFrame AttachMetadata(GestureFrame f)
        {f.rotation=submitted.rotation;f.displayMatrix=submitted.displayMatrix;f.hasDisplayMatrix=submitted.hasDisplayMatrix;f.sensorTimestamp=submitted.sensorTimestamp;f.hasCameraPose=submitted.hasCameraPose;f.cameraPosition=submitted.cameraPosition;f.cameraRotation=submitted.cameraRotation;f.acquireMs=submitted.acquireMs;f.convertReadyMs=submitted.convertReadyMs;f.submitMs=submitted.submitMs;f.consumeMs=Now;if(ClockErrorMs.HasValue){f.nativeToUnityMs=clockOffset;f.clockErrorMs=ClockErrorMs;}return f;}
        void CalibrateClock()''')
edit('Assets/Plugins/Android/GestureBridge.kt','fun onState(epoch:', 'fun onHands(epoch: Int,frameId: Long,labels: Array<String>,scores: FloatArray,handed: Array<String>,landmarks: FloatArray,world: FloatArray,ts: Long,w: Int,h: Int,startNs: Long,resultNs: Long)\n        fun onState(epoch:')
edit('Assets/Plugins/Android/GestureBridge.kt','private var generation = 0','private var handCount=1\n    private var generation = 0')
edit('Assets/Plugins/Android/GestureBridge.kt','.setNumHands(1)','.setNumHands(handCount)')
edit('Assets/Plugins/Android/GestureBridge.kt','val categories=result.gestures().firstOrNull()', '''if(handCount==2){
                                val count=minOf(2,result.landmarks().size,result.worldLandmarks().size)
                                val labels=Array(count){"None"};val scores=FloatArray(count);val handed=Array(count){""};val norm=FloatArray(count*63);val xyz=FloatArray(count*63)
                                for(i in 0 until count){val category=result.gestures().getOrNull(i)?.maxByOrNull{it.score()};labels[i]=category?.categoryName()?:"None";scores[i]=category?.score()?:0f;handed[i]=result.handedness().getOrNull(i)?.firstOrNull()?.categoryName()?:"";result.landmarks()[i].forEachIndexed{j,p->norm[i*63+j*3]=p.x();norm[i*63+j*3+1]=p.y();norm[i*63+j*3+2]=p.z()};result.worldLandmarks()[i].forEachIndexed{j,p->xyz[i*63+j*3]=p.x();xyz[i*63+j*3+1]=p.y();xyz[i*63+j*3+2]=p.z()}}
                                listener.onHands(f.epoch,f.id,labels,scores,handed,norm,xyz,f.ts,f.w,f.h,f.start,done)
                            }else{
                            val categories=result.gestures().firstOrNull()''')
edit('Assets/Plugins/Android/GestureBridge.kt','norm,xyz,labels,scores,labels.toSet().size==8,norm.isNotEmpty(),f.ts,f.w,f.h,f.start,done)','norm,xyz,labels,scores,labels.toSet().size==8,norm.isNotEmpty(),f.ts,f.w,f.h,f.start,done)\n                            }')
edit('Assets/Plugins/Android/GestureBridge.kt','fun clockNanos(): Long', 'fun setHandCount(epoch: Int,count: Int){ready.set(false);handler.post {if(!closed.get()){handCount=if(count==2)2 else 1;create(gpu,epoch)}}}\n    fun clockNanos(): Long')
edit('Assets/Plugins/Android/mainTemplate.gradle',"implementation 'com.google.mediapipe:tasks-vision:1.0.0'", "implementation 'com.google.mediapipe:tasks-vision:1.0.0'\n    implementation 'com.alphacephei:vosk-android:0.3.75@aar'\n    implementation 'net.java.dev.jna:jna:5.18.1@aar'")
for p in Path('Assets/ARRift/Runtime').glob('ART*.cs'):
    if not p.with_suffix('.cs.meta').exists():p.with_suffix('.cs.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
for name in ['ARDepthCollision.cs','ARVoiceCommands.cs','ARClipRecorder.cs']:
    p=Path('Assets/ARRift/Runtime')/name
    if not p.with_suffix('.cs.meta').exists():p.with_suffix('.cs.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
for p in Path('Assets/Plugins/Android').glob('*Bridge.kt'):
    if not p.with_suffix('.kt.meta').exists():p.with_suffix('.kt.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
print('Integrated contract/native/settings/HUD/probe')
