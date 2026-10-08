using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.Learning;
using CampusRift.Localization;
using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class LocalizationSetup
{
    [Serializable] sealed class Content {public List<Lesson> lessons;}
    [Serializable] sealed class Lesson {public string id,titleVN;public List<LessonPage> pages;public List<Question> questions;}
    [Serializable] sealed class Question {public string promptVN,explanationVN;public List<string> optionsVN;}
    [MenuItem("Campus Rift/Localization/Install EN-VN")]
    public static void Install()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        const string path="Assets/Localization/Resources/LocalizationCatalog.asset";
        var catalog=AssetDatabase.LoadAssetAtPath<LocalizationCatalog>(path);
        if(catalog==null){catalog=ScriptableObject.CreateInstance<LocalizationCatalog>();AssetDatabase.CreateAsset(catalog,path);}
        catalog.vietnameseFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/CampusRiftUI/Fonts/CampusRiftBody.asset");
        catalog.entries.Clear();
        foreach(string line in File.ReadAllLines("Assets/Localization/Editor/UIVietnamese.txt"))
        {
            if(string.IsNullOrWhiteSpace(line))continue;int split=line.IndexOf('|');if(split<1)throw new InvalidDataException(line);
            catalog.entries.Add(new TranslationEntry {en=line.Substring(0,split),vi=line.Substring(split+1)});
        }
        var content=JsonUtility.FromJson<Content>(File.ReadAllText("Assets/Localization/Editor/LearningVietnamese.json"));
        var learning=Resources.Load<LearningCatalog>("LearningCatalog");
        foreach(var course in learning.courses)
        {
            if(course.id!="intro-algorithms")continue;
            course.titleVN="Nhập môn thuật toán";
            course.descriptionVN="Lĩnh hội năm bài học ngắn. Biến tri thức thành sức mạnh sinh tồn.";
            foreach(var lesson in course.lessons)
            {
                var source=content.lessons.Find(l=>l.id==lesson.id);if(source==null)continue;
                lesson.titleVN=source.titleVN;
                for(int i=0;i<lesson.pages.Count && i<source.pages.Count;i++)
                {
                    var p=lesson.pages[i];var s=source.pages[i];p.titleVN=s.titleVN;p.contentVN=s.contentVN;p.exampleVN=s.exampleVN;p.takeawayVN=s.takeawayVN;
                }
                var questions=lesson.questionBank.questions.Where(q=>q.lessonId==lesson.id).ToList();
                if(questions.Count!=source.questions.Count)throw new InvalidDataException("Question translation count: "+lesson.id);
                for(int i=0;i<questions.Count;i++)
                {
                    var q=questions[i];var s=source.questions[i];q.promptVN=s.promptVN;q.explanationVN=s.explanationVN;
                    if(q.options.Count!=s.optionsVN.Count)throw new InvalidDataException("Answer translation count: "+q.id);
                    for(int n=0;n<q.options.Count;n++)q.options[n].textVN=s.optionsVN[n];
                }
                EditorUtility.SetDirty(lesson);EditorUtility.SetDirty(lesson.questionBank);
            }
            foreach(var tier in course.skillTiers)if(tier.skill!=null)
            {
                tier.skill.displayNameVN="Thiên Thủ Trấn Áp";
                tier.skill.descriptionVN="Nhấn F để trấn áp quái vật gần nhất. Trên thiết bị cảm ứng, dùng nút Thiên Thủ.";
                EditorUtility.SetDirty(tier.skill);
            }
            EditorUtility.SetDirty(course);
        }
        string characters=string.Join("",catalog.entries.Select(x=>x.vi))+File.ReadAllText("Assets/Localization/Editor/LearningVietnamese.json");
        catalog.vietnameseFont.TryAddCharacters(characters,out string missing);
        EditorUtility.SetDirty(catalog.vietnameseFont);EditorUtility.SetDirty(catalog);
        foreach(string scenePath in new[]{UIFoundationBuilder.MainPath,UIFoundationBuilder.GamePath})
        {
            var scene=EditorSceneManager.OpenScene(scenePath);
            var settings=Object.FindAnyObjectByType<UIManager>().Settings.GetComponent<SettingsUI>();
            if(settings.Language==null)
            {
                var card=settings.transform.Find("Settings Card");
                UIFoundationBuilder.Text(card,"Language Label","LANGUAGE",736,30,336,30,17,UIFoundationBuilder.Theme.TextSecondary);
                var dd=Object.Instantiate(settings.Quality,card);dd.name="Language Dropdown";
                var rect=(RectTransform)dd.transform;rect.anchoredPosition=new Vector2(825,-78);rect.sizeDelta=new Vector2(247,56);
                dd.onValueChanged=new TMP_Dropdown.DropdownEvent();dd.ClearOptions();dd.AddOptions(new List<string>{"EN — English","VN — Tiếng Việt"});dd.SetValueWithoutNotify(1);
                var caption=dd.captionText.rectTransform;caption.anchoredPosition=new Vector2(16,0);caption.sizeDelta=new Vector2(196,56);
                dd.captionText.enableAutoSizing=true;dd.captionText.fontSizeMin=18;dd.captionText.fontSizeMax=23;
                var arrow=dd.transform.Find("Arrow") as RectTransform;if(arrow!=null)arrow.anchoredPosition=new Vector2(214,0);
                dd.template.sizeDelta=new Vector2(247,112);dd.template.anchoredPosition=new Vector2(0,-56);
                var contentRect=dd.template.Find("Viewport/Content") as RectTransform;contentRect.sizeDelta=new Vector2(247,48);
                var item=contentRect.Find("Item") as RectTransform;item.sizeDelta=new Vector2(247,48);
                dd.itemText.rectTransform.sizeDelta=new Vector2(211,48);dd.itemText.enableAutoSizing=true;dd.itemText.fontSizeMin=18;dd.itemText.fontSizeMax=22;
                settings.Language=dd;
            }
            settings.transform.Find("Settings Card/Language Label").GetComponent<TMP_Text>().text="LANGUAGE";
            EditorUtility.SetDirty(settings);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(UIFoundationBuilder.MainPath);
        Debug.Log("EN/VN installed: shared UI table, Settings language dropdown, complete sample course translations.");
    }
}
