from pathlib import Path
p=Path(__file__).resolve().parents[1]/'Assets/Localization/Runtime/LocalizationPlayTest.cs'
s=p.read_text(encoding='utf-8-sig')
a=s.index('        IEnumerator Run()')
b=s.rindex('\n    }')
run='''        // P09 Hub and P17 full curriculum replace P06 title-button/5-lesson breakthrough assumptions.
        IEnumerator Run()
        {
            GameSceneManager.Instance.LoadMainMenu();yield return Loaded();
            if(State.State==UIState.Hub)State.Back();yield return Frames();
            var start=SettingsManager.Instance.Current.Copy();start.Language=GameLanguage.Vietnamese;SettingsManager.Instance.Apply(start,false);yield return Frames();
            var play=UI.MainMenu.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="PLAY").GetComponentInChildren<TMP_Text>(true);
            Check(play!=null && play.text==Loc.Translate("PLAY"),"Current comic title menu displays translated PLAY label");
            Check(Loc.VietnameseFont.HasCharacters("TRI THỨC = SỨC MẠNH; Thiên Thủ Trấn Áp; ĐỘT PHÁ; Nguyễn",out uint[] missing,true,true),"Vietnamese font contains representative accented glyphs");
            State.OpenSettings();yield return Frames();var settings=Object.FindAnyObjectByType<SettingsUI>();
            Check(settings.Language!=null && settings.Language.options.Count==2,"Settings has EN/VN selector");
            settings.Language.value=0;yield return Frames();
            Check(Loc.Language==GameLanguage.English && play.text=="PLAY","English preview updates hidden and visible current comic text");
            Check(SettingsManager.Instance.Current.Language==GameLanguage.Vietnamese,"Preview does not commit saved language");
            settings.Cancel();yield return Frames();Check(Loc.Language==GameLanguage.Vietnamese && play.text==Loc.Translate("PLAY"),"Cancel restores Vietnamese language");
            State.OpenSettings();yield return Frames();settings.Language.value=0;settings.Apply();yield return Frames();
            Check(SettingsManager.Instance.ReadSaved().Language==GameLanguage.English,"Apply persists English");
            settings.Language.value=1;settings.Apply();yield return Frames();
            Check(SettingsManager.Instance.ReadSaved().Language==GameLanguage.Vietnamese,"Apply persists Vietnamese");settings.Cancel();
            bool content=true,options=true;int lessons=0,questions=0;
            foreach(var c in engine.Catalog.courses)foreach(var l in c.lessons)
            {
                lessons++;content &= !string.IsNullOrEmpty(l.titleVN) && Loc.Translate(l.title)==l.titleVN;
                foreach(var page in l.pages)content &= Loc.Translate(page.content)==page.contentVN && Loc.Translate(page.example)==page.exampleVN && Loc.Translate(page.takeaway)==page.takeawayVN;
                foreach(var q in l.questionBank.questions){questions++;content &= Loc.Translate(q.prompt)==q.promptVN && Loc.Translate(q.explanation)==q.explanationVN;foreach(var o in q.options)options &= Loc.Translate(o.text)==o.textVN;}
            }
            Check(content && lessons==21 && questions==440,"P17 curriculum21 lessons/440 questions and explanations resolve to Vietnamese");
            Check(options,"All answer options retain their ids and localized/math text");
            State.OpenHub();yield return Frames();Check(HubUI.Instance!=null&&HubUI.Instance.Visible,"P09 title enters actual Hub");
            State.OpenCourse();View.ShowCourses();yield return Frames();Check(View.heading.text==Loc.Translate("COURSES"),"Library uses Vietnamese heading");
            var course=engine.Catalog.courses[0];var lesson=course.lessons[0];View.ShowLessons(course);View.OpenLesson(lesson);yield return Frames();
            Check(View.content.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==lesson.pages[0].contentVN),"Actual lesson body is Vietnamese");
            for(int i=0;i<lesson.pages.Count;i++){Click(i<lesson.pages.Count-1?"READ & NEXT PAGE":"COMPLETE READING");yield return Frames();}
            Click("START QUIZ");yield return Frames();var first=Session.Questions[0];string signature=Session.Signature;
            Check(View.content.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==first.Data.promptVN),"Actual quiz question displays Vietnamese");
            Loc.Preview(GameLanguage.English);yield return Frames();
            Check(Session.Signature==signature && Session.Answered==0 && View.content.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==first.Data.prompt),"Mid-quiz preview preserves order/session");Loc.Restore();yield return Frames();
            View.Answer(first.Data.correctOptionIds);yield return Frames();
            Check(View.content.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==first.Data.explanationVN),"Actual answer explanation displays Vietnamese");View.NextQuestion();
            while(!Session.Complete){var q=Session.Questions[Session.Answered].Data;View.Answer(q.correctOptionIds);View.NextQuestion();}
            yield return Frames();Check(engine.Progress.Lesson(lesson.id).rewarded,"Localized quiz submits actual first-mastery reward");
            string earned=JsonUtility.ToJson(engine.Progress);Loc.Preview(GameLanguage.English);yield return Frames();
            Check(JsonUtility.ToJson(engine.Progress)==earned,"Language preview preserves all earned progress");Loc.Restore();View.Close();
            GameSceneManager.Instance.StartSandbox();yield return Loaded();State.EnterScene(true);yield return Frames();
            var player=Object.FindAnyObjectByType<CampusExplorer>();float hp=player.GetComponent<Monsters.PlayerMonsterHealth>().maxHealth;
            State.Pause();State.OpenSettings();yield return Frames();settings=Object.FindAnyObjectByType<SettingsUI>();settings.Language.value=0;settings.Apply();yield return Frames();
            Check(Loc.Language==GameLanguage.English && player.GetComponent<Monsters.PlayerMonsterHealth>().maxHealth==hp,"Gameplay language Apply preserves player stats");settings.Cancel();State.Resume();
            var vn=SettingsManager.Instance.Current.Copy();vn.Language=GameLanguage.Vietnamese;SettingsManager.Instance.Apply(vn,false);
            GameSceneManager.Instance.LoadMainMenu();yield return Loaded();
            Check(State.State==UIState.Hub && Loc.Language==GameLanguage.Vietnamese,"P09 return to Hub preserves Vietnamese");
        }
'''
s=s[:a]+run+s[b:]
s=s.replace('original=LearningService.Instance.Engine;settingsBefore=', 'original=LearningService.Instance.Engine;CampusRift.Progression.ProfileService.Instance.UseTransient(new CampusRift.Progression.ProfileData());settingsBefore=')
s=s.replace('LearningService.Instance.EditorUseEngine(original);','CampusRift.Progression.ProfileService.Instance.EndTransient();LearningService.Instance.EditorUseEngine(original);')
s=s.replace('settingsBefore.Language=GameLanguage.Vietnamese;','')
p.write_text(s,encoding='utf-8')
print('Localization fixture updated for current Hub,21 lessons,440 questions and per-lesson mastery.')
