using UnityEngine;
namespace CampusRift.AR
{
    // Session preferences only. No camera, hand, room or voice data is persisted.
    [DefaultExecutionOrder(-95)]
    public sealed class ARTechSettings : MonoBehaviour
    {
        public bool TwoHands,StandMode,DepthCollision,Voice,Recording=true,RoomProbe=true;
        public ARAdaptiveQuality Quality {get;private set;}
        public bool TwoHandsAllowed=>TwoHands&&Quality!=null&&!Quality.Reduced&&!Quality.ThermalLimited&&(StandMode||Quality.TwoHandsAllowed);
        public bool VoiceAllowed=>Voice&&Quality!=null&&Quality.VoiceAllowed;
        public bool RecordingAllowed=>Recording&&Quality!=null&&Quality.RecordingAllowed;
        void Awake(){Quality=FindAnyObjectByType<ARAdaptiveQuality>();}
        void Update(){var bridge=GetComponent<GestureRecognizerBridge>();var caster=GetComponent<ARSkillCaster>();var field=GetComponent<ARBattlefield>();if(bridge!=null)bridge.SetHandCount(TwoHandsAllowed&&!(caster?.Practice??false)&&!(field?.CheckLoad??false)?2:1);}
    }
}
