import time,json
from ar_goia_session import connect,code,save
connect()
result=code('var m=new CampusRift.AR.ARReviewMetrics();for(int i=0;i<12;i++){m.active+=1;m.RenderFrame(i<2?.1:.03);}foreach(var c in m.cells){c.n=4;c.correctUnique=4;c.first[c.g]=4;c.intents[c.g]=4;c.latency.Add(123);}m.active=600;m.unique=12000;m.clockError=.123456;m.thermalMax=3;foreach(var h in m.skillCast)h.Add(123);foreach(var h in m.skillVfx)h.Add(15);m.Reason("Success");string j=m.Export("0.1-goiA","RMX3031","CPU","winner","R","normal-estimated",true,new[]{512,384},"12345678");var parsed=Newtonsoft.Json.Linq.JObject.Parse(j);return new {rolling=m.rollingFrames.Summary(),all=m.frames.Summary(),bytes=System.Text.Encoding.UTF8.GetByteCount("[ARCheck] "+j),cells=parsed["cells"].Count()};')
assert result['rolling']==[11,30,100] and result['bytes']<=3500,result
save('metrics-focused-final.json',result)
code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.CheckLoad=true;f.Shrine.Revive(1,0);var h=f.GetComponent<CampusRift.AR.ARBattleHUD>();h.SetHelp(false);h.SetMenu(false);var m=f.GetComponent<CampusRift.AR.MockGestureSource>();m.enabled=false;var c=f.GetComponent<CampusRift.AR.ARGestureCheck>();c.NewSession();c.Open();return true;')
for width,height in [(2400,1080),(1600,720)]:
    code(f'UIValidation.SetResolution({width},{height});return true;');time.sleep(.4)
    code(f'UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/goiA/check-prepare-{width}x{height}.png");return true;');time.sleep(.4)
audit=code('return CampusRift.UI.ComicTextAudit.Scan("prepare selector focused");');save('prepare-focused-text.json',audit)
result=code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();c.PreviewPhase(CampusRift.AR.ARGestureCheck.Phase.Warmup);var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(CampusRift.AR.ARGestureCheck).GetMethod("Outcome",flags).Invoke(c,new object[]{new CampusRift.AR.GestureIntent{label="Open_Palm"},CampusRift.AR.CastOutcome.Success});return c.Metrics.allIntents;')
assert result==[0,0,0,0,0],result
save('warmup-excluded.json',result)
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();c.PreviewPhase(CampusRift.AR.ARGestureCheck.Phase.Play);return true;')
for i in range(5):
    code(f'var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();typeof(CampusRift.AR.ARGestureCheck).GetField("stageAt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(c,UnityEngine.Time.realtimeSinceStartupAsDouble-{5+i*10+.1});return true;');time.sleep(.6)
result=code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();return new {c.Metrics.scheduledVfx,allIntent=c.Metrics.allIntents};')
assert result['scheduledVfx']==5 and result['allIntent']==[0,0,0,0,0],result
save('scheduled-vfx-focused.json',result)
for width,height in [(2400,1080),(1600,720)]:
    code(f'UIValidation.SetResolution({width},{height});return true;');time.sleep(.4)
    code(f'UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/goiA/check-play-{width}x{height}.png");return true;');time.sleep(.4)
save('play-focused-text.json',code('return CampusRift.UI.ComicTextAudit.Scan("play scheduled cue focused");'))
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();c.PreviewPhase(CampusRift.AR.ARGestureCheck.Phase.Results);c.Export();System.IO.File.WriteAllText("task/ar/goiA/mock-export-final-metrics.json",c.LastJson);return true;')
print(json.dumps(result))

