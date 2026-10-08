using UnityEngine;

namespace CampusRift.Skills
{
    [DefaultExecutionOrder(300), DisallowMultipleComponent]
    public sealed class GiantHandCameraImpulse : MonoBehaviour
    {
        [Range(0,1)] public float intensity=0.7f;
        CampusExplorer explorer;
        float amplitude,until;
        void Awake(){explorer=GetComponent<CampusExplorer>();}
        public void Pulse(float strength){amplitude=Time.time<until?Mathf.Max(amplitude,strength):strength;until=Time.time+0.24f;}
        // The cinematic owns its camera and unscaled clock while player movement is locked.
        public void ApplyCinematic(Camera view,float elapsed,float strength)
        {
            if(view==null || elapsed<0 || elapsed>=0.24f)return;
            float accessibility=UI.SettingsManager.Instance!=null&&(UI.SettingsManager.Instance.Current.ReduceCameraShake||UI.SettingsManager.Instance.Current.ReduceSkillFlashes)?.2f:1;
            float t=1-elapsed/0.24f;float wave=Mathf.Sin((1-t)*43)*t*t*strength*intensity*accessibility;
            view.transform.rotation*=Quaternion.Euler(wave*1.1f,wave*0.35f,-wave*0.5f);
        }
        void LateUpdate()
        {
            if(explorer==null || !explorer.enabled || explorer.followCamera==null || Time.time>=until || Time.timeScale<=0)return;
            if(UI.UIStateManager.Instance!=null && !UI.UIStateManager.Instance.GameplayInputEnabled)return;
            ApplyCinematic(explorer.followCamera,0.24f-(until-Time.time),amplitude);
        }
    }
}
