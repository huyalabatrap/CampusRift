using UnityEngine;
namespace CampusRift.AR
{
    [CreateAssetMenu(menuName="Campus Rift/AR Mode Settings")]
    public sealed class ARModeSettings : ScriptableObject
    {
        public bool allUnlocked=true;
        public float tableScale=.15f, floorScale=.6f, spirit=200, spiritRegen=30;
        public int maxMonsters=6;
        public float tableMinimumArea=.15f, floorMinimumArea=.8f;
        public bool enableRescue=false;public float gestureDwell=.1f,stableSwitch=.2f;
        public float modelThreshold=.6f, rescueThreshold=.45f;
        public float fingerExtendedAngle=150, fingerFoldedAngle=120, fingerDistanceRatio=1.25f;
        public float victorySeparation=.35f, directionMargin=.15f;
        public float evidenceThreshold=1.5f, evidenceLead=.6f, evidenceDecay=.25f;
        public float gestureLockout=.35f, handRelease=.2f, maxPalmSpeed=1.5f;
        public bool Floor { get => PlayerPrefs.GetInt("CampusRift.AR.Floor",0)==1; set { PlayerPrefs.SetInt("CampusRift.AR.Floor",value?1:0); } }
        public bool Occlusion { get => PlayerPrefs.GetInt("CampusRift.AR.Occlusion",0)==1; set { PlayerPrefs.SetInt("CampusRift.AR.Occlusion",value?1:0); } }
        public float Scale => Floor?floorScale:tableScale;
        public float MinimumArea => Floor?floorMinimumArea:tableMinimumArea;
    }
}
