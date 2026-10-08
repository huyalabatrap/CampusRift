from ar_session import *
Path('task/ar/m7').mkdir(exist_ok=True)
print(code('CampusRift.AR.Editor.ARRiftMilestones.M7();UIValidation.SetResolution(1920,1080);return true;'),flush=True)
console('task/ar/m7/compile-console.json')
call('read_console',{'action':'clear'});call('manage_editor',{'action':'play'});background()
time.sleep(2)
save('task/ar/m7/before-hub.json',code('return UnityEngine.JsonUtility.ToJson(CampusRift.Progression.ProfileService.Instance.Data);'))
code('CampusRift.UI.UIStateManager.Instance.EnterScene(false);CampusRift.UI.UIStateManager.Instance.OpenHub();return true;')
time.sleep(2)
save('task/ar/m7/before-ar.json',code('return UnityEngine.JsonUtility.ToJson(CampusRift.Progression.ProfileService.Instance.Data);'))
print(code('new UnityEngine.GameObject("AR navigation smoke").AddComponent<CampusRift.AR.ARNavigationPlayTest>();return true;'),flush=True)
stamp=time.time()
while time.time()<stamp+700:
 if Path('task/ar/m7/DONE.txt').exists():break
 time.sleep(2)
else:raise TimeoutError('AR navigation')
console('task/ar/m7/console.json');print(Path('task/ar/m7/DONE.txt').read_text(),flush=True)

save('task/ar/m7/after-ar.json',code('return UnityEngine.JsonUtility.ToJson(CampusRift.Progression.ProfileService.Instance.Data);'))
