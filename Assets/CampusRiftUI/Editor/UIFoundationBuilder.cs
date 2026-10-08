using System.IO;
using System.Linq;
using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class UIFoundationBuilder
{
    public const string Root="Assets/CampusRiftUI";
    public const string MainPath="Assets/Scenes/MainMenu.unity", GamePath="Assets/Scenes/SampleScene.unity";
    static CampusRiftUITheme theme;
    public static CampusRiftUITheme Theme => theme != null ? theme : theme=AssetDatabase.LoadAssetAtPath<CampusRiftUITheme>(Root+"/CampusRiftUITheme.asset");
    static void Prepare()
    {
        Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Resources");
        if(Theme==null)
        {
            theme=ScriptableObject.CreateInstance<CampusRiftUITheme>();
            theme.Font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            AssetDatabase.CreateAsset(theme,Root+"/CampusRiftUITheme.asset");
        }
    }
    public static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h,Vector2? anchor=null)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;
        var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=anchor??new Vector2(0,1);r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    public static RectTransform Stretch(Transform parent,string name)
    {
        var r=Rect(parent,name,0,0,0,0);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;
    }
    public static Image Image(Transform parent,string name,float x,float y,float w,float h,Color color)
    {
        var image=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    public static RiftGraphic Shape(Transform parent,string name,float x,float y,float w,float h,Color color,RiftGraphic.Shape form)
    { var g=Rect(parent,name,x,y,w,h).gameObject.AddComponent<RiftGraphic>();g.color=color;g.Form=form;g.raycastTarget=false;return g; }
    public static TMP_Text Text(Transform parent,string name,string value,float x,float y,float w,float h,float size,Color? color=null)
    {
        var t=Rect(parent,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();
        t.font=Theme.Font;t.text=value;t.fontSize=size;t.color=color??Theme.TextPrimary;t.raycastTarget=false;
        t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Truncate;t.verticalAlignment=VerticalAlignmentOptions.Middle;return t;
    }
    public static RiftButton Button(Transform parent,string name,string label,float x,float y,float w,float h,UnityAction action=null,bool gold=false,bool danger=false)
    {
        var r=Rect(parent,name,x,y,w,h);var bg=r.gameObject.AddComponent<RiftGraphic>();bg.Form=RiftGraphic.Shape.Panel;bg.color=Theme.PanelBackground;
        var button=r.gameObject.AddComponent<RiftButton>();button.Theme=Theme;button.Gold=gold;button.Danger=danger;button.targetGraphic=bg;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.8f,1.8f,2,1);colors.selectedColor=colors.highlightedColor;
        colors.pressedColor=new Color(1.3f,1.3f,1.7f,1);colors.disabledColor=new Color(.5f,.5f,.5f,.45f);colors.fadeDuration=.12f;button.colors=colors;
        button.Edge=Image(r,"Accent",0,5,3,h-10,gold?Theme.SecondaryAccent:danger?Theme.DangerColor:Theme.PrimaryAccent);
        button.Label=Text(r,"Label",label,24,0,w-56,h,24);
        Text(r,"Arrow",">",w-32,0,22,h,22,Theme.TextSecondary);
        if(action!=null) UnityEventTools.AddPersistentListener(button.onClick,action);
        return button;
    }
    public static GameObject Canvas(string name,int order=0)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.layer=5;
        var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=order;
        var sc=go.GetComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(1920,1080);sc.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;sc.matchWidthOrHeight=.5f;
        return go;
    }
    static void EventSystem()
    {
        if(Object.FindAnyObjectByType<EventSystem>()!=null) return;
        var go=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }
    public static PanelTransition Panel(Transform parent,string name,bool dark=true)
    {
        var r=Stretch(parent,name);var cg=r.gameObject.AddComponent<CanvasGroup>();cg.alpha=0;
        if(dark){var bg=r.gameObject.AddComponent<Image>();bg.color=new Color(.015f,.02f,.045f,.92f);bg.raycastTarget=true;}
        var p=r.gameObject.AddComponent<PanelTransition>();r.gameObject.SetActive(false);return p;
    }
    public static RectTransform Card(Transform parent,string name,float w,float h)
    {
        var r=Rect(parent,name,-w/2,-h/2,w,h,new Vector2(.5f,.5f));
        var bg=r.gameObject.AddComponent<RiftGraphic>();bg.Form=RiftGraphic.Shape.Panel;bg.color=Theme.PanelBackground;
        Image(r,"TopAccent",0,0,w,2,Theme.PrimaryAccent);return r;
    }
    [MenuItem("Campus Rift/UI/Phase 2 - Build Main Menu")]
    public static void BuildMainMenu()
    {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        Prepare();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var cam=new GameObject("Menu Camera",typeof(Camera),typeof(AudioListener));cam.tag="MainCamera";
        cam.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;cam.GetComponent<Camera>().backgroundColor=Theme.PrimaryBackground;cam.GetComponent<Camera>().cullingMask=0;
        var canvas=Canvas("Main UI Canvas");EventSystem();
        var manager=canvas.AddComponent<UIManager>();
        var bg=Stretch(canvas.transform,"Campus Background").gameObject.AddComponent<RawImage>();
        bg.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/CampusBackdrop.png");bg.color=new Color(.22f,.25f,.4f,1);bg.raycastTarget=false;
        var wash=Stretch(canvas.transform,"Night Overlay").gameObject.AddComponent<Image>();wash.color=new Color(.015f,.02f,.05f,.63f);wash.raycastTarget=false;
        var art=Rect(canvas.transform,"Rift Emblem",-860,-410,760,820,new Vector2(1,.5f));
        Shape(art,"Outer Orbit",40,70,680,680,new Color(.4f,.45f,.9f,.16f),RiftGraphic.Shape.Ring);
        Shape(art,"Inner Orbit",95,125,570,570,new Color(.45f,.5f,.95f,.12f),RiftGraphic.Shape.Ring);
        Shape(art,"Rift Shadow",195,25,400,780,new Color(.2f,.12f,.45f,.55f),RiftGraphic.Shape.Slash);
        Shape(art,"Rift Core",295,65,210,690,new Color(.57f,.42f,1,.45f),RiftGraphic.Shape.Slash);
        Shape(art,"Rift Light",370,100,52,610,new Color(.48f,.72f,1,.65f),RiftGraphic.Shape.Slash);
        Text(art,"Coordinate","R I F T   /   0 0 1",390,750,360,35,18,Theme.TextSecondary);
        manager.MainMenu=Panel(canvas.transform,"MainMenu",false);var menu=manager.MainMenu.transform;
        Text(menu,"Eyebrow","C A M P U S   /   A N O M A L Y   D I V I S I O N",120,103,690,32,18,Theme.BlueAccent);
        Text(menu,"Title","CAMPUS",110,165,810,125,108).fontStyle=FontStyles.Bold;
        Text(menu,"Rift Title","RIFT",114,277,650,112,94,Theme.PrimaryAccent).fontStyle=FontStyles.Bold;
        Text(menu,"Subtitle","LEARN  •  BREAKTHROUGH  •  SURVIVE",120,407,650,35,21,Theme.TextSecondary);
        var buttons=Rect(menu,"Navigation",120,490,410,442);
        manager.MainMenu.FirstSelection=Button(buttons,"PLAY","PLAY",0,0,410,62,manager.Play);
        var resume=Button(buttons,"CONTINUE","CONTINUE",0,76,410,52,manager.Continue);resume.interactable=false;
        Text(buttons,"NoSave","NO SAVE",256,76,120,52,14,Theme.TextSecondary);
        Button(buttons,"COURSES","COURSES",0,140,410,52,manager.Courses);
        Button(buttons,"SETTINGS","SETTINGS",0,204,410,52,manager.OpenSettings);
        Button(buttons,"CREDITS","CREDITS",0,268,410,52,manager.OpenCredits);
        Button(buttons,"QUIT","QUIT",0,332,410,52,manager.Quit,false,true);
        var foot=Rect(canvas.transform,"Footer",120,-62,900,40,new Vector2(0,0));
        Text(foot,"Text","CAMPUS RIFT     /     THE WORLD BEYOND THE CLASSROOM",0,0,900,40,16,Theme.TextSecondary);
        BuildCourseAndCredits(canvas.transform,manager);
        manager.Settings=Panel(canvas.transform,"Settings");
        var settingsCard=Card(manager.Settings.transform,"Settings Card",1020,800);
        Text(settingsCard,"Title","SETTINGS",48,32,600,65,48);
        manager.Settings.FirstSelection=Button(settingsCard,"BACK","BACK",48,688,280,60,manager.Back);
        BuildLearningStructure(canvas.transform);
        EditorSceneManager.SaveScene(scene,MainPath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(MainPath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=MainPath)).ToArray();
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(MainPath);
        BuildLoadingAndServices();BuildBasePrefabs();AssetDatabase.SaveAssets();
        Debug.Log("CAMPUS RIFT: Main Menu and native UI theme created.");
    }
    static void BuildCourseAndCredits(Transform canvas,UIManager manager)
    {
        manager.Course=Panel(canvas,"CoursePlaceholderPanel");var card=Card(manager.Course.transform,"Course Card",1060,680);
        Text(card,"Eyebrow","LEARNING ARCHIVE",48,34,700,32,18,Theme.PrimaryAccent);
        Text(card,"Title","COURSES",48,89,850,76,58);
        Text(card,"Description","Learning system coming soon.",48,184,880,62,28,Theme.TextSecondary);
        Image(card,"Rule",48,285,964,1,new Color(.5f,.5f,.8f,.25f));
        Text(card,"Future","LEARN  /  TEST YOUR KNOWLEDGE  /  BREAK THROUGH",48,325,920,60,20,Theme.SecondaryAccent);
        Text(card,"Note","A new path through the rift awaits.",48,410,920,52,24,Theme.TextSecondary);
        manager.Course.FirstSelection=Button(card,"BACK","BACK",48,550,280,62,manager.Back);
        manager.Credits=Panel(canvas,"CreditsPanel");card=Card(manager.Credits.transform,"Credits Card",1060,840);
        Text(card,"Title","CREDITS",48,36,860,76,54);
        var viewport=Rect(card,"Viewport",48,150,964,520);viewport.gameObject.AddComponent<RectMask2D>();
        var hit=viewport.gameObject.AddComponent<Image>();hit.color=new Color(0,0,0,.01f);
        var content=Rect(viewport,"Content",0,0,940,760);
        string[] titles={"GAME DEVELOPMENT","3D ASSETS","AUDIO","TOOLS","SPECIAL THANKS"};
        for(int i=0;i<titles.Length;i++) {Text(content,"Heading "+i,titles[i],0,i*148,880,35,22,Theme.PrimaryAccent);Text(content,"Attribution "+i,"Attribution / license details to be added.",0,i*148+45,880,50,23,Theme.TextSecondary);}
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.content=content;scroll.viewport=viewport;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=36;
        Text(card,"Scroll Hint","SCROLL TO VIEW ALL CREDITS",48,675,700,30,15,Theme.TextSecondary);
        manager.Credits.FirstSelection=Button(card,"BACK","BACK",48,730,280,62,manager.Back);
    }
    static void BuildLearningStructure(Transform parent)
    {
        var root=Stretch(parent,"LearningUI");
        foreach(var name in new[]{"CourseListPanel","LessonPanel","QuizPanel","ResultPanel","BreakthroughPanel","SkillUnlockPanel"}) Stretch(root,name).gameObject.SetActive(false);
        root.gameObject.SetActive(false);
    }
    static void BuildLoadingAndServices()
    {
        var canvas=Canvas("LoadingScreen",1000);var bg=Stretch(canvas.transform,"Background").gameObject.AddComponent<Image>();bg.color=Theme.PrimaryBackground;
        var card=Rect(canvas.transform,"Content",-400,-160,800,320,new Vector2(.5f,.5f));
        Text(card,"Title","CAMPUS RIFT",0,0,800,100,70).alignment=TextAlignmentOptions.Center;
        Text(card,"Loading","ENTERING THE RIFT",0,136,800,45,20,Theme.TextSecondary).alignment=TextAlignmentOptions.Center;
        Image(card,"Track",60,230,680,5,Theme.SecondaryBackground);
        var fill=Image(card,"Fill",60,230,680,5,Theme.PrimaryAccent);fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
        // A white sprite is assigned below so Filled has a proper mesh.
        fill.sprite=WhiteSprite();
        var loading=canvas.AddComponent<LoadingScreenUI>();loading.Fill=fill;loading.Progress=Text(card,"Progress","00%",60,250,680,36,18,Theme.TextSecondary);loading.Progress.alignment=TextAlignmentOptions.Right;
        var loadingPrefab=PrefabUtility.SaveAsPrefabAsset(canvas,Root+"/Prefabs/LoadingScreen.prefab").GetComponent<LoadingScreenUI>();Object.DestroyImmediate(canvas);
        var config=AssetDatabase.LoadAssetAtPath<SceneFlowConfig>(Root+"/SceneFlow.asset");
        if(config==null){config=ScriptableObject.CreateInstance<SceneFlowConfig>();AssetDatabase.CreateAsset(config,Root+"/SceneFlow.asset");}
        config.MainMenuScene=MainPath;config.GameplayScene=GamePath;config.LoadingPrefab=loadingPrefab;EditorUtility.SetDirty(config);
        var services=new GameObject("Campus Rift Services");services.AddComponent<UIServices>();services.AddComponent<UIStateManager>();services.AddComponent<GameSceneManager>().Config=config;services.AddComponent<UIAudioManager>();
        PrefabUtility.SaveAsPrefabAsset(services,Root+"/Resources/CampusRiftServices.prefab");Object.DestroyImmediate(services);
    }
    public static Sprite WhiteSprite()
    {
        var path=Root+"/Art/White.png";
        if(!File.Exists(path)) {Directory.CreateDirectory(Root+"/Art");var tex=new Texture2D(2,2);tex.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);}
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(importer.textureType!=TextureImporterType.Sprite) {importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();}
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void BuildBasePrefabs()
    {
        var root=Rect(null,"Prefab Authoring",0,0,600,400);
        var button=Button(root,"Button_Primary","ACTION",0,0,360,64);SaveComponent(button.gameObject,"Button_Primary");
        button=Button(root,"Button_Secondary","BACK",0,0,300,56);SaveComponent(button.gameObject,"Button_Secondary");
        var panel=Card(root,"Panel_Dark",600,400);SaveComponent(panel.gameObject,"Panel_Dark");
        panel=Card(root,"ModalPanel",680,420);Text(panel,"Title","TITLE",36,26,608,64,40);Text(panel,"Body","Message",36,112,608,150,24,Theme.TextSecondary);Button(panel,"Close","BACK",36,310,260,58);SaveComponent(panel.gameObject,"ModalPanel");
        Object.DestroyImmediate(root.gameObject);
    }
    public static void SaveComponent(GameObject go,string name) {PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+name+".prefab");Object.DestroyImmediate(go);}
}
