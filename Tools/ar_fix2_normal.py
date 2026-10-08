from ar_fix2_local import *
import shutil
dest=out/'HubFlow';dest.mkdir(exist_ok=True)
assert not (dest/'HubFlow-DONE.txt').exists(),'Do not repeat completed smoke'
code('UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARXRLoaderControl>().Shutdown();return true;');time.sleep(.5)
code('UnityEditor.EditorApplication.isPlaying=false;return true;');time.sleep(3)
for i in range(40):
    if not code('return UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;'):break
    time.sleep(1)
code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
console(out/'ar-cleanup-console.json')
root=Path('Artifacts/UI');prior=out/'prior-ui';shutil.copytree(root,prior)
clear();code('UnityEditor.EditorApplication.isPlaying=true;return true;');time.sleep(4);background()
code('CampusRift.UI.TutorialDirector.Suppress=true;var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.ControlMode=CampusRift.Controls.ControlMode.PC;CampusRift.UI.SettingsManager.Instance.Apply(s,false);CampusRift.UI.UIStateManager.Instance.EnterScene(true);return true;')
stamp=time.time()
code('new UnityEngine.GameObject("AR fix2 HubFlow smoke").AddComponent<CampusRift.UI.HubFlowPlayTest>();return true;')
done=root/'HubFlow-DONE.txt'
for i in range(600):
    if done.exists() and done.stat().st_mtime>=stamp:break
    time.sleep(1)
else:raise TimeoutError('HubFlow')
for p in root.iterdir():
    if p.is_file() and p.stat().st_mtime>=stamp:shutil.copy2(p,dest/p.name)
console(dest/'console.json')
shutil.copytree(prior,root,dirs_exist_ok=True)
code('UnityEditor.EditorApplication.isPlaying=false;return true;');time.sleep(3)
code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
console(out/'normal-cleanup-console.json')
with Path('task/ar/PROGRESS.md').open('a',encoding='utf-8') as f:
    f.write('\n## AR fix2 — normal smoke hoàn tất\n- HubFlow chạy1lượt: '+(dest/'HubFlow-DONE.txt').read_text()+'. Evidence fix2/HubFlow; Artifacts/UI gốc phục hồi. ShutdownXR trước Stop; Editor SampleScene. Tiếp build fix2, chưaREPORT.\n')
print((dest/'HubFlow-DONE.txt').read_text(),flush=True)
