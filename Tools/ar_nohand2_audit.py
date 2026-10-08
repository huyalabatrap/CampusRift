from ar_nohand2 import *
files=['Assets/ARRift/Runtime/GestureStateMachine.cs','Assets/ARRift/Runtime/ARHandMotion.cs','Assets/ARRift/Runtime/ARSkillCaster.cs','Assets/ARRift/Runtime/ARTwoHands.cs','Assets/ARRift/Runtime/GestureGeometry.cs','Assets/ARRift/Runtime/GestureRecognizerBridge.cs','Assets/ARRift/Runtime/ARRecoveryGate.cs','Assets/ARRift/Validation/ARGestureUnitTests.cs','Assets/Plugins/Android/GestureBridge.kt']
rows=[]
for name in files:
    before=(BACKUP/name).read_text(encoding='utf-8-sig');after=(ROOT/name).read_text(encoding='utf-8-sig')
    diff=''.join(difflib.unified_diff(before.splitlines(True),after.splitlines(True),fromfile='before/'+name,tofile='after/'+name))
    dest=OUT/'source-diffs'/((ROOT/name).stem+'.diff');dest.parent.mkdir(exist_ok=True);dest.write_text(diff,encoding='utf-8')
    rows.append(dict(path=name,beforeSha256=hashlib.sha256((BACKUP/name).read_bytes()).hexdigest(),afterSha256=hashlib.sha256((ROOT/name).read_bytes()).hexdigest(),lines=after.splitlines()))
save('sources.json',rows)
save('geometry-performance-findings.json',dict(
    oldLog='task/ar/device-logs/nohand-fix2.txt',oldUnityPid=21238,geometryNone=335,gestureRows=354,
    worldEvidence='ARDiag norm=63 world=63 in the allowed frames; winner/categories=1 refers to classifier scores, not landmark count.',
    reasons=['disallowed Process returns before Evaluate; suspended geometry can be the previous/default result','missing/nonfinite/degenerate/basis/bone world returns Neutral, Contradicts=false; normalized framing remains enforced','valid world can have neutral fingers or an unmatched exact finger/direction pattern; quality=true, clear=false'],
    perFrameUnknown='Old log has no vectors, fingers, inFrame, quality or geometry.reason. Exact split of 335 frames cannot be reconstructed honestly. New ARG logs expose all these reasons and lengths.',
    nativeSingleContract='Kotlin passes 63 normalized and 63 world floats independently; unchanged JNI onResult signature. Categories count does not affect either array.',
    performance='Old session has ~128-163ms end-to-end at 8.3-9.5Hz on CPU. Inference serialized to one flight; ConvertAsync overlaps inference, pending frame replaced with latest. No duplicate simultaneous GPU+CPU model in source. Inference is plausible dominant cost but old log lacks stage timings to prove it.',
    lowCostChanges='Separate graph initialization deadline of 5s from submitted inference watchdog of 1s. Pending recovery at old log line46464 is consistent with premature startup timeout. Log GPU exception before CPU fallback and C# recovery reason/target/source; ARG convertMs/inferMs/queueMs measured from existing timestamps. No change to resolution, delegate preference, JNI, model or D1 continuity threshold.'))
print('Saved',len(rows),'source diffs and geometry/performance findings')
