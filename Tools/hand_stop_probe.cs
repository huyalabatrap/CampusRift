var n=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterNavigation>();var a=n.Agent;
a.isStopped=true;bool before=a.isStopped;a.ResetPath();bool after=a.isStopped;a.isStopped=true;bool reordered=a.isStopped;
return new{before,after,reordered};
