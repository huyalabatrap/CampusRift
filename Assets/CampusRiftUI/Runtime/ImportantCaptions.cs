using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CampusRift.UI
{
    // Sound descriptions are unscaled, localised and visible above cinematic HUD hiding.
    public sealed class ImportantCaptions : MonoBehaviour
    {
        public static ImportantCaptions Instance {get;private set;}
        Canvas canvas; TMP_Text label; float until,lastShown=-10; string vi,en;
        public string CurrentText=>label!=null&&canvas.enabled?label.text:"";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Boot()
        {var go=new GameObject("Important sound captions");DontDestroyOnLoad(go);go.AddComponent<ImportantCaptions>();}
        void Awake()
        {
            if(Instance!=null){Destroy(gameObject);return;}Instance=this;
            canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=110;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=1;
            var card=ComboUIFactory.Rect("Sound description",transform,new Vector2(1200,140),new Vector2(0,-260));ComicTheme.Frame(card.gameObject);
            label=ComboUIFactory.Text("Caption",card,new Vector2(1140,120),Vector2.zero,26);label.textWrappingMode=TextWrappingModes.Normal;label.fontStyle=FontStyles.Normal;
            canvas.enabled=false;
        }
        public static void Show(string vietnamese,string english,float seconds=3)
        {
            if(Instance==null)return;var c=Instance;
            // Warning dispatch can roar in the same frame. Preserve the alarm and both descriptions.
            bool together=Time.unscaledTime-c.lastShown<.1f&&Time.unscaledTime<c.until;
            c.vi=together?(c.vi.Contains(vietnamese)?c.vi:c.vi+"\n"+vietnamese):vietnamese;
            c.en=together?(c.en.Contains(english)?c.en:c.en+"\n"+english):english;
            c.until=together?Mathf.Max(c.until,Time.unscaledTime+Mathf.Max(1,seconds)):Time.unscaledTime+Mathf.Max(1,seconds);c.lastShown=Time.unscaledTime;c.Refresh();
        }
        void Update(){Refresh();}
        void Refresh()
        {
            if(canvas==null)return;
            bool state=UIStateManager.Instance==null||UIStateManager.Instance.State==UIState.Gameplay||UIStateManager.Instance.State==UIState.Modal;
            bool cinematic=SkyBeast.P21StoryCinematic.Active!=null||SkyBeast.HeavenSwordCinematic.Active!=null;
            canvas.enabled=(SettingsManager.Instance?.Current.Subtitles??true)&&Time.unscaledTime<until&&(state||cinematic);
            if(canvas.enabled)label.text=LevelHUD.Vietnamese?vi:en;
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
