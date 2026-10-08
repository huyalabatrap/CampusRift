var enemies=CampusRift.Monsters.MonsterVitality.Active;var list=new System.Collections.Generic.List<object>();
foreach(var m in enemies){var renderers=m.GetComponentsInChildren<UnityEngine.Renderer>();foreach(var r in renderers)list.Add(new{name=m.name,position=m.transform.position.ToString(),renderer=r.name,enabled=r.enabled,off=r.forceRenderingOff,bounds=r.bounds.ToString(),mesh=r.GetComponent<UnityEngine.MeshFilter>()!=null?r.GetComponent<UnityEngine.MeshFilter>().sharedMesh.name:"skin"});}
return list;
