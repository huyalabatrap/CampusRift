using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class ComicUIBuilder
{
    const string Art = "Assets/CampusRiftUI/Comic/Resources/Comic/";
    [MenuItem("Campus Rift/UI/Apply Comic Theme")]
    public static string Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode before authoring scenes.");
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Art, "*.png"))
        {
            var importer = AssetImporter.GetAtPath(path.Replace('\\','/')) as TextureImporter;
            if (importer == null) continue;
            string id = Path.GetFileNameWithoutExtension(path);
            if (id.StartsWith("sky-")) continue;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = id == "round-mask" ? new Vector4(16,16,16,16) : id.StartsWith("round-") || id == "panel" || id == "paper" || id == "selected" || id.StartsWith("button-") || id == "currency" || id == "rarity" ? new Vector4(28,28,28,28) : Vector4.zero;
            importer.SaveAndReimport();
        }
        CreateFont();
        InstallInkFeature();
        var localization = AssetDatabase.LoadAssetAtPath<CampusRift.Localization.LocalizationCatalog>("Assets/Localization/Resources/LocalizationCatalog.asset");
        if (localization != null) { localization.vietnameseFont = ComicTheme.Font; EditorUtility.SetDirty(localization); }
        var theme = AssetDatabase.LoadAssetAtPath<CampusRiftUITheme>("Assets/CampusRiftUI/CampusRiftUITheme.asset");
        ComicTheme.Palette(theme); EditorUtility.SetDirty(theme);
        string original = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        foreach (string path in new [] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SampleScene.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path);
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)) Style(canvas.transform);
            var sky = AssetDatabase.LoadAssetAtPath<Material>(Art + "sky-dusk.mat");
            if (path.EndsWith("SampleScene.unity") && sky != null) RenderSettings.skybox = sky;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        foreach (string path in Directory.GetFiles("Assets/CampusRiftUI/Prefabs", "*.prefab"))
        {
            string normalized = path.Replace('\\','/'); var root = PrefabUtility.LoadPrefabContents(normalized);
            Style(root.transform); PrefabUtility.SaveAsPrefabAsset(root, normalized); PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
        return "Comic theme applied to 2 scenes and UI prefabs; Vietnamese font validated.";
    }
    static void CreateFont()
    {
        string path = Art + "ComicVietnamese.asset";
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/CampusRiftUI/Fonts/BeVietnamPro-Medium.ttf");
            font = TMP_FontAsset.CreateFontAsset(source, 90, 12, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.name = "Comic Vietnamese"; AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
        }
        string characters = new string(Enumerable.Range(32,224).Concat(Enumerable.Range(0x1e00,256)).Concat(new [] {0x102,0x103,0x110,0x111,0x128,0x129,0x168,0x169,0x1a0,0x1a1,0x1af,0x1b0,0x2022,0x2026,0x2014,0x2013,0x2192,0x00d7}).Select(c=>(char)c).ToArray());
        string missing; font.TryAddCharacters(characters, out missing);
        string vietnamese = "ĐAN CÁC HỒI PHỤC TĂNG SỨC MẠNH PHÁP BẢO VẬT PHẨM CẤP CAO LINH THẠCH QUAY LẠI VỀ HUB ĐẾN KHÓA HỌC ăâđêôơư ĂÂĐÊÔƠƯ ẮẰẲẴẶ ẤẦẨẪẬ ẾỀỂỄỆ ỐỒỔỖỘ ỚỜỞỠỢ ỨỪỬỮỰ";
        uint[] missingVietnamese;
        if (!font.HasCharacters(vietnamese, out missingVietnamese, true, true)) throw new Exception("Missing Vietnamese glyphs: " + string.Join(",", missingVietnamese));
        string matPath = Art + "ComicHeading.mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(font.material); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetColor("_OutlineColor", Color.black); mat.SetFloat("_OutlineWidth", .28f);
        mat.EnableKeyword("UNDERLAY_ON"); mat.SetColor("_UnderlayColor", Color.black);
        mat.SetFloat("_UnderlayOffsetX", 1f); mat.SetFloat("_UnderlayOffsetY", -1f); mat.SetFloat("_UnderlaySoftness", 0);
        EditorUtility.SetDirty(mat); EditorUtility.SetDirty(font); AssetDatabase.SaveAssets();
    }
    static void Style(Transform root)
    {
        foreach (var panel in root.GetComponentsInChildren<PanelTransition>(true)) DesignFrame(panel.transform as RectTransform);
        foreach (var hud in root.GetComponentsInChildren<GameplayHUD>(true)) DesignFrame(hud.transform as RectTransform);
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            bool heading = text.fontSize >= 34 || (text.text.Length < 64 && text.text.Any(char.IsLetter) && text.text == text.text.ToUpperInvariant());
            ComicTheme.Text(text, heading);
            if (heading) text.color = ComicTheme.Gold;
            text.enableAutoSizing = true; text.fontSizeMax = text.fontSize; text.fontSizeMin = Mathf.Min(text.fontSizeMax * .55f, 18);
            EditorUtility.SetDirty(text);
        }
        foreach(var hud in root.GetComponentsInChildren<GameplayHUD>(true))
            foreach(var text in hud.GetComponentsInChildren<TMP_Text>(true))
                if(text.name.IndexOf("Instruction",StringComparison.OrdinalIgnoreCase)>=0 || text.name.IndexOf("Hint",StringComparison.OrdinalIgnoreCase)>=0 || text.text.Contains("RIFT SKILLS") || text.text.Contains("KỸ NĂNG KHE NỨT") || text.text.Contains("WASD"))ComicTheme.ReadabilityPlate(text);
        foreach(var hud in root.GetComponentsInChildren<GameplayHUD>(true))
        {
            if(hud.Health!=null){ComicTheme.ClipBar(hud.Health.Fill);ComicTheme.ClipBar(hud.Health.DelayedFill);}
            var vitals=hud.transform.Find("Vitals");
            if(vitals!=null)
                foreach(var name in new[]{"HealthTrack","EnergyTrack"})
                {var track=vitals.Find(name);if(track!=null){var image=track.GetComponent<Image>();if(image!=null){image.sprite=ComicTheme.Sprite("round-mask");image.type=Image.Type.Sliced;}}}
            var wall=hud.GetComponent<CampusRift.Skills.VoidWallHUD>();if(wall!=null){SkillArt(wall.slot,"VoidWall");if(wall.glyph!=null&&wall.glyph!=wall.slot.Icon)wall.glyph.gameObject.SetActive(false);}
            var hand=hud.GetComponent<CampusRift.Skills.GiantHandHUD>();if(hand!=null){SkillArt(hand.slot,"GiantHandSeal");if(hand.glyph!=null&&hand.glyph!=hand.slot.Icon)hand.glyph.gameObject.SetActive(false);}
            var phantom=hud.GetComponent<CampusRift.Skills.PhantomDecoyHUD>();if(phantom!=null){SkillArt(phantom.slot,"PhantomDecoy");if(phantom.glyph!=null&&phantom.glyph!=phantom.slot.Icon)phantom.glyph.gameObject.SetActive(false);}
            if(wall!=null)HudHint(wall.instruction,118);
            if(hand!=null)HudHint(hand.instruction,166);
            if(phantom!=null)HudHint(phantom.instruction,214);
            if(hud.Skills!=null)
                foreach(var title in hud.Skills.GetComponentsInChildren<TMP_Text>(true))
                    if(title.GetComponentInParent<SkillSlotUI>()==null)
                    {
                        var r=title.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(0,28);r.sizeDelta=new Vector2(560,34);
                        title.fontSize=18;title.fontSizeMax=18;title.fontSizeMin=16;title.enableAutoSizing=true;title.alignment=TextAlignmentOptions.Center;
                        ComicTheme.ReadabilityPlate(title);title.GetComponent<ComicTextPlate>().Refresh();
                    }
        }
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            string sprite = image.sprite != null ? image.sprite.name : "";
            string name = image.name;
            if (sprite == "PanelFrame" || sprite == "ButtonFrame" || sprite == "KeyBadge" || name.EndsWith(" Card") || name.EndsWith("Plate")) ComicTheme.Frame(image.gameObject, "panel", image.raycastTarget);
            if(sprite=="panel"||sprite=="paper"||sprite=="selected"||sprite=="currency")ComicTheme.Frame(image.gameObject,sprite,image.raycastTarget);
            if(name.EndsWith("Dark Plate")){image.sprite=ComicTheme.Sprite("round-mask");image.type=Image.Type.Sliced;image.color=new Color(.035f,.065f,.10f,.91f);}
            if (sprite == "Gem") { image.sprite = ComicTheme.Sprite("crystal"); image.color = Color.white; }
            if (sprite == "SliderFill") image.color = ComicTheme.Gold;
            if (sprite == "ToggleCheck") image.color = ComicTheme.Green;
            if (sprite == "ButtonGlow" || sprite == "RadialGlow" || name == "Motes" || name == "Side Shade") image.gameObject.SetActive(false);
            EditorUtility.SetDirty(image);
        }
        foreach (var result in root.GetComponentsInChildren<LevelResultUI>(true))
        {
            var card = result.Stats.transform.parent as RectTransform;
            card.sizeDelta = new Vector2(850,700);
            result.Stats.rectTransform.anchoredPosition = new Vector2(40,-232);
            result.Stats.rectTransform.sizeDelta = new Vector2(770,260);
            result.Stats.fontStyle = FontStyles.Normal;
            result.Stats.fontSharedMaterial = ComicTheme.Font.material;
            foreach(var star in result.Stars) if(star != null) { var pos=star.rectTransform.anchoredPosition; pos.y=-518;star.rectTransform.anchoredPosition=pos; }
            foreach(var name in new[]{"REPLAY","NEXT LEVEL","MAIN MENU"})
            { var t=card.Find(name) as RectTransform; if(t!=null) {var pos=t.anchoredPosition;pos.y=-568;t.anchoredPosition=pos;t.sizeDelta=new Vector2(t.sizeDelta.x,78);} }
        }
        foreach (var button in root.GetComponentsInChildren<RiftButton>(true)) { ComicTheme.Button(button); EditorUtility.SetDirty(button); }
        foreach (var slot in root.GetComponentsInChildren<SkillSlotUI>(true)) { ComicSkillLayout.Apply(slot); EditorUtility.SetDirty(slot); }
        foreach (var graphic in root.GetComponentsInChildren<RiftGraphic>(true))
            if (graphic.Form == RiftGraphic.Shape.Panel && graphic.GetComponent<RiftButton>() == null) { ComicTheme.Frame(graphic.gameObject, "panel", graphic.raycastTarget); EditorUtility.SetDirty(graphic); }
        var background = root.Find("Campus Background");
        if (background != null)
        {
            var raw = background.GetComponent<RawImage>();
            if (raw != null) { raw.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "academy-background.png"); raw.color = Color.white; EditorUtility.SetDirty(raw); }
            var ambience = root.GetComponent<MenuAmbience>(); if (ambience != null) ambience.enabled = false;
            var night = root.Find("Night Overlay"); if (night != null) night.GetComponent<Image>().color = new Color(0,0,0,.12f);
            Overlay(root);
        }
        var manager = root.GetComponent<UIManager>();
        if (manager != null && manager.MainMenu != null)
        {
            var title = manager.MainMenu.transform.Find("Title");
            if (title != null) Burst(title.GetComponent<RectTransform>());
            var rift = manager.MainMenu.transform.Find("Rift Title"); if (rift != null) rift.GetComponent<TMP_Text>().color = ComicTheme.Orange;
        }
    }
    static void DesignFrame(RectTransform r)
    {
        if (r == null) return;
        r.anchorMin = r.anchorMax = new Vector2(.5f,.5f); r.pivot = new Vector2(.5f,.5f); r.sizeDelta = new Vector2(1920,1080); r.anchoredPosition = Vector2.zero;
        var frames = r.GetComponents<FitFrame>();
        if (frames.Length == 0) r.gameObject.AddComponent<FitFrame>();
        else for (int i = 1; i < frames.Length; i++) Object.DestroyImmediate(frames[i]);
    }
    static void SkillArt(SkillSlotUI slot,string name)
    {
        if(slot==null||slot.Icon==null)return;
        slot.Icon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CampusRiftUI/Art/Skills/"+name+".png");slot.Icon.gameObject.SetActive(true);slot.Icon.enabled=true;slot.Icon.color=Color.white;
        ComicTheme.Frame(slot.gameObject);
    }
    static void HudHint(TMP_Text text,float height)
    {
        if(text==null)return;
        var r=text.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(-140,height);r.sizeDelta=new Vector2(760,40);
        text.fontSize=18;text.fontSizeMax=18;text.fontSizeMin=14;text.enableAutoSizing=true;text.alignment=TextAlignmentOptions.Center;
        ComicTheme.ReadabilityPlate(text);text.GetComponent<ComicTextPlate>().Refresh();
    }
    static void Overlay(Transform root)
    {
        if (root.Find("Comic Halftone") != null) return;
        var go = new GameObject("Comic Halftone", typeof(RectTransform), typeof(Image)); go.transform.SetParent(root,false);
        var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>(); image.sprite = ComicTheme.Sprite("halftone"); image.raycastTarget = false;
        go.transform.SetSiblingIndex(2);
    }
    static void Burst(RectTransform title)
    {
        if (title.parent.Find("Comic Logo Burst") != null) return;
        var go = new GameObject("Comic Logo Burst", typeof(RectTransform), typeof(Image)); go.transform.SetParent(title.parent,false);
        var r = (RectTransform)go.transform; r.anchorMin = title.anchorMin; r.anchorMax = title.anchorMax; r.pivot = title.pivot;
        r.anchoredPosition = title.anchoredPosition + new Vector2(-36,24); r.sizeDelta = new Vector2(770,285);
        var image = go.GetComponent<Image>(); image.sprite = ComicTheme.Sprite("burst"); image.color = new Color(1,1,1,.72f); image.raycastTarget=false;
        go.transform.SetSiblingIndex(title.GetSiblingIndex());
    }
    [MenuItem("Campus Rift/UI/Import Comic Skies")]
    public static string ImportSkies()
    {
        AssetDatabase.Refresh();
        foreach (string id in new [] { "dusk", "night", "blood", "inferno", "eclipse" })
        {
            string texturePath = Art + "sky-" + id + ".png";
            if (!File.Exists(texturePath)) continue;
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = true; importer.wrapModeU = TextureWrapMode.Repeat; importer.wrapModeV = TextureWrapMode.Clamp;
            importer.maxTextureSize=4096; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.crunchedCompression=false; importer.filterMode=FilterMode.Trilinear; importer.anisoLevel=2; importer.SaveAndReimport();
            string path=Art+"sky-"+id+".mat"; var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null) {mat=new Material(Shader.Find("Skybox/Panoramic"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); mat.SetFloat("_Exposure",1); mat.SetFloat("_Mapping",1); mat.SetColor("_Tint",Color.gray);
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets(); return "Imported comic panoramic sky materials.";
    }
    static void InstallInkFeature()
    {
        var shader=Shader.Find("Campus Rift/Comic Ink"); if(shader==null) throw new Exception("Comic Ink shader missing");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"ComicInk.mat");
        if(material==null) { material=new Material(shader);AssetDatabase.CreateAsset(material,Art+"ComicInk.mat"); }
        material.SetFloat("_InkStrength",.88f);EditorUtility.SetDirty(material);
        var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        var feature=data.rendererFeatures.OfType<ComicInkFeature>().FirstOrDefault();
        if(feature==null) { feature=ScriptableObject.CreateInstance<ComicInkFeature>();feature.name="Comic Ink";AssetDatabase.AddObjectToAsset(feature,data);data.rendererFeatures.Add(feature); }
        feature.Create();feature.SetActive(true);EditorUtility.SetDirty(feature);EditorUtility.SetDirty(data);
    }
}
