UnityEngine.Application.runInBackground=true;
CampusRift.UI.UIStateManager.Instance.EnterScene(true);
var player=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();player.enabled=false;
var cc=player.GetComponent<UnityEngine.CharacterController>();cc.enabled=false;player.transform.position=new UnityEngine.Vector3(8,.13f,-7);cc.enabled=true;
var brain=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterBrain>();brain.enabled=false;
var nav=brain.GetComponent<CampusRift.Monsters.MonsterNavigation>();nav.Stop();nav.Agent.Warp(new UnityEngine.Vector3(8,.1f,-1.3f));
var skill=player.GetComponent<CampusRift.Skills.GiantHandSkill>();
var target=skill.Targeting.Evaluate(new UnityEngine.Vector3(8,.05f,-1.3f),player.transform,skill.config);
skill.Visual.Begin(target,UnityEngine.Quaternion.identity);skill.Visual.Animate(.54f,player.transform.position);
var camera=UnityEngine.Camera.main;camera.transform.position=target.point+new UnityEngine.Vector3(7,5.5f,8);camera.transform.LookAt(target.point+UnityEngine.Vector3.up*2.0f);camera.fieldOfView=48;
var rt=UnityEngine.RenderTexture.GetTemporary(1600,1000,24);var old=UnityEngine.RenderTexture.active;var prior=camera.targetTexture;
var image=new UnityEngine.Texture2D(1600,1000,UnityEngine.TextureFormat.RGB24,false);
try{camera.targetTexture=rt;camera.Render();UnityEngine.RenderTexture.active=rt;image.ReadPixels(new UnityEngine.Rect(0,0,1600,1000),0,0);image.Apply();System.IO.File.WriteAllBytes("Artifacts/GiantHandSeal/01-Summon.png",image.EncodeToPNG());}
finally{camera.targetTexture=prior;UnityEngine.RenderTexture.active=old;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(image);}
return new{target.valid,target.reason,target.height,target.visualScale,health=brain.GetComponent<CampusRift.Monsters.MonsterVitality>().Health,skill.CooldownRemaining};
