using UnityEngine;
namespace CampusRift.AR
{
    public static class ARHaptics
    {
        static readonly long[][] patterns={new long[]{0,35,35,55},new long[]{0,70,25,25},new long[]{0,18,25,18,25,45},new long[]{0,20,20,20,20,20},new long[]{0,65,55,25},new long[]{0,30,25,70}};
        public static void Pulse()=>Play(new long[]{0,30},150);
        public static void Skill(string label){int i=System.Array.IndexOf(GestureSkillMapper.Labels,label);if(i>=0)Play(patterns[i],170);}
        public static void Ultimate(int kind)=>Play(kind==0?new long[]{0,45,35,45,35,110}:kind==1?new long[]{0,100,70,25,25,110}:new long[]{0,30,25,50,25,130},230);
        public static void Impact()=>Play(new long[]{0,140,35,75},255);
        public static void Dodge()=>Play(new long[]{0,20,20,20},100);
        public static void Block(bool perfect)=>Play(perfect?new long[]{0,35,20,90}:new long[]{0,55},200);
        static void Play(long[] timings,int strength)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var settings=CampusRift.UI.SettingsManager.Instance;
            if(settings!=null&&(!settings.Current.MobileHaptics||settings.Current.ReduceCameraShake))return;
            try
            {
                using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))using(var vibrator=activity.Call<AndroidJavaObject>("getSystemService","vibrator"))using(var version=new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if(!vibrator.Call<bool>("hasVibrator"))return;
                    if(version.GetStatic<int>("SDK_INT")>=26)
                    {var amplitudes=new int[timings.Length];for(int i=1;i<amplitudes.Length;i+=2)amplitudes[i]=strength;using(var effectClass=new AndroidJavaClass("android.os.VibrationEffect"))using(var effect=effectClass.CallStatic<AndroidJavaObject>("createWaveform",timings,amplitudes,-1))vibrator.Call("vibrate",effect);}
                    else vibrator.Call("vibrate",timings,-1);
                }
            }
            catch{Handheld.Vibrate();}
#endif
        }
    }
}
