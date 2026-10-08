using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARGestureDebug : MonoBehaviour
    {
        public GestureRecognizerBridge bridge;public ARModeSettings settings;TMP_Text label,fingers;ARLandmarkGraphic overlay;readonly ARArcGraphic[] bars=new ARArcGraphic[5];readonly GestureStateMachine evidence=new GestureStateMachine();float nextLog;
        void Start()
        {
            var canvas=ARUI.Canvas(transform,"Gesture debug");var r=ARUI.Rect(canvas.parent,"21 landmarks",0,0,0,0);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;r.SetAsFirstSibling();overlay=r.gameObject.AddComponent<ARLandmarkGraphic>();overlay.color=ComicTheme.Green;overlay.raycastTarget=false;
            label=ARUI.Text(ARUI.Panel(canvas,"Gesture metrics",0,428,1380,165),"",0,0,1320,150,25);fingers=ARUI.Text(ARUI.Panel(canvas,"Finger states",0,288,1380,80),"",0,0,1320,70,22);
            for(int i=0;i<5;i++){var panel=ARUI.Panel(canvas,"Evidence "+i,-770+i*385,-265,350,146);ARUI.Text(panel,GestureSkillMapper.Labels[i],0,43,330,54,23);bars[i]=ARUI.Rect(panel,"Evidence ring",0,-20,68,68).gameObject.AddComponent<ARArcGraphic>();bars[i].color=ComicTheme.Gold;bars[i].raycastTarget=false;}
            ARUI.Button(canvas,LevelHUD.Vietnamese?"CHIẾN TRƯỜNG AR":"AR BATTLE",-220,-470,400,()=>ARSceneNavigation.Enter());ARUI.Button(canvas,LevelHUD.Vietnamese?"THOÁT":"EXIT",220,-470,400,ARSessionBootstrap.Exit);
            evidence.Settings=settings;
            bridge.Result+=Received;
        }
        void Received(GestureFrame f)
        {
            evidence.Process(f);overlay.Set(f);label.text=f.label+"   "+f.score.ToString("0.00")+"   "+f.handed+"\n"+bridge.RecognitionFps.ToString("0.0")+" Hz / "+bridge.LatencyMs.ToString("0")+" ms / "+bridge.DelegateName+" / "+f.width+"x"+f.height+"\n"+evidence.TopLabel+" "+evidence.TopEvidence.ToString("0.00")+" / "+evidence.SecondLabel+" "+evidence.SecondEvidence.ToString("0.00")+" / "+evidence.Decision;
            fingers.text=evidence.Geometry.label+" / "+evidence.Geometry.Fingers;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(Time.unscaledTime>=nextLog){nextLog=Time.unscaledTime+.2f;Debug.Log($"[ARGesture] model={f.label} score={f.score:0.00} geometry={evidence.Geometry.label} top={evidence.TopLabel}:{evidence.TopEvidence:0.00} second={evidence.SecondLabel}:{evidence.SecondEvidence:0.00} decision={evidence.Decision} latencyMs={bridge.LatencyMs:0.0} Hz={bridge.RecognitionFps:0.0} delegate={bridge.DelegateName} input={f.width}x{f.height}");}
#endif
        }
        void Update(){if(label==null)return;if(!string.IsNullOrEmpty(bridge.Error))label.text=bridge.Error;for(int i=0;i<5;i++)bars[i].Amount=Mathf.MoveTowards(bars[i].Amount,Mathf.Clamp01(evidence.Evidence[i]/1.5f),Time.unscaledDeltaTime*6);}
        void OnDestroy(){if(bridge!=null)bridge.Result-=Received;}
    }
}
