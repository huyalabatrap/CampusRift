UnityEngine.Application.runInBackground=true;
var e=CampusRift.Learning.LearningService.Instance.Engine;
var result=new {breakthroughs=e.Progress.breakthroughs,skills=e.Progress.unlockedSkills.ToArray(),hp=e.Progress.healthBonus,move=e.Progress.movementBonus,lessons=e.Progress.lessons.FindAll(l=>l.rewarded).Count,savePath=CampusRift.Learning.LearningService.SavePath};
System.IO.File.WriteAllText("Artifacts/Learning/Restart.txt", "breakthroughs="+result.breakthroughs+"; mastered="+result.lessons+"; hp="+result.hp+"; movement="+result.move+"; unlocked="+string.Join(",",result.skills));
CampusRift.UI.GameSceneManager.Instance.ContinueGame();
return result;
