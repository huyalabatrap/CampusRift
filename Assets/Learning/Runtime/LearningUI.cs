using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.UI;
using CampusRift.Progression;
namespace CampusRift.Learning
{
    public sealed partial class LearningUI : MonoBehaviour
    {
        public TMP_Text heading, summary, status, textTemplate;
        public RiftButton buttonTemplate, back, close;
        public RectTransform content;
        public ScrollRect scroll;
        public Image imageTemplate;
        LearningEngine engine;
        CourseData course;
        LessonData lesson;
        QuizSession quiz;
        QuizResult result;
        StudyAward award, readAward;
        // Exam / spaced review / quick practice share the question screens of the lesson quiz.
        QuizKind mode = QuizKind.Lesson;
        BreakthroughExam exam;
        ExamOutcome examOutcome;
        float practiceEndsAt, nextTick;
        // Ordering questions: the arrangement the player is building and the item picked for swapping (-1 = none).
        readonly List<string> order = new List<string>();
        int pickedIndex = -1, orderQuestion = -1;
        static bool Vn=>LevelHUD.Vietnamese;
        static string L(string en,string vn)=>Vn?vn:en;
        int page;
        Action previous;
        Coroutine sequence;
        GameObject glow;
        bool skipEffect;
        // The Hub sets this to open the panel on a given screen ("Shop", "Realm"); anything else shows the course list.
        public static string PendingScreen;
        public string Screen {get;private set;}
        public QuizKind Mode=>mode;
        public bool ExamRunning=>exam!=null && !exam.Committed;
        bool decorated;
        // One-time styling of the study panel: art backdrop and a gold heading, so it matches the Hub pages.
        void Decorate()
        {
            if(decorated)return;decorated=true;
            ComicTheme.Text(heading,true); ComicTheme.Text(textTemplate); ComicTheme.Button(buttonTemplate); ComicTheme.Button(back); ComicTheme.Button(close);
            var art=ContentImages.Get("Scenes","hub-background");
            if(art!=null)
            {
                var go=new GameObject("Backdrop",typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);
                var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
                var img=go.GetComponent<Image>();img.sprite=art;img.color=new Color(.5f,.5f,.58f,1);img.raycastTarget=false;go.transform.SetAsFirstSibling();
            }
        }
        void OnEnable()
        {
            Decorate();transform.SetAsLastSibling();
            back.onClick.AddListener(GoBack);close.onClick.AddListener(Close);
            OpenPendingScreen();
        }
        // Also called when the Course panel reopens during its closing fade (OnEnable does not fire then).
        public void OpenPendingScreen()
        {
            engine=LearningService.Instance?.Engine;
            if(engine==null)return;
            close.Label.text=UIStateManager.Instance.ReturnState==UIState.Paused?L("RETURN TO PAUSE","VỀ TẠM DỪNG"):
                UIStateManager.Instance.ReturnState==UIState.Menu?L("RETURN TO MENU","VỀ MENU"):UIStateManager.Instance.ReturnState==UIState.Hub?L("RETURN TO HUB","VỀ HUB"):L("RETURN TO GAME","VỀ MÀN CHƠI");
            mode=QuizKind.Lesson;exam=null;practiceEndsAt=0;
            var pending=PendingScreen;PendingScreen=null;
            if(pending!=null && pending.StartsWith("Lessons:") && int.TryParse(pending.Substring(8),out var lessonsOf) && lessonsOf>=0 && lessonsOf<engine.Catalog.courses.Count)ShowLessons(engine.Catalog.courses[lessonsOf]);
            else if(pending!=null && pending.StartsWith("Exam:") && int.TryParse(pending.Substring(5),out var examOf) && examOf>=0 && examOf<engine.Catalog.courses.Count)ShowExamIntro(engine.Catalog.courses[examOf]);
            else if(pending=="Cards")ShowCards();
            else if(pending=="Notebook")ShowNotebook();
            else if(pending=="Daily")ShowDaily();
            else if(pending=="Shrine")OpenShrineQuestion();
            else if(pending=="Reviews")ShowReviews();
            else if(pending=="Practice")StartPractice();
            else if(pending=="Shop" && ProfileService.Instance!=null)ShowShop();
            else if(pending=="Realm")ShowRealm();
            else ShowCourses();
        }
        void OnDisable()
        {
            back.onClick.RemoveListener(GoBack);close.onClick.RemoveListener(Close);
            CancelShrine();
            // Leaving the panel in the middle of an exam is a failed attempt.
            if(engine!=null && ExamRunning)engine.AbandonExam(exam);
            exam=null;practiceEndsAt=0;
            if(sequence!=null)StopCoroutine(sequence);sequence=null;
            if(glow!=null)Destroy(glow);
        }
        public void Close(){CancelShrine();if(ExamRunning){engine.AbandonExam(exam);exam=null;}UIStateManager.Instance.Back();}
        void GoBack(){previous?.Invoke();}
        void Clear(string screen,string title,string subtitle,Action parent)
        {
            Screen=screen;heading.text=title;summary.text=subtitle;previous=parent;back.gameObject.SetActive(parent!=null);
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            scroll.verticalNormalizedPosition=1;
            status.text=string.IsNullOrEmpty(engine.SaveError)?"SCROLL / SWIPE TO EXPLORE  /  PROGRESS SAVED":"SAVE PENDING: "+engine.SaveError;
        }
        TMP_Text Text(string value,Color? color=null)
        {
            var t=Instantiate(textTemplate,content);t.gameObject.SetActive(true);t.text=value;ComicTheme.Text(t);
            t.fontStyle=FontStyles.Normal;t.textWrappingMode=TextWrappingModes.Normal;
            if(color.HasValue)t.color=color.Value;
            Localization.LocalizationService.Bind(t);
            var layout=t.GetComponent<LayoutElement>();layout.preferredHeight=t.GetPreferredValues(t.text,content.rect.width-24,0).y+24;
            return t;
        }
        RiftButton Button(string label,Action action,bool enabled=true,float height=0,bool prose=false)
        {
            var b=Instantiate(buttonTemplate,content);b.gameObject.SetActive(true);b.Label.text=label;b.interactable=enabled;ComicTheme.Button(b);
            if(prose)
            {
                b.Label.fontStyle=FontStyles.Normal;b.Label.fontSize=24;b.Label.fontSizeMax=24;b.Label.fontSizeMin=18;
                b.Label.textWrappingMode=TextWrappingModes.Normal;b.Label.fontSharedMaterial=ComicTheme.Font.material;
                float width=Mathf.Max(200,content.rect.width-112);
                height=Mathf.Max(84,b.Label.GetPreferredValues(label,width,0).y+34);
                b.Label.alignment=TextAlignmentOptions.MidlineLeft;
            }
            Localization.LocalizationService.Bind(b.Label);b.onClick.AddListener(()=>action());
            if(height>0){var layout=b.GetComponent<LayoutElement>();if(layout!=null)layout.preferredHeight=Mathf.Max(ComicTheme.MinimumButtonHeight,height);}
            return b;
        }
        // Small gold diamonds on the corners of a button, like the Hub panels.
        static void Corners(Transform button)
        {
            foreach(var corner in new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)})
            {
                var go=new GameObject("Corner",typeof(RectTransform),typeof(Image));go.transform.SetParent(button,false);
                var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=corner;r.sizeDelta=new Vector2(9,9);r.anchoredPosition=Vector2.zero;r.localRotation=Quaternion.Euler(0,0,45);
                var img=go.GetComponent<Image>();img.color=UiKit.GoldLo;img.raycastTarget=false;
            }
        }
        static string Clock(float seconds){int s=Mathf.CeilToInt(Mathf.Max(0,seconds));return (s/60).ToString("00")+":"+(s%60).ToString("00");}

        // ---------------------------------------------------------------- timers (exam and practice run on unscaled time)
        void Update()
        {
            if(engine==null || Time.unscaledTime<nextTick)return;
            nextTick=Time.unscaledTime+0.25f;
            if(ExamRunning)
            {
                if(exam.Expired){FinishExam();return;}
                if(Screen=="Quiz")summary.text=TimerLine(exam.Remaining);
            }
            else if(mode==QuizKind.Practice && practiceEndsAt>0 && quiz!=null && !quiz.Committed)
            {
                float left=practiceEndsAt-Time.unscaledTime;
                if(left<=0){quiz.FinishUnanswered();FinishPractice();return;}
                if(Screen=="Quiz")summary.text=L("TIME LEFT  ","CÒN LẠI  ")+Clock(left);
            }
        }
        string TimerLine(float seconds)=>L("TIME LEFT  ","CÒN LẠI  ")+Clock(seconds)+"  /  "+L("PASS AT ","ĐẠT TỪ ")+exam.Course.examPassPercent+"%";

        // ---------------------------------------------------------------- courses
        public void ShowCourses()
        {
            // Opened from the Hub, the course list is the Hub's own Library page: leaving here means going home.
            if(UIStateManager.Instance!=null && UIStateManager.Instance.ReturnState==UIState.Hub){Close();return;}
            Clear("Courses","COURSES","Learn. Prove your knowledge. Return stronger.",null);
            mode=QuizKind.Lesson;
            var c0=engine.Cultivation;
            if(c0!=null)
            {
                var info=CultivationUI.Describe(c0,Vn);
                Button($"{L("REALM","CẢNH GIỚI")}  /  {info.title}  /  {info.bar}",ShowRealm);
            }
            Button(L("FLASHCARDS","THẺ GHI NHỚ"),ShowCards);
            Button(L("MISTAKE NOTEBOOK","SỔ TAY CÂU SAI"),ShowNotebook);
            int due=engine.DueReviewCount;
            Button($"{L("SCHEDULED REVIEW","ÔN TẬP THEO LỊCH")}  /  {due} {L("due","đến hạn")}",ShowReviews,due>0);
            int pool=engine.PracticePool().Count;
            var wallet=ProfileService.Instance!=null?ProfileService.Instance.Wallet:null;
            if(wallet!=null)Button($"{L("ALCHEMY PAVILION","ĐAN CÁC")}  /  {wallet.Balance} Linh Thạch",ShowShop);
            Button($"{L("QUICK PRACTICE","LUYỆN TẬP NHANH")}  /  {L("10 questions, 5 minutes","10 câu, 5 phút")}  /  {engine.PracticeTuViLeft:0} Tu Vi {L("left today","còn hôm nay")}",StartPractice,pool>0);
            foreach(var c in engine.Catalog.courses)
            {
                int done=c.lessons.Count(l=>engine.Progress.Lesson(l.id).rewarded);
                string prefix=c.chapterIndex>0?L("CHAPTER ","CHƯƠNG ")+c.chapterIndex+": ":"";
                Button($"{prefix}{c.title}  /  {done}/{c.lessons.Count} mastered",()=>ShowLessons(c),engine.CourseAvailable(c));
            }
        }
        // Realm screen (P06-T07): tier dots, Tu Vi bar, stats, what opens next and the breakthrough exam entry.
        public void ShowRealm()
        {
            var cultivation=engine.Cultivation;if(cultivation==null){ShowCourses();return;}
            var info=CultivationUI.Describe(cultivation,Vn);
            Clear("Realm",L("REALM","CẢNH GIỚI"),info.title,ShowCourses);
            Picture(ContentImages.Realm(cultivation.RealmIndex),190);
            Text(info.title+"\n"+info.tiers,new Color(1,.8f,.4f));
            Text(info.bar);
            CultivationUI.CreateBar(content,info.fraction,new Color(.12f,.13f,.2f,1),info.bottleneck?new Color(1,.8f,.4f):new Color(.45f,.35f,.95f));
            Text(info.stats);
            Text(L("NEXT UNLOCKS","MỞ KHÓA TIẾP THEO")+"\n"+info.nextUnlock);
            Text(info.breakthroughHint);
            var chapter=engine.CurrentExamCourse;
            bool can=chapter!=null && engine.CanTakeExam(chapter,out _);
            Button(L("BREAKTHROUGH EXAM","THI ĐỘT PHÁ"),()=>ShowExamIntro(chapter),can);
        }
        public void ShowLessons(CourseData selected)
        {
            course=selected;mode=QuizKind.Lesson;Clear("Lessons",course.title,course.description,ShowCourses);
            Picture(ContentImages.Realm(engine.ChapterRealm(course)),150);
            Text(L("Studying pauses the hunt. Every lesson adds Tu Vi (cultivation).","Học bài làm thế giới tạm dừng. Mỗi bài học đều cộng Tu Vi."));
            foreach(var l in course.lessons)
            {
                var progress=engine.Progress.Lesson(l.id);bool available=engine.Available(course,l);
                string state=progress.rewarded?"MASTERED":progress.completed?"QUIZ READY":"READ";
                if(progress.rewarded)state+="  /  "+ReviewScheduler.BadgeName(progress.mastery,Vn);
                if(!available)state="LOCKED / pass prerequisites";
                Button(l.title+"  /  "+state,()=>OpenLesson(l),available);
            }
            if(course.chapterIndex>0 && engine.Cultivation!=null)
            {
                bool can=engine.CanTakeExam(course,out var reason);
                Button(L("BREAKTHROUGH EXAM","THI ĐỘT PHÁ")+(can?"":"  /  "+ExamReason(reason)),()=>ShowExamIntro(course),can);
            }
            Text(L("Reading and the first quiz of a lesson pay Tu Vi once. Retakes improve knowledge without farming power.","Đọc hết bài và làm quiz lần đầu mới được Tu Vi. Làm lại chỉ giúp nhớ bài, không cày sức mạnh."));
        }
        string ExamReason(string reason)
        {
            if(!Vn)return reason;
            if(reason.StartsWith("Retry in"))return "Thi lại sau "+reason.Substring(9);
            if(reason.StartsWith("Fill tier 5"))return "Hãy đầy Tu Vi tầng 5";
            if(reason.StartsWith("This exam"))return "Thuộc cảnh giới khác";
            return "Chưa đủ câu hỏi";
        }
        public void OpenLesson(LessonData selected)
        {
            lesson=selected;award=null;readAward=null;mode=QuizKind.Lesson;page=Mathf.Clamp(engine.Progress.Lesson(lesson.id).pagesRead,0,lesson.pages.Count-1);ShowPage();
        }
        public void ShowPage()
        {
            var data=lesson.pages[page];var progress=engine.Progress.Lesson(lesson.id);
            Clear("Lesson",lesson.title,$"PAGE {page+1}/{lesson.pages.Count}  /  READ {progress.pagesRead}/{lesson.pages.Count}",()=>ShowLessons(course));
            Text(data.title);Text(data.content);
            if(data.illustration!=null){var img=Instantiate(imageTemplate,content);img.gameObject.SetActive(true);img.sprite=data.illustration;img.preserveAspect=true;}
            if(!string.IsNullOrEmpty(data.example))Text("EXAMPLE\n"+data.example);
            if(!string.IsNullOrEmpty(data.takeaway))Text("REMEMBER\n"+data.takeaway,new Color(1,.8f,.4f));
            if(page>0)Button("PREVIOUS PAGE",()=>{page--;ShowPage();});
            Button(page<lesson.pages.Count-1?"READ & NEXT PAGE":"COMPLETE READING",()=>
            {
                var gained=engine.ReadPage(course,lesson,page);
                if(gained!=null)readAward=gained;
                if(page<lesson.pages.Count-1){page++;ShowPage();}else ShowReady();
            });
            if(engine.LessonCompleted(lesson))Button("START QUIZ",StartQuiz);
        }
        void ShowReady()
        {
            Clear("Ready","LESSON COMPLETE",lesson.title,ShowPage);
            if(readAward!=null)Text(TuViLine(readAward),new Color(1,.8f,.4f));
            Text($"Draw {lesson.quizSize} random questions. Pass at {lesson.passPercent}%.\nMistakes have explanations. Failing never costs health, items or levels.");
            Button("START QUIZ",StartQuiz);
            if(engine.Progress.Lesson(lesson.id).lastResult!=null)Button("REVIEW LAST ATTEMPT",()=>{result=engine.Progress.Lesson(lesson.id).lastResult;ShowReview();});
        }
        public void StartQuiz()
        {
            try{mode=QuizKind.Lesson;quiz=engine.StartQuiz(course,lesson);award=null;orderQuestion=-1;ShowQuestion();}
            catch(Exception e){status.text=e.Message;}
        }

        // ---------------------------------------------------------------- questions (all types, all modes)
        public void ShowQuestion()
        {
            var q=quiz.Questions[quiz.Answered];
            string title,subtitle;Action parent;
            switch(mode)
            {
                case QuizKind.Notebook: title=L("MISTAKE NOTEBOOK","SỔ TAY CÂU SAI")+"  /  "+(quiz.Answered+1)+"/"+quiz.Questions.Count;subtitle=L("Review at your own pace","Ôn theo nhịp của bạn");parent=ShowNotebook;break;
                case QuizKind.Shrine: title=L("FORTUNE STELE","LINH BIA CƠ DUYÊN");subtitle=L("One question from a lesson you have read","Một câu từ bài đã đọc");parent=null;break;
                case QuizKind.Exam: title=$"{L("BREAKTHROUGH EXAM","THI ĐỘT PHÁ")}  /  {quiz.Answered+1}/{quiz.Questions.Count}";subtitle=TimerLine(exam.Remaining);parent=AskAbandon;break;
                case QuizKind.Review: title=$"{L("REVIEW","ÔN TẬP")}  /  {quiz.Answered+1}/{quiz.Questions.Count}";subtitle=quiz.Lesson.title;parent=ShowReviews;break;
                case QuizKind.Practice: title=$"{L("QUICK PRACTICE","LUYỆN TẬP NHANH")}  /  {quiz.Answered+1}/{quiz.Questions.Count}";subtitle=L("TIME LEFT  ","CÒN LẠI  ")+Clock(practiceEndsAt-Time.unscaledTime);parent=ShowCourses;break;
                default: title=$"KNOWLEDGE TRIAL  /  {quiz.Answered+1}/{quiz.Questions.Count}";subtitle=$"PASS AT {lesson.passPercent}%  /  {lesson.title}";parent=ShowReady;break;
            }
            Clear("Quiz",title,subtitle,parent);
            Text(q.Data.prompt);
            switch(q.Data.type)
            {
                case "multi-choice": case "matching": case "fill-blank": DrawExtraQuestion(q);break;
                case "true-false":
                    foreach(var option in q.Options){var o=option;Button(o.text,()=>Answer(o.id),true,96,true);}
                    break;
                case "ordering":
                    if(orderQuestion!=quiz.Answered){orderQuestion=quiz.Answered;order.Clear();order.AddRange(q.Options.Select(o=>o.id));pickedIndex=-1;}
                    DrawOrdering(q);
                    break;
                default:
                    foreach(var option in q.Options){var o=option;Button(o.text,()=>Answer(o.id),true,0,true);}
                    break;
            }
        }
        // Ordering: tap one item, then another, to swap them (works the same on PC and phones); then confirm.
        void DrawOrdering(QuizQuestion q)
        {
            Text(L("Tap two items to swap them, then confirm the order.","Chạm hai mục để đổi chỗ, rồi xác nhận thứ tự."),new Color(1,.8f,.4f));
            for(int i=0;i<order.Count;i++)
            {
                int index=i;var option=q.Data.options.Find(o=>o.id==order[i]);
                string mark=pickedIndex==i?">> ":"";
                Button($"{mark}{i+1}.  {option.text}",()=>
                {
                    if(pickedIndex<0)pickedIndex=index;
                    else if(pickedIndex==index)pickedIndex=-1;
                    else{var t=order[pickedIndex];order[pickedIndex]=order[index];order[index]=t;pickedIndex=-1;}
                    float keep=scroll.verticalNormalizedPosition;RedrawQuestionKeepingScroll(keep);
                },true,0,true);
            }
            Button(L("CONFIRM ORDER","XÁC NHẬN THỨ TỰ"),()=>Answer(order.ToArray()),true,80);
        }
        void RedrawQuestionKeepingScroll(float keep){var save=orderQuestion;ShowQuestion();orderQuestion=save;Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=keep;}
        public void Answer(string optionId)=>Answer(new[]{optionId});
        public void Answer(IReadOnlyList<string> selected)
        {
            var answer=quiz.Answer(selected);
            // An exam shows no explanations until it is over; the other modes explain every answer at once.
            if(mode==QuizKind.Exam){NextQuestion();return;}
            Clear("Explanation",answer.correct?"CORRECT":"LET'S REVIEW",$"QUESTION {quiz.Answered}/{quiz.Questions.Count}",null);
            Text(answer.prompt);Text("YOUR ANSWER  /  "+answer.selected);
            if(!answer.correct)Text("CORRECT ANSWER  /  "+answer.expected,new Color(1,.8f,.4f));
            Text(answer.explanation);
            if(!string.IsNullOrEmpty(answer.source))Text(L("SOURCE  /  ","NGUỒN  /  ")+answer.source);
            Button(quiz.Complete?"VIEW RESULT":"NEXT QUESTION",NextQuestion);
        }
        public void NextQuestion()
        {
            if(!quiz.Complete){ShowQuestion();return;}
            switch(mode)
            {
                case QuizKind.Notebook: case QuizKind.Shrine: ExtraResult();return;
                case QuizKind.Exam: FinishExam();return;
                case QuizKind.Review: result=quiz.Result();award=engine.SubmitReview(quiz);ShowReviewResult();return;
                case QuizKind.Practice: FinishPractice();return;
                default: result=quiz.Result();award=engine.Submit(quiz);ShowResult();return;
            }
        }

        // ---------------------------------------------------------------- lesson quiz result
        public void ShowResult()
        {
            Clear("Result",result.passed?"QUIZ PASSED":"KEEP LEARNING",lesson.title,()=>ShowLessons(course));
            Text($"{result.correct} CORRECT  /  {result.total-result.correct} INCORRECT  /  {result.Percent:0.#}%\n{(result.passed?"PASS":"FAIL")}  /  REQUIRED {lesson.passPercent}%");
            Text(result.passed?(award!=null?"Knowledge mastered. Your power has grown.":"Already mastered. Your best score is saved; no duplicate reward."):"No power lost. Review, retry, or return to the hunt whenever you wish.");
            AddLinhLines();
            if(award!=null)Button(L("CULTIVATION GAINED","TU VI NHẬN ĐƯỢC"),ShowBreakthrough);
            Button("REVIEW ANSWERS",ShowReview);Button("RETRY QUIZ",StartQuiz);Button("REVIEW LESSON",()=>{page=0;ShowPage();});
        }
        public void ShowReview()
        {
            Clear("Review","TRIAL REVIEW",lesson.title,ShowResult);
            AnswerLines(result.answers,false);
            Button("RETRY QUIZ",StartQuiz);Button("REVIEW LESSON",()=>{page=0;ShowPage();});
        }
        void AnswerLines(List<AnswerRecord> answers,bool onlyWrong)
        {
            foreach(var a in answers)
            {
                if(onlyWrong && a.correct)continue;
                string yours=string.IsNullOrEmpty(a.selected)?L("(no answer)","(chưa trả lời)"):a.selected;
                string source=string.IsNullOrEmpty(a.source)?"":"\n"+L("Source: ","Nguồn: ")+a.source;
                Text($"{(a.correct?"CORRECT":"INCORRECT")} / {a.prompt}\nYou: {yours}\nAnswer: {a.expected}\n{a.explanation}{source}");
            }
        }
        public void ShowBreakthrough()
        {
            Clear("Breakthrough",L("TU VI GAINED","TU VI TĂNG"),L("KNOWLEDGE IS POWER","TRI THỨC LÀ SỨC MẠNH"),null);
            if(sequence!=null)StopCoroutine(sequence);sequence=StartCoroutine(Present());
        }
        string TuViLine(StudyAward a)
        {
            string line=Vn?$"+{a.tuVi:0.#} Tu Vi":$"+{a.tuVi:0.#} cultivation";
            if(a.realmAfter!=a.realmBefore||a.tierAfter!=a.tierBefore)
                line+="\n"+(Vn?"Lên ":"Reached ")+engine.Cultivation.Name(Vn);
            else if(a.Bottleneck)line+="\n"+(Vn?"Bình cảnh - hãy Thi Đột Phá.":"Bottleneck - take the Breakthrough Exam.");
            return line;
        }
        IEnumerator Present()
        {
            Text(TuViLine(award),new Color(1,.8f,.4f));
            yield return new WaitForSecondsRealtime(.45f);
            if(award.TierUp)heading.text=L("REALM UP","LÊN TẦNG");
            for(float t=0;t<.45f;t+=Time.unscaledDeltaTime)
            {heading.transform.localScale=Vector3.one*(1+.08f*Mathf.Sin(t/.45f*Mathf.PI));yield return null;}
            heading.transform.localScale=Vector3.one;
            var info=CultivationUI.Describe(engine.Cultivation,Vn);
            Text(info.title+"  /  "+info.bar);
            CultivationUI.CreateBar(content,info.fraction,new Color(.12f,.13f,.2f,1),new Color(.45f,.35f,.95f));
            Text(info.stats);
            Button(L("CONTINUE LEARNING","TIẾP TỤC HỌC"),()=>{if(mode==QuizKind.Lesson && course!=null)ShowLessons(course);else ShowCourses();});Canvas.ForceUpdateCanvases();sequence=null;
        }

        // ---------------------------------------------------------------- spaced review
        public void ShowReviews()
        {
            mode=QuizKind.Review;Clear("Reviews",L("SCHEDULED REVIEW","ÔN TẬP THEO LỊCH"),L("5 questions, pass at 80%","5 câu, đạt từ 80%"),ShowCourses);
            var due=engine.DueReviews();
            if(due.Count==0)Text(L("Nothing is due. Master a lesson to schedule its first review after one day.","Chưa có bài đến hạn. Qua một bài để hẹn lần ôn đầu sau một ngày."));
            foreach(var pair in due)
            {
                var c=pair.Key;var l=pair.Value;var p=engine.Progress.Lesson(l.id);
                Button($"{l.title}  /  {L("badge","huy hiệu")}: {ReviewScheduler.BadgeName(p.mastery,Vn)}  ->  {ReviewScheduler.BadgeName(p.mastery+1,Vn)}",()=>StartReview(c,l));
            }
            Text(L("Bronze after the first review (next in 3 days), Silver after the second (next in 7 days), Gold after the third. A failed review keeps your badge and returns after one day.","Đồng sau lần ôn đầu (lần sau 3 ngày), Bạc sau lần hai (lần sau 7 ngày), Vàng sau lần ba. Ôn trượt vẫn giữ huy hiệu và quay lại sau một ngày."));
        }
        public void StartReview(CourseData c,LessonData l)
        {
            try{course=c;lesson=l;mode=QuizKind.Review;quiz=engine.StartReview(c,l);award=null;orderQuestion=-1;ShowQuestion();}
            catch(Exception e){status.text=e.Message;}
        }
        void ShowReviewResult()
        {
            var p=engine.Progress.Lesson(quiz.Lesson.id);
            Clear("ReviewResult",result.passed?L("REVIEW PASSED","ÔN TẬP ĐẠT"):L("REVIEW NOT PASSED","ÔN TẬP CHƯA ĐẠT"),quiz.Lesson.title,ShowReviews);
            Text($"{result.correct}/{result.total}  /  {result.Percent:0.#}%");
            if(result.passed)Picture(ContentImages.Mastery(p.mastery),140);
            Text(result.passed?L("Badge: ","Huy hiệu: ")+ReviewScheduler.BadgeName(p.mastery,Vn):L("Your badge is kept. Try again after one day.","Huy hiệu vẫn giữ. Thử lại sau một ngày."),new Color(1,.8f,.4f));
            if(award!=null)Text(TuViLine(award),new Color(1,.8f,.4f));
            AddLinhLines();
            Button("REVIEW ANSWERS",()=>{Clear("Review","TRIAL REVIEW",quiz.Lesson.title,ShowReviewResult);AnswerLines(result.answers,false);});
            Button(L("BACK TO REVIEWS","VỀ DANH SÁCH ÔN"),ShowReviews);
        }

        // ---------------------------------------------------------------- quick practice
        public void StartPractice()
        {
            try
            {
                mode=QuizKind.Practice;quiz=engine.StartPractice();
                if(quiz==null){status.text=L("No question is available right now.","Hiện chưa có câu hỏi phù hợp.");return;}
                award=null;orderQuestion=-1;practiceEndsAt=Time.unscaledTime+LearningEngine.PracticeMinutes*60f*Accessibility.ReadingTime(QuizKindProxy.Study);ShowQuestion();
            }
            catch(Exception e){status.text=e.Message;}
        }
        void FinishPractice()
        {
            practiceEndsAt=0;result=quiz.Result();award=engine.SubmitPractice(quiz);
            Clear("PracticeResult",L("PRACTICE RESULT","KẾT QUẢ LUYỆN TẬP"),L("QUICK PRACTICE","LUYỆN TẬP NHANH"),ShowCourses);
            Text($"{result.correct}/{result.total}  /  {result.Percent:0.#}%");
            Text(award!=null?TuViLine(award):L("No Tu Vi this time (no correct answers or today's limit of 150 is reached).","Lần này không có Tu Vi (không đúng câu nào hoặc đã đạt trần 150 hôm nay)."),new Color(1,.8f,.4f));
            AddLinhLines();
            Text(L("Only questions not answered correctly in the last 24 hours are used.","Chỉ dùng câu chưa trả lời đúng trong 24 giờ qua."));
            Button("REVIEW ANSWERS",()=>{Clear("Review","TRIAL REVIEW",L("QUICK PRACTICE","LUYỆN TẬP NHANH"),()=>FinishPracticeView());AnswerLines(result.answers,false);});
            Button(L("PRACTICE AGAIN","LUYỆN TẬP TIẾP"),StartPractice,engine.PracticePool().Count>0);
        }
        void FinishPracticeView(){FinishPracticeShown();}
        void FinishPracticeShown()
        {
            Clear("PracticeResult",L("PRACTICE RESULT","KẾT QUẢ LUYỆN TẬP"),L("QUICK PRACTICE","LUYỆN TẬP NHANH"),ShowCourses);
            Text($"{result.correct}/{result.total}  /  {result.Percent:0.#}%");
            Button("REVIEW ANSWERS",()=>{Clear("Review","TRIAL REVIEW",L("QUICK PRACTICE","LUYỆN TẬP NHANH"),FinishPracticeShown);AnswerLines(result.answers,false);});
            Button(L("PRACTICE AGAIN","LUYỆN TẬP TIẾP"),StartPractice,engine.PracticePool().Count>0);
        }

        // ---------------------------------------------------------------- breakthrough exam
        public void ShowExamIntro(CourseData chapter)
        {
            course=chapter;mode=QuizKind.Exam;
            Clear("ExamIntro",L("BREAKTHROUGH EXAM","THI ĐỘT PHÁ"),chapter.title,()=>ShowLessons(chapter));
            Text(L($"{chapter.examSize} questions in {chapter.examMinutes} minutes. Pass at {chapter.examPassPercent}%.\nPassing carries you to the next realm and pays {BreakthroughExam.RewardLinhThach} Linh Thạch.",
                   $"{chapter.examSize} câu trong {chapter.examMinutes} phút. Đạt từ {chapter.examPassPercent}%.\nĐạt thì lên cảnh giới kế tiếp và nhận {BreakthroughExam.RewardLinhThach} Linh Thạch."));
            Text(L($"If you fail you may retry after {BreakthroughExam.RetryMinutes} minutes and review the mistakes meanwhile. Leaving the exam counts as a failed attempt. Nothing is lost.",
                   $"Nếu trượt, bạn thi lại sau {BreakthroughExam.RetryMinutes} phút và có thể xem lại câu sai trong lúc chờ. Thoát giữa chừng tính là trượt. Không mất gì cả."));
            bool can=engine.CanTakeExam(chapter,out var reason);
            if(!can)Text(ExamReason(reason),new Color(1,.8f,.4f));
            Button(L("START EXAM","BẮT ĐẦU THI"),StartExam,can);
        }
        void StartExam()
        {
            try
            {
                mode=QuizKind.Exam;exam=engine.StartExam(course,()=>Time.unscaledTime);quiz=exam.Session;orderQuestion=-1;ShowQuestion();
            }
            catch(Exception e){status.text=e.Message;}
        }
        void AskAbandon()
        {
            var running=exam;
            Clear("ExamLeave",L("LEAVE THE EXAM?","THOÁT BÀI THI?"),TimerLine(running.Remaining),null);
            Text(L("Leaving counts as a failed attempt and you must wait 30 minutes to retry.","Thoát giữa chừng tính là trượt và phải chờ 30 phút để thi lại."));
            Button(L("CONTINUE THE EXAM","TIẾP TỤC THI"),ShowQuestion);
            Button(L("LEAVE (FAIL)","BỎ THI (TRƯỢT)"),()=>{engine.AbandonExam(exam);exam=null;ShowLessons(course);});
        }
        void FinishExam()
        {
            if(!ExamRunning)return;
            examOutcome=engine.SubmitExam(exam);result=examOutcome.result;
            if(examOutcome.passed){ShowExamPassed();}
            else ShowExamFailed();
        }
        void ShowExamPassed()
        {
            Clear("ExamPassed",L("BREAKTHROUGH!","ĐỘT PHÁ THÀNH CÔNG!"),course.title,null);
            if(sequence!=null)StopCoroutine(sequence);sequence=StartCoroutine(PassedSequence());
        }
        IEnumerator PassedSequence()
        {
            skipEffect=false;
            var skip=Button(L("SKIP","BỎ QUA"),()=>skipEffect=true);
            BuildGlow();
            float t=0;
            while(t<3f && !skipEffect){t+=Time.unscaledDeltaTime;AnimateGlow(t);yield return null;}
            if(glow!=null)Destroy(glow);glow=null;
            if(skip!=null)Destroy(skip.gameObject);
            Text($"{result.correct}/{result.total}  /  {result.Percent:0.#}%",new Color(1,.8f,.4f));
            Text(L("You reached ","Bạn đã lên ")+engine.Cultivation.Name(Vn)+"\n"+L("Reward: ","Thưởng: ")+examOutcome.linhThach+" Linh Thạch");
            var info=CultivationUI.Describe(engine.Cultivation,Vn);
            Text(info.stats);
            Text(info.nextUnlock);
            Button("REVIEW ANSWERS",()=>{Clear("Review","TRIAL REVIEW",course.title,ShowExamPassed2);AnswerLines(result.answers,false);});
            Button(L("CONTINUE","TIẾP TỤC"),ShowCourses);
            Canvas.ForceUpdateCanvases();sequence=null;
        }
        void ShowExamPassed2()
        {
            Clear("ExamPassed",L("BREAKTHROUGH!","ĐỘT PHÁ THÀNH CÔNG!"),course.title,null);
            Text($"{result.correct}/{result.total}  /  {result.Percent:0.#}%",new Color(1,.8f,.4f));
            Text(L("You reached ","Bạn đã lên ")+engine.Cultivation.Name(Vn));
            Button("REVIEW ANSWERS",()=>{Clear("Review","TRIAL REVIEW",course.title,ShowExamPassed2);AnswerLines(result.answers,false);});
            Button(L("CONTINUE","TIẾP TỤC"),ShowCourses);
        }
        void ShowExamFailed()
        {
            Clear("ExamFailed",L("NOT PASSED YET","CHƯA ĐẠT"),course.title,ShowCourses);
            Text($"{result.correct}/{result.total}  /  {result.Percent:0.#}%  /  {L("REQUIRED","CẦN")} {course.examPassPercent}%"+(examOutcome.timedOut?"\n"+L("Time ran out.","Hết giờ."):""));
            Text(L($"Retry after {BreakthroughExam.RetryMinutes} minutes. Review the mistakes below meanwhile. Nothing was lost.",$"Thi lại sau {BreakthroughExam.RetryMinutes} phút. Hãy xem lại các câu sai bên dưới. Bạn không mất gì."),new Color(1,.8f,.4f));
            AnswerLines(result.answers,true);
            Button(L("BACK TO THE CHAPTER","VỀ CHƯƠNG"),()=>ShowLessons(course));
        }
        // Gold frame and lightning around the card while the breakthrough plays (3 seconds, skippable).
        readonly List<Image> bolts=new List<Image>();readonly List<Image> frame=new List<Image>();
        void BuildGlow()
        {
            var card=heading.transform.parent as RectTransform;if(card==null)return;
            if(glow!=null)Destroy(glow);
            glow=new GameObject("Breakthrough Glow",typeof(RectTransform));glow.layer=5;
            var rect=glow.GetComponent<RectTransform>();rect.SetParent(card,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            bolts.Clear();frame.Clear();
            var gold=new Color(1f,.82f,.32f,0f);
            Image Bar(string name,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.layer=5;var r=go.GetComponent<RectTransform>();r.SetParent(rect,false);
                r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;var im=go.GetComponent<Image>();im.color=gold;im.raycastTarget=false;return im;
            }
            frame.Add(Bar("Top",new Vector2(0,1),new Vector2(1,1),new Vector2(0,-8),Vector2.zero));
            frame.Add(Bar("Bottom",Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,8)));
            frame.Add(Bar("Left",Vector2.zero,new Vector2(0,1),Vector2.zero,new Vector2(8,0)));
            frame.Add(Bar("Right",new Vector2(1,0),Vector2.one,new Vector2(-8,0),Vector2.zero));
            for(int i=0;i<7;i++)
            {
                float x=(i+0.5f)/7f;var b=Bar("Bolt "+i,new Vector2(x,0),new Vector2(x,1),new Vector2(-3,0),new Vector2(3,0));b.rectTransform.localRotation=Quaternion.Euler(0,0,UnityEngine.Random.Range(-6f,6f));bolts.Add(b);
            }
        }
        void AnimateGlow(float t)
        {
            bool reduced=SettingsManager.Instance?.Current.ReduceSkillFlashes??false;
            float pulse=reduced?.30f:0.55f+0.45f*Mathf.Sin(t*9f);float fade=Mathf.Clamp01((3f-t)/0.6f);
            foreach(var f in frame)if(f!=null)f.color=new Color(1f,.82f,.32f,pulse*fade);
            foreach(var b in bolts)if(b!=null){bool flash=!reduced&&UnityEngine.Random.value<0.28f;b.color=new Color(1f,.95f,.6f,flash?0.85f*fade:0f);}
        }
    }
}
