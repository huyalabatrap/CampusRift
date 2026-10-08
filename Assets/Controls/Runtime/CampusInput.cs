using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CampusRift.UI;
using CampusRift.Skills;

namespace CampusRift.Controls
{
    public enum ControlMode { PC, Mobile }
    // Values index the queued-press array: append only.
    // Wall/Hand/Phantom are skill identities: CampusInput resolves them to the loadout slot that holds the skill.
    // Skill1..Skill4 are the four slots (PC: Q, E, R, F).
    public enum CampusAction { Jump, Interact, Wall, Hand, Confirm, Cancel, Respawn, Dash, Grapple, Attack, Phantom,
        Skill1, Skill2, Skill3, Skill4, Ultimate, Item1, Item2, Item3, LockOn }
    // Both devices feed the existing controller and skill methods. Future abilities consume
    // their action here; they do not require another movement implementation.
    [DefaultExecutionOrder(-150), DisallowMultipleComponent]
    public sealed class CampusInput : MonoBehaviour
    {
        public static bool Mobile => SettingsManager.Instance != null
            ? SettingsManager.Instance.Current.ControlMode == ControlMode.Mobile : Application.isMobilePlatform;
        public Vector2 TouchMove {get;set;}
        public bool TouchSprint {get;set;}
        public Vector2 SkillDrag {get;private set;}
        public CampusAction? Aiming {get;private set;}
        public event Action ResetTouch;
        public event Action<CampusAction> ActionRequested;
        Vector2 pendingLook;
        static readonly int ActionCount = Enum.GetValues(typeof(CampusAction)).Length;
        readonly bool[] queued = new bool[ActionCount];
        readonly bool[] heldMobile = new bool[ActionCount];
        SkillLoadout loadout;
        public bool UltimateLocked {get;set;}
        public bool ItemWheelOpen {get;set;}
        public bool Allowed => !UltimateLocked && Time.timeScale>0 && (UIStateManager.Instance==null || UIStateManager.Instance.GameplayInputEnabled)
            && (GetComponent<CampusRift.Enemies.PlayerEnemyControl>()==null || !GetComponent<CampusRift.Enemies.PlayerEnemyControl>().Stunned);
        void Awake(){loadout=GetComponent<SkillLoadout>();}
        public SkillLoadout Loadout => loadout!=null?loadout:loadout=GetComponent<SkillLoadout>();
        void Start()
        {
            if(SettingsManager.Instance!=null)SettingsManager.Instance.Changed+=SettingsChanged;
            if(UIStateManager.Instance!=null)UIStateManager.Instance.Changed+=StateChanged;
        }
        void SettingsChanged(GameSettings _) {ResetAll();UIStateManager.Instance?.RefreshCursor();}
        void StateChanged(UIState state){if(state!=UIState.Gameplay)ResetAll();}
        public Vector2 Move
        {
            get
            {
                if(!Allowed)return Vector2.zero;if(Mobile)return TouchMove;
                var k=Keyboard.current;if(k==null)return Vector2.zero;
                return Vector2.ClampMagnitude(new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),
                    (k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0)),1);
            }
        }
        public bool Sprint => Allowed && (Mobile ? TouchMove.magnitude>.15f &&
            (TouchSprint || (SettingsManager.Instance!=null && SettingsManager.Instance.Current.JoystickAutoSprint && TouchMove.magnitude>.9f)) :
            Keyboard.current!=null && (Keyboard.current.leftShiftKey.isPressed||Keyboard.current.rightShiftKey.isPressed));
        public Vector2 TakeLook()
        {
            if(!Allowed){pendingLook=Vector2.zero;return Vector2.zero;}
            if(!Mobile)return !ItemWheelOpen&&Mouse.current!=null?Mouse.current.delta.ReadValue():Vector2.zero;
            var result=pendingLook;pendingLook=Vector2.zero;return result;
        }
        public void AddLook(Vector2 pixels)
        {
            if(!Mobile||!Allowed)return;
            // Normalize to screen height: same swipe fraction gives the same orbit on every device.
            float sensitivity=SettingsManager.Instance!=null?SettingsManager.Instance.Current.TouchSensitivity:1;
            pendingLook+=pixels*(180f/Mathf.Max(1,Screen.height))*sensitivity;
        }
        public void Press(CampusAction action){if(!Mobile||!Allowed)return;queued[(int)action]=true;ActionRequested?.Invoke(action);}
        public static bool IsSkillIdentity(CampusAction action) => action==CampusAction.Wall||action==CampusAction.Hand||action==CampusAction.Phantom;
        // Identity (Wall/Hand/Phantom) → the Skill1..4 action of the slot holding that skill; unequipped → null.
        public CampusAction? Resolve(CampusAction action)
        {
            if(!IsSkillIdentity(action))return action;
            int slot=Loadout!=null?Loadout.SlotOfAction(action):-1;
            return slot<0?(CampusAction?)null:SkillLoadout.SlotAction(slot);
        }
        public bool Pressed(CampusAction action)
        {
            if(!Allowed)return false;
            var resolved=Resolve(action);if(!resolved.HasValue)return false;action=resolved.Value;
            if(Mobile){bool value=queued[(int)action];queued[(int)action]=false;return value;}
            var control=KeyFor(action);if(control!=null)return control.wasPressedThisFrame;
            if(ItemWheelOpen)return false;
            var m=Mouse.current;
            switch(action)
            {
                case CampusAction.Confirm:return m!=null&&m.leftButton.wasPressedThisFrame;
                case CampusAction.Cancel:return m!=null&&m.rightButton.wasPressedThisFrame;
                // Attack shares the left button; while a skill is aimed the click confirms instead.
                case CampusAction.Attack:return m!=null&&m.leftButton.wasPressedThisFrame&&!AnySkillAiming;
                default:return false;
            }
        }
        // Touch buttons that care about being held (attack charge, lock-on release) report through here.
        public void SetHeld(CampusAction action,bool value){if(!Mobile)return;heldMobile[(int)action]=value;}
        // Held state for any action on either device. Attack ignores the left button while a skill is aimed.
        public bool IsHeld(CampusAction action)
        {
            if(!Allowed)return false;
            if(Mobile)return heldMobile[(int)action];
            if(action==CampusAction.Attack)return !ItemWheelOpen&&Mouse.current!=null&&Mouse.current.leftButton.isPressed&&!AnySkillAiming;
            var control=KeyFor(action);return control!=null&&control.isPressed;
        }
        /// <summary>Whether the skill key is still held (PC hold-to-aim). Mobile holds are tracked by the touch zone.</summary>
        public bool Holding(CampusAction action)
        {
            if(Mobile||!Allowed)return false;
            var resolved=Resolve(action);if(!resolved.HasValue)return false;
            if(resolved.Value==CampusAction.Attack)return !ItemWheelOpen&&Mouse.current!=null&&Mouse.current.leftButton.isPressed;
            var control=KeyFor(resolved.Value);return control!=null&&control.isPressed;
        }
        // PC key per action (plan §6). Respawn has no key in V2: R is the third skill slot.
        static UnityEngine.InputSystem.Controls.KeyControl KeyFor(CampusAction action)
        {
            var k=Keyboard.current;if(k==null)return null;
            switch(action)
            {
                case CampusAction.Jump:return k.spaceKey;
                case CampusAction.Interact:return k.gKey;
                case CampusAction.Skill1:return k.qKey;
                case CampusAction.Skill2:return k.eKey;
                case CampusAction.Skill3:return k.rKey;
                case CampusAction.Skill4:return k.fKey;
                case CampusAction.Dash:return k.leftCtrlKey;
                case CampusAction.Ultimate:return k.vKey;
                case CampusAction.Item1:return k.digit1Key;
                case CampusAction.Item2:return k.digit2Key;
                case CampusAction.Item3:return k.digit3Key;
                case CampusAction.LockOn:return k.tabKey;
                default:return null;
            }
        }
        // Short key label for HUD text: "Q", "E", …; empty when the skill is not equipped.
        public string KeyLabel(CampusAction action)
        {
            var resolved=Resolve(action);if(!resolved.HasValue)return "";
            switch(resolved.Value)
            {
                case CampusAction.Skill1:return "Q";case CampusAction.Skill2:return "E";case CampusAction.Skill3:return "R";case CampusAction.Skill4:return "F";
                case CampusAction.Interact:return "G";case CampusAction.Dash:return "CTRL";case CampusAction.Ultimate:return "V";
                case CampusAction.Item1:return "1";case CampusAction.Item2:return "2";case CampusAction.Item3:return "3";case CampusAction.LockOn:return "TAB";
                case CampusAction.Jump:return "SPACE";case CampusAction.Attack:return "LMB";default:return "";
            }
        }
        public bool AnySkillAiming{get{if(Loadout==null)return false;for(int i=0;i<SkillLoadout.SlotCount;i++){var r=Loadout.Get(i);if(r!=null&&r.IsAiming)return true;}return false;}}
        public SkillRuntime RuntimeFor(CampusAction action)
        {
            var resolved=Resolve(action);if(!resolved.HasValue||Loadout==null)return null;
            return Loadout.Get(SkillLoadout.SlotOf(resolved.Value));
        }
        // Mobile hold-to-aim. Accepts a slot action (touch buttons) or a skill identity.
        public bool BeginAim(CampusAction action)
        {
            if(!Mobile||!Allowed)return false;CancelAim();SkillDrag=Vector2.zero;
            var runtime=RuntimeFor(action);
            bool ok=runtime!=null&&runtime.IsUnlocked&&runtime.BeginAim();
            // Aiming keeps the identity so direction helpers (WallDirection, AimViewport…) still recognise the skill.
            if(ok)Aiming=runtime.IdentityAction??Resolve(action);return ok;
        }
        public void DragAim(Vector2 normalized){SkillDrag=Vector2.ClampMagnitude(normalized,1);}
        public bool EndAim(bool cancel)
        {
            if(!Aiming.HasValue)return false;
            var runtime=RuntimeFor(Aiming.Value);
            bool result=!cancel&&Allowed&&runtime!=null&&runtime.Confirm();
            CancelAim();if(result)MobileFeedback.Confirm();return result;
        }
        public void CancelAim(){if(Loadout!=null)Loadout.CancelAll();Aiming=null;SkillDrag=Vector2.zero;}
        public Vector3 PhantomDirection(Camera camera)
        {
            Vector3 flat=Vector3.ProjectOnPlane(camera!=null?camera.transform.forward:transform.forward,Vector3.up).normalized;
            if(flat.sqrMagnitude<.01f)flat=transform.forward;
            if(Aiming==CampusAction.Phantom&&SkillDrag.sqrMagnitude>.025f)
                return (Quaternion.LookRotation(flat)*new Vector3(SkillDrag.x,0,SkillDrag.y)).normalized;
            return flat;
        }
        public Vector3 WallDirection(Camera camera)
        {
            Vector3 forward=camera!=null?camera.transform.forward:transform.forward;
            if(Aiming==CampusAction.Wall)
            {
                var flat=Vector3.ProjectOnPlane(forward,Vector3.up).normalized;
                if(flat.sqrMagnitude<.01f)flat=transform.forward;
                // Horizontal drag steers the wall; vertical drag controls distance.
                forward=Quaternion.AngleAxis(SkillDrag.x*90f,Vector3.up)*flat;
            }
            return forward;
        }
        public Vector2 AimViewport => Aiming==CampusAction.Hand ? new Vector2(.5f+SkillDrag.x*.36f,.43f+SkillDrag.y*.32f):new Vector2(.5f,.5f);
        public void ResetAll(){TouchMove=Vector2.zero;TouchSprint=false;pendingLook=Vector2.zero;Array.Clear(queued,0,queued.Length);Array.Clear(heldMobile,0,heldMobile.Length);CancelAim();ResetTouch?.Invoke();}
        void OnApplicationFocus(bool focus){if(!focus)ResetAll();}
        void OnDisable(){ResetAll();}
        void OnDestroy()
        {if(SettingsManager.Instance!=null)SettingsManager.Instance.Changed-=SettingsChanged;if(UIStateManager.Instance!=null)UIStateManager.Instance.Changed-=StateChanged;}
    }
    public static class ControlHints
    {
        public static string Interact => CampusInput.Mobile?"Tap Interact":"Press G";
        public static string Skill(string pcKey) => CampusInput.Mobile?"Hold, drag to aim, release to cast": "Press "+pcKey;
    }
    public static class MobileFeedback
    {
        static float next;
        public static void Confirm()
        {
            if(!CampusInput.Mobile||Time.unscaledTime<next)return;next=Time.unscaledTime+.15f;
            UIAudioManager.Instance?.Click();
#if UNITY_ANDROID && !UNITY_EDITOR
            if(SettingsManager.Instance==null||!SettingsManager.Instance.Current.MobileHaptics)return;
            // Android's semantic haptic respects system preferences, with no vibration permission.
            using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))
                activity.Call("runOnUiThread",new AndroidJavaRunnable(()=>{
                    using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using(var current=player.GetStatic<AndroidJavaObject>("currentActivity"))
                    using(var window=current.Call<AndroidJavaObject>("getWindow"))
                    using(var view=window.Call<AndroidJavaObject>("getDecorView"))view.Call<bool>("performHapticFeedback",1);
                }));
#endif
        }
    }
}
