using System;
using System.Collections.Generic;
using System.IO;
using CampusRift.Learning;
using CampusRift.UI;
using CampusRift.Skills;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class LearningSetup
{
    const string Root="Assets/Learning";
    [Serializable] sealed class Samples {public List<Sample> lessons;}
    [Serializable] sealed class Sample {public string id,title;public List<LessonPage> pages;public List<QuestionData> questions;}
    static T Asset<T>(string path) where T:ScriptableObject
    {
        var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset!=null)return asset;
        asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
    }
    [MenuItem("Campus Rift/Learning/Install MVP")]
    public static void Install()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        Directory.CreateDirectory(Root+"/Data");Directory.CreateDirectory(Root+"/Resources");
        var catalog=Asset<LearningCatalog>(Root+"/Resources/LearningCatalog.asset");
        var reward=Asset<SkillRewardData>(Root+"/Data/GiantHandReward.asset");
        reward.id="giant-hand-seal";reward.displayName="Thien Thu Tran Ap / Giant Hand Seal";
        reward.description="Press F to seal the nearest monster. On mobile, use the Hand skill button.";
        if(catalog.courses.Count==0)
        {
            var course=Asset<CourseData>(Root+"/Data/Algorithms.asset");course.id="intro-algorithms";course.title="Introduction to Algorithms";course.subject="Computer Science";
            course.description="Master five short field lessons. Knowledge becomes survival power.";
            var samples=JsonUtility.FromJson<Samples>(File.ReadAllText(Root+"/Editor/SampleContent.json"));
            for(int i=0;i<samples.lessons.Count;i++)
            {
                var s=samples.lessons[i];var bank=Asset<QuestionBankData>(Root+"/Data/"+s.id+"-bank.asset");bank.questions=s.questions;
                var lesson=Asset<LessonData>(Root+"/Data/"+s.id+".asset");lesson.id=s.id;lesson.title=s.title;lesson.topic=s.title;
                lesson.pages=s.pages;lesson.questionBank=bank;lesson.quizSize=5;lesson.passPercent=80;
                lesson.requiredSurvivalSeconds=i*15;
                if(i>0)lesson.prerequisiteLessonIds.Add(samples.lessons[i-1].id);
                lesson.reward=new StatReward {healthFraction=.05f,movementFraction=.02f,energyFraction=i>=3?.025f:0};
                course.lessons.Add(lesson);EditorUtility.SetDirty(bank);EditorUtility.SetDirty(lesson);
            }
            course.skillTiers.Add(new SkillTier {tier=1,skill=reward});catalog.courses.Add(course);EditorUtility.SetDirty(course);
        }
        EditorUtility.SetDirty(catalog);EditorUtility.SetDirty(reward);
        foreach(string path in new[]{UIFoundationBuilder.MainPath,UIFoundationBuilder.GamePath})
        {
            var scene=EditorSceneManager.OpenScene(path);var ui=Object.FindAnyObjectByType<UIManager>();
            if(ui.GetComponentInChildren<LearningUI>(true)==null)BuildUI(ui);
            if(ui.IsGameplay)
            {
                var player=Object.FindAnyObjectByType<CampusRift.CampusExplorer>();BindPlayer(player.gameObject,reward);
                var card=ui.PauseMenu.transform.Find("Pause Card");
                if(card!=null && card.Find("COURSES")==null)
                    UIFoundationBuilder.Button(card,"COURSES","COURSES",56,638,528,50,ui.Courses);
                var hint=card!=null?card.Find("Escape Hint"):null;if(hint!=null)hint.gameObject.SetActive(false);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        string prefabPath="Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        try{BindPlayer(prefab,reward);PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(UIFoundationBuilder.MainPath);
        Debug.Log("Learning MVP installed: existing UI, player, skill gate and sample data connected.");
    }
    static void BindPlayer(GameObject player,SkillRewardData reward)
    {
        // V2: stats come from cultivation and skills open by realm, so the old reward binding is no longer created.
        if(player.GetComponent<CampusRift.Progression.CultivationPlayerBridge>()==null)player.AddComponent<CampusRift.Progression.CultivationPlayerBridge>();
        if(player.GetComponent<LearningSkillGate>()==null)player.AddComponent<LearningSkillGate>();
    }
    static void BuildUI(UIManager ui)
    {
        var old=ui.transform.Find("LearningUI");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var panel=ui.Course;
        if(panel==null)panel=UIFoundationBuilder.Panel(ui.transform,"LearningUI");
        else {for(int i=panel.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);panel.name="LearningUI";}
        ui.Course=panel;
        var view=panel.gameObject.AddComponent<LearningUI>();
        var card=UIFoundationBuilder.Card(panel.transform,"Learning Archive",1400,900);
        view.heading=UIFoundationBuilder.Text(card,"Heading","COURSES",48,30,1304,70,44);
        view.heading.enableAutoSizing=true;view.heading.fontSizeMin=28;view.heading.fontSizeMax=44;
        view.summary=UIFoundationBuilder.Text(card,"Summary","KNOWLEDGE IS POWER",48,110,1304,64,22,UIFoundationBuilder.Theme.TextSecondary);
        var viewport=UIFoundationBuilder.Rect(card,"Viewport",48,195,1304,565);viewport.gameObject.AddComponent<RectMask2D>();
        var hit=viewport.gameObject.AddComponent<Image>();hit.color=new Color(0,0,0,.01f);
        view.content=UIFoundationBuilder.Rect(viewport,"Content",0,0,1304,565);
        view.content.anchorMin=new Vector2(0,1);view.content.anchorMax=Vector2.one;view.content.sizeDelta=new Vector2(0,565);
        var layout=view.content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=16;layout.padding=new RectOffset(12,12,6,18);
        layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
        var fitter=view.content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        view.scroll=viewport.gameObject.AddComponent<ScrollRect>();view.scroll.viewport=viewport;view.scroll.content=view.content;
        view.scroll.horizontal=false;view.scroll.movementType=ScrollRect.MovementType.Clamped;view.scroll.scrollSensitivity=45;
        view.status=UIFoundationBuilder.Text(card,"Save Status","",48,767,1304,30,16,UIFoundationBuilder.Theme.TextSecondary);
        view.back=UIFoundationBuilder.Button(card,"BACK","BACK",48,814,360,56);
        view.close=UIFoundationBuilder.Button(card,"RETURN","RETURN TO "+(ui.IsGameplay?"GAME":"MENU"),900,814,452,56);
        panel.FirstSelection=view.close;
        var templates=UIFoundationBuilder.Rect(card,"Templates",0,0,1,1);templates.gameObject.SetActive(false);
        view.textTemplate=UIFoundationBuilder.Text(templates,"Text","",0,0,1280,80,25);view.textTemplate.overflowMode=TextOverflowModes.Overflow;
        view.textTemplate.gameObject.AddComponent<LayoutElement>();
        view.buttonTemplate=UIFoundationBuilder.Button(templates,"Choice","",0,0,1280,82);
        view.buttonTemplate.gameObject.AddComponent<LayoutElement>().preferredHeight=82;
        var label=view.buttonTemplate.Label.rectTransform;label.anchorMax=new Vector2(1,1);label.offsetMax=new Vector2(-56,0);label.offsetMin=new Vector2(24,-82);
        view.buttonTemplate.Label.enableAutoSizing=true;view.buttonTemplate.Label.fontSizeMin=18;view.buttonTemplate.Label.fontSizeMax=24;
        view.imageTemplate=UIFoundationBuilder.Image(templates,"Illustration",0,0,1280,260,Color.white);view.imageTemplate.gameObject.AddComponent<LayoutElement>().preferredHeight=260;
        EditorUtility.SetDirty(ui);
    }
    [MenuItem("Campus Rift/Learning/DEV - Pass Next Lesson")]
    static void Pass(){if(Application.isPlaying)LearningService.Instance.Engine.DebugPassNext(LearningService.Instance.Engine.Catalog.courses[0]);}
    [MenuItem("Campus Rift/Learning/DEV - Reset Learning Progress")]
    static void Reset(){if(Application.isPlaying)LearningService.Instance.Engine.DebugReset();}
}
