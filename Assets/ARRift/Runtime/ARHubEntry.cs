using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARHubEntry:MonoBehaviour
    {
        public Button button;public TMP_Text unavailable;
        public bool Supported {get;private set;}
        IEnumerator Start()
        {
            button.gameObject.SetActive(false);unavailable.text="";
#if UNITY_EDITOR
            Supported=true;
#elif UNITY_ANDROID
            // AR Foundation 6.6 CheckAvailability requires an active loader. Do not initialize
            // XR in the Hub: use the same ARCore availability API without starting a session/camera.
            float until=Time.realtimeSinceStartup+12;
            while(Time.realtimeSinceStartup<until){bool pending=false;try{using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))using(var api=new AndroidJavaClass("com.google.ar.core.ArCoreApk"))using(var apk=api.CallStatic<AndroidJavaObject>("getInstance"))using(var state=apk.Call<AndroidJavaObject>("checkAvailability",activity)){pending=state.Call<bool>("isTransient");Supported=state.Call<bool>("isSupported");}}catch{Supported=false;}if(!pending)break;yield return new WaitForSecondsRealtime(.5f);}
#endif
            button.gameObject.SetActive(Supported);unavailable.text=Supported?"":(LevelHUD.Vietnamese?"AR không có trên thiết bị này":"AR unavailable on this device");yield break;
        }
    }
}
