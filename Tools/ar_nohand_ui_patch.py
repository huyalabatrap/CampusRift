from pathlib import Path
root=Path.cwd()
def change(rel,a,b):
 p=root/rel;s=p.read_text(encoding='utf-8-sig');assert a in s,rel;p.write_text(s.replace(a,b,1),encoding='utf-8')
rel='Assets/ARRift/Runtime/ARDeviceDiagnostics.cs'
change(rel,'float nextLog, lastTap;', 'float nextLog, nextRefresh, lastTap;')
change(rel,'ARUI.Panel(content, "ARDiag", 610, 345, 670, 330)', 'ARUI.Panel(content, "ARDiag", 580, 180, 740, 620)')
change(rel,'ARUI.Text(rect, "", 0, 0, 625, 292, 19)', 'ARUI.Text(rect, "", 0, 0, 700, 582, 18)')
change(rel,'label.alignment = TextAlignmentOptions.TopLeft;', 'label.alignment = TextAlignmentOptions.TopLeft;label.raycastTarget=false;')
change(rel,'void Update()\n        {','''void Update()
        {
            var recognizer=GetComponent<GestureRecognizerBridge>();
            // Native errors open the diagnostic box on the next Unity frame.
            if(recognizer!=null&&!string.IsNullOrEmpty(recognizer.Error)){panel.SetActive(true);Refresh();}
            if(Visible&&Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.25f;Refresh();}''')
# Append a common live status used by both surfaces.
change(rel,'static string R(Rect r)', '''public static string RecognitionStatus(GameObject owner,bool compact=false)
        {
            var bridge=owner.GetComponent<GestureRecognizerBridge>();var caster=owner.GetComponent<ARSkillCaster>();
            var field=owner.GetComponent<ARBattlefield>();var sampler=owner.GetComponent<FrameSampler>();
            var motion=owner.GetComponent<ARHandInteractions>()?.motion;
            if(bridge==null)return "recognizer=pending";
            var d1=caster!=null?caster.HandState(bridge.PrimaryHandId):null;
            string decision=field!=null&&field.placement.InputBlocked?"input-blocked":field!=null&&field.Paused?"paused":!bridge.SamplingActive?"sampling-paused":d1!=null?d1.Decision:"pending";
            if(decision=="dynamic-motion")decision="blocked-by-motion";
            if(decision=="release-required")decision="requireRelease";
            string error=string.IsNullOrEmpty(bridge.Error)?"none":bridge.Error;
            string last=string.IsNullOrEmpty(bridge.LastNativeError)?"none":bridge.LastNativeError;
            var f=bridge.Latest;
            string text=$"recognizer ready={bridge.Ready} hands={bridge.HandCount} delegate={bridge.DelegateName} recovering={bridge.Recovering}\\n"+
                $"results/s={bridge.ResultsPerSecond:0.0} accepted/s={bridge.AcceptedPerSecond:0.0} total={bridge.ResultCount} model={f.label??"None"} score={f.score:0.00}\\n"+
                $"D1={decision} lastReject={(d1!=null?d1.LastRejection:"none")} motion={(motion!=null?motion.State.ToString():"none")} block={(motion!=null&&motion.BlocksStatic)} ({motion?.Reason??"none"})\\n";
            if(!compact)text+=$"sampling={bridge.SamplingActive} paused={(field!=null&&field.Paused)} input-blocked={(field!=null&&field.placement.InputBlocked)} epoch={bridge.Epoch} frame={f.frameId} discard={bridge.LastDiscard}\\n"+
                $"hand={f.handPresent} norm={(f.landmarks?.Length??0)} world={(f.worldLandmarks?.Length??0)} submitted={(sampler!=null?sampler.Submitted:0)} targetHz={(sampler!=null?sampler.TargetHz:0)}\\n";
            text+="native error="+error+(last!="none"&&last!=error?"\\nlast native error="+last:"");
            return text;
        }
        static string R(Rect r)''')
change(rel,'lightCurrent={(manager!=null?manager.currentLightEstimation.ToString():"none")}";', 'lightCurrent={(manager!=null?manager.currentLightEstimation.ToString():"none")}\\n"+RecognitionStatus(gameObject);')
rel='Assets/ARRift/Runtime/ARGestureCheck.cs'
change(rel,'TMP_Text title,body,timer,summary,', 'TMP_Text diagnostics;TMP_Text title,body,timer,summary,')
change(rel,'ARUI.Button(canvas,"ĐỔI BỘ NHẬN",720,-205,320,', 'ARUI.Button(canvas,"ĐỔI BỘ NHẬN",720,-305,320,')
change(rel,'ARUI.Button(canvas,"THỬ LẠI NHẬN DẠNG",0,-300,630,', 'ARUI.Button(canvas,"THỬ LẠI NHẬN DẠNG",0,-335,630,')
change(rel,'handLabel=ARUI.Text(canvas,"",-350,-125,620,58,23);lightLabel=ARUI.Text(canvas,"",350,-125,620,58,23);', 'handLabel=ARUI.Text(canvas,"",-350,-90,620,58,23);lightLabel=ARUI.Text(canvas,"",350,-90,620,58,23);')
change(rel,'ARUI.Button(canvas,"ĐỔI TAY",-350,-205,400,', 'ARUI.Button(canvas,"ĐỔI TAY",-350,-150,400,')
change(rel,'ARUI.Button(canvas,"ĐỔI ÁNH SÁNG",350,-205,400,', 'ARUI.Button(canvas,"ĐỔI ÁNH SÁNG",350,-150,400,')
change(rel,'canvas.gameObject.SetActive(false);', '''diagnostics=ARUI.Text(canvas,"",0,-240,1320,124,17);
            diagnostics.alignment=TextAlignmentOptions.TopLeft;diagnostics.raycastTarget=false;
            canvas.gameObject.SetActive(false);''')
change(rel,'if(!Opened||field==null)return;double dt=', 'if(!Opened||field==null)return;diagnostics.gameObject.SetActive(Stage!=Phase.Results);diagnostics.text="[ARDiag] "+ARDeviceDiagnostics.RecognitionStatus(gameObject,true);double dt=')
print('Both development diagnostic surfaces updated.')
