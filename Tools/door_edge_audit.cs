var names=new[]{"Block_A_SecDoor_Side","Block_A_Stair_East_GndExit"};
return UnityEngine.Object.FindObjectsByType<UnityEngine.Collider>().Where(c=>names.Any(n=>c.name.Contains(n))).Select(c=>new{c.name,type=c.GetType().Name,center=c.bounds.center.ToString(),size=c.bounds.size.ToString(),enabled=c.enabled,convex=c is UnityEngine.MeshCollider&&((UnityEngine.MeshCollider)c).convex}).ToArray();
