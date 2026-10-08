from ar_session import *
out=Path('task/ar/fix1')
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=false;return true;')
for yaw in [135,150,165,180,195,210,225,180]:
    code('var c=UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();c.transform.position=new UnityEngine.Vector3(.75f,1.5f,.1f);c.transform.rotation=UnityEngine.Quaternion.Euler(40,'+str(yaw)+',0);return true;')
    time.sleep(.3)
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=true;return true;')
# Allow observations to settle; hold the preview before automatic placement.
for i in range(30):
    time.sleep(.2)
    if code('return UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().Message.Contains("giữ yên");'):
        break
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.RiftPlacementService>().enabled=false;return true;')
save(out/'placement-final.json',code('var f=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var p=f.transform.Find("AR Battle HUD/AR HUD content/Placement guidance");var m=f.transform.Find("AR Battle HUD/AR HUD content/Placement mode");var combo=f.transform.Find("AR Battle HUD/AR HUD content/Combo lesson");UnityEngine.ScreenCapture.CaptureScreenshot("task/ar/screens/fix1/placement-comic.png");return new {panel=p.gameObject.activeSelf,modes=m.gameObject.activeSelf,combo=combo.gameObject.activeSelf,anchored=f.Root!=null,previewMessage=f.placement.Message,runeShaderError=UnityEditor.ShaderUtil.ShaderHasError(UnityEngine.Shader.Find("Campus Rift/AR/Runic Circle"))};'))
time.sleep(.5)
console(out/'placement-final-console.json')
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>().Shutdown();return true;')
time.sleep(.5)
call('manage_editor',{'action':'stop'})
for i in range(30):
    if not code('return UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;'):break
    time.sleep(1)
console(out/'cleanup-console.json')
# Scene opening can return a null response while Unity loads; verify state in a separate call.
try:code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
except RuntimeError as e: print('Scene-open response:',e,flush=True)
print('Placement capture and explicit cleanup complete',flush=True)
