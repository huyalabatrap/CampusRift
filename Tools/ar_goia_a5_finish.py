from pathlib import Path
p=Path('Assets/ARRift/Runtime/ARBattleHUD.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('if(Debug.isDebugBuild||Application.isEditor)MenuItem(L("Kiểm cử chỉ","Gesture debug"),"?",-155,()=>ARSceneNavigation.Enter("ARGestureDebug"));', '''#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var check=GetComponent<ARGestureCheck>()??gameObject.AddComponent<ARGestureCheck>();
            MenuItem("Kiểm Ấn","?",-155,()=>{SetMenu(false);SetHelp(false);check.Open();});
#endif''')
s=s.replace('toastPanel.gameObject.SetActive(Time.unscaledTime<toastUntil);', 'if(!string.IsNullOrEmpty(caster.source.Error)){toast.text=caster.source.Error;toastUntil=Time.unscaledTime+.2f;}\n            toastPanel.gameObject.SetActive(Time.unscaledTime<toastUntil);')
p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARBattlefield.cs');s=p.read_text(encoding='utf-8-sig').replace('!applicationPaused&&(PracticeActive||!Paused)','!applicationPaused&&!MenuPaused&&!UserPaused&&(PracticeActive||!Paused)');p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARGestureCheck.cs');s=p.read_text(encoding='utf-8-sig').replace('1450,635','1450,530').replace('1370,590','1370,495')
s=s.replace('ARUI.Round(canvas,"Đóng Kiểm Ấn"', 'ARUI.Button(canvas,"THỬ LẠI NHẬN DẠNG",0,-300,630,()=>bridge.Retry());\n            ARUI.Round(canvas,"Đóng Kiểm Ấn"')
s=s.replace('if(Stage==Phase.Trials&&trialOpen&&!caster.gestures.Geometry.inFrame)', 'if(Stage==Phase.Trials&&trialOpen&&f.handPresent&&!caster.gestures.Geometry.inFrame)')
s=s.replace('foreach(var obj in prepareObjects)obj.SetActive(prepare);','foreach(var obj in prepareObjects)obj.SetActive(prepare);canvas.Find("THỬ LẠI NHẬN DẠNG").gameObject.SetActive(prepare||!string.IsNullOrEmpty(bridge.Error));')
s=s.replace('public void NewSession()', 'public void NewSession()')
p.write_text(s,encoding='utf-8')
p=Path('Assets/ARRift/Runtime/ARDeviceDiagnostics.cs');s=p.read_text(encoding='utf-8-sig').replace('input={(sampler!=null?sampler.InputSize.x:0)}x{(sampler!=null?sampler.InputSize.y:0)}";', 'input={(sampler!=null?sampler.InputSize.x:0)}x{(sampler!=null?sampler.InputSize.y:0)} scores={(gesture!=null&&gesture.Latest.fullScores?"full":"winner")} categories={(gesture!=null&&gesture.Latest.categoryLabels!=null?gesture.Latest.categoryLabels.Length:0)} lightRequested={(manager!=null?manager.requestedLightEstimation.ToString():"none")} lightCurrent={(manager!=null?manager.currentLightEstimation.ToString():"none")}";');p.write_text(s,encoding='utf-8')
