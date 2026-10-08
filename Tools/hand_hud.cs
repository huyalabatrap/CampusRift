UnityEngine.Application.runInBackground=true;CampusRift.UI.UIStateManager.Instance.EnterScene(true);
var brain=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterBrain>();brain.enabled=false;brain.GetComponent<CampusRift.Monsters.MonsterNavigation>().Stop();
var player=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();player.enabled=false;
var camera=player.followCamera;camera.transform.position=player.transform.position+new UnityEngine.Vector3(0,1.8f,-3.4f);camera.transform.LookAt(player.transform.position+new UnityEngine.Vector3(0,.1f,6));camera.fieldOfView=60;
var skill=player.GetComponent<CampusRift.Skills.GiantHandSkill>();skill.BeginPreview();
UnityEngine.ScreenCapture.CaptureScreenshot("Artifacts/GiantHandSeal/06-HUD.png");
return new{skill.IsPreviewing,skill.Target.valid,skill.Target.reason,position=skill.Target.point.ToString(),skill.config.cooldown};
