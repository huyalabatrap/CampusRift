using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Monsters;

public sealed class P12LodImporter : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if(!assetPath.Contains("/Models/LOD/"))return;
        var i=(ModelImporter)assetImporter;i.animationType=ModelImporterAnimationType.Generic;
        i.importAnimation=false;i.importCameras=false;i.importLights=false;i.isReadable=true;
        i.optimizeGameObjects=false;i.preserveHierarchy=true;i.globalScale=1;i.useFileUnits=true;
        i.skinWeights=ModelImporterSkinWeights.Custom;i.maxBonesPerVertex=4;
    }
}

public static class P12AssetSetup
{
    [Serializable] public class Check {public string id,prefab;public int[] triangles;public int unweighted;public bool passed;public float walkStrideSpeed,runStrideSpeed;}
    [Serializable] public class Audit {public Check[] models;public bool passed;public string at;}
    sealed class Spec
    {
        public string folder,id,name,vn,attack,special;public Element element;public float hp,dmg,speed,height;public bool ranged;
    }
    static readonly Spec[] Specs={
        new Spec{folder="010_PumpkinMummy",id="tieu-yeu",name="TieuYeu",vn="Tiểu Yêu",element=Element.Moc,hp=60,dmg=8,speed=5.5f,height=1.2f,attack="Attack_Light",special="Happy_Bounce"},
        new Spec{folder="013_FlowerAlien",id="doc-nhan",name="DocNhan",vn="Độc Nhãn Xạ Thủ",element=Element.Moc,hp=45,dmg=10,speed=4,height=2.2f,ranged=true,attack="Spore_Cast",special="Spore_Cast"},
        new Spec{folder="027_TwinHeadDemon",id="thiet-giap-nguu",name="ThietGiapNguu",vn="Thiết Giáp Ngưu",element=Element.Kim,hp=220,dmg=18,speed=4,height=2.8f,attack="Double_Claw",special="Roar_Both"},
        new Spec{folder="005_Zombie",id="bao-thi",name="BaoThi",vn="Bạo Thi",element=Element.Am,hp=50,dmg=8,speed=6,height=2,attack="Claw_Swipe_Left",special="Sniff"},
        new Spec{folder="008_GrimReaper",id="liem-hon",name="LiemHon",vn="Liêm Hồn",element=Element.Am,hp=60,dmg=8,speed=4.5f,height=1.5f,attack="Scythe_Sweep",special="Lantern_Cast"},
        new Spec{folder="012_HaloDemon",id="quang-ma",name="QuangMa",vn="Quang Ma",element=Element.Hoa,hp=60,dmg=8,speed=3.8f,height=2.1f,ranged=true,attack="Curse_Cast",special="Halo_Channel"},
        new Spec{folder="025_FlowerCrawler",id="hoa-trung",name="HoaTrung",vn="Hoa Trùng",element=Element.Moc,hp=60,dmg=8,speed=4.5f,height=2,attack="Ground_Slam",special="Spore_Roar"},
    };
    [MenuItem("Campus Rift/P12/Install Models and LODs")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before installing P12 assets");
        Directory.CreateDirectory("Assets/Enemies/Animation/Profiles");Directory.CreateDirectory("Assets/Enemies/Materials/P12");
        Directory.CreateDirectory("Assets/SkyBeast/Prefabs");Directory.CreateDirectory("Artifacts/P12");AssetDatabase.Refresh();
        foreach(var p in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/GameReadyModels","Assets/GameReadyDragons"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var t=AssetImporter.GetAtPath(p) as TextureImporter;if(t==null)continue;
            t.maxTextureSize=2048;t.mipmapEnabled=true;
            var android=t.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;
            android.maxTextureSize=1024;android.format=TextureImporterFormat.ASTC_6x6;android.compressionQuality=50;t.SetPlatformTextureSettings(android);
            t.SaveAndReimport();
        }
        var checks=new List<Check>();
        foreach(var s in Specs)
        {
            string folder="Assets/GameReadyModels/"+s.folder;string prefabPath="Assets/Enemies/Prefabs/"+s.name+".prefab";
            if((s.name=="TieuYeu"||s.name=="DocNhan") && !File.Exists("Assets/Enemies/Prefabs/"+s.name+"_Legacy.prefab"))
                AssetDatabase.CopyAsset(prefabPath,"Assets/Enemies/Prefabs/"+s.name+"_Legacy.prefab");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Generated/"+s.folder+".prefab");
            if(source==null)throw new Exception("Run source builder first: "+folder);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name=s.name;
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=7;
            var profile=Asset<EnemyAnimationProfile>("Assets/Enemies/Animation/Profiles/"+s.folder+".asset");
            profile.modelId=s.folder.Substring(0,3);profile.attack=s.attack;profile.special=s.special;profile.idle=s.id=="liem-hon"?"Hover":"Idle_Breathe";
            profile.clips=AssetDatabase.LoadAllAssetsAtPath(folder+"/"+s.folder+".fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            var bones=go.GetComponentsInChildren<Transform>(true);
            profile.headBones=bones.Where(t=>t.name.IndexOf("Head",StringComparison.OrdinalIgnoreCase)>=0 && !t.name.Contains("Rig") && !t.name.Contains("Mesh") && !t.name.StartsWith("0")).Select(t=>t.name).ToArray();
            profile.footBones=bones.Where(t=>t.name.StartsWith("Foot")||t.name.StartsWith("foot")||t.name=="L_Foot"||t.name=="R_Foot").Select(t=>t.name).ToArray();
            profile.walkMetersPerSecond=MeasureStride(go,profile.Clip("Walk_Forward"),profile.footBones);
            profile.runMetersPerSecond=MeasureStride(go,profile.Clip("Run_Forward"),profile.footBones);
            profile.attackImpactSeconds=profile.Clip(s.attack).length*.5f;EditorUtility.SetDirty(profile);
            // Restore bind pose after measuring clips so that no sampled pose is serialized into the variant.
            var bind=source.GetComponentsInChildren<Transform>(true);
            for(int i=0;i<bind.Length;i++){bones[i].localPosition=bind[i].localPosition;bones[i].localRotation=bind[i].localRotation;bones[i].localScale=bind[i].localScale;}
            var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Generated/"+s.folder+".mat");
            var comic=AssetDatabase.LoadAssetAtPath<Material>("Assets/Enemies/Materials/P12/"+s.folder+".mat");
            if(comic==null){comic=new Material(material);AssetDatabase.CreateAsset(comic,"Assets/Enemies/Materials/P12/"+s.folder+".mat");}
            comic.shader=Shader.Find("Campus Rift/Enemy Comic Dissolve");comic.SetColor("_EdgeColor",ElementChart.ColorOf(s.element));comic.SetColor("_EmissionColor",ElementChart.ColorOf(s.element)*.24f);comic.SetFloat("_Dissolve",0);EditorUtility.SetDirty(comic);
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(comic,r.sharedMaterials.Length).ToArray();
            var agent=Get<NavMeshAgent>(go);agent.enabled=false;agent.agentTypeID=-1372625422;agent.radius=s.id=="thiet-giap-nguu"?.6f:.32f;agent.height=s.height;
            agent.speed=s.speed;agent.angularSpeed=360;agent.stoppingDistance=.15f;agent.autoTraverseOffMeshLink=true;
            var collider=Get<CapsuleCollider>(go);collider.height=s.height;collider.radius=agent.radius;collider.center=Vector3.up*s.height*.5f;
            Get<MinionMotor>(go);Get<StatusEffectHost>(go);Get<MonsterVitality>(go);Get<EnemyInstance>(go);Get<MinionBrain>(go);
            var driver=Get<EnemyAnimationDriver>(go);driver.profile=profile;
            Get<EnemyAbilityRunner>(go);Get<EnemyFootPlant>(go);
            var a=Get<Animator>(go);a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
            var check=AddLODs(go,s.folder,"Enemies",comic);check.id=profile.modelId;check.prefab=prefabPath;check.walkStrideSpeed=profile.walkMetersPerSecond;check.runStrideSpeed=profile.runMetersPerSecond;
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,prefabPath);UnityEngine.Object.DestroyImmediate(go);
            var arch=Asset<EnemyArchetype>("Assets/Enemies/Data/"+s.id+".asset");arch.id=s.id;arch.displayName=s.name;arch.displayNameVN=s.vn;arch.element=s.element;
            arch.prefab=prefab;arch.prefabPending=false;arch.baseHealth=s.hp;arch.baseDamage=s.dmg;arch.baseSpeed=s.speed;arch.ranged=s.ranged;
            arch.defense=s.id=="thiet-giap-nguu"?.2f:0;arch.lateHealthMultiplier=s.id=="thiet-giap-nguu"?1:.92f;if(s.id=="doc-nhan")arch.baseDamage=8.5f;arch.attackRange=s.ranged?12:s.id=="liem-hon"?4:s.id=="hoa-trung"?3:s.id=="thiet-giap-nguu"?2.2f:1.6f;
            arch.attackClipSeconds=profile.Clip(s.attack).length;arch.impactFraction=.5f;arch.swordIntentWeight=s.id=="thiet-giap-nguu"?2:1;
            arch.isFlying=s.id=="liem-hon";EditorUtility.SetDirty(arch);checks.Add(check);
        }
        foreach(var folder in new[]{"020_SilverCloudDragon","023_AzureSerpentDragon","026_LavaWingDragon"})
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameReadyDragons/"+folder+"/Generated/"+folder+".prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name=folder;
            var sourceMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/GameReadyDragons/"+folder+"/Generated/"+folder+".mat");
            Directory.CreateDirectory("Assets/SkyBeast/Materials");var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/SkyBeast/Materials/"+folder+".mat");
            if(material==null){material=new Material(sourceMaterial);AssetDatabase.CreateAsset(material,"Assets/SkyBeast/Materials/"+folder+".mat");}
            material.shader=Shader.Find("Campus Rift/Enemy Comic Dissolve");material.SetColor("_EmissionColor",folder.StartsWith("026")?new Color(1.8f,.4f,.08f):folder.StartsWith("023")?new Color(.9f,.35f,.16f):new Color(.8f,1.1f,1.4f));EditorUtility.SetDirty(material);
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();
            var check=AddLODs(go,folder,"SkyBeast",material);check.id=folder.Substring(0,3);check.prefab="Assets/SkyBeast/Prefabs/"+folder+".prefab";
            go.GetComponent<Animator>().applyRootMotion=false;go.GetComponent<Animator>().cullingMode=AnimatorCullingMode.CullUpdateTransforms;
            PrefabUtility.SaveAsPrefabAsset(go,check.prefab);UnityEngine.Object.DestroyImmediate(go);checks.Add(check);
        }
        AssetDatabase.SaveAssets();File.WriteAllText("Artifacts/P12/AssetValidation.json",JsonUtility.ToJson(new Audit{models=checks.ToArray(),passed=checks.All(c=>c.passed),at=DateTime.UtcNow.ToString("o")},true));
        Debug.Log("P12_ASSETS "+checks.Count+" models; "+(checks.All(c=>c.passed)?"PASS":"FAIL"));
    }
    public static T Get<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
    public static T Asset<T>(string path) where T:ScriptableObject
    {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a==null){Directory.CreateDirectory(Path.GetDirectoryName(path));a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;}
    static float MeasureStride(GameObject go,AnimationClip clip,string[] footNames)
    {
        if(clip==null)return 1;var feet=go.GetComponentsInChildren<Transform>().Where(t=>footNames.Contains(t.name)).ToArray();
        if(feet.Length==0)return 1;float total=0;int count=0;const int samples=120;
        var positions=new Vector3[feet.Length];var min=new float[feet.Length];for(int f=0;f<feet.Length;f++)min[f]=float.MaxValue;
        for(int i=0;i<=samples;i++){clip.SampleAnimation(go,clip.length*i/samples);for(int f=0;f<feet.Length;f++)min[f]=Mathf.Min(min[f],feet[f].position.y);}
        clip.SampleAnimation(go,0);for(int f=0;f<feet.Length;f++)positions[f]=feet[f].position;
        for(int i=1;i<=samples;i++){clip.SampleAnimation(go,clip.length*i/samples);for(int f=0;f<feet.Length;f++){
            var p=feet[f].position;float retreat=(positions[f].z-p.z)*samples/clip.length;
            if(p.y<min[f]+.055f && retreat>.01f){total+=retreat;count++;}positions[f]=p;}}
        return Mathf.Max(.05f,count>0?total/count:.5f);
    }
    static Check AddLODs(GameObject go,string folder,string family,Material material)
    {
        var original=go.GetComponentsInChildren<SkinnedMeshRenderer>();var renders=new List<Renderer[]>();renders.Add(original.Cast<Renderer>().ToArray());
        var skeleton=go.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());var check=new Check{triangles=new int[3]};
        foreach(var r in original)check.triangles[0]+=Triangles(r.sharedMesh);
        for(int level=1;level<=2;level++)
        {
            string path="Assets/"+family+"/Models/LOD/"+folder+"_LOD"+level+".fbx";var lodSource=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(lodSource==null)throw new Exception("Missing LOD "+path);
            var renderers=new List<Renderer>();
            foreach(var src in lodSource.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var child=new GameObject("SM_"+folder+"_LOD"+level);child.layer=go.layer;child.transform.SetParent(go.transform,false);
                var r=child.AddComponent<SkinnedMeshRenderer>();
                // FBX round-tripping writes centimetre vertices and a 100x armature.
                // Convert vertices to the source renderer's space, then use its bind poses.
                // Reusing the exported bind poses against the source bones collapses the mesh.
                var reference=original[0];var mesh=UnityEngine.Object.Instantiate(src.sharedMesh);
                var conversion=reference.transform.worldToLocalMatrix*src.transform.localToWorldMatrix;
                var vertices=mesh.vertices;for(int v=0;v<vertices.Length;v++)vertices[v]=conversion.MultiplyPoint3x4(vertices[v]);mesh.vertices=vertices;
                var normals=mesh.normals;for(int v=0;v<normals.Length;v++)normals[v]=conversion.inverse.transpose.MultiplyVector(normals[v]).normalized;mesh.normals=normals;
                var tangents=mesh.tangents;for(int v=0;v<tangents.Length;v++){var t=conversion.MultiplyVector(new Vector3(tangents[v].x,tangents[v].y,tangents[v].z)).normalized;tangents[v]=new Vector4(t.x,t.y,t.z,tangents[v].w);}mesh.tangents=tangents;
                var bindMap=reference.bones.Select((bone,index)=>new{bone.name,index}).ToDictionary(x=>x.name,x=>reference.sharedMesh.bindposes[x.index]);
                mesh.bindposes=src.bones.Select(b=>bindMap[b.name]).ToArray();mesh.RecalculateBounds();mesh.name=folder+"_LOD"+level;
                string meshPath="Assets/"+family+"/Models/LOD/"+mesh.name+"-Skin.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(existing==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
                r.sharedMesh=mesh;
                r.bones=src.bones.Select(b=>skeleton.ContainsKey(b.name)?skeleton[b.name]:throw new Exception("Missing bone "+b.name)).ToArray();
                r.rootBone=skeleton[src.rootBone.name];r.localBounds=original[0].localBounds;r.sharedMaterials=Enumerable.Repeat(material,src.sharedMaterials.Length).ToArray();
                // Retain the original mesh-to-armature coordinates, not the FBX scene transforms.
                child.transform.localPosition=original[0].transform.localPosition;child.transform.localRotation=original[0].transform.localRotation;child.transform.localScale=original[0].transform.localScale;
                check.triangles[level]+=Triangles(r.sharedMesh);var weights=r.sharedMesh.GetBonesPerVertex();foreach(var n in weights)if(n==0)check.unweighted++;weights.Dispose();
                renderers.Add(r);
            }
            renders.Add(renderers.ToArray());
        }
        var group=Get<LODGroup>(go);group.SetLODs(new[]{new LOD(.5f,renders[0]),new LOD(.12f,renders[1]),new LOD(.025f,renders[2])});group.RecalculateBounds();
        check.passed=check.unweighted==0 && check.triangles[0]>50000 && Mathf.Abs(check.triangles[1]-15000)<750 && Mathf.Abs(check.triangles[2]-5000)<250;return check;
    }
    static int Triangles(Mesh m){int n=0;for(int s=0;s<m.subMeshCount;s++)n+=(int)m.GetIndexCount(s)/3;return n;}
}
