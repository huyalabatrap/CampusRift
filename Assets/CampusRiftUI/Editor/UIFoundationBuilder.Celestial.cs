using System.Collections.Generic;
using System.Linq;
using CampusRift.Skills;
using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using UImage=UnityEngine.UI.Image;

// Celestial restyle: textured 9-slice frames, Cinzel / Be Vietnam Pro typography and the skill key art.
// Idempotent: it restyles the existing hierarchies in place, so scene edits and references survive.
public static partial class UIFoundationBuilder
{
    const string CelestialArt=Root+"/Art/Celestial/", SkillArt=Root+"/Art/Skills/", FontDir=Root+"/Fonts/";
    static TMP_FontAsset display, displaySemi, body;
    static Material titleGlow, logoShadow;
    static readonly Color Parchment=new Color(.95f,.92f,.86f,1), Muted=new Color(.70f,.66f,.80f,1), GoldText=new Color(.93f,.77f,.47f,1);
    static readonly VertexGradient GoldGradient=new VertexGradient(new Color(1,.96f,.84f),new Color(1,.96f,.84f),new Color(.86f,.64f,.34f),new Color(.86f,.64f,.34f));
    static readonly VertexGradient RiftGradient=new VertexGradient(new Color(.92f,.86f,1),new Color(.92f,.86f,1),new Color(.55f,.38f,1),new Color(.55f,.38f,1));
    static readonly VertexGradient BloodGradient=new VertexGradient(new Color(1,.8f,.76f),new Color(1,.8f,.76f),new Color(.78f,.2f,.28f),new Color(.78f,.2f,.28f));

    static T Get<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
    static Sprite Art(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(CelestialArt+name+".png");
    static Sprite SkillIcon(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(SkillArt+name+".png");

    [MenuItem("Campus Rift/UI/Restyle - Celestial Theme")]
    public static void RestyleCelestial()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode first.");
        Prepare();ImportCelestialArt();CreateCelestialFonts();ApplyCelestialPalette();
        var original=EditorSceneManager.GetActiveScene().path;
        foreach(var path in new[]{MainPath,GamePath})
        {
            var scene=EditorSceneManager.OpenScene(path);var ui=Object.FindAnyObjectByType<UIManager>();
            RestyleCommon(ui.transform);
            if(path==MainPath)RestyleMainMenu(ui);else RestyleGameplay(ui);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        RestylePrefabs();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(string.IsNullOrEmpty(original)?MainPath:original);
        Debug.Log("CAMPUS RIFT: Celestial UI theme applied.");
    }

    // ------------------------------------------------------------------ assets
    static void ImportCelestialArt()
    {
        var borders=new Dictionary<string,Vector4>{
            {"ButtonFrame",new Vector4(34,34,34,34)},{"ButtonPrimary",new Vector4(34,34,34,34)},{"ButtonGlow",new Vector4(58,58,58,58)},
            {"PanelFrame",new Vector4(96,96,96,96)},{"KeyBadge",new Vector4(16,16,16,16)},
            {"SliderTrack",new Vector4(12,0,12,0)},{"SliderFill",new Vector4(12,0,12,0)}};
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Art/Celestial",Root+"/Art/Skills"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=100;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.spriteBorder=borders.TryGetValue(System.IO.Path.GetFileNameWithoutExtension(path),out var border)?border:Vector4.zero;
            importer.SaveAndReimport();
        }
        var backdrop=(TextureImporter)AssetImporter.GetAtPath(Root+"/Art/MenuBackdrop.png");
        backdrop.textureType=TextureImporterType.Default;backdrop.mipmapEnabled=false;backdrop.wrapMode=TextureWrapMode.Clamp;
        backdrop.npotScale=TextureImporterNPOTScale.None;backdrop.maxTextureSize=2048;backdrop.textureCompression=TextureImporterCompression.CompressedHQ;backdrop.SaveAndReimport();
    }
    static TMP_FontAsset FontAsset(string ttf,string asset,string name)
    {
        var path=FontDir+asset+".asset";var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(font!=null)return font;
        var source=AssetDatabase.LoadAssetAtPath<Font>(FontDir+ttf);
        font=TMP_FontAsset.CreateFontAsset(source,90,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
        font.name=name;AssetDatabase.CreateAsset(font,path);
        font.material.name=name+" Material";AssetDatabase.AddObjectToAsset(font.material,font);
        foreach(var tex in font.atlasTextures){tex.name=name+" Atlas";AssetDatabase.AddObjectToAsset(tex,font);}
        return font;
    }
    static Material FontMaterial(TMP_FontAsset font,string name,Color underlay,float softness,float dilate,float offsetY)
    {
        var path=FontDir+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(font.material){name=name};AssetDatabase.CreateAsset(mat,path);}
        mat.shader=font.material.shader;mat.SetTexture(ShaderUtilities.ID_MainTex,font.material.GetTexture(ShaderUtilities.ID_MainTex));
        mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        mat.SetColor(ShaderUtilities.ID_UnderlayColor,underlay);mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness,softness);
        mat.SetFloat(ShaderUtilities.ID_UnderlayDilate,dilate);mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY,offsetY);
        EditorUtility.SetDirty(mat);return mat;
    }
    static void CreateCelestialFonts()
    {
        var legacy=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/CampusRiftFont.asset");
        body=FontAsset("BeVietnamPro-Medium.ttf","CampusRiftBody","Campus Rift Body");
        displaySemi=FontAsset("Cinzel-SemiBold.ttf","CampusRiftDisplaySemiBold","Campus Rift Display SemiBold");
        display=FontAsset("Cinzel-ExtraBold.ttf","CampusRiftDisplay","Campus Rift Display");
        // Cinzel has no Vietnamese; Be Vietnam Pro supplies those glyphs, the legacy sans supplies any leftovers.
        body.fallbackFontAssetTable=legacy!=null?new List<TMP_FontAsset>{legacy}:new List<TMP_FontAsset>();
        displaySemi.fallbackFontAssetTable=new List<TMP_FontAsset>{body};display.fallbackFontAssetTable=new List<TMP_FontAsset>{body};
        foreach(var f in new[]{body,displaySemi,display})EditorUtility.SetDirty(f);
        titleGlow=FontMaterial(display,"Campus Rift Display - Glow",new Color(.5f,.3f,1,.55f),.75f,.25f,0);
        logoShadow=FontMaterial(display,"Campus Rift Display - Shadow",new Color(0,0,0,.7f),.45f,.1f,-.6f);
    }
    static void ApplyCelestialPalette()
    {
        var t=Theme;t.Font=body;
        t.PrimaryBackground=new Color(.03f,.024f,.06f,1);t.SecondaryBackground=new Color(.07f,.055f,.13f,1);t.PanelBackground=new Color(.055f,.045f,.11f,.94f);
        t.PrimaryAccent=new Color(.64f,.48f,1,1);t.BlueAccent=new Color(.5f,.68f,1,1);t.SecondaryAccent=new Color(.95f,.78f,.46f,1);
        t.DangerColor=new Color(1,.38f,.42f,1);t.SuccessColor=new Color(.5f,.92f,.82f,1);
        t.TextPrimary=Parchment;t.TextSecondary=Muted;t.DisabledColor=new Color(.42f,.4f,.5f,.6f);
        EditorUtility.SetDirty(t);
    }

    // ------------------------------------------------------------------ primitives
    static UImage SlicedImage(GameObject go,Sprite sprite,Color color,bool raycast=false)
    {
        foreach(var shape in go.GetComponents<RiftGraphic>())Object.DestroyImmediate(shape);
        var image=Get<UImage>(go);
        image.sprite=sprite;image.type=UImage.Type.Sliced;image.pixelsPerUnitMultiplier=2;image.fillCenter=true;
        image.color=color;image.raycastTarget=raycast;return image;
    }
    static UImage Child(Transform parent,string name,Sprite sprite,Color color)
    {
        var existing=parent.Find(name);
        var r=existing!=null?(RectTransform)existing:Rect(parent,name,0,0,10,10);
        var image=Get<UImage>(r.gameObject);
        image.sprite=sprite;image.color=color;image.raycastTarget=false;return image;
    }
    static void Place(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    static void PlaceCentered(RectTransform r,float cx,float cy,float w,float h)
    {r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(cx,-cy);r.sizeDelta=new Vector2(w,h);}
    static void Fill(RectTransform r,float inset=0)
    {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=new Vector2(-inset,-inset);r.offsetMax=new Vector2(inset,inset);}
    static void Remove(Transform parent,string name){var t=parent.Find(name);if(t!=null)Object.DestroyImmediate(t.gameObject);}
    static TMP_Text Find(Transform parent,string name)=>parent.Find(name)?.GetComponent<TMP_Text>();
    static void Style(TMP_Text text,TMP_FontAsset font,float size,Color color,float spacing=0,Material material=null)
    {
        if(text==null)return;
        text.font=font;text.fontSharedMaterial=material!=null?material:font.material;text.fontSize=size;text.color=color;
        text.characterSpacing=spacing;text.fontStyle=FontStyles.Normal;text.enableVertexGradient=false;
        // Cinzel's taller line box would make Truncate drop whole headline lines.
        if(font==display||text.overflowMode==TextOverflowModes.Truncate)text.overflowMode=TextOverflowModes.Overflow;
        EditorUtility.SetDirty(text);
    }
    static void Gradient(TMP_Text text,VertexGradient gradient){text.color=Color.white;text.enableVertexGradient=true;text.colorGradient=gradient;}
    static bool IsCaps(string s)=>!string.IsNullOrEmpty(s)&&s.Length<48&&s.Any(char.IsLetter)&&s==s.ToUpperInvariant();

    // ------------------------------------------------------------------ shared widgets
    static void RestyleCommon(Transform root)
    {
        // Typography first: small caps labels take Cinzel, sentences and values take Be Vietnam Pro.
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            bool caps=IsCaps(text.text);
            Style(text,caps?displaySemi:body,text.fontSize,RemapColor(text.color),caps?Mathf.Max(2,text.characterSpacing):0);
        }
        foreach(var button in root.GetComponentsInChildren<RiftButton>(true))StyleButton(button);
        foreach(var panel in root.GetComponentsInChildren<PanelTransition>(true))
        {
            var dim=panel.GetComponent<UImage>();if(dim!=null){dim.color=new Color(.012f,.008f,.03f,.84f);EditorUtility.SetDirty(dim);}
            foreach(Transform child in panel.transform)if(child.name.EndsWith(" Card"))StyleCard((RectTransform)child);
        }
        foreach(var settings in root.GetComponentsInChildren<SettingsUI>(true))StyleSettings(settings);
    }
    static Color RemapColor(Color c)
    {
        // Old cold palette -> warm celestial palette, keeping semantic accents.
        if(c.b>.95f&&c.r>.9f)return Parchment;
        if(Mathf.Abs(c.r-.61f)<.05f&&Mathf.Abs(c.b-.8f)<.06f)return Muted;
        if(Mathf.Abs(c.r-.57f)<.05f&&c.b>.95f)return new Color(.72f,.58f,1,c.a);
        if(Mathf.Abs(c.r-.27f)<.05f&&Mathf.Abs(c.g-.68f)<.05f)return new Color(.62f,.74f,1,c.a);
        if(c.r>.95f&&Mathf.Abs(c.g-.77f)<.05f)return GoldText;
        return c;
    }
    static bool IsTab(RiftButton b)=>b.name=="VIDEO"||b.name=="AUDIO"||b.name=="GAMEPLAY"||b.name=="PC Mode"||b.name=="Mobile Mode";
    static void StyleButton(RiftButton button,bool primary=false)
    {
        var r=(RectTransform)button.transform;float h=r.rect.height;bool tab=IsTab(button);
        primary|=button.name=="PLAY"||button.name=="RESUME"||button.name=="RETRY"||button.name=="PLAY AGAIN"||button.name=="APPLY";
        var frame=SlicedImage(button.gameObject,Art(primary?"ButtonPrimary":"ButtonFrame"),Color.white,true);
        button.targetGraphic=frame;button.transition=Selectable.Transition.ColorTint;
        var colors=button.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;
        colors.pressedColor=new Color(.8f,.78f,.9f,1);colors.disabledColor=new Color(.55f,.55f,.62f,.55f);colors.colorMultiplier=1;colors.fadeDuration=.1f;button.colors=colors;
        Remove(r,"Accent");Remove(r,"Arrow");button.Edge=null;button.Theme=Theme;
        var glow=Child(r,"Glow",Art("ButtonGlow"),new Color(1,1,1,0));glow.type=UImage.Type.Sliced;glow.pixelsPerUnitMultiplier=2;
        Fill(glow.rectTransform,12);glow.transform.SetSiblingIndex(0);button.Glow=glow;
        if(tab){Remove(r,"Gem L");Remove(r,"Gem R");button.Gems=new Graphic[0];}
        else
        {
            float gh=Mathf.Round(h*.62f),gw=Mathf.Round(gh*.75f);
            var left=Child(r,"Gem L",Art("Gem"),Color.white);var right=Child(r,"Gem R",Art("Gem"),Color.white);
            foreach(var gem in new[]{left,right}){var g=gem.rectTransform;g.anchorMin=g.anchorMax=new Vector2(gem==left?0:1,.5f);g.pivot=new Vector2(.5f,.5f);g.sizeDelta=new Vector2(gw,gh);g.anchoredPosition=Vector2.zero;}
            button.Gems=new Graphic[]{left,right};
        }
        var label=r.Find("Label").GetComponent<TMP_Text>();button.Label=label;
        Style(label,displaySemi,Mathf.Clamp(Mathf.Round(h*.36f),17,24),Parchment,tab?4:7);
        var lr=label.rectTransform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.pivot=new Vector2(.5f,.5f);lr.offsetMin=new Vector2(26,0);lr.offsetMax=new Vector2(-26,2);
        label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Ellipsis;
        label.transform.SetAsLastSibling();
        var active=r.Find("Active Tab");
        if(active!=null){var line=(RectTransform)active;line.anchorMin=new Vector2(.5f,0);line.anchorMax=new Vector2(.5f,0);line.pivot=new Vector2(.5f,.5f);line.sizeDelta=new Vector2(r.rect.width*.55f,3);line.anchoredPosition=new Vector2(0,5);line.SetAsLastSibling();}
        EditorUtility.SetDirty(button);
    }
    static void StyleCard(RectTransform card)
    {
        SlicedImage(card.gameObject,Art("PanelFrame"),Color.white,true);Remove(card,"TopAccent");
        var crest=Child(card,"Crest",Art("GemGlow"),Color.white);var c=crest.rectTransform;
        c.anchorMin=c.anchorMax=new Vector2(.5f,1);c.pivot=new Vector2(.5f,.5f);c.sizeDelta=new Vector2(64,80);c.anchoredPosition=new Vector2(0,2);crest.transform.SetAsLastSibling();
        foreach(var name in new[]{"Title","Heading"}){var t=Find(card,name);if(t!=null){Style(t,display,t.fontSize,Color.white,4);Gradient(t,GoldGradient);}}
        var eyebrow=Find(card,"Eyebrow");if(eyebrow!=null)Style(eyebrow,displaySemi,16,eyebrow.color,10);
    }
    static UImage Divider(Transform parent,string name,float cx,float y,float w)
    {var d=Child(parent,name,Art("Divider"),new Color(1,1,1,.9f));PlaceCentered(d.rectTransform,cx,y,w,w*48/1024f);return d;}
    static void CenterColumn(RectTransform card,string name,float y,float h,float size)
    {
        var t=Find(card,name);if(t==null)return;float w=card.rect.width;Place(t.rectTransform,40,y,w-80,h);
        t.alignment=TextAlignmentOptions.Center;t.fontSize=size;
    }
    static void StyleSettings(SettingsUI settings)
    {
        var card=(RectTransform)settings.transform.Find("Settings Card");
        foreach(var line in settings.TabLines)if(line!=null){line.sprite=null;EditorUtility.SetDirty(line);}
        Style(Find(card,"Heading"),display,50,Color.white,6);Gradient(Find(card,"Heading"),GoldGradient);
        Find(card,"Eyebrow").text="CAMPUS RIFT  ·  PREFERENCES";
        Divider(card,"Footer Rule",card.rect.width/2,724,card.rect.width-120);
        foreach(var dropdown in settings.GetComponentsInChildren<TMP_Dropdown>(true))
        {
            var bg=SlicedImage(dropdown.gameObject,Art("ButtonFrame"),Color.white,true);dropdown.targetGraphic=bg;
            var arrow=dropdown.transform.Find("Arrow");if(arrow!=null)arrow.gameObject.SetActive(false);
            var chevron=Child(dropdown.transform,"Chevron",Art("Chevron"),Color.white);var cr=chevron.rectTransform;
            cr.anchorMin=cr.anchorMax=new Vector2(1,.5f);cr.pivot=new Vector2(.5f,.5f);cr.sizeDelta=new Vector2(22,15);cr.anchoredPosition=new Vector2(-30,-1);
            Style(dropdown.captionText,body,22,Parchment);
            var template=dropdown.template;SlicedImage(template.gameObject,Art("ButtonFrame"),Color.white,true);
            var item=template.GetComponentInChildren<Toggle>(true);
            var itemBg=item.GetComponent<UImage>();itemBg.sprite=null;itemBg.color=new Color(1,1,1,0);
            var colors=item.colors;colors.normalColor=new Color(1,1,1,0);colors.highlightedColor=colors.selectedColor=new Color(.6f,.45f,1,.22f);colors.pressedColor=new Color(.6f,.45f,1,.35f);item.colors=colors;
            var mark=item.transform.Find("Selection").GetComponent<UImage>();mark.sprite=Art("Gem");mark.color=Color.white;mark.preserveAspect=true;
            var mr=mark.rectTransform;mr.anchorMin=mr.anchorMax=new Vector2(0,.5f);mr.pivot=new Vector2(.5f,.5f);mr.sizeDelta=new Vector2(14,20);mr.anchoredPosition=new Vector2(18,0);
            Style(dropdown.itemText,body,20,Parchment);var ir=dropdown.itemText.rectTransform;ir.anchoredPosition=new Vector2(38,ir.anchoredPosition.y);
            EditorUtility.SetDirty(dropdown);
        }
        foreach(var toggle in settings.GetComponentsInChildren<Toggle>(true))
        {
            var box=toggle.transform.Find("Box");if(box==null)continue;
            var boxImage=box.GetComponent<UImage>();boxImage.sprite=Art("ToggleBox");boxImage.color=Color.white;
            var br=(RectTransform)box;br.sizeDelta=new Vector2(44,44);br.anchoredPosition=new Vector2(br.anchoredPosition.x-4,-4);
            var check=box.Find("Check");if(check!=null)check.gameObject.SetActive(false);
            var mark=Child(box,"Mark",Art("ToggleCheck"),Color.white);Fill(mark.rectTransform);
            toggle.targetGraphic=boxImage;toggle.graphic=mark;EditorUtility.SetDirty(toggle);
        }
        foreach(var slider in settings.GetComponentsInChildren<Slider>(true))
        {
            var track=slider.transform.Find("Track").GetComponent<UImage>();track.sprite=Art("SliderTrack");track.type=UImage.Type.Sliced;track.pixelsPerUnitMultiplier=2;track.color=Color.white;
            var tr=track.rectTransform;tr.sizeDelta=new Vector2(tr.sizeDelta.x,14);tr.anchoredPosition=new Vector2(tr.anchoredPosition.x,-7);
            var area=(RectTransform)slider.transform.Find("Fill Area");area.sizeDelta=new Vector2(area.sizeDelta.x,10);area.anchoredPosition=new Vector2(area.anchoredPosition.x,-9);
            var fill=slider.fillRect.GetComponent<UImage>();fill.sprite=Art("SliderFill");fill.type=UImage.Type.Sliced;fill.pixelsPerUnitMultiplier=2;fill.color=Color.white;
            var handle=slider.handleRect.GetComponent<UImage>();handle.sprite=Art("Gem");handle.type=UImage.Type.Simple;handle.color=Color.white;handle.preserveAspect=true;
            slider.handleRect.sizeDelta=new Vector2(26,10);
            var colors=slider.colors;colors.normalColor=Color.white;colors.highlightedColor=colors.selectedColor=new Color(1,.95f,.85f,1);colors.pressedColor=new Color(.85f,.8f,.95f,1);slider.colors=colors;
            EditorUtility.SetDirty(slider);
        }
        foreach(var text in settings.GetComponentsInChildren<TMP_Text>(true))
        {
            if(text.GetComponentInParent<TMP_Dropdown>(true)!=null)continue;
            if(text.name.EndsWith(" Label")||(text.name=="Label"&&text.transform.parent.GetComponent<Toggle>()!=null))
                Style(text,displaySemi,18,Muted,4);
            else if(text.name.EndsWith(" Value"))Style(text,body,20,Parchment);
            else if(text.name=="Note")Style(text,body,17,Muted);
        }
        if(settings.Feedback!=null)Style(settings.Feedback,body,17,new Color(.55f,.92f,.82f,1));
        EditorUtility.SetDirty(settings);
    }

    // ------------------------------------------------------------------ main menu
    static void RestyleMainMenu(UIManager ui)
    {
        var canvas=ui.transform;
        var bg=canvas.Find("Campus Background").GetComponent<RawImage>();
        bg.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/MenuBackdrop.png");bg.color=Color.white;EditorUtility.SetDirty(bg);
        var night=canvas.Find("Night Overlay").GetComponent<UImage>();night.color=new Color(.03f,.01f,.07f,.18f);
        var shade=Child(canvas,"Side Shade",Art("SideShade"),new Color(1,1,1,.94f));var sr=shade.rectTransform;
        sr.anchorMin=new Vector2(0,0);sr.anchorMax=new Vector2(0,1);sr.pivot=new Vector2(0,.5f);sr.sizeDelta=new Vector2(1180,0);sr.anchoredPosition=Vector2.zero;
        shade.transform.SetSiblingIndex(night.transform.GetSiblingIndex()+1);
        var vignette=Child(canvas,"Vignette",Art("Vignette"),Color.white);Fill(vignette.rectTransform);vignette.transform.SetSiblingIndex(shade.transform.GetSiblingIndex()+1);
        var motes=canvas.Find("Motes") as RectTransform;if(motes==null)motes=Stretch(canvas,"Motes");motes.SetSiblingIndex(vignette.transform.GetSiblingIndex()+1);

        // Replace the abstract slashes/orbits with the three Rift Arts medallions.
        var emblem=(RectTransform)canvas.Find("Rift Emblem");
        for(int i=emblem.childCount-1;i>=0;i--)Object.DestroyImmediate(emblem.GetChild(i).gameObject);
        emblem.anchorMin=emblem.anchorMax=new Vector2(1,.5f);emblem.pivot=new Vector2(1,.5f);emblem.sizeDelta=new Vector2(900,1000);emblem.anchoredPosition=new Vector2(-60,10);
        emblem.SetSiblingIndex(motes.GetSiblingIndex()+1);
        var halo=Child(emblem,"Halo",Art("RadialGlow"),new Color(.55f,.35f,1,.42f));PlaceCentered(halo.rectTransform,470,470,980,980);
        var wall=Medallion(emblem,"Void Wall Medallion","VoidWall",170,215,250,.82f);
        var decoy=Medallion(emblem,"Phantom Decoy Medallion","PhantomDecoy",760,730,250,.82f);
        var hand=Medallion(emblem,"Giant Hand Medallion","GiantHandSeal",470,470,560,1);
        var captionShade=Child(emblem,"Caption Shade",Art("RadialGlow"),new Color(.01f,.005f,.03f,.85f));PlaceCentered(captionShade.rectTransform,470,852,1000,190);
        var caption=Text(emblem,"Caption","RIFT ARTS",170,800,600,30,16,GoldText);Style(caption,displaySemi,16,GoldText,14);caption.alignment=TextAlignmentOptions.Center;
        var names=Text(emblem,"Arts","GIANT HAND SEAL   ·   VOID WALL   ·   PHANTOM DECOY",70,834,800,30,15,Muted);Style(names,displaySemi,15,Muted,5);names.alignment=TextAlignmentOptions.Center;
        Divider(emblem,"Caption Rule",470,882,420);

        var menu=ui.MainMenu.transform;
        var eyebrow=Find(menu,"Eyebrow");eyebrow.text="ANOMALY DIVISION";Style(eyebrow,displaySemi,17,GoldText,16);Place(eyebrow.rectTransform,124,150,560,30);
        var title=Find(menu,"Title");Style(title,display,92,Color.white,12,logoShadow);Gradient(title,GoldGradient);Place(title.rectTransform,116,184,700,104);
        var rift=Find(menu,"Rift Title");Style(rift,display,168,Color.white,34,titleGlow);Gradient(rift,RiftGradient);Place(rift.rectTransform,108,262,760,178);
        Divider(menu,"Logo Rule",345,452,470).transform.SetSiblingIndex(rift.transform.GetSiblingIndex()+1);
        var sub=Find(menu,"Subtitle");sub.text="LEARN   ·   BREAK THROUGH   ·   SURVIVE";Style(sub,displaySemi,17,Muted,6);Place(sub.rectTransform,110,472,470,30);sub.alignment=TextAlignmentOptions.Center;
        var nav=(RectTransform)menu.Find("Navigation");Place(nav,125,560,440,420);
        string[] order={"PLAY","CONTINUE","COURSES","SETTINGS","CREDITS","QUIT"};float y=0;
        foreach(var name in order)
        {
            var b=(RectTransform)nav.Find(name);float h=name=="PLAY"?68:56;Place(b,0,y,440,h);y+=h+12;
            StyleButton(b.GetComponent<RiftButton>());
        }
        var noSave=nav.Find("NoSave");
        if(noSave!=null)
        {
            var cont=nav.Find("CONTINUE");noSave.SetParent(cont,false);var t=noSave.GetComponent<TMP_Text>();
            Style(t,displaySemi,12,new Color(.55f,.52f,.62f,.9f),4);t.alignment=TextAlignmentOptions.Right;
            var nr=t.rectTransform;nr.anchorMin=new Vector2(1,0);nr.anchorMax=new Vector2(1,1);nr.pivot=new Vector2(1,.5f);nr.sizeDelta=new Vector2(110,0);nr.anchoredPosition=new Vector2(-34,0);
        }
        var foot=canvas.Find("Footer/Text").GetComponent<TMP_Text>();foot.text="CAMPUS RIFT   ·   THE WORLD BEYOND THE CLASSROOM   ·   v0.1";Style(foot,displaySemi,13,new Color(.62f,.58f,.72f,.75f),5);
        var intro=ui.MainMenu.GetComponent<MenuIntro>();
        var elements=new List<CanvasGroup>();
        foreach(var name in new[]{"Eyebrow","Title","Rift Title","Logo Rule","Subtitle","Navigation/PLAY","Navigation/CONTINUE","Navigation/COURSES","Navigation/SETTINGS","Navigation/CREDITS","Navigation/QUIT"})
        {var t=menu.Find(name);if(t!=null)elements.Add(Get<CanvasGroup>(t.gameObject));}
        intro.Elements=elements.ToArray();intro.Stagger=.055f;intro.FadeDuration=.35f;EditorUtility.SetDirty(intro);
        var ambience=Get<MenuAmbience>(canvas.gameObject);
        ambience.Floaters=new[]{hand.rectTransform,wall.rectTransform,decoy.rectTransform};ambience.Halos=new Graphic[]{halo};
        ambience.MoteArea=motes;ambience.MoteSprite=Art("Mote");EditorUtility.SetDirty(ambience);
        foreach(var n in new[]{"CoursePlaceholderPanel/Course Card","CreditsPanel/Credits Card"})
        {
            var card=(RectTransform)canvas.Find(n);var rule=card.Find("Rule");if(rule!=null)rule.gameObject.SetActive(false);
            Divider(card,"Heading Rule",card.rect.width/2,n.StartsWith("Course")?168:128,card.rect.width-160);
            var viewport=card.Find("Viewport") as RectTransform;if(viewport!=null){viewport.sizeDelta=new Vector2(viewport.sizeDelta.x,496);Place((RectTransform)card.Find("Scroll Hint"),48,666,700,26);}
        }
        EditorUtility.SetDirty(ui);
    }
    static UImage Medallion(Transform parent,string name,string icon,float cx,float cy,float size,float tint)
    {
        var shadow=Child(parent,name+" Shadow",Art("RadialGlow"),new Color(0,0,0,.55f));PlaceCentered(shadow.rectTransform,cx,cy+size*.06f,size*1.15f,size*1.15f);
        var m=Child(parent,name,SkillIcon(icon),new Color(tint,tint,tint,1));m.preserveAspect=true;PlaceCentered(m.rectTransform,cx,cy,size,size);
        return m;
    }

    // ------------------------------------------------------------------ gameplay scene
    static void RestyleGameplay(UIManager ui)
    {
        var pause=(RectTransform)ui.PauseMenu.transform.Find("Pause Card");
        pause.sizeDelta=new Vector2(620,760);pause.anchoredPosition=new Vector2(-310,380);
        StyleCard(pause);
        CenterColumn(pause,"Eyebrow",62,26,16);CenterColumn(pause,"Heading",90,84,64);CenterColumn(pause,"Hint",210,36,20);
        Style(Find(pause,"Hint"),body,20,Muted);Divider(pause,"Heading Rule",310,190,380);
        string[] order={"RESUME","SETTINGS","RESTART","MAIN MENU","QUIT GAME"};float y=268;
        foreach(var name in order){var b=(RectTransform)pause.Find(name);float h=name=="RESUME"?64:56;Place(b,90,y,440,h);y+=h+14;StyleButton(b.GetComponent<RiftButton>());}
        CenterColumn(pause,"Escape Hint",y+22,28,14);Style(Find(pause,"Escape Hint"),displaySemi,14,new Color(.6f,.56f,.7f,.85f),6);
        foreach(var info in new[]{("GameOver/Defeat Card",BloodGradient),("Victory/Victory Card",GoldGradient)})
        {
            var card=(RectTransform)ui.transform.Find(info.Item1);card.sizeDelta=new Vector2(850,470);card.anchoredPosition=new Vector2(-425,235);StyleCard(card);
            CenterColumn(card,"Eyebrow",64,30,16);CenterColumn(card,"Title",100,104,84);CenterColumn(card,"Description",252,44,22);
            Gradient(Find(card,"Title"),info.Item2);Style(Find(card,"Description"),body,22,Muted);Divider(card,"Heading Rule",425,224,420);
            var buttons=card.GetComponentsInChildren<RiftButton>(true);
            for(int i=0;i<buttons.Length;i++){Place((RectTransform)buttons[i].transform,85+i*360,338,320,62);StyleButton(buttons[i]);}
        }
        RestyleHUD(ui.HUD.transform);
        EditorUtility.SetDirty(ui);
    }
    static void RestyleHUD(Transform hud)
    {
        foreach(var plate in new[]{"Vitals/Readability Plate","Objective/Readability Plate"})
        {var t=hud.Find(plate);if(t!=null)SlicedImage(t.gameObject,Art("ButtonFrame"),new Color(1,1,1,.92f));}
        var accent=hud.Find("Objective/Accent")?.GetComponent<UImage>();if(accent!=null)accent.color=GoldText;
        var game=hud.GetComponent<GameplayHUD>();
        var wall=hud.GetComponent<VoidWallHUD>();var hand=hud.GetComponent<GiantHandHUD>();var decoy=hud.GetComponent<PhantomDecoyHUD>();
        if(wall!=null)wall.glyph=SkillSlot(wall.slot,"VoidWall");
        if(hand!=null)hand.glyph=SkillSlot(hand.slot,"GiantHandSeal");
        if(decoy!=null)decoy.glyph=SkillSlot(decoy.slot,"PhantomDecoy");
        foreach(var slot in game.Skills.Slots)if(slot!=null&&slot.Icon.sprite==null)SkillSlot(slot,null);
        if(wall!=null)
        {
            var charges=wall.charges.rectTransform;charges.anchorMin=charges.anchorMax=new Vector2(0,1);charges.pivot=new Vector2(0,1);charges.anchoredPosition=new Vector2(0,-120);charges.sizeDelta=new Vector2(96,22);
            Style(wall.charges,body,15,Parchment);wall.charges.alignment=TextAlignmentOptions.Center;
            foreach(var crystal in wall.crystals)if(crystal!=null)crystal.gameObject.SetActive(false);
            EditorUtility.SetDirty(wall);
        }
        foreach(var h in new MonoBehaviour[]{hand,decoy})if(h!=null)EditorUtility.SetDirty(h);
        var label=hud.Find("SkillBar/Label")?.GetComponent<TMP_Text>();if(label!=null){label.text="RIFT ARTS";Style(label,displaySemi,14,GoldText,10);}
    }
    static UImage SkillSlot(SkillSlotUI slot,string icon)
    {
        var r=slot.transform;
        foreach(var name in new[]{"Border","Background","Placeholder Sigil","VoidWall Glyph","Giant Hand Glyph","Phantom Glyph"}){var t=r.Find(name);if(t!=null)t.gameObject.SetActive(false);}
        var image=slot.Icon;image.sprite=icon!=null?SkillIcon(icon):null;image.enabled=true;image.preserveAspect=true;
        image.color=icon!=null?Color.white:new Color(1,1,1,0);
        if(icon==null){image.sprite=Art("Circle");image.color=new Color(.05f,.04f,.1f,.9f);}
        Place(image.rectTransform,0,0,96,96);image.transform.SetSiblingIndex(0);
        var cd=slot.CooldownOverlay;cd.sprite=Art("Circle");cd.type=UImage.Type.Filled;cd.fillMethod=UImage.FillMethod.Radial360;cd.fillOrigin=(int)UImage.Origin360.Top;
        cd.color=new Color(.02f,.01f,.05f,.72f);Place(cd.rectTransform,11,11,74,74);
        var locked=slot.LockedOverlay.GetComponent<UImage>();locked.sprite=Art("Circle");locked.color=new Color(.02f,.015f,.05f,.82f);Place(locked.rectTransform,11,11,74,74);
        var padlock=locked.transform.Find("Lock");if(padlock!=null)PlaceCentered((RectTransform)padlock,37,37,24,32);
        Style(slot.CooldownText,display,28,Parchment,0,logoShadow);Place(slot.CooldownText.rectTransform,0,26,96,44);
        var badge=Child(r,"Key Badge",Art("KeyBadge"),Color.white);badge.type=UImage.Type.Sliced;badge.pixelsPerUnitMultiplier=2;Place(badge.rectTransform,6,88,84,24);
        badge.transform.SetSiblingIndex(slot.KeyLabel.transform.GetSiblingIndex());
        Style(slot.KeyLabel,displaySemi,13,Parchment,3);Place(slot.KeyLabel.rectTransform,6,88,84,24);slot.KeyLabel.alignment=TextAlignmentOptions.Center;
        slot.KeyLabel.textWrappingMode=TextWrappingModes.NoWrap;
        Style(slot.Tooltip,body,17,Parchment);
        EditorUtility.SetDirty(slot);EditorUtility.SetDirty(image);return image;
    }

    // ------------------------------------------------------------------ prefabs
    static void RestylePrefabs()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var go=PrefabUtility.LoadPrefabContents(path);
            foreach(var text in go.GetComponentsInChildren<TMP_Text>(true)){bool caps=IsCaps(text.text);Style(text,caps?displaySemi:body,text.fontSize,RemapColor(text.color),caps?3:0);}
            foreach(var b in go.GetComponentsInChildren<RiftButton>(true))StyleButton(b,go.name=="Button_Primary");
            if(go.name=="Panel_Dark"||go.name=="ModalPanel")StyleCard((RectTransform)go.transform);
            if(go.name=="SkillSlot")SkillSlot(go.GetComponent<SkillSlotUI>(),null);
            if(go.name=="LoadingScreen")
            {
                var title=go.transform.Find("Content/Title").GetComponent<TMP_Text>();Style(title,display,72,Color.white,14,titleGlow);Gradient(title,GoldGradient);
                var loading=go.transform.Find("Content/Loading").GetComponent<TMP_Text>();Style(loading,displaySemi,17,Muted,10);
                var fill=go.GetComponent<LoadingScreenUI>().Fill;fill.color=Theme.SecondaryAccent;
                go.transform.Find("Background").GetComponent<UImage>().color=Theme.PrimaryBackground;
                Divider(go.transform.Find("Content"),"Rule",400,118,420);
            }
            PrefabUtility.SaveAsPrefabAsset(go,path);PrefabUtility.UnloadPrefabContents(go);
        }
    }
}
