using System.Collections.Generic;
using UnityEngine;
namespace CampusRift.Levels
{
    public sealed class BlackoutEvent : MonoBehaviour
    {
        readonly Dictionary<Light,bool> saved=new Dictionary<Light,bool>();Color ambient;float intensity;bool applied;PlayerFlashlight flashlight;bool originalFlashlight;
        public int LightsDisabled {get;private set;}
        public void Apply(){Restore();applied=true;ambient=RenderSettings.ambientLight;intensity=RenderSettings.ambientIntensity;
            foreach(var l in FindObjectsByType<Light>(FindObjectsSortMode.None)){
                if(l.type==LightType.Directional||l.GetComponentInParent<CampusExplorer>()!=null)continue;
                Vector3 p=l.transform.position;bool inDE=p.z>-36&&p.z<-16&&p.x>-28&&p.x<15;
                if(inDE){saved[l]=l.enabled;l.enabled=false;LightsDisabled++;}}
            RenderSettings.ambientLight=ambient*.25f;RenderSettings.ambientIntensity=intensity*.25f;
            flashlight=FindAnyObjectByType<PlayerFlashlight>();if(flashlight!=null){originalFlashlight=flashlight.alwaysOn;flashlight.alwaysOn=true;}
            var canvas=UI.ComboUIFactory.Canvas("Blackout flashlight hint",transform,60);
            var text=UI.ComboUIFactory.Text("Hint",canvas.transform,new Vector2(800,50),new Vector2(0,180),26);text.text=UI.LevelHUD.Vietnamese?"MẤT ĐIỆN D–E · Đèn pin tự bật trong đêm":"D–E BLACKOUT · Flashlight enabled for the night";
            Destroy(canvas.gameObject,5);
        }
        public void Restore(){foreach(var p in saved)if(p.Key!=null)p.Key.enabled=p.Value;if(applied){RenderSettings.ambientLight=ambient;RenderSettings.ambientIntensity=intensity;}if(flashlight!=null)flashlight.alwaysOn=originalFlashlight;applied=false;saved.Clear();LightsDisabled=0;}
        void OnDisable(){Restore();}
    }
}
