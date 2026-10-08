using System.Collections;
using UnityEngine;
namespace CampusRift.SkyBeast
{
    // Preserve P12's entry point; P21 owns the reveal and restoration.
    public static class SkyBeastEnding
    {
        public static bool Skipped=>P21StoryCinematic.LastSkipped;
        public static bool Playing=>P21StoryCinematic.Active!=null&&P21StoryCinematic.Active.Playing;
        public static void Cancel()=>P21StoryCinematic.CancelActive();
        internal static void SuppressHud(){}
        public static IEnumerator Play(){yield return P21StoryCinematic.Play(false);}
    }
    public sealed class SkyBeastEndingHudGuard:MonoBehaviour{}
}
