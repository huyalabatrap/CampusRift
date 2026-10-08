#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using CampusRift.UI;
using CampusRift.Monsters;
using CampusRift.Skills;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CampusRift.Controls
{
    public sealed class MobileChasePlayTest : MonoBehaviour
    {
        bool running=true;
        void Update(){if(running&&UIStateManager.Instance.State!=UIState.Gameplay)UIStateManager.Instance.EnterScene(true);}
        IEnumerator Start()
        {
            Application.runInBackground=true;UIStateManager.Instance.EnterScene(true);
            var settings=SettingsManager.Instance.Current.Copy();settings.ControlMode=ControlMode.Mobile;SettingsManager.Instance.Apply(settings,false);
            var p=FindAnyObjectByType<CampusExplorer>();p.ReturnToSpawn();var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=new Vector3(0,.12f,-9);cc.enabled=true;
            var brain=FindAnyObjectByType<MonsterBrain>();var nav=brain.GetComponent<MonsterNavigation>();nav.Agent.Warp(new Vector3(0,.08f,-3));brain.transform.rotation=Quaternion.LookRotation(Vector3.back);
            brain.GetComponent<MonsterMemory>().ObserveVisual(p.transform.position,Vector3.zero,Time.time);yield return new WaitForSeconds(.25f);brain.Decide();
            bool chase=brain.CurrentState==MonsterState.Chase;Vector3 monsterBefore=brain.transform.position;
            var hud=FindAnyObjectByType<MobileControlsHUD>();var input=p.GetComponent<CampusInput>();var wall=p.GetComponent<VoidWallSkill>();
            var movement=hud.Zones.First(z=>z.role==TouchRole.Move);var wallRole=hud.RoleFor(CampusAction.Wall);var ability=hud.Zones.First(z=>z.role==wallRole);
            var movePosition=RectTransformUtility.WorldToScreenPoint(null,movement.transform.position);var aimPosition=RectTransformUtility.WorldToScreenPoint(null,ability.transform.position);
            var left=new PointerEventData(EventSystem.current){pointerId=800,position=movePosition};var right=new PointerEventData(EventSystem.current){pointerId=801,position=aimPosition};
            movement.OnPointerDown(left);left.position=movePosition+Vector2.down*90*hud.PixelScale;movement.OnDrag(left);
            int charges=wall.Charges;ability.OnPointerDown(right);right.position=aimPosition+Vector2.right*90*hud.PixelScale;ability.OnDrag(right);
            yield return new WaitForSeconds(.3f);bool runningPreview=wall.IsPreviewing&&p.CurrentSpeed>2;
            ability.OnPointerUp(right);yield return new WaitForSeconds(.2f);movement.OnPointerUp(left);
            bool deployed=wall.Charges==charges-1&&wall.LastDeployed!=null;bool pursued=Vector3.Distance(monsterBefore,brain.transform.position)>.1f;
            File.WriteAllText("Artifacts/MobileControls/Chase.txt",$"Monster chase={chase}; running touch preview={runningPreview}; one barrier deployed={deployed}; monster pursuit progressed={pursued}");
            ScreenCapture.CaptureScreenshot("Artifacts/MobileControls/07-Mobile-Chase.png");
            yield return null;
            // ItemBar's PC layout is built lazily on its first activation (P08/P09).
            // Warm both layouts before checking that subsequent switches allocate nothing.
            settings.ControlMode=ControlMode.PC;SettingsManager.Instance.Apply(settings,false);yield return null;yield return null;
            settings.ControlMode=ControlMode.Mobile;SettingsManager.Instance.Apply(settings,false);yield return null;yield return null;
            int objects=hud.GetComponentsInChildren<Transform>(true).Length;var materials=hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Select(g=>g.material).ToArray(); // P09/LOOK HUD ownership; unrelated expiring world VFX are outside this assertion.
            var names=hud.GetComponentsInChildren<Transform>(true).Select(t=>t.name).ToArray();
            for(int i=0;i<20;i++){settings.ControlMode=i%2==0?ControlMode.PC:ControlMode.Mobile;SettingsManager.Instance.Apply(settings,false);yield return null;}
            yield return new WaitForEndOfFrame(); // ItemBar swaps layout in Update; Destroy settles at frame end.
            bool stable=objects==hud.GetComponentsInChildren<Transform>(true).Length&&materials.SequenceEqual(hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Select(g=>g.material))&&wall.Charges==charges-1;
            File.AppendAllText("Artifacts/MobileControls/Chase.txt",$"\nTwenty control-mode changes keep UI objects/materials and inventory stable={stable}");
            File.WriteAllText("Artifacts/MobileControls/Chase-details.txt",$"objects={objects}/{hud.GetComponentsInChildren<Transform>(true).Length}; graphics={materials.Length}/{hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Length}; inventory={wall.Charges}/{charges-1}; materialReferences={materials.SequenceEqual(hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Select(g=>g.material))}\nBEFORE: "+string.Join("/",names)+"\nAFTER: "+string.Join("/",hud.GetComponentsInChildren<Transform>(true).Select(t=>t.name)));
            brain.gameObject.SetActive(false);p.ReturnToSpawn();running=false;
        }
    }
}
#endif
