#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
namespace CampusRift.AR
{
    // A single volunteered session: RAM scalars only, one aggregate log on explicit export.
    public sealed class ARGestureCheck:MonoBehaviour
    {
        public enum Phase { Prepare,Warmup,Placement,Transition,Trials,Negative,Play,Results }
        public Phase Stage {get;private set;}=Phase.Prepare;public bool Opened {get;private set;}
        public ARReviewMetrics Metrics {get;private set;}=new ARReviewMetrics();
        public string LastJson {get;private set;}public int TrialNumber=>Mathf.Max(0,trialIndex)+1;public int RequestedGesture=>cell!=null?cell.g:0;
        public bool ExportConsent {get;private set;}
        ARBattlefield field;ARSkillCaster caster;GestureRecognizerBridge bridge;RiftPlacementService placement;ARMonsterDirector director;
        RectTransform canvas,panel,resultPanel,diagnosticPanel;TMP_Text diagnostics;TMP_Text title,body,timer,summary,consentLabel,handLabel,lightLabel,skipLabel;ARHandGraphic illustration;
        Button begin,skip,export,next;double stageAt,lastUpdate,loadAt;int slot=-1,trialIndex=-1,block=-1,negativeStep=-1,stepIntents,playVfx;double slotTracking=-1;
        bool slotDone,trialOpen,trialSkip,releaseReady,fullScores,reduced;int first=-1,trialIntents,late,frameRejects;double trialUntil;double? trialCast,trialVfx,trialIntent,trialSpan;
        ARReviewMetrics.Cell cell;readonly int[] order=new int[120];string hand="R",light="normal-estimated",sid;bool exported;
        static readonly string[] names={"XÒE TAY","NẮM TAY","CHỈ NGÓN TRỎ LÊN","HAI NGÓN CHỮ V","NGÓN CÁI CHỈ XUỐNG"};
        double Now=>Time.realtimeSinceStartupAsDouble;
        void Start()
        {
            field=GetComponent<ARBattlefield>();caster=GetComponent<ARSkillCaster>();bridge=GetComponent<GestureRecognizerBridge>();placement=field.placement;director=GetComponent<ARMonsterDirector>();
            bridge.Result+=Frame;caster.Outcome+=Outcome;caster.FirstVfx+=Vfx;placement.Anchored+=Placed;
            canvas=ARUI.Canvas(transform,"Kiểm Ấn");canvas.GetComponentInParent<Canvas>().sortingOrder=400;
            panel=ARUI.Panel(canvas,"Kiểm Ấn hướng dẫn",0,345,1420,300);title=ARUI.Text(panel,"KIỂM ẤN · khoảng 14 phút",0,105,1340,60,30);
            body=ARUI.Text(panel,"",0,5,1320,142,24);timer=ARUI.Text(panel,"",0,-110,1240,56,22);
            illustration=ARUI.Rect(canvas,"Cử chỉ yêu cầu",-770,0,130,170).gameObject.AddComponent<ARHandGraphic>();illustration.raycastTarget=false;
            var controls=ARUI.Panel(canvas,"Kiểm Ấn thao tác",0,-420,1450,125);
            begin=ARUI.Button(controls,"BẮT ĐẦU",-510,0,250,Begin);
            skip=ARUI.Button(controls,"BỎ LƯỢT",-235,0,250,Skip);skipLabel=skip.GetComponentInChildren<TMP_Text>();
            next=ARUI.Button(controls,"TIẾP TỤC",40,0,250,Advance);
            export=ARUI.Button(controls,"XUẤT KẾT QUẢ",340,0,330,Export);
            ARUI.Button(canvas,"ĐỔI BỘ NHẬN",720,-335,320,()=>bridge.SelectDelegate(bridge.DevDelegate=="AUTO"?"CPU":bridge.DevDelegate=="CPU"?"GPU":"AUTO"));
            ARUI.Button(canvas,"THỬ LẠI NHẬN DẠNG",0,-335,630,()=>bridge.Retry());
            ARUI.Round(canvas,"Đóng Kiểm Ấn","X",850,450,64,Close);
            consentLabel=ARUI.Text(canvas,"",0,85,1280,70,24);var consent=ARUI.Button(canvas,"ĐỒNG Ý XUẤT CHỈ SỐ",0,-15,630,ToggleConsent);
            handLabel=ARUI.Text(canvas,"",-350,-90,620,58,23);lightLabel=ARUI.Text(canvas,"",350,-90,620,58,23);
            ARUI.Button(canvas,"ĐỔI TAY",-350,-150,400,()=>hand=hand=="R"?"L":"R");ARUI.Button(canvas,"ĐỔI ÁNH SÁNG",350,-150,400,()=>light=light=="normal-estimated"?"bright-estimated":"normal-estimated");
            resultPanel=ARUI.Panel(canvas,"Kiểm Ấn kết quả",0,-10,1450,530);summary=ARUI.Text(resultPanel,"",0,0,1370,495,21);summary.alignment=TextAlignmentOptions.TopLeft;
            prepareObjects=new GameObject[]{consentLabel.gameObject,consent.gameObject,handLabel.gameObject,lightLabel.gameObject,handLabel.transform.parent.Find("ĐỔI TAY").gameObject,lightLabel.transform.parent.Find("ĐỔI ÁNH SÁNG").gameObject};
            diagnosticPanel=ARUI.Panel(canvas,"Recognition diagnostics",0,-240,1380,134);
            diagnosticPanel.GetComponent<Image>().raycastTarget=false;
            diagnostics=ARUI.Text(diagnosticPanel,"",0,0,1320,124,17);
            diagnostics.alignment=TextAlignmentOptions.TopLeft;diagnostics.raycastTarget=false;
            canvas.gameObject.SetActive(false);
        }
        GameObject[] prepareObjects;
        public void Open(){Opened=true;canvas.gameObject.SetActive(true);if(Stage==Phase.Prepare){field.PracticeActive=true;field.CheckLoad=true;}Refresh();}
        public void ToggleConsent(){ExportConsent=!ExportConsent;if(!ExportConsent){StopSession();Metrics=new ARReviewMetrics();LastJson=null;Stage=Phase.Prepare;}Refresh();}
        public void Begin()
        {
            if(!ExportConsent)return;Metrics=new ARReviewMetrics();sid=Guid.NewGuid().ToString("N").Substring(0,8);exported=false;slot=trialIndex=block=negativeStep=-1;trialOpen=false;fullScores=false;loadAt=0;lastUpdate=Now;
            var rng=new System.Random();for(int b=0;b<6;b++){var items=new List<int>();for(int g=0;g<5;g++)for(int r=0;r<4;r++)items.Add(g);for(int i=items.Count-1;i>0;i--){int j=rng.Next(i+1);int v=items[i];items[i]=items[j];items[j]=v;}for(int i=0;i<20;i++)order[b*20+i]=items[i];}
            field.UserPaused=false;field.PracticeActive=true;SetPhase(Phase.Warmup);
        }
        void SetPhase(Phase phase)
        {
            Stage=phase;stageAt=Now;caster.Practice=phase==Phase.Trials;caster.InjectedRejection=null;field.CheckLoad=phase==Phase.Warmup||phase>=Phase.Transition&&phase<=Phase.Play;
            if(phase==Phase.Placement){placement.Reposition();slot=-1;}
            if(phase==Phase.Transition){placement.Reposition();loadAt=Now;}
            if(phase==Phase.Play)playVfx=0;
            lastUpdate=Now;
            if(phase==Phase.Results){FinishTrial();StopSession();RefreshSummary();}
            Refresh();
        }
        public void Advance()
        {if(Stage==Phase.Warmup)SetPhase(Phase.Placement);else if(Stage==Phase.Transition)SetPhase(Phase.Trials);}
        public void Skip(){if(Stage==Phase.Trials&&trialOpen)trialSkip=true;else if(Stage==Phase.Placement&&!slotDone){slotDone=true;Metrics.placementSkips++;if(slot<8){Metrics.placementTimes.Add(null);Metrics.placementTimeouts++;}}}
        void StopSession(){if(field==null)return;field.PracticeActive=false;field.CheckLoad=false;caster.Practice=false;caster.InjectedRejection=null;}
        public void Close(){if(Stage!=Phase.Prepare&&Stage!=Phase.Results){FinishTrial();Stage=Phase.Results;StopSession();RefreshSummary();}Opened=false;canvas.gameObject.SetActive(false);field.PracticeActive=false;}
        public void NewSession(){StopSession();Stage=Phase.Prepare;Metrics=new ARReviewMetrics();trialOpen=false;Refresh();}
        void Update()
        {
            if(!Opened||field==null)return;diagnosticPanel.gameObject.SetActive(Stage!=Phase.Results);diagnostics.text="[ARDiag] "+ARDeviceDiagnostics.RecognitionStatus(gameObject,true);double dt=Math.Max(0,Now-lastUpdate);lastUpdate=Now;double t=Now-stageAt;
            bool active=Stage>=Phase.Trials&&Stage<=Phase.Play&&!field.Paused;
            if(active){Metrics.active+=dt;Metrics.RenderFrame(dt);Metrics.below27=dt>1f/27?Metrics.below27+dt:0;Metrics.slowestRun=Math.Max(Metrics.slowestRun,Metrics.below27);if(Stage==Phase.Negative&&negativeStep==0)Metrics.noneExposure+=dt;else if(Stage==Phase.Negative&&negativeStep<4)Metrics.poseExposure+=dt;}
            var quality=GetComponent<ARAdaptiveQuality>();if(quality!=null){if(quality.ThermalSeverity.HasValue)Metrics.thermalMax=Math.Max(Metrics.thermalMax??0,quality.ThermalSeverity.Value);if(quality.Reduced&&!reduced){Metrics.tierChanges++;reduced=true;}}
            if(Stage==Phase.Warmup&&t>=60)SetPhase(Phase.Placement);
            else if(Stage==Phase.Placement)
            {
                int current=Math.Min(9,(int)(t/12));if(current!=slot){EndSlot();slot=current;slotDone=false;slotTracking=-1;placement.Reposition();}
                if(slotTracking<0&&ARSession.state==ARSessionState.SessionTracking)slotTracking=Now;
                if(!slotDone&&slotTracking>=0&&Now-slotTracking>=10)EndSlot();if(t>=120){EndSlot();SetPhase(Phase.Transition);}
            }
            else if(Stage==Phase.Transition){if(placement.Adjusting)placement.StartBattlefield();if(t>=30)SetPhase(Phase.Trials);}
            else if(Stage==Phase.Trials)
            {
                if(t>=360){FinishTrial();SetPhase(Phase.Negative);}
                else {int b=(int)(t/60);double within=t-b*60;if(b!=block){FinishTrial();block=b;trialIndex=-1;}if(within>=5){int index=b*20+Math.Min(19,(int)((within-5)/2.75));if(index!=trialIndex){FinishTrial();StartTrial(index,stageAt+b*60+5+(index-b*20)*2.75);}}}
            }
            else if(Stage==Phase.Negative)
            {
                int step=Math.Min(5,(int)(t/30));int sub=step==4?(int)((t-120)/6):step==5?(int)((t-150)/10):0;int key=step*10+sub;
                if(key!=negativeKey){negativeKey=key;stepIntents=0;}negativeStep=step;caster.InjectedRejection=step==5&&(t-150)%10<5?(CastOutcome?)(sub==0?CastOutcome.Cooldown:sub==1?CastOutcome.Spirit:CastOutcome.Aim):null;
                if(t>=180)SetPhase(Phase.Play);
            }
            else if(Stage==Phase.Play){if(playVfx<5&&t>=5+playVfx*10){if(caster.CheckScheduledVfx(playVfx))Metrics.scheduledVfx++;else Metrics.Reason("scheduled-vfx-unavailable");playVfx++;}if(t>=60)SetPhase(Phase.Results);}
            Refresh();
        }
        int negativeKey=-1;
        void StartTrial(int index,double start)
        {trialIndex=index;int g=order[index],d=new[]{25,40,55}[block/2],o=block%2;cell=Metrics.Find(g,d,o);first=-1;trialIntents=late=frameRejects=0;trialSkip=false;trialCast=trialVfx=trialIntent=trialSpan=null;releaseReady=caster.gestures.ReleaseReady;if(!releaseReady)Metrics.Reason("release-not-ready");trialOpen=true;trialUntil=start+2.25;}
        void FinishTrial()
        {
            if(!trialOpen)return;trialOpen=false;if(trialSkip){cell.skip++;return;}cell.n++;if(first<0)cell.timeout++;else{cell.first[first]++;if(first==cell.g)Metrics.correctIntent[first]++;}
            if(first==cell.g&&trialIntents==1){cell.correctUnique++;if(trialIntent.HasValue)Metrics.firstValidIntent.Add(trialIntent.Value);if(trialSpan.HasValue)Metrics.captureSpan.Add(trialSpan.Value);if(trialCast.HasValue){cell.latency.Add(trialCast.Value);Metrics.firstValidCast.Add(trialCast.Value);Metrics.skillCast[cell.g].Add(trialCast.Value);}if(trialVfx.HasValue){Metrics.triggerVfx.Add(trialVfx.Value);Metrics.skillVfx[cell.g].Add(trialVfx.Value);}}if(trialIntents>1)cell.multi++;cell.late+=late;cell.framing+=frameRejects;
        }
        void EndSlot(){if(slot<0||slotDone)return;slotDone=true;if(slot<8){Metrics.placementTimes.Add(null);Metrics.placementTimeouts++;}else Metrics.expectedRejects++;}
        void Placed()
        {
            if(!Opened||Stage!=Phase.Placement||slotDone)return;slotDone=true;if(slot<8)Metrics.placementTimes.Add(slotTracking>=0?(object)Math.Round((Now-slotTracking)*1000):null);else Metrics.wrongConfirm++;
        }
        void Frame(GestureFrame f)
        {
            if(!Opened||!ExportConsent||Stage==Phase.Prepare||Stage==Phase.Results)return;fullScores|=f.fullScores;
            if(Stage>=Phase.Trials&&Stage<=Phase.Play&&!field.Paused){Metrics.unique++;Metrics.Reason("frame-"+caster.gestures.Decision);if(f.convertReadyMs>=f.acquireMs&&f.acquireMs>0)Metrics.acquireConvert.Add(f.convertReadyMs-f.acquireMs);if(f.inferStartMs>0)Metrics.inferResult.Add(f.resultMs-f.inferStartMs);if(f.acquireMs>0)Metrics.acquireConsume.Add(f.consumeMs-f.acquireMs);if(f.nativeToUnityMs.HasValue){Metrics.clockError=Math.Max(Metrics.clockError??0,f.clockErrorMs??0);if(f.inferStartMs>0)Metrics.submitInfer.Add(Math.Max(0,f.inferStartMs+f.nativeToUnityMs.Value-f.submitMs));Metrics.resultConsume.Add(Math.Max(0,f.consumeMs-f.resultMs-f.nativeToUnityMs.Value));}}
            if(Stage==Phase.Trials&&trialOpen&&f.handPresent&&!caster.gestures.Geometry.inFrame)frameRejects++;
        }
        void Outcome(GestureIntent intent,CastOutcome outcome)
        {
            if(!Opened||!ExportConsent||Stage<Phase.Trials||Stage>Phase.Play)return;int g=Array.IndexOf(GestureSkillMapper.Labels,intent.label);if(g<0||g>=Metrics.allIntents.Length)return;Metrics.allIntents[g]++;Metrics.Reason(outcome.ToString());
            if(Stage==Phase.Trials&&trialOpen){cell.intents[g]++;trialIntents++;if(Now>trialUntil){late++;}else if(first<0&&releaseReady){first=g;trialIntent=intent.triggerConsumeMs-intent.firstValidMs;trialSpan=intent.captureSpanMs;}}
            if(Stage==Phase.Negative){if(negativeStep<4)Metrics.negativeIntents[g]++;else if(++stepIntents>1){if(negativeStep==4)Metrics.holdDuplicates++;else Metrics.rejectRetries++;}}
            if(outcome==CastOutcome.Success&&Stage==Phase.Trials&&trialOpen&&g==cell.g&&first==g&&trialIntents==1)trialCast=GestureRecognizerBridge.Now-intent.firstValidMs;
        }
        void Vfx(GestureIntent intent,double now){if(!Opened||!ExportConsent||Stage!=Phase.Trials)return;int g=Array.IndexOf(GestureSkillMapper.Labels,intent.label);if(trialOpen&&g==cell.g&&trialIntents<=1)trialVfx=intent.triggerResultMs.HasValue?(double?)(now-intent.triggerResultMs.Value):null;}
        public void Export()
        {
            if(!ExportConsent||Stage!=Phase.Results||exported)return;
            try{int total=0;foreach(var c in Metrics.cells)total+=c.n+c.skip;var sampler=GetComponent<FrameSampler>();if(sampler!=null)foreach(var pair in sampler.Drops)Metrics.drops[pair.Key]=pair.Value;LastJson=Metrics.Export(Application.version+"-goiA",SystemInfo.deviceModel,bridge.DelegateName,fullScores?"full":"winner",hand,light,total==120&&Metrics.placementTimes.Count==8&&Metrics.expectedRejects+Metrics.wrongConfirm==2&&Metrics.placementSkips==0&&Metrics.active>=600,new[]{sampler!=null?sampler.InputSize.x:0,sampler!=null?sampler.InputSize.y:0},sid);Debug.Log("[ARCheck] "+LastJson);exported=true;timer.text="Đã xuất · cắm cáp để đọc kết quả";}
            catch(Exception e){timer.text=e.Message;}
        }
        void Refresh()
        {
            if(canvas==null)return;bool prepare=Stage==Phase.Prepare;foreach(var obj in prepareObjects)obj.SetActive(prepare);canvas.Find("ĐỔI BỘ NHẬN").gameObject.SetActive(prepare);canvas.Find("THỬ LẠI NHẬN DẠNG").gameObject.SetActive(prepare||!string.IsNullOrEmpty(bridge.Error));begin.gameObject.SetActive(prepare);begin.interactable=ExportConsent;skip.gameObject.SetActive(Stage==Phase.Trials||Stage==Phase.Placement);next.gameObject.SetActive(Stage==Phase.Warmup||Stage==Phase.Transition);export.gameObject.SetActive(Stage==Phase.Results);resultPanel.gameObject.SetActive(Stage==Phase.Results);illustration.gameObject.SetActive(Stage==Phase.Trials&&trialOpen&&releaseReady);
            consentLabel.text=ExportConsent?"Đã đồng ý xuất chỉ số của phiên này. Không lưu ảnh hoặc điểm tay.":"Chỉ số chỉ ở bộ nhớ. Bật đồng ý để bắt đầu và xuất kết quả.";handLabel.text="Tay thao tác: "+(hand=="R"?"PHẢI":"TRÁI");lightLabel.text="Ánh sáng tự ước lượng: "+(light=="normal-estimated"?"ĐỦ SÁNG":"RẤT SÁNG");
            double t=Now-stageAt;title.text="KIỂM ẤN · "+StageName();timer.text=Stage==Phase.Prepare?"Chuẩn bị thước, bàn đủ rộng và cáp USB · Bộ nhận: "+bridge.DevDelegate:Stage==Phase.Results?"Kết quả chỉ ở bộ nhớ · nhấn XUẤT KẾT QUẢ":$"Còn {Math.Max(0,Duration()-t):0} giây · hạ tay giữa các lượt";
            switch(Stage)
            {
                case Phase.Prepare:body.text="Phiên khoảng 14 phút. Ngồi yên, đủ sáng; thước đánh dấu 25 / 40 / 55 cm.\nChọn tay quen dùng. Đồng ý xuất chỉ số riêng cho phiên này.";break;
                case Phase.Warmup:body.text="Bắt đầu ở 40 cm. Giơ lòng rồi mu tay, giữ đủ năm đầu ngón trong khung.\nHạ tay để thử lại. Đưa ngón cầm máy khỏi ống kính. Chuẩn bị bàn rộng.";break;
                case Phase.Placement:body.text=$"Đặt lần {Math.Max(1,slot+1)}/10 · "+(slot<6?"MẶT BÀN RỘNG":slot<8?"GẦN MÉP · vòng vẫn phải nằm trọn trên bàn":"CHỖ QUÁ HẸP · máy phải từ chối")+"\nLia máy nhẹ; chạm ĐẶT TRẬN khi vòng vàng. Vòng mờ: đang tìm mặt.";break;
                case Phase.Transition:body.text="Đặt lại trên bàn rộng để vào phần cử chỉ.\nNgắm tâm màn hình vào vòng trận. Trận sẽ giữ sáu quái; không thua trong Kiểm Ấn.";break;
                case Phase.Trials:
                    if(!trialOpen)body.text=$"Đổi sang {new[]{25,40,55}[Math.Max(0,block)/2]} cm · "+(block%2==0?"LÒNG TAY":"MU TAY")+"\nHạ tay. Thước đo từ camera sau đến bàn tay.";
                    else {illustration.Gesture=cell.g;body.text=$"Lượt {trialIndex+1}/120 · {cell.d} cm · "+(cell.o==0?"LÒNG TAY":"MU TAY")+"\n"+(Now>trialUntil?"HẠ TAY RA KHỎI KHUNG":!releaseReady?"Chưa nhả tay xong · hạ tay, lượt này vẫn tính":"GIƠ: "+names[cell.g])+" · ngắm tâm máy vào trận";}
                    break;
                case Phase.Negative:
                    string[] cues={"KHÔNG GIƠ TAY · ngắm vào vòng trận","Giơ ngón cái lên rồi dấu I love you · máy không được ra chiêu","Đổi tư thế liên tục, không giữ ổn định","Để ngón cầm máy lọt mép rồi bỏ ra · thử giơ tay lại","GIỮ 6 GIÂY: "+names[Math.Clamp((int)((t-120)/6),0,4)]+" · chỉ ra một lần","Hạ tay khi đổi yêu cầu rồi giơ một chiêu; GIỮ NGUYÊN khi báo "+(t<160?"HỒI CHIÊU":t<170?"THIẾU LINH LỰC":"NGẮM NGOÀI TRẬN")+" · sau 5 giây bỏ từ chối, vẫn giữ tay"};body.text=cues[Math.Clamp(negativeStep,0,5)]+"\nQuan sát chiêu phát nhầm/lặp. Kết quả tự đếm; từ chối được tạo để thử.";break;
                case Phase.Play:body.text="Chơi với sáu quái; máy tự phát năm hiệu ứng, cách nhau 10 giây. Giữ tâm ngắm trong trận.\nMở/đóng menu một lần; nhấn Home rồi trở lại nếu kịp. Sau khi trở lại, hạ tay.";break;
                case Phase.Results:body.text="Độ trễ tay đến màn hình chưa đo bằng quay chậm.\nĐây là phiên tự làm theo yêu cầu; Bỏ lượt khi biết mình làm sai.";break;
            }
        }
        double Duration()=>Stage==Phase.Warmup?60:Stage==Phase.Placement?120:Stage==Phase.Transition?30:Stage==Phase.Trials?360:Stage==Phase.Negative?180:Stage==Phase.Play?60:0;
        string StageName()=>Stage==Phase.Prepare?"CHUẨN BỊ":Stage==Phase.Warmup?"LÀM QUEN":Stage==Phase.Placement?"ĐẶT TRẬN":Stage==Phase.Transition?"VÀO TRẬN":Stage==Phase.Trials?"120 LƯỢT":Stage==Phase.Negative?"THỬ BẪY":Stage==Phase.Play?"CHƠI THẬT":"BẢNG KẾT QUẢ";
        void RefreshSummary()
        {
            var text=new System.Text.StringBuilder("ĐÚNG DUY NHẤT / SỐ LƯỢT · mỗi ô cần ≥3/4\n");int total=0;for(int g=0;g<5;g++){text.Append(names[g]).Append(": ");foreach(int d in new[]{25,40,55})for(int o=0;o<2;o++){var c=Metrics.Find(g,d,o);text.Append(d).Append(o==0?"L ":"M ").Append(c.correctUnique).Append('/').Append(c.n).Append("  ");total+=c.n+c.skip;}text.Append('\n');}
            int wrong=0,timeout=0,multi=0,skipN=0;foreach(var c in Metrics.cells){timeout+=c.timeout;multi+=c.multi;skipN+=c.skip;for(int g=0;g<5;g++)if(g!=c.g)wrong+=c.first[g];}
            text.Append($"\nĐã làm {total}/120 · sai {wrong} · hết giờ {timeout} · lặp {multi} · bỏ {skipN}\n");
            text.Append($"Thử bẫy: {ArraySum(Metrics.negativeIntents)} chiêu nhầm · giữ tay lặp {Metrics.holdDuplicates} · tự thử lại {Metrics.rejectRetries}\n");
            text.Append($"Đặt đúng {Metrics.placementTimes.Count-Metrics.placementTimeouts}/8 · từ chối chỗ hẹp {Metrics.expectedRejects}/2 · đặt sai {Metrics.wrongConfirm}\n");
            text.Append($"Độ trễ ra chiêu p50/p95: {Metrics.firstValidCast.Quantile(.5)}/{Metrics.firstValidCast.Quantile(.95)} ms · VFX: {Metrics.triggerVfx.Quantile(.95)} ms\n");
            text.Append($"Frame p50/p95: {Metrics.frames.Quantile(.5)}/{Metrics.frames.Quantile(.95)} ms · tải hoạt động {Metrics.active:0} s / 600 s\n");
            text.Append("Tỉ lệ đúng theo chiêu: ");for(int g=0;g<5;g++){int n=0,c=0;foreach(var row in Metrics.cells)if(row.g==g){n+=row.n;c+=row.correctUnique;}text.Append(n==0?"— ":$"{c*100/n}% ");}
            text.Append("\nLòng / mu: ");for(int o=0;o<2;o++){int n=0,c=0;foreach(var row in Metrics.cells)if(row.o==o){n+=row.n;c+=row.correctUnique;}text.Append(n==0?"— ":$"{c*100/n}% ");}
            text.Append(total==120&&Metrics.active>=600?"\nĐã đủ lượt; xem từng ô và lỗi trước khi nghiệm thu.":"\nCHƯA ĐỦ PHIÊN · giữ nguyên kết quả thiếu.");summary.text=text.ToString();
        }
        static int ArraySum(int[] items){int n=0;foreach(int v in items)n+=v;return n;}
        // Editor smoke visits a few slots only; export remains marked incomplete.
        public void PreviewPhase(Phase phase,int trial=0){FinishTrial();if(!ExportConsent){ExportConsent=true;Begin();}SetPhase(phase);if(phase==Phase.Trials){block=trial/20;StartTrial(trial,Now);}if(phase==Phase.Results)RefreshSummary();}
        void OnDestroy(){StopSession();if(bridge!=null)bridge.Result-=Frame;if(caster!=null){caster.Outcome-=Outcome;caster.FirstVfx-=Vfx;}if(placement!=null)placement.Anchored-=Placed;Metrics=null;LastJson=null;}
    }
}
#endif
