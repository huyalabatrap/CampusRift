#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Rendering.Universal;
using TMPro;

namespace CampusRift.AR
{
    public sealed class ARDeviceDiagnostics : MonoBehaviour
    {
        ARCameraManager manager;
        ARCameraBackground background;
        Camera camera;
        ARXRLoaderControl loader;
        GameObject panel;
        TMP_Text label; RectTransform box; CanvasGroup group; Button[] controls; bool collapsed,holdTriggered;
        long frames;
        float nextLog, nextRefresh, holdSince=-1;
        public bool Visible => panel != null && panel.activeSelf;

        void Start()
        {
            loader = GetComponent<ARXRLoaderControl>();
            manager = FindAnyObjectByType<ARCameraManager>();
            if (manager != null)
            {
                manager.frameReceived += Frame;
                camera = manager.GetComponent<Camera>();
                background = manager.GetComponent<ARCameraBackground>();
            }
            var content = ARUI.Canvas(transform, "AR diagnostics");
            content.GetComponentInParent<Canvas>().sortingOrder = 500;
            var rect = ARUI.Panel(content, "ARDiag", 0, 30, 560, 420);box=rect;group=rect.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;
            var image=rect.GetComponent<Image>();image.sprite=null;image.color=new Color(.025f,.04f,.08f,.7f);
            panel = rect.gameObject;
            foreach (var graphic in panel.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            ARUI.Text(rect,"[ARDiag]",-155,178,210,24,14).alignment=TextAlignmentOptions.Left;
            var collapse=ARUI.Round(rect,"Collapse ARDiag","−",206,178,42,Collapse);collapse.GetComponentInChildren<TMP_Text>().fontSizeMax=14;
            var close=ARUI.Round(rect,"Close ARDiag","X",253,178,42,Toggle);close.GetComponentInChildren<TMP_Text>().fontSizeMax=14;
            controls=new[]{collapse,close};
            // The diagnostic surface passes touches through; only its explicit controls intercept.
            foreach(var button in new[]{collapse,close}){button.gameObject.AddComponent<Canvas>().overrideSorting=true;button.GetComponent<Canvas>().sortingOrder=501;button.gameObject.AddComponent<GraphicRaycaster>();button.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups=true;}
            label = ARUI.Text(rect, "", 0, -15, 528, 350, 12);
            label.alignment = TextAlignmentOptions.TopLeft;label.raycastTarget=false;
            panel.SetActive(false);
        }
        void Frame(ARCameraFrameEventArgs _) { frames++; }
        public void Toggle() { panel.SetActive(!panel.activeSelf); if (Visible) Refresh(); }
        public void Collapse(){collapsed=!collapsed;box.sizeDelta=new Vector2(560,collapsed?52:420);box.anchoredPosition=new Vector2(0,collapsed?214:30);foreach(Transform child in box){var r=child as RectTransform;if(r!=null&&child!=label.transform)r.anchoredPosition=new Vector2(r.anchoredPosition.x,collapsed?0:178);}label.gameObject.SetActive(!collapsed);}
        void Update()
        {
            if(panel==null)return;
            var hud=FindAnyObjectByType<ARBattleHUD>();group.alpha=hud!=null&&hud.ModalOpen?0:1;foreach(var control in controls)control.gameObject.SetActive(group.alpha>0);
            if(Visible&&Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.25f;Refresh();}
            Vector2 position = default;
            bool pressed = false;
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            { pressed = true; position = touch.primaryTouch.position.ReadValue(); }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            { pressed = true; position = Mouse.current.position.ReadValue(); }
            var safe = Screen.safeArea;
            float corner = Mathf.Min(safe.width, safe.height) * .12f;
            if (pressed && position.x >= safe.xMax - corner && position.x <= safe.xMax &&
                position.y >= safe.yMax - corner && position.y <= safe.yMax)
            {
                if(holdSince<0)holdSince=Time.unscaledTime;
                if(!holdTriggered&&Time.unscaledTime-holdSince>=2){holdTriggered=true;Toggle();}
            }
            else {holdSince=-1;holdTriggered=false;}
            if (Time.unscaledTime >= nextLog)
            {
                nextLog = Time.unscaledTime + 2;
                Refresh();
                var field=FindAnyObjectByType<ARBattlefield>();Debug.Log("[ARDiag] "+details.Replace('\n', ' ')+" lightHasValue="+(field!=null?field.LightValueFlags:-1));
            }
        }
        public static string RecognitionStatus(GameObject owner,bool compact=false)
        {
            var bridge=owner.GetComponent<GestureRecognizerBridge>()??FindAnyObjectByType<GestureRecognizerBridge>();var caster=bridge!=null?bridge.GetComponent<ARSkillCaster>():null;
            var field=bridge!=null?bridge.GetComponent<ARBattlefield>():null;var sampler=bridge!=null?bridge.GetComponent<FrameSampler>():null;
            var motion=bridge!=null?bridge.GetComponent<ARHandInteractions>()?.motion:null;
            if(bridge==null)return "recognizer=pending";
            var d1=caster!=null?caster.HandState(bridge.PrimaryHandId):null;
            string decision=field!=null&&field.placement.InputBlocked?"input-blocked":field!=null&&field.Paused?"paused":!bridge.SamplingActive?"sampling-paused":d1!=null?d1.Decision:"pending";
            if(decision=="dynamic-motion")decision="blocked-by-motion";
            if(decision=="release-required")decision="requireRelease";
            string error=string.IsNullOrEmpty(bridge.Error)?"none":bridge.Error;
            string last=string.IsNullOrEmpty(bridge.LastNativeError)?"none":bridge.LastNativeError;
            var f=bridge.Latest;
            string text=$"recognizer ready={bridge.Ready} hands={bridge.HandCount} delegate={bridge.DelegateName} recovering={bridge.Recovering}\n"+
                $"results/s={bridge.ResultsPerSecond:0.0} accepted/s={bridge.AcceptedPerSecond:0.0} total={bridge.ResultCount} model={f.label??"None"} score={f.score:0.00}\n"+
                $"D1={decision} lastReject={(d1!=null?d1.LastRejection:"none")} motion={(motion!=null?motion.State.ToString():"none")} block={(motion!=null&&motion.BlocksStatic)} ({motion?.Reason??"none"})\n";
            if(!compact)text+=$"sampling={bridge.SamplingActive} paused={(field!=null&&field.Paused)} input-blocked={(field!=null&&field.placement.InputBlocked)} epoch={bridge.Epoch} frame={f.frameId} discard={bridge.LastDiscard}\n"+
                $"hand={f.handPresent} norm={(f.landmarks?.Length??0)} world={(f.worldLandmarks?.Length??0)} submitted={(sampler!=null?sampler.Submitted:0)} targetHz={(sampler!=null?sampler.TargetHz:0)}\n";
            text+="native error="+error+(last!="none"&&last!=error?"\nlast native error="+last:"");
            return text;
        }
        static string R(Rect r) => $"({r.x:0},{r.y:0},{r.width:0},{r.height:0})";
        string details;
        void Refresh()
        {
            var pipeline = loader != null ? loader.SessionPipeline : null;
            var data = camera != null ? camera.GetComponent<UniversalAdditionalCameraData>() : null;
            string renderer = data != null && pipeline != null ? data.scriptableRenderer.GetType().Name : "pending";
            if (pipeline != null && pipeline.rendererDataList.Length > 0)
                renderer += "/" + pipeline.rendererDataList[0].name;
            var gesture=FindAnyObjectByType<GestureRecognizerBridge>();var sampler=FindAnyObjectByType<FrameSampler>();
            details = $"Screen={Screen.width}x{Screen.height} {Screen.orientation}\n" +
                $"safeArea={R(Screen.safeArea)} Display={Display.main.renderingWidth}x{Display.main.renderingHeight}\n" +
                $"pixelRect={(camera != null ? R(camera.pixelRect) : "pending")} GPU={SystemInfo.graphicsDeviceType}\n" +
                $"ARSession={ARSession.state} Background={(background != null && background.enabled)}\n" +
                $"BackgroundRendering={(background != null && background.backgroundRenderingEnabled)} frames={frames}\n" +
                $"renderer={renderer} pipeline={(pipeline != null ? pipeline.name : "pending")}\n" +
                $"renderScale={(pipeline != null ? pipeline.renderScale : 0):0.00} ready={(loader != null && loader.Ready)}\n"+
                $"gestureHz={(gesture!=null?gesture.RecognitionFps:0):0.0} latencyMs={(gesture!=null?gesture.LatencyMs:0):0.0} delegate={(gesture!=null?gesture.DelegateName:"pending")} input={(sampler!=null?sampler.InputSize.x:0)}x{(sampler!=null?sampler.InputSize.y:0)} scores={(gesture!=null&&gesture.Latest.fullScores?"full":"winner")} categories={(gesture!=null&&gesture.Latest.categoryLabels!=null?gesture.Latest.categoryLabels.Length:0)} lightRequested={(manager!=null?manager.requestedLightEstimation.ToString():"none")} lightCurrent={(manager!=null?manager.currentLightEstimation.ToString():"none")}\n"+RecognitionStatus(gameObject);
            label.text=details;
        }
        void OnDestroy() { if (manager != null) manager.frameReceived -= Frame; }
    }
}
#endif
