var go=new UnityEngine.GameObject("AcceptanceDebug");var c=go.AddComponent<UIPlayValidation>();
return new {playing=UnityEngine.Application.isPlaying,component=c!=null,go=go!=null,scene=go.scene.name,enabled=c!=null&&c.enabled,components=go.GetComponents<UnityEngine.Component>().Length};
