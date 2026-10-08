using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace CampusRift.Controls
{
    // Append only. Skill1..Skill4 are the loadout slots; Wall/Hand/Phantom remain for older layouts.
    public enum TouchRole { Move, Look, Jump, Sprint, Wall, Hand, Interact, Pause, Cancel, Phantom, Skill1, Skill2, Skill3, Skill4, Attack, Dash, LockOn, Ultimate, Item }
    // InputSystemUIInputModule dispatches separate pointer IDs for every touch.
    // Ownership persists outside the initial hit rectangle until release/cancellation.
    public sealed class MobileTouchZone : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IDragHandler,IInitializePotentialDragHandler,ICanvasRaycastFilter
    {
        public TouchRole role;
        public MobileControlsHUD hud;
        public RectTransform knob;
        public int Owner {get;private set;}=int.MinValue;
        public static bool IsSlot(TouchRole r)=>r>=TouchRole.Skill1&&r<=TouchRole.Skill4;
        public static bool IsAim(TouchRole r)=>IsSlot(r)||r==TouchRole.Wall||r==TouchRole.Hand||r==TouchRole.Phantom;
        public static CampusAction AimAction(TouchRole r)=>IsSlot(r)?(CampusAction)((int)CampusAction.Skill1+(r-TouchRole.Skill1)):
            r==TouchRole.Wall?CampusAction.Wall:r==TouchRole.Hand?CampusAction.Hand:CampusAction.Phantom;
        public bool Held => Owner!=int.MinValue;
        Vector2 origin;bool cancel;
        // Filter only initial acquisition. An owned touch may drag outside the circle to aim/cancel.
        public bool IsRaycastLocationValid(Vector2 screen,Camera eventCamera)
        {
            if(role==TouchRole.Look)return true;
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,screen,eventCamera,out var point))return false;
            var radius=Mathf.Min(rect.rect.width,rect.rect.height)*.5f;
            return (point-rect.rect.center).sqrMagnitude<=radius*radius;
        }
        public void OnInitializePotentialDrag(PointerEventData e){e.useDragThreshold=false;}
        public void OnPointerDown(PointerEventData e)
        {
            if(Held||!CampusInput.Mobile||!IsRaycastLocationValid(e.position,e.pressEventCamera))return;
            if(role!=TouchRole.Pause&&!hud.Input.Allowed)return;
            Owner=e.pointerId;origin=e.position;cancel=false;
            if(role==TouchRole.Item)hud.Items.Begin(e.position);
            if(IsAim(role))
            {if(!hud.Input.BeginAim(AimAction(role))){Release();hud.FlashUnavailable(role);return;}hud.ShowAim();}
            else if(role==TouchRole.Jump)hud.Input.Press(CampusAction.Jump);
            else if(role==TouchRole.Attack){hud.Input.SetHeld(CampusAction.Attack,true);hud.Input.Press(CampusAction.Attack);}
            else if(role==TouchRole.Dash)hud.Input.Press(CampusAction.Dash);
            else if(role==TouchRole.Ultimate)hud.Input.Press(CampusAction.Ultimate);
            else if(role==TouchRole.LockOn){hud.Input.SetHeld(CampusAction.LockOn,true);hud.Input.Press(CampusAction.LockOn);}
            else if(role==TouchRole.Sprint)hud.PressBoost();
            else if(role==TouchRole.Interact)hud.Context.Activate();
            else if(role==TouchRole.Pause){UI.UIStateManager.Instance?.Pause();Release();return;}
            else if(role==TouchRole.Cancel){hud.Input.CancelAim();Release();return;}
            if(role!=TouchRole.Look)UI.UIAudioManager.Instance?.Hover();
            hud.PressVisual(role,true);
        }
        public void OnDrag(PointerEventData e)
        {
            if(e.pointerId!=Owner)return;
            float unit=hud.PixelScaleFor(role);
            if(role==TouchRole.Item)hud.Items.Drag(e.position);
            if(role==TouchRole.Move)
            {
                Vector2 delta=(e.position-origin)/Mathf.Max(1,unit*82);
                float dead=0.12f;float magnitude=delta.magnitude;
                hud.Input.TouchMove=magnitude<=dead?Vector2.zero:delta.normalized*Mathf.Clamp01((magnitude-dead)/(1-dead));
                if(knob!=null)knob.anchoredPosition=Vector2.ClampMagnitude(delta,1)*62;
            }
            else if(role==TouchRole.Look)
            {if(e.delta.sqrMagnitude>=.25f)hud.Input.AddLook(e.delta);}
            else if(IsAim(role))
            {
                cancel=hud.InCancelZone(e.position);
                hud.Input.DragAim((e.position-origin)/Mathf.Max(1,unit*180));hud.SetCancelVisual(cancel);
            }
        }
        public void OnPointerUp(PointerEventData e)
        {
            if(e.pointerId!=Owner)return;
            if(e is ExtendedPointerEventData touch && touch.control!=null && touch.control.device is Touchscreen screen)
                foreach(var contact in screen.touches)
                    if(contact.touchId.ReadValue()==touch.touchId && contact.phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled)cancel=true;
            if(IsAim(role))hud.Input.EndAim(cancel||hud.InCancelZone(e.position));
            if(role==TouchRole.Item)hud.Items.End(e.position,cancel);
            Release();
        }
        public void Release()
        {
            if(role==TouchRole.Item&&hud!=null&&hud.Items!=null)hud.Items.Cancel();
            if(role==TouchRole.Move&&hud!=null&&hud.Input!=null){hud.Input.TouchMove=Vector2.zero;if(knob!=null)knob.anchoredPosition=Vector2.zero;}
            if(role==TouchRole.Sprint&&hud!=null&&hud.Input!=null)hud.ReleaseBoost();
            if(hud!=null&&hud.Input!=null){if(role==TouchRole.Attack)hud.Input.SetHeld(CampusAction.Attack,false);else if(role==TouchRole.LockOn)hud.Input.SetHeld(CampusAction.LockOn,false);}
            Owner=int.MinValue;cancel=false;if(hud!=null)hud.PressVisual(role,false);
        }
        void OnDisable(){Release();}
    }
}
