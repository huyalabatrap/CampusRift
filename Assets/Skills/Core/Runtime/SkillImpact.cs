using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using CampusRift.Monsters;
namespace CampusRift.Skills
{
    // Bounded victim-local hit-stop and a 55 ms comic flash. Time.timeScale belongs to UI/pause.
    [DefaultExecutionOrder(310)]
    public sealed class SkillImpact : MonoBehaviour
    {
        struct Hold {public Animator animator;public NavMeshAgent agent;public float speed,until;public bool stopped;}
        readonly Hold[] holds=new Hold[64];
        Graphic flash;AR.ARCombatContext ar;
        RawImage speedLines;
        float flashUntil,flashStrength;
        GiantHandCameraImpulse impulse;
        public float LastHitStop {get;private set;}
        public float LastImpulse {get;private set;}
        public int HitStops {get;private set;}
        public int ImpactFrames {get;private set;}
        public bool FlashVisible=>Time.unscaledTime<flashUntil;
        void Awake()
        {
            ar=GetComponent<AR.ARCombatContext>();impulse=GetComponent<GiantHandCameraImpulse>();
            var go=new GameObject("P10 pooled impact canvas",typeof(RectTransform),typeof(Canvas));go.transform.SetParent(transform,false);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=-10;
            var fg=new GameObject("Ink impact",typeof(RectTransform));fg.transform.SetParent(go.transform,false);flash=ar!=null?(Graphic)fg.AddComponent<AR.ARImpactBorder>():fg.AddComponent<Image>();Stretch(flash.rectTransform);flash.raycastTarget=false;flash.enabled=false;
            var lines=new GameObject("Radial speed lines",typeof(RectTransform),typeof(RawImage));lines.transform.SetParent(go.transform,false);speedLines=lines.GetComponent<RawImage>();Stretch(speedLines.rectTransform);speedLines.raycastTarget=false;speedLines.enabled=false;
            var speed=GetComponent<SpeedForceVFX>();if(speed!=null)speedLines.texture=speed.speedLinesTexture;
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        public void HoldVictim(MonsterVitality victim,float seconds=.065f)
        {
            if(victim==null)return;var animator=victim.GetComponentInChildren<Animator>();var agent=victim.GetComponent<NavMeshAgent>();
            if(animator==null&&agent==null)return;int slot=-1;
            for(int i=0;i<holds.Length;i++){if(holds[i].animator==animator&&animator!=null){slot=i;break;}if(holds[i].until<=Time.unscaledTime)slot=i;}
            if(slot<0)return;
            if(holds[slot].until>Time.unscaledTime){var h=holds[slot];h.until=Mathf.Max(h.until,Time.unscaledTime+Mathf.Clamp(seconds,.05f,.08f));holds[slot]=h;return;}
            if(holds[slot].until!=0)Restore(holds[slot]);
            LastHitStop=Mathf.Clamp(seconds,.05f,.08f);
            holds[slot]=new Hold{animator=animator,agent=agent,speed=animator!=null?animator.speed:1,stopped=agent!=null&&agent.enabled&&agent.isOnNavMesh?agent.isStopped:false,until=Time.unscaledTime+LastHitStop};
            if(animator!=null)animator.speed=0;HitStops++;
        }
        public void Pulse(float strength,float seconds=.065f)
        {
            LastImpulse=strength;if(impulse!=null)impulse.Pulse(strength);
            flashStrength=Mathf.Clamp01(.7f+strength*.15f);flashUntil=Time.unscaledTime+.055f;ImpactFrames++;
            speedLines.rectTransform.localEulerAngles=new Vector3(0,0,Random.Range(-5f,5f));
        }
        void LateUpdate()
        {
            for(int i=0;i<holds.Length;i++)
            {
                var h=holds[i];if(h.until==0)continue;
                if(Time.unscaledTime<h.until){if(h.animator!=null)h.animator.speed=0;if(h.agent!=null&&h.agent.enabled&&h.agent.isOnNavMesh)h.agent.isStopped=true;}
                else {Restore(h);holds[i]=default(Hold);}
            }
            bool visible=Time.unscaledTime<flashUntil;flash.enabled=visible;speedLines.enabled=ar==null&&visible&&speedLines.texture!=null;
            if(!visible)return;
            bool reduced=UI.SettingsManager.Instance!=null&&UI.SettingsManager.Instance.Current.ReduceSkillFlashes;
            float remaining=(flashUntil-Time.unscaledTime)/.055f;
            flash.color=remaining>.5f?new Color(.008f,.002f,.018f,reduced?.10f:flashStrength):new Color(.03f,.008f,.055f,reduced?.07f:.48f);
            if(ar!=null)flash.color=new Color(1,.78f,.25f,reduced?.08f:.35f);
            speedLines.color=new Color(1,.91f,.55f,reduced?.15f:.8f);
        }
        static void Restore(Hold h){if(h.animator!=null)h.animator.speed=h.speed;if(h.agent!=null&&h.agent.enabled&&h.agent.isOnNavMesh)h.agent.isStopped=h.stopped;}
        void OnDisable(){for(int i=0;i<holds.Length;i++){if(holds[i].until!=0)Restore(holds[i]);holds[i]=default(Hold);}if(flash!=null)flash.enabled=false;if(speedLines!=null)speedLines.enabled=false;}
    }
}
