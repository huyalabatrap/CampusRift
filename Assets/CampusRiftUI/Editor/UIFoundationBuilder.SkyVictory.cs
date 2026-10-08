using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static partial class UIFoundationBuilder
{
    [MenuItem("Campus Rift/UI/Add Sky Setting and Victory")]
    public static void AddSkyAndVictory()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
        Prepare();var original=SceneManager.GetActiveScene();string originalPath=original.path;
        if(original.isDirty)EditorSceneManager.SaveScene(original);
        foreach(string path in new[]{MainPath,GamePath})
        {
            var scene=EditorSceneManager.OpenScene(path);
            var ui=Object.FindAnyObjectByType<UIManager>();var settings=ui.Settings.GetComponent<SettingsUI>();
            if(settings.SkyBrightness==null)
            {
                var video=settings.Tabs[0].transform;
                settings.SkyBrightness=Slider(video,"SKY BRIGHTNESS",326,0,1,out settings.SkyValue);
                settings.SkyBrightness.SetValueWithoutNotify(1);settings.SkyValue.text="DAY";
                var note=video.Find("Note").GetComponent<TMP_Text>();
                note.text="NIGHT  /  DAY  -  Preview the sky, then Apply to save.";note.fontSize=17;
                note.rectTransform.anchoredPosition=new Vector2(0,-376);note.rectTransform.sizeDelta=new Vector2(1000,32);
                EditorUtility.SetDirty(settings);
            }
            if(path==GamePath)
            {
                if(ui.Victory==null)
                {
                    ui.Victory=Panel(ui.transform,"Victory");
                    var card=Card(ui.Victory.transform,"Victory Card",850,520);
                    Text(card,"Eyebrow","THE MONSTER HAS FALLEN",60,42,730,40,18,Theme.SuccessColor).alignment=TextAlignmentOptions.Center;
                    Text(card,"Title","VICTORY",40,126,770,110,86,Theme.SecondaryAccent).alignment=TextAlignmentOptions.Center;
                    Text(card,"Description","You defeated Shaban. Campus is safe.",40,270,770,50,25,Theme.TextSecondary).alignment=TextAlignmentOptions.Center;
                    ui.Victory.FirstSelection=Button(card,"PLAY AGAIN","PLAY AGAIN",60,380,345,64,ui.Restart);
                    Button(card,"MAIN MENU","MAIN MENU",445,380,345,64,ui.Main);
                }
                var objective=ui.HUD.GetComponent<GameplayHUD>().Objective;
                if(objective!=null && objective.Body!=null){objective.Body.text="Defeat Shaban to escape.";EditorUtility.SetDirty(objective.Body);}
            }
            EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(originalPath))EditorSceneManager.OpenScene(originalPath);
    }
}
