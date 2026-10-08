using CampusRift.UI;
namespace CampusRift.Learning
{
    public sealed partial class LearningUI
    {
        LearningShrine shrine;
        void OpenShrineQuestion()
        {
            shrine=LearningShrine.Pending;if(shrine==null){Close();return;}mode=QuizKind.Shrine;quiz=shrine.Session;ShowQuestion();
            close.Label.text=L("LEAVE STELE","RỜI LINH BIA");
        }
        void FinishShrine(QuizResult answer)
        {
            if(shrine==null){Close();return;}shrine.Resolve(answer.passed);
            Clear("ShrineResult",L("FORTUNE STELE","LINH BIA CƠ DUYÊN"),answer.passed?L("Choose one fortune","Chọn 1 cơ duyên"):L("No penalty. Review it later in your notebook.","Không phạt. Có thể ôn lại trong sổ tay câu sai."),null);
            if(answer.passed)
            {
                foreach(var choice in shrine.Choices){var selected=choice;Button(LearningShrine.FortuneText(choice,Vn),()=>{shrine.Choose(selected);Close();},true,96,true);}
            }
            else {AnswerLines(answer.answers,false);Button(L("RETURN TO GAME","VỀ MÀN CHƠI"),Close,true,84);}
        }
        void CancelShrine(){if(shrine!=null){shrine.Cancel();shrine=null;}}
    }
}
namespace CampusRift.UI
{
    public static class LearningUIState
    {
        public static void OpenShrine(CampusRift.Learning.LearningShrine shrine)
        {if(shrine==null||UIStateManager.Instance==null||UIStateManager.Instance.State!=UIState.Gameplay)return;CampusRift.Learning.LearningUI.PendingScreen="Shrine";UIStateManager.Instance.OpenShrine();}
    }
}
