#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.UI;
using CampusRift.Skills;
using CampusRift.Monsters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TouchPhase=UnityEngine.InputSystem.TouchPhase;

namespace CampusRift.Controls
{
    // Explicit MCP QA harness; never saved on a scene or prefab.
    public sealed class MobileControlPlayTest : MonoBehaviour
    {
        [Serializable] public class Results { public List<string> passed=new List<string>(),failed=new List<string>(),notes=new List<string>(); }
        Results results=new Results();const string Root="Artifacts/MobileControls/";
        MobileControlsHUD hud;CampusExplorer player;CampusInput input;VoidWallSkill wall;GiantHandSkill hand;
        Keyboard keyboard,oldKeyboard;Mouse oldMouse,mouse;Touchscreen touch;InputSettings originalSettings,testSettings;
        bool forceGameplay=true;GameSettings originalGame;bool originalSaved;string saved;
        void Update(){if(forceGameplay&&UIStateManager.Instance.State!=UIState.Gameplay)UIStateManager.Instance.EnterScene(true);}
        void Check(bool ok,string label){(ok?results.passed:results.failed).Add(label);Write();Debug.Log("CONTROL QA "+(ok?"PASS ":"FAIL ")+label);}
        void Write(){File.WriteAllText(Root+"Validation.json",JsonUtility.ToJson(results,true));}
        Vector2 Point(TouchRole role)=>RectTransformUtility.WorldToScreenPoint(null,hud.Zones.First(z=>z.role==role).transform.position);
        void Touch(int id,TouchPhase phase,Vector2 point){InputSystem.QueueStateEvent(touch,new TouchState{touchId=id,phase=phase,position=point,pressure=phase==TouchPhase.Ended?0:1});}
        IEnumerator Frames(int n=3){for(int i=0;i<n;i++)yield return null;}
        void Mode(ControlMode mode){var s=SettingsManager.Instance.Current.Copy();s.ControlMode=mode;SettingsManager.Instance.Apply(s,false);}
        void Position(Vector3 position)
        {var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}
        IEnumerator Screenshot(string file)
        {yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Root+file+".png");yield return Frames();}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Root);Application.runInBackground=true;
            originalSettings=InputSystem.settings;testSettings=Instantiate(originalSettings);InputSystem.settings=testSettings;
            testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            oldKeyboard=Keyboard.current;oldMouse=Mouse.current;if(oldKeyboard!=null)InputSystem.DisableDevice(oldKeyboard);if(oldMouse!=null)InputSystem.DisableDevice(oldMouse);
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();touch=InputSystem.AddDevice<Touchscreen>();keyboard.MakeCurrent();mouse.MakeCurrent();
            originalGame=SettingsManager.Instance.Current.Copy();originalSaved=PlayerPrefs.HasKey("CampusRift.Settings.v1");saved=PlayerPrefs.GetString("CampusRift.Settings.v1","");
            File.WriteAllText(Root+"Settings-before.json",saved);
            hud=FindAnyObjectByType<MobileControlsHUD>();player=FindAnyObjectByType<CampusExplorer>();input=player.GetComponent<CampusInput>();wall=player.GetComponent<VoidWallSkill>();hand=player.GetComponent<GiantHandSkill>();
            var sealMonster=FindAnyObjectByType<MonsterBrain>(FindObjectsInactive.Include);
            sealMonster.gameObject.SetActive(false);UIStateManager.Instance.EnterScene(true);player.ReturnToSpawn();yield return Frames();
            Mode(ControlMode.PC);yield return Frames();
            Check(!hud.SafeRoot.gameObject.activeInHierarchy&&Cursor.lockState==CursorLockMode.Locked,"PC has no mobile overlay and keeps mouse capture");
            var mouseRotation=player.followCamera.transform.rotation;InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(85,12)});yield return Frames();
            Check(Quaternion.Angle(mouseRotation,player.followCamera.transform.rotation)>3,"PC mouse-look still drives original orbit camera");player.ReturnToSpawn();
            var before=player.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));yield return new WaitForSeconds(.4f);
            Check(Vector3.Distance(player.transform.position,before)>1.5f&&player.IsSprinting,"PC keyboard sprint moves original player controller");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));yield return Frames(8);
            Check(!player.IsGrounded,"PC Space jumps");InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(.8f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));yield return Frames();Check(wall.IsPreviewing,"PC Q opens existing barrier preview");InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frames();wall.CancelPreview();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));yield return Frames();Check(!hand.IsPreviewing && !hand.IsCasting && hand.CooldownRemaining==0 && hand.Feedback=="NO MONSTER IN RANGE","PC F quick cast without a target reports unavailable and spends no cooldown");InputSystem.QueueStateEvent(keyboard,new KeyboardState());hand.CancelPreview();
            Mode(ControlMode.Mobile);player.ReturnToSpawn();yield return Frames();
            Check(hud.SafeRoot.gameObject.activeInHierarchy&&Cursor.lockState==CursorLockMode.None,"Mobile overlay enables and releases cursor for touch");
            Check(!hud.GetComponent<GameplayHUD>().Skills.gameObject.activeSelf&&hud.GetComponent<GameplayHUD>().Health.gameObject.activeInHierarchy,"Mobile hides PC skill hints and preserves health HUD");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.Q));yield return Frames();Check(input.Move==Vector2.zero&&!wall.IsPreviewing,"Keyboard cannot leak into mobile movement or skill casting");InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return Screenshot("01-Mobile-HUD");
            var move=Point(TouchRole.Move);var look=new Vector2(Screen.width*.62f,Screen.height*.64f);before=player.transform.position;
            Touch(1,TouchPhase.Began,move);Touch(2,TouchPhase.Began,look);yield return Frames();
            Touch(1,TouchPhase.Moved,move+Vector2.up*100*hud.PixelScale);Touch(2,TouchPhase.Moved,look+Vector2.right*90);yield return Frames();
            var rotation=player.followCamera.transform.rotation;
            Touch(2,TouchPhase.Moved,look+Vector2.right*170);yield return new WaitForSeconds(.35f);
            Check(player.CurrentSpeed>5&&Quaternion.Angle(rotation,player.followCamera.transform.rotation)>2,"Two real touchscreen contacts run and orbit camera together");
            Check(hud.Zones.First(z=>z.role==TouchRole.Move).Owner!=hud.Zones.First(z=>z.role==TouchRole.Look).Owner,"Joystick and camera own distinct pointer IDs");
            var jump=Point(TouchRole.Jump);Touch(3,TouchPhase.Began,jump);yield return Frames(6);Check(!player.IsGrounded&&input.Move.y>.5f,"Third touch jumps while joystick and camera remain owned");Touch(3,TouchPhase.Ended,jump);
            Touch(2,TouchPhase.Ended,look+Vector2.right*170);Touch(1,TouchPhase.Ended,move+Vector2.up*100*hud.PixelScale);yield return Frames();Check(input.TouchMove==Vector2.zero&&!input.TouchSprint,"Releasing joystick clears movement and sprint");yield return new WaitForSeconds(.8f);
            player.ReturnToSpawn();yield return Frames();var wallPoint=Point(hud.RoleFor(CampusAction.Wall));int charges=wall.Charges;
            Touch(4,TouchPhase.Began,wallPoint);yield return Frames();Check(wall.IsPreviewing,"Holding barrier button starts actual placement preview");
            float startingDistance=wall.PreviewDistance;
            Touch(4,TouchPhase.Moved,wallPoint+Vector2.down*100*hud.PixelScale);yield return Frames();
            Check(wall.PreviewDistance<startingDistance-.5f,"Dragging down pulls Void Wall close to the player");
            Touch(4,TouchPhase.Moved,wallPoint+Vector2.up*100*hud.PixelScale);yield return Frames();
            Check(wall.PreviewDistance>startingDistance+.5f,"Dragging up pushes Void Wall farther away");
            var facing=wall.Placement.rotation;Touch(4,TouchPhase.Moved,wallPoint+Vector2.right*100*hud.PixelScale);yield return Frames();Check(Quaternion.Angle(facing,wall.Placement.rotation)>20,"Barrier drag adjusts placement direction without rotating camera");
            Touch(4,TouchPhase.Ended,wallPoint+Vector2.right*100*hud.PixelScale);yield return Frames();Check(wall.Charges==charges-1&&wall.LastDeployed!=null,"Release deploys one barrier from the shared charge pool");
            yield return new WaitForSeconds(wall.config.deployCooldown+.1f);Touch(5,TouchPhase.Began,wallPoint);yield return Frames();
            var cancel=Point(TouchRole.Cancel);Touch(5,TouchPhase.Moved,cancel);yield return Frames();Touch(5,TouchPhase.Ended,cancel);yield return Frames();Check(!wall.IsPreviewing&&wall.Charges==charges-1,"Dragging to cancel consumes no barrier charge");
            Touch(15,TouchPhase.Began,wallPoint);yield return Frames();Touch(15,TouchPhase.Canceled,wallPoint);yield return Frames();
            Check(!wall.IsPreviewing&&wall.Charges==charges-1,"OS-canceled touch cancels skill without deploying");
            sealMonster.enabled=false;sealMonster.GetComponent<MonsterNavigation>().enabled=false;
            sealMonster.GetComponent<MonsterPerception>().enabled=false;sealMonster.GetComponent<MonsterHearing>().enabled=false;
            sealMonster.GetComponent<MonsterCombat>().enabled=false;sealMonster.gameObject.SetActive(true);
            sealMonster.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;
            var monsterPoint=player.transform.position+Vector3.ProjectOnPlane(player.followCamera.transform.forward,Vector3.up).normalized*4f;
            if(Physics.Raycast(monsterPoint+Vector3.up*3,Vector3.down,out var monsterFloor,6))monsterPoint.y=monsterFloor.point.y;
            sealMonster.transform.position=monsterPoint;Physics.SyncTransforms();
            var handPoint=Point(hud.RoleFor(CampusAction.Hand));move=Point(TouchRole.Move);Touch(6,TouchPhase.Began,move);Touch(7,TouchPhase.Began,handPoint);yield return Frames();
            Touch(6,TouchPhase.Moved,move+Vector2.up*35*hud.PixelScale);Touch(7,TouchPhase.Moved,handPoint+new Vector2(20,-15)*hud.PixelScale);yield return Frames();
            Check(hand.IsPreviewing&&hand.LockedMonster==sealMonster.GetComponent<MonsterVitality>()&&input.Move.y>.1f,
                "Mobile Hand button locks the nearby monster while moving");
            results.notes.Add("Touch seal target: "+hand.Target.reason+" "+hand.Target.point);Write();
            Touch(7,TouchPhase.Ended,handPoint+new Vector2(20,-15)*hud.PixelScale);Touch(6,TouchPhase.Ended,move+Vector2.up*35*hud.PixelScale);yield return Frames();
            Check(hand.IsCasting&&hand.CooldownRemaining>17,"Seal release commits shared AoE cast and cooldown");yield return new WaitForSeconds(.9f);Check(hand.ImpactCount>0,"Touch seal reaches actual impact phase");
            sealMonster.gameObject.SetActive(false);
            yield return Screenshot("02-Mobile-Cooldown");
            forceGameplay=false;Touch(8,TouchPhase.Began,Point(TouchRole.Pause));yield return Frames();Touch(8,TouchPhase.Ended,Point(TouchRole.Pause));yield return Frames();
            Check(UIStateManager.Instance.State==UIState.Paused&&Time.timeScale==0&&input.TouchMove==Vector2.zero,"Touch Pause freezes game and clears held input");
            UIStateManager.Instance.OpenSettings();yield return Frames();var settings=FindObjectsByType<SettingsUI>(FindObjectsInactive.Include).First();settings.SelectTab(2);
            Check(settings.MobileOptions.activeSelf&&!settings.PCOptions.activeSelf,"Settings Controls shows Mobile options");yield return Screenshot("03-Controls-Settings");
            settings.ChoosePC();settings.Apply();Check(!CampusInput.Mobile&&SettingsManager.Instance.ReadSaved().ControlMode==ControlMode.PC,"Settings Apply persists Mobile to PC switch");
            settings.ChooseMobile();settings.TouchSensitivity.value=1.3f;settings.Apply();Check(CampusInput.Mobile&&Mathf.Abs(SettingsManager.Instance.ReadSaved().TouchSensitivity-1.3f)<.01f,"Settings stores mobile mode and sensitivity in existing save key");
            UIStateManager.Instance.Back();UIStateManager.Instance.Resume();forceGameplay=true;yield return Frames();
            var door=FindObjectsByType<CampusAutomaticDoor>().First(d=>d.name=="Block_A_SecDoor_Side Automatic");Position(door.doorway.center-door.normal*.8f-Vector3.up*1.05f);yield return new WaitForSeconds(.2f);hud.Context.Scan();Check(hud.Context.Available&&hud.Context.Label=="OPEN DOOR","Context action identifies a nearby door");hud.Context.Activate();yield return new WaitForSeconds(.7f);Check(door.OpenAmount>.9f,"Context action opens existing automatic door");
            var lift=player.GetComponent<ElevatorInteraction>();var elevator=lift.elevators.First();
            var cabin=elevator.cabinSpaces[0];var feet=cabin.center;feet.y=elevator.floors[elevator.CurrentFloor].height+.12f;Position(feet);yield return new WaitForSeconds(.2f);
            hud.Context.Scan();Check(hud.Context.Available&&hud.Context.Label=="SELECT FLOOR","Context action identifies elevator cabin");
            forceGameplay=false;hud.Context.Activate();yield return Frames();Check(lift.PanelOpen&&hud.SafeRoot.Find("Lift floor selection").gameObject.activeInHierarchy,"Mobile elevator uses touch floor grid and exclusive modal input");
            yield return Screenshot("05-Mobile-Lift");lift.SelectFloor(Mathf.Min(1,elevator.FloorCount-1));yield return Frames();Check(!lift.PanelOpen,"Touch floor selection uses existing lift request and closes modal");forceGameplay=true;
            results.notes.Add("No grapple/dash/swing or basic attack gameplay exists; extension actions are prepared without fake buttons.");
            player.ReturnToSpawn();yield return Frames();
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(2160,1080),new Vector2Int(2340,1080),new Vector2Int(2400,1080),new Vector2Int(1600,1200)})
            {
                UIValidation.SetResolution(size.x,size.y);yield return new WaitForSecondsRealtime(.3f);Canvas.ForceUpdateCanvases();
                bool contained=true;var corners=new Vector3[4];foreach(var zone in hud.Zones.Where(z=>z.role!=TouchRole.Look&&z.role!=TouchRole.Cancel))
                {((RectTransform)zone.transform).GetWorldCorners(corners);foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(null,corner);if(!Screen.safeArea.Contains(point))contained=false;}}
                Check(contained,"Touch zones stay inside screen at "+size.x+"x"+size.y);yield return Screenshot("Layout-"+size.x+"x"+size.y);
            }
            UIValidation.SetResolution(1920,1080);Mode(ControlMode.PC);yield return Frames();Check(!hud.SafeRoot.gameObject.activeInHierarchy&&hud.GetComponent<GameplayHUD>().Skills.gameObject.activeSelf,"Returning to PC restores original skill bar");
            Mode(ControlMode.Mobile);yield return Frames();move=Point(TouchRole.Move);Touch(16,TouchPhase.Began,move);yield return Frames();Touch(16,TouchPhase.Moved,move+Vector2.up*80);yield return Frames();Mode(ControlMode.PC);yield return Frames();
            Check(input.TouchMove==Vector2.zero&&hud.Zones.All(z=>!z.Held),"Switching mode during held touch clears every pointer owner");Touch(16,TouchPhase.Ended,move+Vector2.up*80);
            Mode(ControlMode.Mobile);yield return Frames();
            var large=SettingsManager.Instance.Current.Copy();large.MobileControlScale=1.2f;SettingsManager.Instance.Apply(large,false);yield return Frames();
            var safe=new Rect(100,45,Screen.width-200,Screen.height-90);hud.ApplySafeArea(safe);Canvas.ForceUpdateCanvases();
            bool insideSafe=true,overlap=false;var areas=new List<Rect>();var boxCorners=new Vector3[4];
            foreach(var zone in hud.Zones.Where(z=>z.role!=TouchRole.Look&&z.role!=TouchRole.Cancel))
            {((RectTransform)zone.transform).GetWorldCorners(boxCorners);var a=RectTransformUtility.WorldToScreenPoint(null,boxCorners[0]);var b=RectTransformUtility.WorldToScreenPoint(null,boxCorners[2]);var rect=Rect.MinMaxRect(a.x,a.y,b.x,b.y);insideSafe&=safe.Contains(a)&&safe.Contains(b);foreach(var previous in areas)if(previous.Overlaps(rect))overlap=true;areas.Add(rect);}
            Check(insideSafe&&!overlap,"Maximum button scale with simulated notch has no clipped or overlapping touch zones");yield return Screenshot("06-Notch-MaxScale");hud.ApplySafeArea(Screen.safeArea);
            // Desktop mouse is an additional pointer for trying Mobile mode without a phone.
            var jumpButton=Point(TouchRole.Jump);InputSystem.QueueStateEvent(mouse,new MouseState{position=jumpButton});yield return Frames();InputSystem.QueueStateEvent(mouse,new MouseState{position=jumpButton,buttons=1});yield return Frames(5);Check(!player.IsGrounded,"Editor mouse can press a mobile action button");InputSystem.QueueStateEvent(mouse,new MouseState{position=jumpButton});yield return Frames();
            Mode(ControlMode.PC);yield return Frames();
            yield return Screenshot("04-PC-Regression");
            Write();File.WriteAllText(Root+"DONE.txt",results.passed.Count+" passed; "+results.failed.Count+" failed");
        }
        void OnDestroy()
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(touch!=null)InputSystem.RemoveDevice(touch);if(mouse!=null)InputSystem.RemoveDevice(mouse);
            if(oldKeyboard!=null)InputSystem.EnableDevice(oldKeyboard);if(oldMouse!=null)InputSystem.EnableDevice(oldMouse);
            if(originalSettings!=null)InputSystem.settings=originalSettings;if(testSettings!=null)Destroy(testSettings);
            if(originalGame!=null&&SettingsManager.Instance!=null)SettingsManager.Instance.Apply(originalGame,false);
            if(originalSaved)PlayerPrefs.SetString("CampusRift.Settings.v1",saved);else PlayerPrefs.DeleteKey("CampusRift.Settings.v1");PlayerPrefs.Save();
        }
    }
}
#endif
