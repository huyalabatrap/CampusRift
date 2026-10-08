var hud=UnityEngine.Object.FindAnyObjectByType<CampusRift.Controls.MobileControlsHUD>();
var z=hud.Zones.First(x=>x.role==CampusRift.Controls.TouchRole.Skill4);
var p=RectTransformUtility.WorldToScreenPoint(null,z.transform.position);
var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=p},hits);
var r=hud.Input.Loadout.Get(3);var spirit=r.GetComponent<CampusRift.Combat.SpiritPower>();
return new{point=p.ToString(),width=Screen.width,height=Screen.height,r.IsReady,r.IsUnlocked,r.HasSpirit,r.CooldownRemaining,spirit.Current,spirit.Max,active=r.isActiveAndEnabled,state=r.GetState().ToString(),inputAllowed=hud.Input.Allowed,hits=hits.Select(x=>x.gameObject.name+":"+x.gameObject.GetComponentInParent<CampusRift.Controls.MobileTouchZone>()?.role.ToString()).ToArray()};
