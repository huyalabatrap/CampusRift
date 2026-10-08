using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CampusRift.UI;
namespace CampusRift.Learning
{
    public sealed partial class LearningUI
    {
        readonly HashSet<string> multi=new HashSet<string>();
        readonly Dictionary<string,string> pairs=new Dictionary<string,string>();
        string leftPicked;int extraQuestion=-1;QuizSession extraSession;
        List<StudyCard> cards;int cardIndex;bool cardBack;
        public void ShowCards()
        {
            Clear("Cards",L("FLASHCARDS","THẺ GHI NHỚ"),L("Review notes from lessons you have read. No Tu Vi reward.","Ôn ghi nhớ từ bài đã đọc. Không thưởng Tu Vi."),ShowCourses);
            foreach(var c in engine.Catalog.courses)foreach(var l in c.lessons)
                if(engine.LessonCompleted(l)){var own=l;Button(l.title,()=>OpenCards(own),true,84,true);}
            if(!engine.Catalog.courses.SelectMany(c=>c.lessons).Any(l=>engine.LessonCompleted(l)))Text(L("Read a lesson first.","Hãy đọc xong một bài trước."));
        }
        public void OpenCards(LessonData selected)
        {
            lesson=selected;cards=engine.Cards(selected);cardIndex=0;cardBack=false;DrawCard();
        }
        void DrawCard()
        {
            if(cards.Count==0){ShowCards();return;}var card=cards[cardIndex];
            Clear("Card",L("FLASHCARD","THẺ GHI NHỚ")+"  "+(cardIndex+1)+"/"+cards.Count,lesson.title,ShowCards);
            Text(card.front,UiKit.GoldHi);
            Button(cardBack?card.back:L("Tap to reveal the note","Chạm để xem ghi nhớ"),()=>{cardBack=!cardBack;DrawCard();},true,190,true);
            if(cardBack)
            {
                Text(L("Source: ","Nguồn: ")+card.source);
                Button(L("REMEMBERED","ĐÃ NHỚ"),()=>RateCard(true),true,84);
                Button(L("NOT YET","CHƯA NHỚ"),()=>RateCard(false),true,84);
            }
            var p=engine.CardProgress(card.id);Text(L("Reviews: ","Lần ôn: ")+p.reviews+(p.remembered?L(" · remembered"," · đã nhớ"):L(" · revisit soon"," · ôn lại sớm")));
        }
        public void RateCard(bool remembered)
        {
            var card=cards[cardIndex];engine.ReviewCard(card,remembered);
            if(!remembered){cards.RemoveAt(cardIndex);cards.Insert(Math.Min(cardIndex+2,cards.Count),card);}
            cardIndex=(cardIndex+1)%cards.Count;cardBack=false;DrawCard();
        }
        public void ShowNotebook()
        {
            Clear("Notebook",L("MISTAKE NOTEBOOK","SỔ TAY CÂU SAI"),L("Two consecutive correct answers remove a question.","Đúng 2 lần liên tiếp để câu rời sổ. Thưởng theo luật luyện lại."),ShowCourses);
            var pool=engine.NotebookPool();Button(L("REVIEW THESE QUESTIONS","ÔN CÂU TRONG SỔ"),StartNotebook,pool.Count>0,84);
            if(pool.Count==0)Text(L("No mistakes to review.","Chưa có câu sai cần ôn."));
            foreach(var q in pool){var p=engine.Progress.notebook.Find(n=>n.id==q.id);Text(q.prompt+"\n"+L("Last mistake (UTC): ","Sai gần nhất (UTC): ")+p.lastWrongUtc+"\n"+p.consecutiveCorrect+"/2 · "+q.source);}
        }
        public void StartNotebook()
        {
            quiz=engine.StartNotebook();if(quiz==null){ShowNotebook();return;}mode=QuizKind.Notebook;ShowQuestion();
        }
        void ExtraResult()
        {
            result=engine.SubmitExtra(quiz);
            if(mode==QuizKind.Shrine){FinishShrine(result);return;}
            Clear("NotebookResult",L("NOTEBOOK REVIEW","ÔN SỔ TAY"),result.correct+"/"+result.total,ShowNotebook);AddLinhLines();AnswerLines(result.answers,false);Button(L("BACK TO NOTEBOOK","VỀ SỔ TAY"),ShowNotebook);
        }
        public void ShowDaily()
        {
            Clear("Daily",L("DAILY STUDY","NHIỆM VỤ NGÀY"),engine.Now.ToString("yyyy-MM-dd")+" UTC",ShowCourses);
            var d=engine.Daily.State;
            Text(L("Study at your own pace. Rewards are paid automatically.","Học theo nhịp của bạn. Thưởng tự nhận khi hoàn thành."));
            Text(L("Read one lesson or review its full card deck","Học 1 bài hoặc ôn đủ thẻ của 1 bài")+"  "+Math.Min(d.lessons,1)+"/1 · +30 Linh Thạch");
            Text(L("Answer 20 questions correctly","Đúng 20 câu")+"  "+Math.Min(d.correct,20)+"/20 · +30 Linh Thạch");
            Text(L("Pass one due review","Ôn đạt 1 bài đến hạn")+"  "+Math.Min(d.reviews,1)+"/1 · +30 Linh Thạch");
            Text(L("Study streak: ","Chuỗi ngày học: ")+d.streak+" · "+(d.streak%7)+"/7 · +150 Linh Thạch");
            Text(engine.Daily.RestUsedThisWeek?L("This week's rest day has been used.","Đã dùng ngày nghỉ phép tuần này."):L("One automatic rest day each UTC week preserves your streak.","Mỗi tuần UTC có 1 ngày nghỉ phép tự động giữ chuỗi."));
            Text(L("Missing two days resets only the streak. Your knowledge and currency stay.","Bỏ 2 ngày chỉ bắt đầu lại chuỗi. Bài đã học và Linh Thạch được giữ."));
            Button(L("FLASHCARDS","THẺ GHI NHỚ"),ShowCards,true,84);
        }
        void DrawExtraQuestion(QuizQuestion question)
        {
            var q=question.Data;
            if(extraSession!=quiz||extraQuestion!=quiz.Answered){extraSession=quiz;extraQuestion=quiz.Answered;multi.Clear();pairs.Clear();leftPicked=null;}
            if(q.type=="multi-choice")
            {
                Text(L("Select all correct answers, then confirm.","Chọn tất cả đáp án đúng, rồi xác nhận."));
                foreach(var o in question.Options){string id=o.id;Button((multi.Contains(id)?"[x] ":"[ ] ")+o.text,()=>{if(!multi.Add(id))multi.Remove(id);RedrawQuestionKeepingScroll(scroll.verticalNormalizedPosition);},true,84,true);}
                Button(L("CONFIRM ANSWERS","XÁC NHẬN ĐÁP ÁN"),()=>Answer(multi.ToArray()),multi.Count>0,84);
            }
            else if(q.type=="fill-blank")
            {
                Text(L("Choose the phrase for the blank.","Chạm chọn cụm từ điền vào chỗ trống."));
                foreach(var o in question.Options){var option=o;Button(o.text,()=>Answer(option.id),true,84,true);}
            }
            else
            {
                Text(L("Tap a left card, then a right card; or drag between them.","Chạm ô trái rồi ô phải, hoặc kéo từ trái sang phải."));
                var left=question.Options.Where(o=>o.side=="left").ToList();var right=question.Options.Where(o=>o.side=="right").ToList();
                for(int i=0;i<left.Count;i++)
                {
                    var row=new GameObject("Matching Row",typeof(RectTransform),typeof(HorizontalLayoutGroup),typeof(LayoutElement));row.transform.SetParent(content,false);
                    var group=row.GetComponent<HorizontalLayoutGroup>();group.spacing=24;group.childControlHeight=group.childControlWidth=true;group.childForceExpandWidth=true;
                    row.GetComponent<LayoutElement>().preferredHeight=128;
                    foreach(var o in new[]{left[i],right[i]})
                    {
                        string id=o.id;string link=o.side=="left"&&pairs.TryGetValue(id,out var p)?" → "+p:"";
                        var b=Button((leftPicked==id?"▶ ":"")+id+". "+o.text+link,()=>SelectMatch(id),true,128,true);b.transform.SetParent(row.transform,false);
                        b.Label.fontSize=b.Label.fontSizeMax=24;b.Label.fontSizeMin=24;b.Label.enableAutoSizing=false;
                        b.GetComponent<LayoutElement>().preferredWidth=400;
                        var drag=b.gameObject.AddComponent<LearningMatchDrag>();drag.owner=this;drag.id=id;
                    }
                }
                Button(L("CONFIRM PAIRS","XÁC NHẬN CẶP NỐI"),()=>Answer(pairs.Select(p=>p.Key+":"+p.Value).ToArray()),pairs.Count==left.Count,84);
            }
        }
        public void SelectMatch(string id)
        {
            var o=quiz.Questions[quiz.Answered].Data.options.Find(x=>x.id==id);if(o==null)return;
            if(o.side=="left")leftPicked=id;
            else if(leftPicked!=null){foreach(var key in pairs.Where(p=>p.Value==id).Select(p=>p.Key).ToList())pairs.Remove(key);pairs[leftPicked]=id;leftPicked=null;}
            RedrawQuestionKeepingScroll(scroll.verticalNormalizedPosition);
        }
        public void DragMatch(string from,string to)
        {
            var options=quiz.Questions[quiz.Answered].Data.options;if(!options.Any(o=>o.id==from&&o.side=="left")||!options.Any(o=>o.id==to&&o.side=="right"))return;
            leftPicked=from;SelectMatch(to);
        }
    }
    public sealed class LearningMatchDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public LearningUI owner;public string id;
        public void OnBeginDrag(PointerEventData data){}
        public void OnDrag(PointerEventData data){}
        public void OnEndDrag(PointerEventData data)
        {
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            foreach(var h in hits){var other=h.gameObject.GetComponentInParent<LearningMatchDrag>();if(other!=null&&other.owner==owner&&other.id!=id){owner.DragMatch(id,other.id);break;}}
        }
    }
}
