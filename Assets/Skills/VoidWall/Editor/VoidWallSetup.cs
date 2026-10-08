using System;
using System.Linq;
using CampusRift;
using CampusRift.Monsters;
using CampusRift.Skills;
using CampusRift.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class VoidWallSetup
{
    const string Root="Assets/Skills/VoidWall";
    const string PlayerPath="Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
    [MenuItem("Campus Rift/Skills/Build Void Wall")]
    public static void Build()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var config=AssetDatabase.LoadAssetAtPath<VoidWallConfig>(Root+"/VoidWallConfig.asset");
        if(config==null){config=ScriptableObject.CreateInstance<VoidWallConfig>();AssetDatabase.CreateAsset(config,Root+"/VoidWallConfig.asset");}
        config.wallMaterial=Material("VoidWall","Campus Rift/Void Wall");
        config.particleMaterial=Material("VoidShard","Campus Rift/Void Shard");
        var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/CampusRiftUI/CampusRiftAudio.mixer");
        config.output=mixer.FindMatchingGroups("SFX").FirstOrDefault();
        config.preview=Audio("forceField_004");config.deploy=Audio("forceField_000");config.hum=Audio("forceField_001");
        config.impact=Audio("impactGlass_medium_000");config.unstable=Audio("forceField_003");config.broken=Audio("impactGlass_heavy_000");
        config.expire=Audio("forceField_002");config.empty=Audio("computerNoise_000");EditorUtility.SetDirty(config);
        foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{Root+"/Audio"}))
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.forceToMono=true;var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=22050;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
        var mesh=BuildMesh();var shard=BuildShard();
        var go=new GameObject("Void Wall");go.SetActive(false);
        VoidWall prefab;
        try
        {
            var wall=go.AddComponent<VoidWall>();wall.config=config;
            wall.solid=go.AddComponent<BoxCollider>();wall.solid.enabled=false;
            wall.obstacle=go.AddComponent<NavMeshObstacle>();wall.obstacle.shape=NavMeshObstacleShape.Box;wall.obstacle.carving=true;wall.obstacle.carveOnlyStationary=false;wall.obstacle.enabled=false;
            var visual=new GameObject("Frozen Rift");visual.transform.SetParent(go.transform,false);visual.AddComponent<MeshFilter>().sharedMesh=mesh;
            wall.surface=visual.AddComponent<MeshRenderer>();wall.surface.sharedMaterial=config.wallMaterial;wall.surface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;wall.surface.receiveShadows=false;
            wall.voice=go.AddComponent<AudioSource>();wall.drone=go.AddComponent<AudioSource>();
            foreach(var a in new[]{wall.voice,wall.drone}){a.playOnAwake=false;a.spatialBlend=1;a.rolloffMode=AudioRolloffMode.Logarithmic;a.minDistance=2;a.maxDistance=23;a.outputAudioMixerGroup=config.output;}
            var particles=new GameObject("Rift shards");particles.transform.SetParent(go.transform,false);wall.motes=particles.AddComponent<ParticleSystem>();
            var ps=wall.motes;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=100;main.startLifetime=new ParticleSystem.MinMaxCurve(0.4f,0.9f);main.startSize=new ParticleSystem.MinMaxCurve(0.015f,0.04f);main.startSpeed=0.2f;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startColor=new ParticleSystem.MinMaxGradient(new Color(0.2f,0.8f,1),new Color(0.65f,0.2f,1));main.startRotation3D=true;main.startRotationZ=new ParticleSystem.MinMaxCurve(0,6.28f);
            var emission=ps.emission;emission.rateOverTime=7;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(2.8f,2.35f,0.1f);
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.y=0.35f;
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
            var render=ps.GetComponent<ParticleSystemRenderer>();render.renderMode=ParticleSystemRenderMode.Mesh;render.mesh=shard;render.sharedMaterial=config.particleMaterial;render.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/VoidWall.prefab").GetComponent<VoidWall>();
        }
        finally{Object.DestroyImmediate(go);}
        var playerPrefab=PrefabUtility.LoadPrefabContents(PlayerPath);
        try{ConfigurePlayer(playerPrefab,config,prefab);PrefabUtility.SaveAsPrefabAsset(playerPrefab,PlayerPath);}
        finally{PrefabUtility.UnloadPrefabContents(playerPrefab);}
        const string monsterPath="Assets/MonsterShaban/Monster_Shaban.prefab";
        var monster=PrefabUtility.LoadPrefabContents(monsterPath);
        try{if(monster.GetComponent<MonsterBarrierTactics>()==null)monster.AddComponent<MonsterBarrierTactics>();PrefabUtility.SaveAsPrefabAsset(monster,monsterPath);}
        finally{PrefabUtility.UnloadPrefabContents(monster);}
        foreach(var player in Object.FindObjectsByType<CampusExplorer>())ConfigurePlayer(player.gameObject,config,prefab);
        foreach(var brain in Object.FindObjectsByType<MonsterBrain>())if(brain.GetComponent<MonsterBarrierTactics>()==null)brain.gameObject.AddComponent<MonsterBarrierTactics>();
        BuildHUD();
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("VOID WALL: installed skill, 20 charges, HUD, audio, original VFX, and monster tactics.");
    }
    static void ConfigurePlayer(GameObject go,VoidWallConfig config,VoidWall prefab)
    {
        var health=go.GetComponent<PlayerMonsterHealth>();if(health!=null){health.showLegacyHUD=false;EditorUtility.SetDirty(health);}
        var skill=go.GetComponent<VoidWallSkill>()??go.AddComponent<VoidWallSkill>();skill.config=config;skill.wallPrefab=prefab;
        EditorUtility.SetDirty(skill);if(PrefabUtility.IsPartOfPrefabInstance(skill))PrefabUtility.RecordPrefabInstancePropertyModifications(skill);
    }
    static AudioClip Audio(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/"+name+".ogg");
    static Material Material(string name,string shader)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+name+".mat");
        if(material==null){material=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(material,Root+"/"+name+".mat");}return material;
    }
    static Mesh BuildMesh()
    {
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/FrozenRift.asset");if(saved!=null)return saved;
        var mesh=new Mesh{name="Frozen spatial slab"};
        Vector2[] pts={new Vector2(-.42f,0),new Vector2(.4f,0),new Vector2(.5f,.09f),new Vector2(.5f,.91f),new Vector2(.42f,1),new Vector2(-.4f,1),new Vector2(-.5f,.91f),new Vector2(-.5f,.09f)};
        mesh.vertices=pts.Select(p=>new Vector3(p.x,p.y,0)).ToArray();mesh.uv=pts.Select(p=>new Vector2(p.x+.5f,p.y)).ToArray();
        mesh.triangles=new[]{0,1,2,0,2,3,0,3,4,0,4,5,0,5,6,0,6,7};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Root+"/FrozenRift.asset");return mesh;
    }
    static Mesh BuildShard()
    {
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/RiftShard.asset");if(saved!=null)return saved;
        var mesh=new Mesh{name="Rift shard"};mesh.vertices=new[]{new Vector3(-.4f,-.35f),new Vector3(.15f,.65f),new Vector3(.4f,-.15f)};mesh.triangles=new[]{0,1,2};mesh.colors=new[]{Color.white,Color.white,Color.white};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Root+"/RiftShard.asset");return mesh;
    }
    static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 anchor,Vector2 position)
    {
        var existing=parent.Find(name);if(existing!=null)return (RectTransform)existing;
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    static TMP_Text Text(Transform parent,string name,string value,Vector2 size,Vector2 anchor,Vector2 pos,float fontSize)
    {
        var r=Rect(parent,name,size,anchor,pos);var t=r.GetComponent<TextMeshProUGUI>()??r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/CampusRiftUI/CampusRiftFont.asset");t.text=value;t.fontSize=fontSize;t.color=new Color(.8f,.9f,1);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
    }
    static void BuildHUD()
    {
        var hud=Object.FindAnyObjectByType<GameplayHUD>();if(hud==null)throw new InvalidOperationException("Open SampleScene with GameplayHUD first.");
        var slot=hud.Skills.GetSlot(0);slot.SetLocked(false);slot.KeyLabel.text="Q";slot.Icon.enabled=false;
        slot.Tooltip.text="HU KHONG BICH / VOID WALL\n20 charges • Q: aim / deploy • Wheel: near / far • RMB: cancel\nWalk through your wall. Monsters must detour or break it.";
        var placeholder=slot.transform.Find("Placeholder Sigil");if(placeholder!=null)placeholder.gameObject.SetActive(false);
        var glyph=Rect(slot.transform,"VoidWall Glyph",new Vector2(52,52),new Vector2(.5f,.5f),new Vector2(0,12));
        if(glyph.GetComponent<CanvasRenderer>()==null)glyph.gameObject.AddComponent<CanvasRenderer>();
        var graphic=glyph.GetComponent<VoidWallGlyph>()??glyph.gameObject.AddComponent<VoidWallGlyph>();graphic.raycastTarget=false;graphic.color=new Color(.65f,.45f,1);
        var ui=hud.GetComponent<VoidWallHUD>()??hud.gameObject.AddComponent<VoidWallHUD>();ui.slot=slot;ui.glyph=graphic;
        ui.charges=Text(slot.transform,"VoidWall Charges","20 / 20",new Vector2(96,24),new Vector2(.5f,0),new Vector2(0,-14),19);
        ui.instruction=Text(hud.transform,"VoidWall Instructions","Q  VOID WALL  •  PHASE THROUGH YOUR WALL",new Vector2(880,30),new Vector2(.5f,0),new Vector2(0,102),16);
        ui.crystals=new Graphic[3];
        for(int i=0;i<3;i++)
        {var r=Rect(slot.transform,"Void charge "+i,new Vector2(8,12),new Vector2(.5f,0),new Vector2((i-1)*16,39));var g=r.GetComponent<RiftGraphic>()??r.gameObject.AddComponent<RiftGraphic>();g.Form=RiftGraphic.Shape.Diamond;g.raycastTarget=false;g.color=new Color(.18f,.92f,1);ui.crystals[i]=g;}
        var label=hud.Skills.transform.Find("Label");if(label!=null)label.GetComponent<TMP_Text>().text="RIFT ARTS  /  ABILITIES";
        EditorUtility.SetDirty(ui);EditorUtility.SetDirty(slot);
    }
}
