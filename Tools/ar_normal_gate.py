"""Run only the named required legacy smoke; preserve its older evidence."""
from ar_session import *
import shutil,sys
name=sys.argv[1];out=Path('task/ar/normal')/name;out.mkdir(parents=True,exist_ok=True)
reportdir=Path('Artifacts/Skills') if name=='SkillSet1PlayTest' else Path('Artifacts/P12/tests')
stem='SkillSet1' if name=='SkillSet1PlayTest' else name
namespace='CampusRift.Skills' if name=='SkillSet1PlayTest' else 'CampusRift.Validation'
files=[reportdir/(stem+'.json'),reportdir/(stem+'-DONE.txt')]
prior={p:p.read_bytes() if p.exists() else None for p in files}
extra=Path('task/p19/regressions/levels');prior_extra=out/'prior-levels'
if name=='Level8to10PlayTest' and extra.exists() and not prior_extra.exists():shutil.copytree(extra,prior_extra)
call('manage_editor',{'action':'stop'})
if '--opened' not in sys.argv: code('UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");return true;')
code('UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;return true;')
call('read_console',{'action':'clear'});call('manage_editor',{'action':'play'});time.sleep(4)
code('UnityEngine.Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());var s=CampusRift.UI.SettingsManager.Instance.Current.Copy();s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;s.ControlMode=CampusRift.Controls.ControlMode.PC;CampusRift.UI.SettingsManager.Instance.Apply(s,false);CampusRift.UI.UIStateManager.Instance.EnterScene(true);return true;')
stamp=time.time()
try:
    code('new UnityEngine.GameObject("AR normal regression").AddComponent<'+namespace+'.'+name+'>();return true;');print(name+' started',flush=True)
    until=time.time()+900
    while time.time()<until:
        if files[1].exists() and files[1].stat().st_mtime>=stamp:break
        time.sleep(2)
    else:raise TimeoutError(name)
    for p in files:shutil.copy2(p,out/p.name)
    if name=='Level8to10PlayTest' and extra.exists():shutil.copytree(extra,out/'screens-evidence',dirs_exist_ok=True)
    console(str(out/'console.json'));print(files[1].read_text(),flush=True)
finally:
    call('manage_editor',{'action':'stop'})
    for p,b in prior.items():
        if b is not None:p.write_bytes(b)
    if name=='Level8to10PlayTest' and prior_extra.exists():shutil.copytree(prior_extra,extra,dirs_exist_ok=True)
