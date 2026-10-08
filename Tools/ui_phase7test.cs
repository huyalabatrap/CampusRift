var state=CampusRift.UI.UIStateManager.Instance;var health=UnityEngine.Object.FindAnyObjectByType<CampusRift.Monsters.PlayerMonsterHealth>();
if(health==null)throw new System.Exception("Gameplay not ready");
// Reset only the temporary Play Mode fixture: the active monster may have killed the idle test player.
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
typeof(CampusRift.Monsters.PlayerMonsterHealth).GetField("currentHealth",flags).SetValue(health,health.maxHealth);
typeof(CampusRift.Monsters.PlayerMonsterHealth).GetField("protectedUntil",flags).SetValue(health,0f);
state.EnterScene(true);var position=health.transform.position;health.TakeDamage(10000);
if(health.CurrentHealth>0||state.State!=CampusRift.UI.UIState.GameOver||UnityEngine.Time.timeScale!=0)throw new System.Exception("Death hook failed");
if(UnityEngine.Vector3.Distance(position,health.transform.position)>.01f)throw new System.Exception("Unexpected automatic respawn");
state.Back();if(state.State!=CampusRift.UI.UIState.GameOver)throw new System.Exception("GameOver ESC priority failed");
return "Phase 7 PASS: death event, no automatic respawn, frozen GameOver, ESC blocked.";
