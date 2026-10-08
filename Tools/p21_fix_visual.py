from pathlib import Path
import shutil
root=Path('task/p21')
archive=root/'diagnostics/capture-second'
archive.mkdir(parents=True,exist_ok=True)
for p in [root/'Cinematic.json',root/'Cinematic-DONE.txt',root/'cinematic-console.json',root/'cinematic-run.json']:
    if p.exists():shutil.copy2(p,archive/p.name)
if not (archive/'screens').exists():shutil.copytree(root/'screens',archive/'screens')
def edit(path,old,new):
    p=Path(path);s=p.read_text(encoding='utf-8');assert old in s,(path,old);p.write_text(s.replace(old,new),encoding='utf-8')
story='Assets/SkyBeast/Runtime/P21StoryCinematic.cs'
menu='Assets/CampusRiftUI/Runtime/P21CreditsMenu.cs'
for path in [story,menu]:
    edit(path,'.text.Split(new[]{"\\n---PAGE---\\n"}', '.text.Replace("\\r\\n","\\n").Split(new[]{"\\n---PAGE---\\n"}') if path==menu else edit(path,'asset.text:"Campus Rift").Split','asset.text:"Campus Rift").Replace("\\r\\n","\\n").Split')
cine='Assets/SkyBeast/Runtime/HeavenSwordCinematic.cs'
edit(cine,'float cameraFov,age,duration;','float cameraFov,cameraFar,age,duration;')
edit(cine,'cameraFov=camera.fieldOfView;','cameraFov=camera.fieldOfView;cameraFar=camera.farClipPlane;camera.farClipPlane=Mathf.Max(cameraFar,700);')
edit(cine,'camera.fieldOfView=cameraFov;camera.allowHDR','camera.fieldOfView=cameraFov;camera.farClipPlane=cameraFar;camera.allowHDR')
edit(story,'Quaternion camRotation;','Quaternion camRotation,playerRotation;')
edit(story,'readonly List<Canvas> hidden=new List<Canvas>();','readonly List<Canvas> hidden=new List<Canvas>();\n        readonly List<Renderer> avatarHidden=new List<Renderer>();')
edit(story,'playerPosition=explorer.transform.position;explorer.CancelDash();','playerPosition=explorer.transform.position;playerRotation=explorer.transform.rotation;explorer.CancelDash();')
edit(story,'explorer.transform.position=new Vector3(-5,.13f,2);if(on)cc.enabled=true;', 'explorer.transform.SetPositionAndRotation(new Vector3(-5,.13f,2),Quaternion.Euler(0,45,0));if(on)cc.enabled=true;foreach(var r in explorer.characterAnimator.GetComponentsInChildren<Renderer>())if(!r.enabled){avatarHidden.Add(r);r.enabled=true;}')
edit(story,'if(Ending){from=center+new Vector3(60,35,-100);to=new Vector3(10,7,-18);look=Vector3.Lerp(center+Vector3.up*18,new Vector3(-5,1.6f,2),Mathf.SmoothStep(0,1,p));sky?.SetDawnProgress', 'if(Ending){if(p<.65f){from=center+new Vector3(60,35,-100);to=center+new Vector3(45,28,-90);look=center+Vector3.up*18;}else{float dolly=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,p));from=to=new Vector3(-5,.13f,2)+Vector3.Lerp(new Vector3(6.36f,3.87f,6.36f),new Vector3(5,3.4f,5),dolly);look=new Vector3(-5,1.5f,2);}sky?.SetDawnProgress')
edit(story,'cam.fieldOfView=Ending?60:65;', 'cam.fieldOfView=Ending?(p<.65f?60:48):65;')
edit(story,'explorer.transform.position=playerPosition;if(on)cc.enabled=true;', 'explorer.transform.SetPositionAndRotation(playerPosition,playerRotation);if(on)cc.enabled=true;foreach(var r in avatarHidden)if(r!=null)r.enabled=false;avatarHidden.Clear();')
harness='Assets/SkyBeast/Validation/P21CinematicSmoke.cs'
edit(harness,'UIStateManager.Instance.Pause();float pausedProgress=story.Progress;', 'story.HoldForCapture=false;UIStateManager.Instance.Pause();float pausedProgress=story.Progress;')
edit(harness,'cine.SeekForCapture(.40f);UIStateManager.Instance.Pause();', 'cine.SeekForCapture(.40f);cine.HoldForCapture=false;UIStateManager.Instance.Pause();')
edit(harness,'hit&&Time.timeScale==1&&!world.player.GetComponent<Controls.CampusInput>().UltimateLocked,"Hit once / unscaled / input restored L"+level', 'hit&&Time.timeScale==1&&(level==10?P21StoryCinematic.Active!=null:!world.player.GetComponent<Controls.CampusInput>().UltimateLocked),"Hit once / unscaled / correct story input handoff L"+level')
edit(harness,'UIStateManager.Instance.State==UIState.Victory&&d.WinsRaised==1', 'UIStateManager.Instance.State==UIState.Victory&&d.WinsRaised==1&&!world.player.GetComponent<Controls.CampusInput>().UltimateLocked')
print('Archived second attempt; corrected far clip, courtyard shot, CRLF paging, and story input handoff assertion.')
