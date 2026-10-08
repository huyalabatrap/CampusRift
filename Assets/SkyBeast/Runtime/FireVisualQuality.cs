using UnityEngine;
namespace CampusRift.SkyBeast
{
    public enum FireEffectQuality { Automatic, PcHigh, PcLow, Mobile }
    // Cosmetic quality only; hazards, timings and damage never depend on this budget.
    public static class FireVisualQuality
    {
        public static FireEffectQuality Override=FireEffectQuality.Automatic;
        public static FireEffectQuality Current=>Application.isMobilePlatform||Controls.CampusInput.Mobile?FireEffectQuality.Mobile:
            Override!=FireEffectQuality.Automatic?Override:QualitySettings.GetQualityLevel()==0?FireEffectQuality.PcLow:FireEffectQuality.PcHigh;
        public static bool Mobile=>Current==FireEffectQuality.Mobile;
        public static int Choose(int high,int low,int mobile)=>Current==FireEffectQuality.Mobile?mobile:Current==FireEffectQuality.PcLow?low:high;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>Override=FireEffectQuality.Automatic;
    }
}
