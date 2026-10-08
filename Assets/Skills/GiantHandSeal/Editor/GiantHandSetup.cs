using System;
using System.Collections.Generic;
using System.Linq;
using CampusRift;
using CampusRift.Skills;
using CampusRift.Monsters;
using CampusRift.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class GiantHandSetup
{
    public const string Root="Assets/Skills/GiantHandSeal";
    const string Player="Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
    const string Monster="Assets/MonsterShaban/Monster_Shaban.prefab";
    [MenuItem("Campus Rift/Skills/Build Giant Hand Seal")]
    public static void Build()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var config=AssetDatabase.LoadAssetAtPath<GiantHandConfig>(Root+"/GiantHandConfig.asset");
        if(config==null){config=ScriptableObject.CreateInstance<GiantHandConfig>();AssetDatabase.CreateAsset(config,Root+"/GiantHandConfig.asset");}
        config.handMesh=Hand(false);config.inlayMesh=Hand(true);config.planeMesh=Plane();
        config.shardMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Skills/VoidWall/RiftShard.asset");
        config.handMaterial=Material("CelestialGold","Campus Rift/Giant Hand Seal");
        config.sigilMaterial=Material("SealSigil","Campus Rift/Seal Sigil");
        config.particleMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Skills/VoidWall/VoidShard.mat");
        config.output=AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/CampusRiftUI/CampusRiftAudio.mixer").FindMatchingGroups("SFX").First();
        config.cast=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/impactBell_heavy_000.ogg");config.charge=Audio("forceField_001");config.rift=Audio("forceField_000");
        config.descent=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/thrusterFire_000.ogg");config.impact=Audio("lowFrequency_explosion_000");config.aftershock=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/explosionCrunch_000.ogg");
        config.dissipate=Audio("forceField_002");config.unavailable=Audio("computerNoise_000");EditorUtility.SetDirty(config);
        foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{Root+"/Audio"}))
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));importer.forceToMono=true;
            var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;sample.sampleRateOverride=22050;importer.defaultSampleSettings=sample;importer.SaveAndReimport();
        }
        var player=PrefabUtility.LoadPrefabContents(Player);
        try{ConfigurePlayer(player,config);PrefabUtility.SaveAsPrefabAsset(player,Player);}finally{PrefabUtility.UnloadPrefabContents(player);}
        var monster=PrefabUtility.LoadPrefabContents(Monster);
        try
        {
            if(monster.GetComponent<MonsterVitality>()==null)monster.AddComponent<MonsterVitality>();
            BuildReaction(monster);PrefabUtility.SaveAsPrefabAsset(monster,Monster);
        }
        finally{PrefabUtility.UnloadPrefabContents(monster);}
        foreach(var p in Object.FindObjectsByType<CampusExplorer>())ConfigurePlayer(p.gameObject,config);
        foreach(var brain in Object.FindObjectsByType<MonsterBrain>())
            if(brain.GetComponent<MonsterVitality>()==null)brain.gameObject.AddComponent<MonsterVitality>();
        BuildHUD();AssetDatabase.SaveAssets();
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("GIANT HAND SEAL installed: F, 16m, 2.8m AoE, 60 damage, immediate binding + 5s suppression, 24s cooldown.");
    }
    static void ConfigurePlayer(GameObject go,GiantHandConfig config)
    {
        // Older scene instances had a second health component added before the
        // combat prefab gained one; all combat and UI must share the same source.
        var healths=go.GetComponents<PlayerMonsterHealth>();
        if(healths.Length>0)
        {
            var health=healths[0];health.respawnOnDefeat=false;health.showLegacyHUD=false;EditorUtility.SetDirty(health);
            foreach(var ui in Object.FindObjectsByType<PlayerHealthUI>())
                if(ui.Source!=null && ui.Source.gameObject==go){ui.Source=health;EditorUtility.SetDirty(ui);}
            for(int i=1;i<healths.Length;i++)Object.DestroyImmediate(healths[i]);
            if(PrefabUtility.IsPartOfPrefabInstance(health))PrefabUtility.RecordPrefabInstancePropertyModifications(health);
        }
        if(go.GetComponent<GiantHandCameraImpulse>()==null)go.AddComponent<GiantHandCameraImpulse>();
        var skill=go.GetComponent<GiantHandSkill>()??go.AddComponent<GiantHandSkill>();skill.config=config;EditorUtility.SetDirty(skill);
        if(PrefabUtility.IsPartOfPrefabInstance(skill))PrefabUtility.RecordPrefabInstancePropertyModifications(skill);
    }
    static AudioClip Audio(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Skills/VoidWall/Audio/"+name+".ogg");
    static Material Material(string name,string shader)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+name+".mat");
        if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,Root+"/"+name+".mat");}return m;
    }
    static Mesh SaveMesh(Mesh mesh,string name)
    {
        mesh.name=name;mesh.RecalculateNormals();mesh.RecalculateBounds();
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/"+name+".asset");
        if(existing!=null){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);return existing;}
        AssetDatabase.CreateAsset(mesh,Root+"/"+name+".asset");return mesh;
    }
    static Mesh Plane()
    {
        var m=new Mesh();m.vertices=new[]{new Vector3(-1,0,-1),new Vector3(-1,0,1),new Vector3(1,0,1),new Vector3(1,0,-1)};
        m.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};m.triangles=new[]{0,1,2,0,2,3};return SaveMesh(m,"SealPlane");
    }
    static Mesh Hand(bool inlay)
    {
        var vertices=new List<Vector3>();var indices=new List<int>();
        void Triangle(Vector3 a,Vector3 b,Vector3 c){int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);indices.Add(n);indices.Add(n+1);indices.Add(n+2);}
        void Plate(Vector3 center,Vector3 size,float yaw=0)
        {
            Vector2[] shape={new Vector2(-.32f,-.5f),new Vector2(.32f,-.5f),new Vector2(.5f,-.32f),new Vector2(.5f,.32f),new Vector2(.32f,.5f),new Vector2(-.32f,.5f),new Vector2(-.5f,.32f),new Vector2(-.5f,-.32f)};
            var rotation=Quaternion.Euler(0,yaw,0);var points=new Vector3[24];
            for(int ring=0;ring<3;ring++)for(int i=0;i<8;i++)
            {float s=ring==1?1:.8f;points[ring*8+i]=center+rotation*new Vector3(shape[i].x*size.x*s,(ring-1)*size.y*.5f,shape[i].y*size.z*s);}
            for(int ring=0;ring<2;ring++)for(int i=0;i<8;i++)
            {int j=(i+1)%8;Triangle(points[ring*8+i],points[(ring+1)*8+i],points[(ring+1)*8+j]);Triangle(points[ring*8+i],points[(ring+1)*8+j],points[ring*8+j]);}
            for(int i=1;i<7;i++){Triangle(points[16],points[16+i+1],points[16+i]);Triangle(points[0],points[i],points[i+1]);}
        }
        if(!inlay)
        {
            Plate(new Vector3(0,0,-.35f),new Vector3(1.95f,.5f,1.65f));
            Plate(new Vector3(0,0,-1.35f),new Vector3(1.15f,.36f,.65f));
            float[] xs={-.7f,-.23f,.26f,.72f};float[] lengths={1.62f,1.92f,1.76f,1.38f};
            for(int i=0;i<4;i++)
            {
                float length=lengths[i],yaw=(i-1.5f)*6;
                Plate(new Vector3(xs[i],.01f,.38f+length*.25f),new Vector3(.4f,.34f,length*.48f),yaw);
                Plate(new Vector3(xs[i]+(i-1.5f)*.055f,0,.38f+length*.65f),new Vector3(.35f,.29f,length*.29f),yaw);
                Plate(new Vector3(xs[i]+(i-1.5f)*.085f,-.03f,.38f+length*.91f),new Vector3(.3f,.25f,length*.2f),yaw);
            }
            Plate(new Vector3(-1.02f,-.03f,-.28f),new Vector3(.55f,.36f,.95f),-48);
            Plate(new Vector3(-1.45f,-.04f,.19f),new Vector3(.4f,.28f,.7f),-32);
        }
        else
        {
            Plate(new Vector3(0,.267f,-.3f),new Vector3(.62f,.025f,.62f),45);
            Plate(new Vector3(0,.19f,-1.36f),new Vector3(.95f,.03f,.1f));
            for(int i=0;i<4;i++)Plate(new Vector3(-.7f+i*.47f,.185f,.68f),new Vector3(.22f,.025f,.05f));
            Plate(new Vector3(0,.267f,-.9f),new Vector3(.055f,.025f,.3f));
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);return SaveMesh(mesh,inlay?"SealInlay":"CelestialHand");
    }
    static void BuildReaction(GameObject monster)
    {
        var animator=monster.GetComponentInChildren<Animator>();var controller=(AnimatorController)animator.runtimeAnimatorController;
        controller.animationClips.First(c=>c.name=="CINEMA_4D_Main").SampleAnimation(animator.gameObject,0);
        var bones=animator.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("mixamorig_") || t.name=="zombie_Walk").ToArray();
        var clip=new AnimationClip{name="Shaban_Sealed",frameRate=60};
        foreach(var bone in bones)
        {
            Vector3 bend=Vector3.zero;Vector3 offset=Vector3.zero;
            if(bone.name=="mixamorig_Spine")bend=new Vector3(34,0,8);
            if(bone.name=="mixamorig_Head")bend=new Vector3(26,0,-8);
            if(bone.name=="mixamorig_RightUpLeg" || bone.name=="mixamorig_LeftUpLeg")bend=new Vector3(-20,0,0);
            if(bone.name=="mixamorig_RightLeg" || bone.name=="mixamorig_LeftLeg")bend=new Vector3(34,0,0);
            if(bone.name=="zombie_Walk")offset=new Vector3(0,-0.25f,0);
            string path=AnimationUtility.CalculateTransformPath(bone,animator.transform);
            Quaternion from=bone.localRotation,to=from*Quaternion.Euler(bend);
            for(int axis=0;axis<4;axis++)clip.SetCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[axis],new AnimationCurve(new Keyframe(0,from[axis]),new Keyframe(.12f,to[axis]),new Keyframe(1,to[axis])));
            for(int axis=0;axis<3;axis++)clip.SetCurve(path,typeof(Transform),"m_LocalPosition."+"xyz"[axis],new AnimationCurve(new Keyframe(0,bone.localPosition[axis]),new Keyframe(.12f,bone.localPosition[axis]+offset[axis]),new Keyframe(1,bone.localPosition[axis]+offset[axis])));
        }
        clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
        var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Shaban_Sealed.anim");
        if(saved==null){AssetDatabase.CreateAsset(clip,Root+"/Shaban_Sealed.anim");saved=clip;}else{EditorUtility.CopySerialized(clip,saved);Object.DestroyImmediate(clip);}
        var machine=controller.layers[0].stateMachine;var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Sealed")??machine.AddState("Sealed",new Vector3(450,240));
        state.motion=saved;state.writeDefaultValues=true;EditorUtility.SetDirty(controller);
    }
    static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 anchor,Vector2 position)
    {
        var found=parent.Find(name);var rect=found!=null?(RectTransform)found:new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
    }
    static TMP_Text Text(Transform parent,string name,Vector2 size,Vector2 anchor,Vector2 position,int font)
    {
        var r=Rect(parent,name,size,anchor,position);var t=r.GetComponent<TextMeshProUGUI>()??r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/CampusRiftUI/CampusRiftFont.asset");t.fontSize=font;t.color=new Color(1,.75f,.35f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
    }
    static void BuildHUD()
    {
        var hud=Object.FindAnyObjectByType<GameplayHUD>();if(hud==null)throw new InvalidOperationException("Open SampleScene first.");
        var slot=hud.Skills.GetSlot(1);slot.Configure(null,"F  READY","DAI THU AN / GIANT HAND SEAL\nF: auto lock and cast on nearest monster\n16m range • 2.8m radius • 24s cooldown\nWalls and floors block the seal.");slot.SetLocked(false);
        var placeholder=slot.transform.Find("Placeholder Sigil");if(placeholder!=null)placeholder.gameObject.SetActive(false);
        var r=Rect(slot.transform,"Giant Hand Glyph",new Vector2(52,52),new Vector2(.5f,.5f),new Vector2(0,12));
        if(r.GetComponent<CanvasRenderer>()==null)r.gameObject.AddComponent<CanvasRenderer>();
        var glyph=r.GetComponent<GiantHandGlyph>()??r.gameObject.AddComponent<GiantHandGlyph>();glyph.color=new Color(1,.7f,.22f);glyph.raycastTarget=false;
        glyph.SetAllDirty();
        foreach(var wallGlyph in hud.GetComponentsInChildren<VoidWallGlyph>(true))
        {
            if(wallGlyph.GetComponent<CanvasRenderer>()==null)wallGlyph.gameObject.AddComponent<CanvasRenderer>();
            wallGlyph.SetAllDirty();
        }
        var ui=hud.GetComponent<GiantHandHUD>()??hud.gameObject.AddComponent<GiantHandHUD>();ui.slot=slot;ui.glyph=glyph;
        ui.instruction=Text(hud.transform,"Hand Seal Instructions",new Vector2(820,28),new Vector2(.5f,0),new Vector2(0,132),16);ui.instruction.text="F  AUTO LOCK NEAREST MONSTER";
        ui.hitFeedback=Text(hud.transform,"Hand Seal Hit Feedback",new Vector2(920,30),new Vector2(.5f,.5f),new Vector2(0,-125),18);ui.hitFeedback.text="";
        var label=hud.Skills.transform.Find("Label");if(label!=null)label.GetComponent<TMP_Text>().text="RIFT ARTS  /  ABILITIES";
        EditorUtility.SetDirty(slot);EditorUtility.SetDirty(ui);
    }
}
