var p=UnityEngine.Object.FindAnyObjectByType<CampusRift.Skills.GiantHandSkill>();
var m=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterVitality>();
return new{playing=UnityEditor.EditorApplication.isPlaying,paused=UnityEditor.EditorApplication.isPaused,time=UnityEngine.Time.time,scale=UnityEngine.Time.timeScale,p.IsCasting,p.CooldownRemaining,p.ImpactCount,p.LastHitCount,m.Health,m.SuppressionRemaining,m.Suppressed,particles=p.Visual.LiveParticles};
