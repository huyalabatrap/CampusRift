var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
var field=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var host=field.gameObject;
var profile=CampusRift.Progression.ProfileService.Ensure();profile.UseTransient(new CampusRift.Progression.ProfileData());
foreach(var b in host.GetComponents<Behaviour>())
{if(!(b is CampusRift.AR.ARBattleHUD)&&!(b is CampusRift.AR.ARCombatHUD)&&!(b is CampusRift.AR.ARHandsStudyHUD)&&!(b is CampusRift.AR.ARSpaceHUD)&&!(b is CampusRift.AR.ARTechHUD))b.enabled=false;}
foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
{if(c.name=="AR Mode Selection"||c.name.Contains("safety and camera"))c.gameObject.SetActive(false);}
var selection=host.GetComponent<CampusRift.AR.ARModeSelectionHUD>();selection.enabled=false;var selectionCanvas=(RectTransform)selection.GetType().GetField("canvas",flags).GetValue(selection);selectionCanvas.GetComponentInParent<Canvas>(true).gameObject.SetActive(false);
var root=new GameObject("Photo arena only").transform;root.position=new Vector3(0,0,2);
field.placement.GetType().GetProperty("Root").GetSetMethod(true).Invoke(field.placement,new object[]{root});
field.placement.GetType().GetProperty("Radius").GetSetMethod(true).Invoke(field.placement,new object[]{.85f});
var shrine=new GameObject("Photo shrine");shrine.transform.SetParent(root,false);shrine.transform.localPosition=new Vector3(0,0,.45f);
var hp=shrine.AddComponent<CampusRift.Monsters.PlayerMonsterHealth>();hp.maxHealth=300;hp.respawnOnDefeat=false;hp.Revive(1,0);field.GetType().GetProperty("Shrine").GetSetMethod(true).Invoke(field,new object[]{hp});
field.GetType().GetProperty("Paused").GetSetMethod(true).Invoke(field,new object[]{false});
var rift=new GameObject("Photo Rift").transform;rift.SetParent(root,false);rift.localPosition=new Vector3(0,0,-.4f);CampusRift.AR.ARRune.Create(rift,.35f);field.GetType().GetProperty("Rift").GetSetMethod(true).Invoke(field,new object[]{rift});
var floor=GameObject.CreatePrimitive(PrimitiveType.Cylinder);floor.name="Photo arena platform";floor.transform.position=root.position-Vector3.up*.05f;floor.transform.localScale=new Vector3(1.7f,.05f,1.7f);
var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.12f,.17f,.25f);floor.GetComponent<Renderer>().sharedMaterial=mat;
var camera=field.placement.view;camera.transform.position=new Vector3(0,1.4f,-.2f);camera.transform.rotation=Quaternion.Euler(34,0,0);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.095f);camera.fieldOfView=55;camera.enabled=true;
var key=new GameObject("Photo key light").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.3f;key.transform.rotation=Quaternion.Euler(45,-25,0);RenderSettings.ambientLight=new Color(.4f,.4f,.5f);
var settings=host.GetComponent<CampusRift.AR.ARTechSettings>();settings.TwoHands=true;settings.StandMode=true;settings.enabled=false;
var quality=settings.Quality;quality.enabled=false;quality.GetType().GetProperty("Reduced").GetSetMethod(true).Invoke(quality,new object[]{false});
var caster=host.GetComponent<CampusRift.AR.ARSkillCaster>();caster.twoHands.Identity[0]=1;caster.twoHands.Identity[1]=2;
var hud=host.GetComponent<CampusRift.AR.ARBattleHUD>();hud.enabled=false;
foreach(var name in new[]{"placementPanel","modes","placeButton","scan","preview","toastPanel","comboPanel","castPanel","menu","help","result","exitConfirm","dismiss"})
{var value=hud.GetType().GetField(name,flags).GetValue(hud);if(value is Component)((Component)value).gameObject.SetActive(false);}
foreach(var b in new[]{host.GetComponent<CampusRift.AR.ARCombatHUD>(),(Behaviour)host.GetComponent<CampusRift.AR.ARHandsStudyHUD>(),host.GetComponent<CampusRift.AR.ARSpaceHUD>()})
{b.enabled=false;foreach(var child in host.GetComponentsInChildren<Canvas>(true))if(child.name=="AR combat HUD"||child.name=="AR hands study HUD"||child.name=="AR 360 space HUD")child.gameObject.SetActive(false);}
var technology=host.GetComponent<CampusRift.AR.ARTechHUD>();technology.Open(false);
return "Pose-only scene staged for screenshots. No gestures, casts, collisions, awards or recording requests.";
