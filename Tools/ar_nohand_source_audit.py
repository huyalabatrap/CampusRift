from ar_nohand import *
patterns={
'Assets/Plugins/Android/GestureBridge.kt':['handCount=1','private fun create','setNumHands','listener.onResult','setHandCount'],
'Assets/Plugins/Android/mainTemplate.gradle':['tasks-vision','vosk-android','jna:','noCompress'],
'Assets/ARRift/Editor/ARAndroidBuildProcessor.cs':['File.Copy("Assets/Plugins/Android/GestureBridge.kt"','proguard-ar.pro','SkipPermissionsDialog'],
'Assets/ARRift/Runtime/GestureRecognizerBridge.cs':['HandCount {','NativeFailure','UpdateRates','void Start','SetHandCount','SetSamplingActive','s.ready','while(results','UpdateRates();'],
'Assets/ARRift/Runtime/FrameSampler.cs':['bool low=','if(!sampling)','bridge.Submit','SetSamplingActive'],
'Assets/ARRift/Runtime/ARHandMotion.cs':['public void Cancel','BlocksStatic=false','bool hand=','!geometry.inFrame','bool pinched=','bool moving=','BlocksStatic=true'],
'Assets/ARRift/Runtime/GestureStateMachine.cs':['LastRejection','if(!hand)','if(dynamicBlocked)','f.score<','count>=3'],
'Assets/ARRift/Runtime/ARTwoHands.cs':['public void Reset','!D1[s].ReleaseReady','Motion[s].Process'],
'Assets/ARRift/Runtime/GestureSequenceMatcher.cs':['MaxGap=','public void Tick(float','if(!ready)'],
'Assets/ARRift/Runtime/GestureSkillMapper.cs':['static readonly string[] Labels'],
'Assets/ARRift/Runtime/ARSkillCaster.cs':['runtimes.Length','bool allowed=','gestures.Process','sequences.Submit','DiscAim'],
'Assets/ARRift/Runtime/ARModeSelectionHUD.cs':['bool Blocking','void Confirm'],
'Assets/ARRift/Runtime/ARBattleHUD.cs':['void UpdatePause'],
'Assets/ARRift/Runtime/ARBattlefield.cs':['Paused=Root','sampler.SetSamplingActive'],
'Assets/ARRift/Runtime/ARAdaptiveQuality.cs':['bool Reduced','if(!Reduced'],
'Assets/ARRift/Runtime/ARTechSettings.cs':['bool TwoHands,','void Update()'],
'Assets/ARRift/Runtime/ARDeviceDiagnostics.cs':['public static string RecognitionStatus','if(recognizer!=null','[ARDiag]'],
'Assets/ARRift/Runtime/ARGestureCheck.cs':['Recognition diagnostics','diagnosticPanel.gameObject.SetActive'],
'Assets/ARRift/Runtime/ARVoiceCommands.cs':['VoiceAllowed'],
'Assets/ARRift/Runtime/ARClipRecorder.cs':['StartConfirmed']}
lines={}
for rel,tokens in patterns.items():
 source=(ROOT/rel).read_text(encoding='utf-8-sig').splitlines();lines[rel]={token:[i+1 for i,line in enumerate(source) if token in line] for token in tokens}
save('source-lines.json',lines)
changed=[]
for source in (BACKUP/'Assets/ARRift').rglob('*.cs'):
 rel=source.relative_to(BACKUP);target=ROOT/rel
 if target.read_bytes()!=source.read_bytes():
  changed.append(rel.as_posix());diff=''.join(difflib.unified_diff(source.read_text(encoding='utf-8-sig').splitlines(True),target.read_text(encoding='utf-8-sig').splitlines(True),fromfile=str(rel)+' before',tofile=str(rel)+' after'))
  dest=OUT/'source-diffs'/(source.stem+'.diff');dest.parent.mkdir(exist_ok=True);dest.write_text(diff,encoding='utf-8')
save('changed-sources.json',changed);print(json.dumps(lines,ensure_ascii=True))
