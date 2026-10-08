using System;
using System.Linq;
using CampusRift;
using CampusRift.Controls;
using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using Object=UnityEngine.Object;

public static partial class UIFoundationBuilder
{
    [MenuItem("Campus Rift/Controls/Install PC and Mobile Controls")]
    public static void InstallMobileControls()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");Prepare();
        UnityEngine.InputSystem.InputSettings inputSettings;
        if(!EditorBuildSettings.TryGetConfigObject("com.unity.input.settings",out inputSettings)||inputSettings==null)
        {
            const string inputPath="Assets/Controls/CampusInputSettings.asset";
            inputSettings=AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputSettings>(inputPath);
            if(inputSettings==null){inputSettings=ScriptableObject.CreateInstance<UnityEngine.InputSystem.InputSettings>();AssetDatabase.CreateAsset(inputSettings,inputPath);}
            EditorBuildSettings.AddConfigObject("com.unity.input.settings",inputSettings,true);
        }
        UnityEngine.InputSystem.InputSystem.settings=inputSettings;
        foreach(var path in new[]{MainPath,GamePath})
        {
            EditorSceneManager.OpenScene(path);
            var settings=Object.FindObjectsByType<SettingsUI>(FindObjectsInactive.Include).First();InstallControlsSettings(settings);
            settings.PCModeLabel.fontSize=19;settings.MobileModeLabel.fontSize=19;
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
            {
                if(text.text!="ESC  /  RESUME"&&text.text!="ESC / RESUME")continue;
                var hint=text.GetComponent<ControlHintText>()??text.gameObject.AddComponent<ControlHintText>();hint.PCText=text.text;hint.MobileText="TAP RESUME TO CONTINUE";EditorUtility.SetDirty(hint);
            }
            foreach(var module in Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include))
            {module.pointerBehavior=UIPointerBehavior.AllPointersAsIs;EditorUtility.SetDirty(module);}
            var player=Object.FindAnyObjectByType<CampusExplorer>();
            if(player!=null)
            {
                if(player.GetComponent<CampusInput>()==null)player.gameObject.AddComponent<CampusInput>();
                if(player.GetComponent<ContextInteraction>()==null)player.gameObject.AddComponent<ContextInteraction>();
                var hud=Object.FindAnyObjectByType<GameplayHUD>();if(hud.GetComponent<MobileControlsHUD>()==null)hud.gameObject.AddComponent<MobileControlsHUD>();
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        }
        const string prefabPath="Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        if(prefab.GetComponent<CampusInput>()==null)prefab.AddComponent<CampusInput>();
        if(prefab.GetComponent<ContextInteraction>()==null)prefab.AddComponent<ContextInteraction>();
        PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);PrefabUtility.UnloadPrefabContents(prefab);
        ConfigureMobileDisplay();
        AssetDatabase.SaveAssets();Debug.Log("Campus Rift: shared PC/Mobile input, touch HUD and Controls settings installed.");
    }
    public static void ConfigureMobileDisplay()
    {
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
        PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
        PlayerSettings.vulkanEnablePreTransform=false;
    }
    static void InstallControlsSettings(SettingsUI controller)
    {
        if(controller.MobileOptions!=null)return;
        var game=controller.Tabs[2].transform;
        foreach(var text in controller.TabLines[2].transform.parent.GetComponentsInChildren<TMP_Text>())if(text.text=="GAMEPLAY")text.text="CONTROLS";
        var pc=Rect(game,"PC Options",0,70,1024,345);controller.PCOptions=pc.gameObject;
        var existing=game.Cast<Transform>().Where(t=>t!=pc).ToArray();foreach(var child in existing)child.SetParent(pc,false);
        var note=pc.Find("Note");if(note!=null)note.gameObject.SetActive(false);
        Text(game,"Control Mode Label","CONTROL MODE",0,0,460,52,22,Theme.TextSecondary);
        var pcButton=Button(game,"PC Mode","PC",500,0,235,56,controller.ChoosePC);controller.PCModeLabel=pcButton.Label;
        var mobileButton=Button(game,"Mobile Mode","MOBILE",765,0,235,56,controller.ChooseMobile);controller.MobileModeLabel=mobileButton.Label;
        var mobile=Rect(game,"Mobile Options",0,70,1024,345);controller.MobileOptions=mobile.gameObject;
        controller.TouchSensitivity=Slider(mobile,"TOUCH SENSITIVITY",0,.3f,2.5f,out controller.TouchValue);
        controller.MobileScale=Slider(mobile,"BUTTON SCALE",65,.85f,1.2f,out controller.ScaleValue);
        controller.MobileOpacity=Slider(mobile,"BUTTON OPACITY",130,.35f,1,out controller.OpacityValue);
        controller.AutoSprint=Toggle(mobile,"JOYSTICK OUTER EDGE SPRINT",0,195);controller.MobileHaptics=Toggle(mobile,"TOUCH HAPTICS (ANDROID)",0,260);
        controller.BoostToggle=Toggle(mobile,"TOGGLE BOOST",520,195);
        mobile.gameObject.SetActive(false);EditorUtility.SetDirty(controller);
    }
}
