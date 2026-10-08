using System;
using System.Linq;
using CampusRift;
using CampusRift.Skills;
using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

public static class PhantomDecoySetup
{
    const string Root = "Assets/Skills/PhantomDecoy";
    const string PlayerPrefab = "Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";

    [MenuItem("Campus Rift/Skills/Build Phantom Decoy")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var ghost = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Afterimage/Afterimage.mat");
        var shards = AssetDatabase.LoadAssetAtPath<Material>("Assets/Skills/VoidWall/VoidShard.mat");
        var shardMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Skills/VoidWall/RiftShard.asset");
        if (ghost == null) throw new InvalidOperationException("Existing afterimage material is missing.");
        var preview = AssetDatabase.LoadAssetAtPath<Material>(Root + "/PhantomAim.mat");
        if (preview == null)
        {
            preview = new Material(Shader.Find("Sprites/Default")) { name = "Phantom Aim" };
            AssetDatabase.CreateAsset(preview, Root + "/PhantomAim.mat");
        }
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/CampusRiftUI/CampusRiftAudio.mixer");
        var output = mixer != null ? mixer.FindMatchingGroups("SFX").FirstOrDefault() : null;
        var appear = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Skills/VoidWall/Audio/forceField_004.ogg");
        var vanish = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Skills/VoidWall/Audio/forceField_002.ogg");
        var prefab = PrefabUtility.LoadPrefabContents(PlayerPrefab);
        try { Configure(prefab,ghost,preview,shards,shardMesh,appear,vanish,output); PrefabUtility.SaveAsPrefabAsset(prefab,PlayerPrefab); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        foreach (var player in Object.FindObjectsByType<CampusExplorer>())
            Configure(player.gameObject,ghost,preview,shards,shardMesh,appear,vanish,output);
        BuildHUD();
        AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("PHANTOM DECOY installed: G, mobile directional gesture, one 5.5s clone, 18s cooldown.");
    }

    static void Configure(GameObject go, Material ghost, Material preview, Material shards, Mesh shardMesh, AudioClip appear,
        AudioClip vanish, AudioMixerGroup output)
    {
        var skill = go.GetComponent<PhantomDecoySkill>() ?? go.AddComponent<PhantomDecoySkill>();
        skill.ghostMaterial = ghost; skill.previewMaterial = preview;
        skill.shardMaterial = shards; skill.shardMesh = shardMesh;
        skill.spawnSound = appear; skill.expireSound = vanish; skill.audioOutput = output;
        EditorUtility.SetDirty(skill);
        if (PrefabUtility.IsPartOfPrefabInstance(skill)) PrefabUtility.RecordPrefabInstancePropertyModifications(skill);
    }

    static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 anchor,Vector2 position)
    {
        var found=parent.Find(name);
        var r=found!=null?(RectTransform)found:new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);
        r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    static void BuildHUD()
    {
        var hud=Object.FindAnyObjectByType<GameplayHUD>();
        if(hud==null)throw new InvalidOperationException("Open gameplay scene before building Phantom Decoy.");
        var slot=hud.Skills.GetSlot(2);
        slot.Configure(null,"G  READY","HUYEN ANH DAN DU / PHANTOM DECOY\nG: aim, G or LMB: cast, RMB: cancel\nRun for 5.5s • 18s cooldown • one phantom at a time.");
        slot.SetLocked(false);
        var placeholder=slot.transform.Find("Placeholder Sigil");if(placeholder!=null)placeholder.gameObject.SetActive(false);
        var border=slot.transform.Find("Border");
        if(border!=null){var graphic=border.GetComponent<RiftGraphic>();if(graphic!=null)graphic.color=new Color(.4f,.47f,1,.6f);}
        var icon=Rect(slot.transform,"Phantom Glyph",new Vector2(52,52),new Vector2(.5f,.5f),new Vector2(0,12));
        var glyph=icon.GetComponent<PhantomGlyph>()??icon.gameObject.AddComponent<PhantomGlyph>();
        glyph.color=new Color(.48f,.65f,1);glyph.raycastTarget=false;
        var ui=hud.GetComponent<PhantomDecoyHUD>()??hud.gameObject.AddComponent<PhantomDecoyHUD>();
        ui.slot=slot;ui.glyph=glyph;
        var label=Rect(hud.transform,"Phantom Instructions",new Vector2(850,28),new Vector2(.5f,0),new Vector2(0,164));
        var t=label.GetComponent<TextMeshProUGUI>()??label.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=slot.KeyLabel.font;t.fontSize=16;t.color=new Color(.56f,.7f,1);
        t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;
        t.text="G  PHANTOM DECOY  /  READY";ui.instruction=t;
        EditorUtility.SetDirty(slot);EditorUtility.SetDirty(ui);
    }
}
