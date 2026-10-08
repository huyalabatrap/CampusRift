"""Required Hub gates, once each, in the session that just exited AR twice."""
from ar_session import *
import shutil
root=Path('Artifacts/UI');out=Path('task/ar/m7/hub');out.mkdir(parents=True,exist_ok=True)
prior=out/'prior-ui';shutil.copytree(root,prior,dirs_exist_ok=True)
code('CampusRift.UI.TutorialDirector.Suppress=true;var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.ControlMode=CampusRift.Controls.ControlMode.PC;CampusRift.UI.SettingsManager.Instance.Apply(s,false);return true;')
background()
for name in ['HubFlow','HubLayout']:
    dest=out/name;dest.mkdir(exist_ok=True);stamp=time.time()
    call('read_console',{'action':'clear'})
    print(code('new UnityEngine.GameObject("AR '+name+'").AddComponent<CampusRift.UI.'+name+'PlayTest>();return true;'),flush=True)
    done=root/(name+'-DONE.txt')
    while time.time()<stamp+600:
        if done.exists() and done.stat().st_mtime>=stamp:break
        time.sleep(2)
    else:raise TimeoutError(name)
    for p in root.iterdir():
        if p.is_file() and p.stat().st_mtime>=stamp:shutil.copy2(p,dest/p.name)
    console(str(dest/'console.json'));print(done.read_text(),flush=True)
shutil.copytree(prior,root,dirs_exist_ok=True)
save('task/ar/m7/post-hub.json',code('return new {loader=UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader!=null,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,timeScale=UnityEngine.Time.timeScale};'))
