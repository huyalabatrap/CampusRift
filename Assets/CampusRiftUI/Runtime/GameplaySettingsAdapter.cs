using UnityEngine;
namespace CampusRift.UI
{
    [RequireComponent(typeof(CampusExplorer))]
    public sealed class GameplaySettingsAdapter : MonoBehaviour
    {
        public float BaseMouseSensitivity=.12f, BaseZoomSensitivity=.003f;
        CampusExplorer player;
        void Start()
        {
            player=GetComponent<CampusExplorer>();
            if(SettingsManager.Instance==null)return;
            SettingsManager.Instance.Changed+=Apply;Apply(SettingsManager.Instance.Current);
        }
        void OnDestroy(){if(SettingsManager.Instance!=null)SettingsManager.Instance.Changed-=Apply;}
        void Apply(GameSettings settings)
        {
            player.mouseSensitivity=BaseMouseSensitivity*settings.MouseSensitivity;
            player.zoomSensitivity=BaseZoomSensitivity*settings.CameraSensitivity;player.invertY=settings.InvertY;
            if(player.followCamera!=null)player.followCamera.fieldOfView=settings.FieldOfView;
        }
    }
}
