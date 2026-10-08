from ar_session import *
out=Path('task/ar/fix1')
# Capture after the one-second plane stability check, before the 1.5-second hold confirms.
save(out/'placement-setup.json',code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();f.placement.Reposition();return true;'))
time.sleep(1.3)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=false;return true;')
save(out/'placement-visible.json',code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var p=f.transform.Find("AR Battle HUD/AR HUD content/Placement guidance");var m=f.transform.Find("AR Battle HUD/AR HUD content/Placement mode");UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/fix1/placement-comic.png");return new {panel=p.gameObject.activeSelf,modes=m.gameObject.activeSelf,anchored=f.Root!=null};'))
time.sleep(.5)
console(out/'placement-console.json')
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>().Shutdown();return true;')
time.sleep(.5)
call('manage_editor',{'action':'stop'})
for i in range(30):
    if not code('return UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;'):break
    time.sleep(1)
code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
console(out/'prebuild-console.json')
print('Visual complete; Edit Mode SampleScene',flush=True)
