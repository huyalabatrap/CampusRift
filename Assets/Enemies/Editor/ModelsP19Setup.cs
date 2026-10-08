#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using CampusRift.Enemies;

public sealed class ModelsP19Importer : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if(!assetPath.StartsWith("Assets/Enemies/Models/ReplacementP19/"))return;
        var m=(ModelImporter)assetImporter;m.animationType=ModelImporterAnimationType.Generic;
        m.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;m.importAnimation=!assetPath.Contains("_LOD");
        m.importCameras=false;m.importLights=false;m.isReadable=true;m.optimizeGameObjects=false;
        m.preserveHierarchy=true;m.skinWeights=ModelImporterSkinWeights.Custom;m.maxBonesPerVertex=4;
        m.materialImportMode=ModelImporterMaterialImportMode.None;
    }
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Enemies/Models/ReplacementP19/"))return;
        var t=(TextureImporter)assetImporter;t.mipmapEnabled=true;t.maxTextureSize=2048;
        t.textureType=assetPath.EndsWith("_Normal.png")?TextureImporterType.NormalMap:TextureImporterType.Default;
        t.sRGBTexture=!assetPath.EndsWith("_MetallicSmoothness.png");
        var android=t.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;
        android.maxTextureSize=1024;android.format=TextureImporterFormat.ASTC_6x6;t.SetPlatformTextureSettings(android);
    }
}

public static class ModelsP19Setup
{
    [Serializable] public class ModelAudit {public string id,model;public int[] triangles;public int clips,unweighted,maxInfluences;public float height;public bool rootMotion,passed;}
    [Serializable] public class Audit {public string at;public ModelAudit[] models;public bool passed;}
    static readonly string[] models={"NightDemon","DarkMage","VampireBat","LavaGolem"};
    static readonly string[] prefabs={"AnhYeu","TrieuHonSu","DucYeu","HoaLinh"};
    static readonly string[] ids={"anh-yeu","trieu-hon-su","duc-yeu","hoa-linh"};
    static readonly string[] specials={"Teleport_Vanish","Summon_Cast","Dive","Fire_Cast"};
    static readonly float[] heights={1.8f,1.6f,1.5f,1.9f};
    static readonly Color[] colors={new Color(.55f,.15f,.9f),new Color(.12f,.65f,.38f),new Color(.25f,.5f,.18f),new Color(1,.25f,.025f)};
    static string Folder(string model)=>"Assets/Enemies/Models/ReplacementP19/"+model;
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        Directory.CreateDirectory("Assets/Enemies/Animation/ReplacementP19");Directory.CreateDirectory("Assets/Enemies/Materials/ReplacementP19");
        var audits=new List<ModelAudit>();for(int i=0;i<4;i++)audits.Add(Build(i));
        AssetDatabase.SaveAssets();File.WriteAllText("task/models/unity-asset-audit.json",JsonUtility.ToJson(new Audit{at=DateTime.UtcNow.ToString("o"),models=audits.ToArray(),passed=audits.All(a=>a.passed)},true));
    }
    static ModelAudit Build(int n)
    {
        string model=models[n],folder=Folder(model),path=folder+"/"+model+".fbx";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new Exception("Missing "+path);
        string prefabPath="Assets/Enemies/Prefabs/"+prefabs[n]+".prefab";
        var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var group=root.GetComponent<LODGroup>();group.SetLODs(new LOD[0]);
            // Only replace the visual children, preserving every serialized gameplay component and archetype reference.
            foreach(var child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual=UnityEngine.Object.Instantiate(source,root.transform);visual.name="Visual_"+model;
            var originals=visual.GetComponentsInChildren<SkinnedMeshRenderer>();var bounds=originals[0].bounds;foreach(var r in originals)bounds.Encapsulate(r.bounds);
            float scale=heights[n]/bounds.size.y;visual.transform.localScale*=scale;visual.transform.localPosition-=Vector3.up*bounds.min.y*scale;
            var animator=visual.GetComponent<Animator>()??visual.AddComponent<Animator>();
            var sourceClips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            var profile=P12AssetSetup.Asset<EnemyAnimationProfile>("Assets/Enemies/Animation/ReplacementP19/"+model+".asset");
            profile.modelId=model;profile.idle=n==2?"Hover":"Idle_Breathe";profile.attack="Attack_Light";profile.special=specials[n];
            // Preserve P19's spawn/death timing; play the longer source take within the established lifetime window.
            profile.spawnSeconds=.8f;profile.deathSeconds=.8f;profile.attackImpactSeconds=.45f;profile.blendSeconds=.12f;
            profile.walkMetersPerSecond=n==2?4:1.8f;profile.runMetersPerSecond=n==2?7:4;
            profile.headBones=n==2?new string[0]:new[]{"Head"};profile.footBones=n==2?new string[0]:new[]{"Foot.L","Foot.R"};
            var controllerPath="Assets/Enemies/Animation/ReplacementP19/"+model+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine=controller.layers[0].stateMachine;foreach(var s in machine.states)machine.RemoveState(s.state);
            if(!controller.parameters.Any(p=>p.name=="Speed"))controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            var clips=new List<AnimationClip>();
            foreach(var clip in sourceClips)
            {
                string state=clip.name.Substring(clip.name.IndexOf(model+"_",StringComparison.Ordinal)+model.Length+1);
                string aliasPath="Assets/Enemies/Animation/ReplacementP19/"+model+"_"+state+".anim";
                var alias=AssetDatabase.LoadAssetAtPath<AnimationClip>(aliasPath);
                if(alias==null){alias=UnityEngine.Object.Instantiate(clip);AssetDatabase.CreateAsset(alias,aliasPath);}else EditorUtility.CopySerialized(clip,alias);
                alias.name=model+"_"+state;var settings=AnimationUtility.GetAnimationClipSettings(alias);
                settings.loopTime=state.StartsWith("Idle")||state.StartsWith("Walk")||state.StartsWith("Run")||state.StartsWith("Strafe")||state=="Stun"||state=="Hover"||state=="Fly"||state=="Fire_Idle";
                AnimationUtility.SetAnimationClipSettings(alias,settings);EditorUtility.SetDirty(alias);clips.Add(alias);
                var s=machine.AddState(state);s.motion=alias;if(state==profile.idle)machine.defaultState=s;
                if(state=="Spawn"||state.StartsWith("Death"))s.speed=alias.length/.8f;
            }
            profile.clips=clips.ToArray();profile.attackImpactSeconds=profile.Clip("Attack_Light").length*.675f;EditorUtility.SetDirty(profile);
            if(n==2)foreach(var entry in machine.states)
                if(entry.state.name.StartsWith("Walk")||entry.state.name.StartsWith("Run")||entry.state.name.StartsWith("Strafe"))entry.state.motion=profile.Clip("Fly");
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
            root.GetComponent<EnemyAnimationDriver>().profile=profile;
            if(n!=2&&root.GetComponent<EnemyFootPlant>()==null)root.AddComponent<EnemyFootPlant>();
            string matPath="Assets/Enemies/Materials/ReplacementP19/"+model+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(mat==null){mat=new Material(Shader.Find("Campus Rift/Enemy PBR Dissolve"));AssetDatabase.CreateAsset(mat,matPath);}
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/"+model+"_BaseColor.png"));
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/"+model+"_Normal.png"));
            mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/"+model+"_MetallicSmoothness.png"));
            mat.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/"+model+"_Emission.png"));
            mat.SetColor("_BaseColor",Color.white);mat.SetColor("_RimColor",colors[n]*.2f);mat.SetColor("_EdgeColor",colors[n]);
            mat.SetColor("_EmissionColor",n==3?new Color(3.5f,.6f,.04f):Color.black);mat.SetFloat("_Dissolve",0);EditorUtility.SetDirty(mat);
            foreach(var r in originals){r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();r.updateWhenOffscreen=false;}
            var skeleton=visual.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            var lods=new List<LOD>{new LOD(.5f,originals)};var triangles=new List<int>{originals.Sum(r=>r.sharedMesh.triangles.Length/3)};
            int missing=0,maxWeights=0;
            foreach(var r in originals)Weights(r.sharedMesh,ref missing,ref maxWeights);
            for(int level=1;level<=2;level++)
            {
                var temp=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/"+model+"_LOD"+level+".fbx"),root.transform);
                temp.transform.localScale=visual.transform.localScale;temp.transform.localPosition=visual.transform.localPosition;
                var renderers=new List<Renderer>();int count=0;
                foreach(var src in temp.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var reference=originals[0];var mesh=UnityEngine.Object.Instantiate(src.sharedMesh);
                    var conversion=reference.transform.worldToLocalMatrix*src.transform.localToWorldMatrix;
                    mesh.vertices=mesh.vertices.Select(v=>conversion.MultiplyPoint3x4(v)).ToArray();mesh.normals=mesh.normals.Select(v=>conversion.inverse.transpose.MultiplyVector(v).normalized).ToArray();
                    mesh.tangents=mesh.tangents.Select(v=>{var t=conversion.MultiplyVector(new Vector3(v.x,v.y,v.z)).normalized;return new Vector4(t.x,t.y,t.z,v.w);}).ToArray();
                    var bind=reference.bones.Select((b,i)=>new {b.name,i}).ToDictionary(x=>x.name,x=>reference.sharedMesh.bindposes[x.i]);
                    mesh.bindposes=src.bones.Select(b=>bind[b.name]).ToArray();mesh.RecalculateBounds();
                    string meshPath=folder+"/"+model+"_LOD"+level+"-Skin.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(existing==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
                    var g=new GameObject("SM_"+model+"_LOD"+level);g.transform.SetParent(reference.transform.parent,false);
                    g.transform.localPosition=reference.transform.localPosition;g.transform.localRotation=reference.transform.localRotation;g.transform.localScale=reference.transform.localScale;
                    var r=g.AddComponent<SkinnedMeshRenderer>();r.sharedMesh=mesh;r.bones=src.bones.Select(b=>skeleton[b.name]).ToArray();r.rootBone=skeleton[src.rootBone.name];r.localBounds=reference.localBounds;
                    r.sharedMaterials=Enumerable.Repeat(mat,mesh.subMeshCount).ToArray();renderers.Add(r);count+=mesh.triangles.Length/3;Weights(mesh,ref missing,ref maxWeights);
                }
                UnityEngine.Object.DestroyImmediate(temp);triangles.Add(count);lods.Add(new LOD(level==1?.12f:.025f,renderers.ToArray()));
            }
            // Pitch the bat's upright source presentation into forward flight; the root still owns navigation.
            if(n==2)visual.transform.localRotation=Quaternion.Euler(60,0,0);
            group.SetLODs(lods.ToArray());group.RecalculateBounds();foreach(var t in visual.GetComponentsInChildren<Transform>(true))t.gameObject.layer=7;
            PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            return new ModelAudit{id=ids[n],model=model,triangles=triangles.ToArray(),clips=clips.Count,unweighted=missing,maxInfluences=maxWeights,height=heights[n],rootMotion=animator.applyRootMotion,passed=missing==0&&maxWeights<=4&&triangles[0]<=58000&&triangles[1]<=15000&&triangles[2]<=5000&&clips.Count>=28};
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Weights(Mesh m,ref int missing,ref int max)
    {var weights=m.GetBonesPerVertex();foreach(var w in weights){if(w==0)missing++;max=Mathf.Max(max,w);}weights.Dispose();}
}
#endif
