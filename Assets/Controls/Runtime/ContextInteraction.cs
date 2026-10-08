using System.Collections.Generic;
using UnityEngine;

namespace CampusRift.Controls
{
    // Future terminal/pickup/NPC implementations register a single contextual action.
    public abstract class CampusInteractable : MonoBehaviour
    {
        internal static readonly List<CampusInteractable> Active=new List<CampusInteractable>();
        public float range=2;
        public virtual bool CanInteract=>isActiveAndEnabled;
        public abstract string Label {get;}
        public abstract void Interact(CampusExplorer player);
        protected virtual void OnEnable(){Active.Add(this);}
        protected virtual void OnDisable(){Active.Remove(this);}
    }
    public sealed class ContextInteraction : MonoBehaviour
    {
        public string Label {get;private set;}="INTERACT";
        public bool Available {get;private set;}
        CampusExplorer player;ElevatorInteraction lift;CampusAutomaticDoor[] doors;
        CampusAutomaticDoor door;CampusInteractable target;float next;
        void Awake(){player=GetComponent<CampusExplorer>();lift=GetComponent<ElevatorInteraction>();doors=FindObjectsByType<CampusAutomaticDoor>();}
        void Update()
        {
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.12f;Scan();
        }
        public void Scan()
        {
            door=null;target=null;Available=lift!=null&&lift.CanInteract;
            if(Available){Label=lift.InteractionLabel;return;}
            float best=2.2f*2.2f;
            foreach(var item in CampusInteractable.Active)
            {
                if(item==null||!item.CanInteract)continue;
                float distance=(item.transform.position-transform.position).sqrMagnitude;
                if(distance>best||distance>item.range*item.range)continue;best=distance;target=item;
            }
            if(target!=null){Available=true;Label=target.Label;return;}
            foreach(var candidate in doors)
            {
                if(candidate==null||Mathf.Abs(candidate.doorway.min.y-transform.position.y)>.8f)continue;
                float distance=(candidate.doorway.ClosestPoint(transform.position+Vector3.up)-(transform.position+Vector3.up)).sqrMagnitude;
                if(distance>=best)continue;best=distance;door=candidate;
            }
            Available=door!=null;Label=Available?"OPEN DOOR":"INTERACT";
        }
        public void Activate()
        {
            if(UI.UIStateManager.Instance!=null&&!UI.UIStateManager.Instance.GameplayInputEnabled)return;
            Scan();if(!Available)return;
            if(target!=null)target.Interact(player);else if(door!=null)door.RequestOpenFrom(transform.position);else lift.Interact();
            MobileFeedback.Confirm();
        }
    }
}
