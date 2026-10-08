#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Monsters;
using CampusRift.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;

namespace CampusRift.Controls
{
    // Explicit integration run against the shared PC/mobile controller and actual HUD.
    public sealed class BoostEnergyPlayTest : MonoBehaviour
    {
        [Serializable] sealed class Report {public List<string> passed=new List<string>(),failed=new List<string>();}
        readonly Report report=new Report();const string Output="Artifacts/BoostEnergy/";
        CampusExplorer player;CampusInput input;PlayerEnergyUI hud;GameObject floor;
        public bool ExhaustionOnly;
        Keyboard keyboard,oldKeyboard;Mouse oldMouse;InputSettings originalInput,testInput;
        GameSettings originalGame;bool running,paused;
        void Update(){if(running && !paused && UIStateManager.Instance!=null && UIStateManager.Instance.State!=UIState.Gameplay)UIStateManager.Instance.EnterScene(true);}
        void Check(bool ok,string label)
        {
            (ok?report.passed:report.failed).Add(label);
            File.WriteAllText(Output+"Validation.json",JsonUtility.ToJson(report,true));
            Debug.Log("ENERGY QA "+(ok?"PASS ":"FAIL ")+label);
        }
        void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        void Mode(ControlMode mode)
        {var settings=SettingsManager.Instance.Current.Copy();settings.ControlMode=mode;SettingsManager.Instance.Apply(settings,false);}
        IEnumerator Refill()
        {
            Keys();input.TouchMove=Vector2.zero;input.TouchSprint=false;
            float until=Time.time+12;
            while(player.Energy<player.maxEnergy && Time.time<until)yield return null;
            yield return null;
        }
        bool ExhaustionDisplayed(MobileControlsHUD mobile)
        {
            var boost=mobile.Zones.First(z=>z.role==TouchRole.Sprint);
            var arc=boost.transform.Find("Cooldown arc").GetComponent<MobileArc>();
            var fill=(float)typeof(MobileArc).GetField("fill",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(arc);
            var edge=boost.transform.Find("Rift edge").GetComponent<RiftGraphic>();
            return player.BoostExhausted && hud.Label.text==CampusRift.Localization.LocalizationService.Instance.Translate("RECOVERING") && Mathf.Abs(hud.Fill.fillAmount-player.EnergyFraction)<.01f && arc.isActiveAndEnabled && Mathf.Abs(fill-player.EnergyFraction)<.01f && edge.isActiveAndEnabled && ((Vector4)edge.color-(Vector4)new Color(.4f,.46f,.57f)).sqrMagnitude<.001f;
        }
        IEnumerator ExhaustionFailure()
        {
            Directory.CreateDirectory(Output);running=true;Application.runInBackground=true;UIStateManager.Instance.EnterScene(true);
            originalGame=SettingsManager.Instance.Current.Copy();Mode(ControlMode.Mobile);player=FindAnyObjectByType<CampusExplorer>();input=player.GetComponent<CampusInput>();
            var level=FindAnyObjectByType<CampusRift.Levels.LevelDirector>();if(level!=null)level.enabled=false;foreach(var brain in FindObjectsByType<MonsterBrain>())brain.gameObject.SetActive(false);
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(500,-.1f,600);floor.transform.localScale=new Vector3(100,.2f,500);player.spawnPosition=new Vector3(500,.1f,500);player.ReturnToSpawn();yield return new WaitForSeconds(.3f);hud=FindAnyObjectByType<PlayerEnergyUI>();
            input.TouchMove=Vector2.up;input.TouchSprint=true;float until=Time.time+12;while(!player.BoostExhausted&&Time.time<until)yield return null;yield return new WaitForSeconds(.3f);
            Check(ExhaustionDisplayed(FindAnyObjectByType<MobileControlsHUD>()),"HUD and mobile boost button both display exhaustion");input.TouchMove=Vector2.zero;input.TouchSprint=false;running=false;
            File.WriteAllText(Output+"DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");
        }
        IEnumerator Start()
        {
            if(ExhaustionOnly){yield return ExhaustionFailure();yield break;}
            Directory.CreateDirectory(Output);running=true;Application.runInBackground=true;UIStateManager.Instance.EnterScene(true);
            originalInput=InputSystem.settings;testInput=Instantiate(originalInput);InputSystem.settings=testInput;
            testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            oldKeyboard=Keyboard.current;oldMouse=Mouse.current;
            if(oldKeyboard!=null)InputSystem.DisableDevice(oldKeyboard);if(oldMouse!=null)InputSystem.DisableDevice(oldMouse);
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();
            originalGame=SettingsManager.Instance.Current.Copy();Mode(ControlMode.PC);
            player=FindAnyObjectByType<CampusExplorer>();input=player.GetComponent<CampusInput>();
            FindAnyObjectByType<MonsterBrain>().gameObject.SetActive(false);
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Energy QA floor";
            floor.transform.position=new Vector3(500,-.1f,600);floor.transform.localScale=new Vector3(100,.2f,500);
            player.spawnPosition=new Vector3(500,.1f,500);player.spawnYaw=0;player.ReturnToSpawn();
            yield return new WaitForSeconds(.2f);hud=FindAnyObjectByType<PlayerEnergyUI>();
            Check(Mathf.Approximately(player.Energy,player.maxEnergy) && player.CanBoost && !player.BoostExhausted,"New game starts with full progression-adjusted energy");
            Check(hud!=null && hud.Source==player && hud.Fill.fillAmount==1 && hud.Value.text=="100%","Existing energy placeholder now shows live full energy");
            Check(Mathf.Abs(hud.Fill.rectTransform.rect.width-360)<1,"Energy fill spans the complete existing HUD track");
            Keys(Key.W);yield return new WaitForSeconds(.7f);
            Check(Mathf.Abs(player.CurrentSpeed-player.walkSpeed)<.2f && !player.IsSprinting && Mathf.Approximately(player.Energy,player.maxEnergy),"Normal keyboard movement reaches configured walk speed without consuming energy");
            Keys(Key.W,Key.LeftShift);yield return new WaitForSeconds(1);
            Check(player.CurrentSpeed>player.runSpeed-.3f && player.IsSprinting && Mathf.Abs(player.Energy-(player.maxEnergy-player.boostEnergyPerSecond))<3,"PC boost reaches configured run speed and consumes the configured drain");
            float afterBoost=player.Energy;Keys(Key.W);yield return new WaitForSeconds(.7f);
            Check(player.Energy<=afterBoost+.2f,"Releasing boost waits before regenerating");
            yield return new WaitForSeconds(1.3f);
            Check(player.Energy>afterBoost+4 && player.CurrentSpeed<player.walkSpeed+.2f,"Energy regenerates while moving at normal speed after the delay");
            float beforeIdle=player.Energy;Keys(Key.LeftShift);yield return new WaitForSeconds(.5f);
            Check(player.Energy>=beforeIdle && !player.IsSprinting,"Holding boost without movement does not spend energy");
            yield return Refill();
            Check(player.Energy==player.maxEnergy,"Recovery is capped at maximum energy");
            Keys(Key.W,Key.LeftShift);float began=Time.time;
            while(!player.BoostExhausted && Time.time-began<player.maxEnergy/player.boostEnergyPerSecond+1)yield return null;
            Check(player.BoostExhausted && player.Energy==0 && Mathf.Abs((Time.time-began)-player.maxEnergy/player.boostEnergyPerSecond)<.5f,"Full energy supports capacity divided by drain seconds before exhaustion");
            yield return new WaitForSeconds(.3f);
            Check(!player.CanBoost && !player.IsSprinting && player.CurrentSpeed<player.walkSpeed+.2f,"Holding Shift while exhausted falls back to normal movement");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+"PC-Exhausted.png");
            float frozen=player.Energy;Vector3 position=player.transform.position;
            paused=true;UIStateManager.Instance.Pause();yield return new WaitForSecondsRealtime(.3f);
            Check(player.Energy==frozen && Vector3.Distance(position,player.transform.position)<.01f,"Pause freezes energy and movement");
            UIStateManager.Instance.Resume();paused=false;
            bool stayedLocked=true;float until=Time.time+5;
            while(player.BoostExhausted && Time.time<until)
            {stayedLocked &= !player.IsSprinting && player.EnergyFraction<.301f;yield return null;}
            Check(stayedLocked && player.CanBoost && player.EnergyFraction>=.295f,"Exhausted boost stays locked until 30 percent energy returns");
            yield return new WaitForSeconds(.3f);
            Check(player.IsSprinting && player.EnergyFraction<.3f,"Held boost resumes only after the recovery threshold");
            float beforeReset=player.Energy;player.ReturnToSpawn();
            Check(player.Energy==beforeReset,"Return-to-spawn cannot refill energy for free");
            yield return Refill();Mode(ControlMode.Mobile);yield return null;
            var mobile=FindAnyObjectByType<MobileControlsHUD>();
            var move=mobile.Zones.First(z=>z.role==TouchRole.Move);var boost=mobile.Zones.First(z=>z.role==TouchRole.Sprint);
            var movePoint=RectTransformUtility.WorldToScreenPoint(null,move.transform.position);
            var boostPoint=RectTransformUtility.WorldToScreenPoint(null,boost.transform.position);
            var left=new PointerEventData(EventSystem.current){pointerId=601,position=movePoint};
            var right=new PointerEventData(EventSystem.current){pointerId=602,position=boostPoint};
            boost.OnPointerDown(right);yield return new WaitForSeconds(.5f);
            Check(!player.IsSprinting && player.Energy==player.maxEnergy,"Holding mobile BOOST while idle spends no energy");boost.OnPointerUp(right);
            move.OnPointerDown(left);left.position=movePoint+Vector2.up*110*mobile.PixelScale;move.OnDrag(left);
            yield return new WaitForSeconds(.5f);
            Check(input.TouchMove.magnitude>.9f && !player.IsSprinting && player.Energy==player.maxEnergy,"Full joystick deflection without BOOST stays at walk speed and spends no energy");
            boost.OnPointerDown(right);
            yield return new WaitForSeconds(1);
            Check(move.Held && boost.Held && player.IsSprinting && player.CurrentSpeed>player.runSpeed-.3f && player.Energy<player.maxEnergy-player.boostEnergyPerSecond*.7f,"Held BOOST plus joystick uses the same speed and energy pool");
            boost.OnPointerUp(right);float releasedEnergy=player.Energy;yield return new WaitForSeconds(.3f);
            Check(!player.IsSprinting && player.Energy>=releasedEnergy-.1f,"Releasing mobile BOOST stops sprint and energy drain while joystick stays held");
            boost.OnPointerDown(right);
            until=Time.time+6;while(!player.BoostExhausted && Time.time<until)yield return null;
            yield return new WaitForSeconds(.3f);
            Check(player.BoostExhausted && !player.IsSprinting && player.CurrentSpeed<player.walkSpeed+.2f,"Held mobile BOOST cannot bypass exhaustion");
            Check(ExhaustionDisplayed(mobile),"HUD and mobile boost button both display exhaustion");
            File.WriteAllText(Output+"exhaustion-details.txt",$"exhausted={player.BoostExhausted}; stamina={player.EnergyFraction}; fill={hud.Fill.fillAmount}; label={hud.Label.text}; expectedMobile={CampusRift.Localization.LocalizationService.Instance.Translate("RECOVER")}\n"+string.Join("\n",mobile.GetComponentsInChildren<TMP_Text>().Select(t=>t.name+"="+t.text)));
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+"Mobile-Exhausted.png");
            boost.OnPointerUp(right);move.OnPointerUp(left);yield return Refill();
            Check(player.CanBoost && !player.BoostExhausted && hud.Fill.fillAmount>.99f,"Mobile energy and HUD recover fully after resting");
            Mode(ControlMode.PC);running=false;
            File.WriteAllText(Output+"DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");
        }
        void OnDestroy()
        {
            if(oldKeyboard!=null)InputSystem.EnableDevice(oldKeyboard);if(oldMouse!=null)InputSystem.EnableDevice(oldMouse);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(originalInput!=null)InputSystem.settings=originalInput;if(testInput!=null)Destroy(testInput);
            if(originalGame!=null && SettingsManager.Instance!=null)SettingsManager.Instance.Apply(originalGame,false);
            if(floor!=null)Destroy(floor);
        }
    }
}
#endif
