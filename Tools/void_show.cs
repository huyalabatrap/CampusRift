var skill=UnityEngine.Object.FindAnyObjectByType<CampusRift.Skills.VoidWallSkill>();
CampusRift.UI.UIStateManager.Instance.EnterScene(true);
UnityEngine.Camera.main.transform.rotation=UnityEngine.Quaternion.Euler(12,0,0);
skill.BeginPreview();
bool deployed=skill.Confirm();
var camera=UnityEngine.Camera.main;camera.transform.position=new UnityEngine.Vector3(11,2.0f,3.8f);camera.transform.LookAt(new UnityEngine.Vector3(8,1.15f,-1.3f));camera.fieldOfView=43;
return new{deployed,skill.Charges,active=CampusRift.Skills.VoidWall.Active.Count};
