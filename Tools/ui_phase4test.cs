var hud=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.GameplayHUD>();
if(hud==null)throw new System.Exception("HUD missing");
var hp=hud.Health.Source;hp.TakeDamage(17);
if(!hud.Health.Value.text.StartsWith(UnityEngine.Mathf.CeilToInt(hp.CurrentHealth).ToString()))throw new System.Exception("Health event failed");
hud.Breakthrough.SetProgress(3,5);if(hud.Breakthrough.Current!=3)throw new System.Exception("Progress failed");hud.Breakthrough.SetProgress(0,5);
hud.Objective.SetObjective("Escape from the monster.");
if(hud.GetComponent<UnityEngine.CanvasGroup>().blocksRaycasts)throw new System.Exception("HUD blocks input");
return new {phase="4 PASS",health=hud.Health.Value.text,energy="placeholder",breakthrough=hud.Breakthrough.Count.text,skills=hud.Skills.Slots.Length};
