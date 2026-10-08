"""Focused follow-up to the stale 2.7s Level10 UI assertion; no harness edits."""
from ar_session import *
call('read_console',{'action':'clear'});call('manage_editor',{'action':'play'});time.sleep(3)
code('UnityEngine.Application.runInBackground=true;CampusRift.UI.TutorialDirector.Suppress=true;CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());CampusRift.UI.UIStateManager.Instance.EnterScene(true);CampusRift.Levels.V2DevTools.StartLevel(10);return true;')
time.sleep(1)
print(code('var d=CampusRift.Levels.LevelDirector.Instance;var player=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.PlayerMonsterHealth>();player.SetProgressionMaxHealth(10000);player.Revive(1,30);var chain=player.GetComponent<CampusRift.Combat.GenerationChainTracker>();for(int i=0;i<3;i++){chain.ResetChain();chain.Record(CampusRift.Combat.Element.Moc);chain.Record(CampusRift.Combat.Element.Hoa);chain.Record(CampusRift.Combat.Element.Tho);}d.ReleaseWin("p14-sky-beasts");d.Win();return d.State.ToString();'),flush=True)
time.sleep(1)
before=code('return new {ending=CampusRift.SkyBeast.P21StoryCinematic.Active!=null,state=CampusRift.UI.UIStateManager.Instance.State.ToString(),stars=CampusRift.Levels.LevelDirector.Instance.Result.starMask};');print(before,flush=True)
code('var c=CampusRift.SkyBeast.P21StoryCinematic.Active;if(c!=null){c.Skip();c.Skip();}return true;')
time.sleep(4)
after=code('return new {ending=CampusRift.SkyBeast.P21StoryCinematic.Active!=null,state=CampusRift.UI.UIStateManager.Instance.State.ToString(),stars=CampusRift.Levels.LevelDirector.Instance.Result.starMask};')
save('task/ar/m3-ending-followup.json',{'method':'Focused normal Level10 public ReleaseWin sky-beast hold then Win -> existing ending -> Skip to credits -> Skip credits -> actual WinRoutine results. Prior full run already proved 45 kills / 3 swords / mask7.','before':before,'after':after});print(after,flush=True)
assert before['ending'] and not after['ending'] and after['state']=='Victory' and after['stars']==7
console('task/ar/m3-ending-console.json');call('manage_editor',{'action':'stop'})

