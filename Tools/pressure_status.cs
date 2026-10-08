var brain=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.MonsterBrain>();
var nav=brain.GetComponent<CampusRift.Monsters.MonsterNavigation>();
var combat=brain.GetComponent<CampusRift.Monsters.MonsterCombat>();
var player=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>();
return new {playing=UnityEditor.EditorApplication.isPlaying,brain.PressureSeconds,brain.MovementMultiplier,brain.AttackRateMultiplier,nav.ChaseSpeed,combat.WindupSeconds,combat.CooldownSeconds,brain.config.VisionDistance,brain.config.TrackingDistance,brain.config.SpeedIncreaseInterval,brain.config.ChaseSpeedPerStep,brain.config.MaxChaseSpeed,player.walkSpeed,player.runSpeed,player.Energy,player.BoostExhausted,pressureTests=UnityEngine.Object.FindObjectsByType<ShabanPressurePlayTest>().Length,combatTests=UnityEngine.Object.FindObjectsByType<ShabanCombatPlayTest>().Length,energyTests=UnityEngine.Object.FindObjectsByType<CampusRift.Controls.BoostEnergyPlayTest>().Length};
