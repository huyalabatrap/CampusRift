from ar_session import *
call('read_console',{'action':'clear'});call('manage_editor',{'action':'play'});background()
for i in range(60):
    if code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.MockGestureSource>()!=null;'):break
    time.sleep(1)
r=code('var mock=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.MockGestureSource>();var bridge=mock.GetComponent<CampusRift.AR.GestureRecognizerBridge>();int count=0;foreach(var label in CampusRift.AR.MockGestureSource.Labels){mock.Emit(label,new UnityEngine.Vector2(.5f,.5f));if(bridge.Latest.label!=label)throw new System.Exception("Mock label mismatch "+label);count++;}return new {labels=count,landmarks=bridge.Latest.landmarks.Length,rotationPortrait=CampusRift.AR.FrameSampler.RotationFor(UnityEngine.ScreenOrientation.Portrait),rotationLandscape=CampusRift.AR.FrameSampler.RotationFor(UnityEngine.ScreenOrientation.LandscapeLeft)};')
save('task/ar/m4-mock.json',r);print(r,flush=True);assert r['labels']==6 and r['landmarks']==63
console('task/ar/m4-mock-console.json');call('manage_editor',{'action':'stop'})
