using System;
using System.Linq;
using System.Reflection;
using CampusRift;
using CampusRift.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static partial class UIFoundationBuilder
{
    [MenuItem("Campus Rift/UI/Phase 6 - Build Settings")]
    public static void BuildSettings()
    {
        Prepare();var mixer=BuildMixer();
        var path=Root+"/Resources/CampusRiftServices.prefab";var services=PrefabUtility.LoadPrefabContents(path);
        var settings=services.GetComponent<SettingsManager>();if(settings==null)settings=services.AddComponent<SettingsManager>();settings.Mixer=mixer;
        services.GetComponent<UIAudioManager>().Output=mixer.FindMatchingGroups("UI").First(g=>g.name=="UI");
        PrefabUtility.SaveAsPrefabAsset(services,path);PrefabUtility.UnloadPrefabContents(services);
        foreach(var scenePath in new[]{MainPath,GamePath})
        {
            EditorSceneManager.OpenScene(scenePath);var ui=Object.FindAnyObjectByType<UIManager>();
            if(ui.Settings!=null)Object.DestroyImmediate(ui.Settings.gameObject);
            ui.Settings=Panel(ui.transform,"Settings");var card=Card(ui.Settings.transform,"Settings Card",1120,850);
            var controller=ui.Settings.gameObject.AddComponent<SettingsUI>();controller.Theme=Theme;
            Text(card,"Eyebrow","CAMPUS RIFT  /  PREFERENCES",48,27,980,30,17,Theme.TextSecondary);
            Text(card,"Heading","SETTINGS",48,70,900,72,52);
            var videoButton=Button(card,"VIDEO","VIDEO",48,163,315,58,controller.SelectVideo);
            var audioButton=Button(card,"AUDIO","AUDIO",395,163,315,58,controller.SelectAudio);
            var gameplayButton=Button(card,"GAMEPLAY","GAMEPLAY",742,163,330,58,controller.SelectGameplay);
            controller.TabLines=new[]{(Image)videoButton.Edge,(Image)audioButton.Edge,(Image)gameplayButton.Edge};
            controller.Tabs=new GameObject[3];
            for(int i=0;i<3;i++)controller.Tabs[i]=Rect(card,new[]{"Video","Audio","Gameplay"}[i],48,255,1024,415).gameObject;
            var video=controller.Tabs[0].transform;
            controller.Resolution=Dropdown(video,"RESOLUTION",0,0);controller.Fullscreen=Toggle(video,"FULLSCREEN",0,86);
            controller.Quality=Dropdown(video,"QUALITY",0,172);controller.VSync=Toggle(video,"V-SYNC",0,258);
            Text(video,"Note","Choose your display mode and visual quality.",0,355,980,45,19,Theme.TextSecondary);
            var audio=controller.Tabs[1].transform;controller.Volumes=new Slider[5];controller.VolumeValues=new TMP_Text[5];
            var names=new[]{"MASTER","MUSIC","SFX","MONSTER","UI"};
            for(int i=0;i<5;i++)controller.Volumes[i]=Slider(audio,names[i],i*78,0,1,out controller.VolumeValues[i]);
            var game=controller.Tabs[2].transform;
            controller.MouseSensitivity=Slider(game,"MOUSE SENSITIVITY",0,.25f,3,out controller.MouseValue);
            controller.CameraSensitivity=Slider(game,"CAMERA ZOOM SENSITIVITY",85,.25f,3,out controller.CameraValue);
            controller.FieldOfView=Slider(game,"FIELD OF VIEW",170,45,90,out controller.FovValue);
            controller.InvertY=Toggle(game,"INVERT Y",0,255);
            Text(game,"Note","Mouse controls orbit; camera sensitivity controls scroll zoom.",0,350,980,45,19,Theme.TextSecondary);
            controller.Feedback=Text(card,"Feedback","",48,688,960,30,17,Theme.SuccessColor);
            Button(card,"APPLY","APPLY",738,742,334,62,controller.Apply);
            Button(card,"CANCEL","CANCEL / BACK",48,742,334,62,controller.Cancel);
            ui.Settings.FirstSelection=videoButton;
            if(scenePath==GamePath)
            {
                var player=Object.FindAnyObjectByType<CampusExplorer>();var adapter=player.GetComponent<GameplaySettingsAdapter>();if(adapter==null)adapter=player.gameObject.AddComponent<GameplaySettingsAdapter>();adapter.BaseMouseSensitivity=player.mouseSensitivity;
                var monster=mixer.FindMatchingGroups("Monster").First(g=>g.name=="Monster");var sfx=mixer.FindMatchingGroups("SFX").First(g=>g.name=="SFX");
                foreach(var source in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
                {source.outputAudioMixerGroup=source.GetComponentInParent<CampusRift.Monsters.MonsterAudio>()!=null?monster:sfx;EditorUtility.SetDirty(source);}
            }
            EditorUtility.SetDirty(ui);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        AssetDatabase.SaveAssets();Debug.Log("CAMPUS RIFT: Settings, camera adapter and five mixer groups ready.");
    }
    static Slider Slider(Transform parent,string label,float y,float min,float max,out TMP_Text value)
    {
        Text(parent,label+" Label",label,0,y,480,32,20,Theme.TextSecondary);value=Text(parent,label+" Value","",870,y,130,32,20);value.alignment=TextAlignmentOptions.Right;
        var r=Rect(parent,label+" Slider",500,y+6,340,28);var slider=r.gameObject.AddComponent<Slider>();
        var background=Image(r,"Track",0,10,340,6,Theme.SecondaryBackground);background.raycastTarget=true;
        var fillArea=Rect(r,"Fill Area",7,10,326,6);var fill=Image(fillArea,"Fill",0,0,326,6,Theme.PrimaryAccent);
        fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.sizeDelta=Vector2.zero;fill.rectTransform.anchoredPosition=Vector2.zero;slider.fillRect=fill.rectTransform;
        var handleArea=Rect(r,"Handle Area",7,0,326,28);var handle=Image(handleArea,"Handle",0,0,16,28,Theme.TextPrimary);
        handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.sizeDelta=new Vector2(16,0);handle.rectTransform.anchoredPosition=Vector2.zero;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=min;slider.maxValue=max;return slider;
    }
    static Toggle Toggle(Transform parent,string label,float x,float y)
    {
        var r=Rect(parent,label,x,y,1000,52);var toggle=r.gameObject.AddComponent<Toggle>();
        Text(r,"Label",label,0,0,600,52,21,Theme.TextSecondary);
        var bg=Image(r,"Box",900,8,36,36,Theme.SecondaryBackground);bg.raycastTarget=true;
        var mark=Text(bg.transform,"Check","X",0,0,36,36,24,Theme.PrimaryAccent);mark.alignment=TextAlignmentOptions.Center;
        toggle.targetGraphic=bg;toggle.graphic=mark;return toggle;
    }
    static TMP_Dropdown Dropdown(Transform parent,string label,float x,float y)
    {
        Text(parent,label+" Label",label,x,y,430,60,21,Theme.TextSecondary);
        var r=Rect(parent,label+" Dropdown",x+500,y,500,60);var bg=r.gameObject.AddComponent<Image>();bg.color=Theme.SecondaryBackground;
        var dd=r.gameObject.AddComponent<TMP_Dropdown>();dd.targetGraphic=bg;
        dd.captionText=Text(r,"Selected","",20,0,430,60,23);Text(r,"Arrow","v",456,0,36,60,20,Theme.PrimaryAccent);
        var template=Rect(r,"Template",0,60,500,270);var templateBg=template.gameObject.AddComponent<Image>();templateBg.color=Theme.SecondaryBackground;
        var viewport=Stretch(template,"Viewport");viewport.gameObject.AddComponent<RectMask2D>();
        var content=Rect(viewport,"Content",0,0,500,48);
        var item=Rect(content,"Item",0,0,500,48);var toggle=item.gameObject.AddComponent<Toggle>();
        var itemBg=item.gameObject.AddComponent<Image>();itemBg.color=Theme.PanelBackground;toggle.targetGraphic=itemBg;
        var mark=Image(item,"Selection",0,0,4,48,Theme.PrimaryAccent);toggle.graphic=mark;
        dd.itemText=Text(item,"Label","Option",20,0,456,48,22);
        var scroll=template.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
        dd.template=template;template.gameObject.SetActive(false);return dd;
    }
    static AudioMixer BuildMixer()
    {
        string path=Root+"/CampusRiftAudio.mixer";var loaded=AssetDatabase.LoadAssetAtPath<AudioMixer>(path);if(loaded!=null)return loaded;
        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        var type=typeof(Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
        var mixer=(AudioMixer)type.GetMethod("CreateMixerControllerAtPath",flags).Invoke(null,new object[]{path});
        var master=type.GetProperty("masterGroup",flags).GetValue(mixer);var groupType=master.GetType();
        var groups=Array.CreateInstance(groupType,4);string[] names={"Music","SFX","Monster","UI"};
        for(int i=0;i<4;i++)groups.SetValue(type.GetMethod("CreateNewGroup",flags).Invoke(mixer,new object[]{names[i],true}),i);
        groupType.GetProperty("children",flags).SetValue(master,groups);
        var exposedType=typeof(Editor).Assembly.GetType("UnityEditor.Audio.ExposedAudioParameter");var exposed=Array.CreateInstance(exposedType,5);
        for(int i=0;i<5;i++)
        {
            var group=i==0?master:groups.GetValue(i-1);var parameter=Activator.CreateInstance(exposedType);
            exposedType.GetField("guid",flags).SetValue(parameter,groupType.GetMethod("GetGUIDForVolume",flags).Invoke(group,null));
            exposedType.GetField("name",flags).SetValue(parameter,SettingsManager.VolumeParameters[i]);exposed.SetValue(parameter,i);
        }
        type.GetProperty("exposedParameters",flags).SetValue(mixer,exposed);
        EditorUtility.SetDirty(mixer);AssetDatabase.SaveAssets();return mixer;
    }
}
