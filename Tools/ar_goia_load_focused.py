import time,json
from ar_goia_session import connect,code,save
connect()
code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var d=f.GetComponent<CampusRift.AR.ARMonsterDirector>();var h=f.GetComponent<CampusRift.AR.ARBattleHUD>();h.SetHelp(false);h.SetMenu(false);f.CheckLoad=false;f.Shrine.TakeDamage(100000);return true;');time.sleep(.3)
before=code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();return new {dead=f.Shrine.IsDead,finished=f.GetComponent<CampusRift.AR.ARMonsterDirector>().Finished};')
save('load-dead-before.json',before);assert before['dead'] and before['finished'],before
code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.GetComponent<CampusRift.AR.MockGestureSource>().enabled=false;var c=f.GetComponent<CampusRift.AR.ARGestureCheck>();c.NewSession();c.Open();return true;');time.sleep(5.5)
after=code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var d=f.GetComponent<CampusRift.AR.ARMonsterDirector>();int n=0;foreach(var e in d.Actors)if(e!=null&&e.Alive)n++;var c=f.GetComponent<CampusRift.AR.ARGestureCheck>();return new {dead=f.Shrine.IsDead,d.Finished,alive=n,result=f.GetComponent<CampusRift.AR.ARBattleHUD>().GetComponentsInChildren<UnityEngine.UI.Image>(true).FirstOrDefault(x=>x.name=="Battle result").gameObject.activeSelf};')
save('load-dead-after.json',after);assert not after['dead'] and not after['Finished'] and after['alive']==6 and not after['result'],after
for phase,kind in [('Prepare','check-prepare'),('Play','check-play'),('Negative','check-negative')]:
    if phase!='Prepare':code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();c.PreviewPhase(CampusRift.AR.ARGestureCheck.Phase.'+phase+');return true;')
    for width,height in [(2400,1080),(1600,720)]:
        code(f'UIValidation.SetResolution({width},{height});return true;');time.sleep(.3)
        code(f'UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/goiA/{kind}-{width}x{height}.png");return true;');time.sleep(.3)
    if phase!='Prepare':save(kind+'-final-text.json',code('return CampusRift.UI.ComicTextAudit.Scan("'+kind+' final changed cue");'))
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();c.PreviewPhase(CampusRift.AR.ARGestureCheck.Phase.Trials,0);return true;')
# One clean practice sample checks the clock fields just added, via the same mock -> bridge -> D1 path.
for i in range(7):code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.MockGestureSource>().Emit("None",new UnityEngine.Vector2(.5f,.5f),1,.3f);return true;');time.sleep(.05)
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();c.PreviewPhase(CampusRift.AR.ARGestureCheck.Phase.Trials,1);return true;')
for i in range(4):code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();c.GetComponent<CampusRift.AR.MockGestureSource>().Emit(CampusRift.AR.GestureSkillMapper.Labels[c.RequestedGesture],new UnityEngine.Vector2(.5f,.5f),1,.3f);return true;');time.sleep(.05)
code('var c=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARGestureCheck>();c.PreviewPhase(CampusRift.AR.ARGestureCheck.Phase.Results);c.Export();System.IO.File.WriteAllText("task/ar/goiA/mock-export-final.json",c.LastJson);return true;')
for width,height in [(2400,1080),(1600,720)]:
    code(f'UIValidation.SetResolution({width},{height});return true;');time.sleep(.3)
    code(f'UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/goiA/check-results-{width}x{height}.png");return true;');time.sleep(.3)
print(json.dumps(after))
