using System.Collections.Generic;
using System.Linq;
using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static partial class UIFoundationBuilder
{
    [MenuItem("Campus Rift/UI/Phase 8 - Finalize Foundation")]
    public static void Polish()
    {
        Prepare();
        // A dynamic TMP font supports future English/Vietnamese localization from the shipped font source.
        string fontPath=Root+"/CampusRiftFont.asset";
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if(font==null)
        {
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            font=TMP_FontAsset.CreateFontAsset(source,90,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            font.name="Campus Rift Sans";AssetDatabase.CreateAsset(font,fontPath);
            AssetDatabase.AddObjectToAsset(font.material,font);foreach(var tex in font.atlasTextures)AssetDatabase.AddObjectToAsset(tex,font);
        }
        Theme.Font=font;EditorUtility.SetDirty(Theme);
        foreach(var path in new[]{MainPath,GamePath})
        {
            EditorSceneManager.OpenScene(path);var ui=Object.FindAnyObjectByType<UIManager>();
            foreach(var text in ui.GetComponentsInChildren<TMP_Text>(true)) {text.font=font;EditorUtility.SetDirty(text);}
            foreach(var button in ui.GetComponentsInChildren<RiftButton>(true)) {button.Label=button.transform.Find("Label").GetComponent<TMP_Text>();EditorUtility.SetDirty(button);}
            var settings=ui.Settings.GetComponent<SettingsUI>();
            for(int i=0;i<3;i++)
            {
                var tab=ui.Settings.transform.Find("Settings Card/"+new[]{"VIDEO","AUDIO","GAMEPLAY"}[i]);
                var old=tab.Find("Active Tab");if(old!=null)Object.DestroyImmediate(old.gameObject);
                settings.TabLines[i]=Image(tab,"Active Tab",0,57,((RectTransform)tab).rect.width,2,Theme.DisabledColor);
            }
            if(path==MainPath)
            {
                var intro=ui.MainMenu.GetComponent<MenuIntro>();if(intro==null)intro=ui.MainMenu.gameObject.AddComponent<MenuIntro>();
                var elements=new List<CanvasGroup>();
                foreach(var name in new[]{"Eyebrow","Title","Rift Title","Subtitle","Navigation/PLAY","Navigation/CONTINUE","Navigation/COURSES","Navigation/SETTINGS","Navigation/CREDITS","Navigation/QUIT"})
                {var t=ui.MainMenu.transform.Find(name);if(t!=null){var g=t.GetComponent<CanvasGroup>();if(g==null)g=t.gameObject.AddComponent<CanvasGroup>();elements.Add(g);}}
                intro.Elements=elements.ToArray();EditorUtility.SetDirty(intro);
                // Shift the campus into the background's right half; keep the navigation silhouette clean.
                var bg=ui.transform.Find("Campus Background").GetComponent<RawImage>();bg.color=new Color(.3f,.33f,.5f,1);
            }
            else
            {
                var hud=ui.HUD.transform;
                foreach(var info in new[]{("Vitals",400f,170f),("Objective",390f,136f)})
                {
                    var node=hud.Find(info.Item1);var old=node.Find("Readability Plate");if(old!=null)Object.DestroyImmediate(old.gameObject);
                    var plate=Shape(node,"Readability Plate",-14,-8,info.Item2,info.Item3,new Color(.02f,.025f,.05f,.68f),RiftGraphic.Shape.Panel);plate.transform.SetAsFirstSibling();
                }
                var slots=hud.GetComponent<GameplayHUD>().Skills.Slots;
                slots[2].Tooltip.text="Thiên Thủ Trấn Áp\nBreak through 5 times to unlock.";
            }
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var go=PrefabUtility.LoadPrefabContents(path);
            foreach(var text in go.GetComponentsInChildren<TMP_Text>(true))text.font=font;
            foreach(var button in go.GetComponentsInChildren<RiftButton>(true))button.Label=button.transform.Find("Label").GetComponent<TMP_Text>();
            PrefabUtility.SaveAsPrefabAsset(go,path);PrefabUtility.UnloadPrefabContents(go);
        }
        EditorSceneManager.OpenScene(MainPath);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(MainPath);
        UIValidation.SetResolution(1920,1080);AssetDatabase.SaveAssets();Debug.Log("CAMPUS RIFT: Foundation finalized. MainMenu is launch scene.");
    }
}
