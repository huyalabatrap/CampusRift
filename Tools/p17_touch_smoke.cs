#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using CampusRift.Controls;
using CampusRift.Combat;
using CampusRift.UI;
using CampusRift.Progression;
using CampusRift.Monsters;
namespace CampusRift.Validation
{
    public sealed class P17TouchSmoke:MonoBehaviour
    {
        IEnumerator Start()
        {
            TutorialDirector.Suppress=true;ProfileService.Instance.UseTransient(new ProfileData());UIStateManager.Instance.EnterScene(true);var original=SettingsManager.Instance.Current.Copy();var s=original.Copy();s.ControlMode=ControlMode.Mobile;s.TelemetryConsentAsked=true;s.LocalTelemetryEnabled=false;SettingsManager.Instance.Apply(s,false);yield return null;yield return null;
            var player=FindAnyObjectByType<CampusExplorer>();var combat=player.GetComponent<PlayerCombat>();var dodge=player.GetComponent<DodgeAbility>();var target=player.GetComponent<TargetLock>();var hud=FindAnyObjectByType<MobileControlsHUD>();
            var brain=FindAnyObjectByType<MonsterBrain>();if(brain!=null)brain.gameObject.SetActive(false);
            var dummy=GameObject.CreatePrimitive(PrimitiveType.Capsule);dummy.name="P17 touch target";dummy.layer=7;dummy.transform.position=player.transform.position+target.AimForward*4+Vector3.up;
            var vitality=dummy.AddComponent<Monsters.MonsterVitality>();vitality.SetMaxHealth(50000,true);dummy.AddComponent<StatusEffectHost>();Physics.SyncTransforms();
            string result="";
            PointerEventData Pointer(TouchRole role){var rect=(RectTransform)hud.Zones.First(z=>z.role==role).transform;return new PointerEventData(EventSystem.current){pointerId=917,position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};}
            var attack=hud.Zones.First(z=>z.role==TouchRole.Attack);int swings=combat.SwingCount;var p=Pointer(TouchRole.Attack);attack.OnPointerDown(p);yield return null;yield return null;attack.OnPointerUp(p);yield return null;result+="Attack at center="+(combat.SwingCount>swings)+"\n";
            yield return new WaitForSeconds(.9f);int pierces=combat.PierceCount;p=Pointer(TouchRole.Attack);attack.OnPointerDown(p);yield return new WaitForSeconds(combat.config.holdSeconds+.2f);attack.OnPointerUp(p);yield return null;result+="Hold pierce at center="+(combat.PierceCount>pierces)+"\n";
            yield return new WaitForSeconds(1);player.RefillEnergy();int dodges=dodge.DodgeCount;var dash=hud.Zones.First(z=>z.role==TouchRole.Dash);p=Pointer(TouchRole.Dash);dash.OnPointerDown(p);yield return null;yield return null;dash.OnPointerUp(p);result+="Dodge at center="+(dodge.DodgeCount>dodges)+"\n";
            target.Release();var locking=hud.Zones.First(z=>z.role==TouchRole.LockOn);p=Pointer(TouchRole.LockOn);locking.OnPointerDown(p);yield return null;yield return null;locking.OnPointerUp(p);result+="Lock at center="+(target.Current==vitality)+"\n";
            File.WriteAllText("task/p17/touch-smoke.txt",result);target.Release();Destroy(dummy);if(brain!=null)brain.gameObject.SetActive(true);SettingsManager.Instance.Apply(original,false);ProfileService.Instance.EndTransient();Destroy(gameObject);
        }
    }
}
#endif
