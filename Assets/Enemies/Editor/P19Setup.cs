#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.Animations;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Combat;
using CampusRift.Levels;
using CampusRift.UI;
public static class P19Setup
{
    static readonly string[] ids={"tieu-yeu","doc-nhan","liem-hon","thiet-giap-nguu","bao-thi","quang-ma","hoa-trung","anh-yeu","trieu-hon-su","duc-yeu","hoa-linh"};
    static readonly string[] models={"Ninja","Wizard","Armabee","Ghost"};
    static readonly string[] names={"Ảnh Yêu","Triệu Hồn Sư","Dực Yêu","Hỏa Linh"};
    static readonly string[] resource={"AnhYeu","TrieuHonSu","DucYeu","HoaLinh"};
    static EnemyArchetype Arch(string id)=>AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/"+id+".asset");
    static T Asset<T>(string path)where T:ScriptableObject=>P12AssetSetup.Asset<T>(path);
    static T Get<T>(GameObject go)where T:Component=>P12AssetSetup.Get<T>(go);
    [MenuItem("Campus Rift/P19/Install Expanded Enemies")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
        Directory.CreateDirectory("Assets/Enemies/Animation/P19");Directory.CreateDirectory("Assets/Enemies/Materials/P19");Directory.CreateDirectory("Assets/Enemies/Resources/P19");Directory.CreateDirectory("Assets/Enemies/Data/Affixes");Directory.CreateDirectory("Assets/Enemies/Resources/P19/Affixes");Directory.CreateDirectory("task/p19");
        AssetDatabase.Refresh();
        var tex=AssetImporter.GetAtPath("Assets/Enemies/Models/P19/Atlas_Monsters.png") as TextureImporter;tex.maxTextureSize=256;tex.mipmapEnabled=true;var ats=tex.GetPlatformTextureSettings("Android");ats.name="Android";ats.overridden=true;ats.maxTextureSize=256;ats.format=TextureImporterFormat.ASTC_6x6;tex.SetPlatformTextureSettings(ats);tex.SaveAndReimport();
        var audit=new List<string>();
        for(int n=0;n<4;n++)Build(n,audit);
        var outline=AssetDatabase.LoadAssetAtPath<Material>("Assets/Enemies/Resources/P19/EliteOutline.mat");if(outline==null){outline=new Material(Shader.Find("Campus Rift/P19 Elite Outline"));AssetDatabase.CreateAsset(outline,"Assets/Enemies/Resources/P19/EliteOutline.mat");}
        Affixes();Data();AssetDatabase.SaveAssets();
        File.WriteAllText("task/p19/asset-audit.txt",string.Join("\n",audit));
    }
    static void Build(int n,List<string> audit)
    {
        string model=models[n],path="Assets/Enemies/Models/P19/"+model+".fbx",id=ids[n+7];
        foreach(var p in new[]{path,"Assets/Enemies/Models/P19/LOD/"+model+"_LOD1.fbx","Assets/Enemies/Models/P19/LOD/"+model+"_LOD2.fbx"})
        {var im=(ModelImporter)AssetImporter.GetAtPath(p);im.animationType=ModelImporterAnimationType.Generic;im.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;im.importAnimation=p==path;im.importCameras=im.importLights=false;im.isReadable=true;im.optimizeGameObjects=false;im.skinWeights=ModelImporterSkinWeights.Custom;im.maxBonesPerVertex=4;im.SaveAndReimport();}
        var root=new GameObject(resource[n]);var visual=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);visual.name="Visual_"+model;
        var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        var animator=visual.GetComponent<Animator>()??visual.AddComponent<Animator>();
        var source=clips.First(c=>c.name.EndsWith(n>=2?"Flying_Idle":"Idle"));
        var bounds=new Bounds();bool first=true;
        foreach(var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>()){var world=r.bounds;if(first){bounds=world;first=false;}else bounds.Encapsulate(world);}
        float height=new[]{1.8f,1.6f,1.5f,1.9f}[n];float scale=height/bounds.size.y;visual.transform.localScale*=scale;visual.transform.localPosition-=Vector3.up*bounds.min.y*scale;
        var profile=Asset<EnemyAnimationProfile>("Assets/Enemies/Animation/P19/"+model+".asset");profile.modelId=model;profile.idle="Idle_Breathe";profile.attack="Attack_Light";profile.special="Attack_Heavy";profile.spawnSeconds=.8f;profile.deathSeconds=.8f;profile.walkMetersPerSecond=1.8f;profile.runMetersPerSecond=4;profile.attackImpactSeconds=.45f;profile.blendSeconds=.12f;
        string controllerPath="Assets/Enemies/Animation/P19/"+model+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);if(!controller.parameters.Any(p=>p.name=="Speed"))controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
        var mappings=new Dictionary<string,string>();
        foreach(string s in new[]{"Idle_Breathe","Idle_Alert","Idle_Combat","Spawn","Turn_Left","Turn_Right","Taunt"})mappings[s]=n>=2?"Flying_Idle":"Idle";
        foreach(string s in new[]{"Walk_Forward","Walk_Backward","Strafe_Left","Strafe_Right"})mappings[s]=n>=2?"Fast_Flying":"Walk";
        mappings["Run_Forward"]=n>=2?"Fast_Flying":n==0?"Run":"Walk";
        mappings["Attack_Light"]=n==1?"Bite_Front":"Punch";mappings["Attack_Heavy"]=n==0?"Weapon":n==1?"Dance":"Headbutt";
        foreach(string s in new[]{"Hit_Front","Hit_Back","Hit_Left","Hit_Right","Stagger","Stun"})mappings[s]=n==1?"HitRecieve":"HitReact";
        mappings["Death_Forward"]=mappings["Death_Backward"]="Death";
        var aliases=new List<AnimationClip>();
        foreach(var pair in mappings)
        {
            var original=clips.First(c=>c.name.EndsWith("|"+pair.Value));string ap="Assets/Enemies/Animation/P19/"+model+"_"+pair.Key+".anim";var alias=AssetDatabase.LoadAssetAtPath<AnimationClip>(ap);
            if(alias==null){alias=UnityEngine.Object.Instantiate(original);AssetDatabase.CreateAsset(alias,ap);}else EditorUtility.CopySerialized(original,alias);
            alias.name=model+"_"+pair.Key;var settings=AnimationUtility.GetAnimationClipSettings(alias);settings.loopTime=pair.Key.StartsWith("Idle")||pair.Key.StartsWith("Walk")||pair.Key.StartsWith("Run")||pair.Key.StartsWith("Strafe");AnimationUtility.SetAnimationClipSettings(alias,settings);EditorUtility.SetDirty(alias);
            var state=machine.AddState(pair.Key);state.motion=alias;if(pair.Key==profile.idle)machine.defaultState=state;aliases.Add(alias);
        }
        profile.clips=aliases.ToArray();profile.headBones=new[]{"Head"};profile.footBones=n==0?new[]{"Foot.L","Foot.R"}:new string[0];EditorUtility.SetDirty(profile);
        animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
        string mp="Assets/Enemies/Materials/P19/"+model+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(mp);if(material==null){material=new Material(Shader.Find("Campus Rift/Enemy Comic Dissolve"));AssetDatabase.CreateAsset(material,mp);}
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Enemies/Models/P19/Atlas_Monsters.png"));material.SetColor("_BaseColor",n==0?new Color(.5f,.4f,.85f):n==1?new Color(.6f,.85f,.75f):n==2?new Color(.45f,1,.55f):new Color(1,.3f,.08f));material.SetColor("_EmissionColor",n==3?new Color(2,.65f,.06f):new Color(.35f,.1f,.5f));material.SetColor("_EdgeColor",n==3?new Color(2,.5f,.03f):new Color(.45f,.15f,1));EditorUtility.SetDirty(material);
        var originals=visual.GetComponentsInChildren<SkinnedMeshRenderer>();foreach(var r in originals){r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();r.updateWhenOffscreen=false;}
        var lods=new List<LOD>{new LOD(.45f,originals)};var bones=visual.GetComponentsInChildren<Transform>().GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
        var counts=new List<int>{originals.Sum(r=>r.sharedMesh.triangles.Length/3)};
        for(int l=1;l<=2;l++)
        {
            var temp=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemies/Models/P19/LOD/"+model+"_LOD"+l+".fbx"),root.transform);temp.transform.localScale=visual.transform.localScale;temp.transform.localPosition=visual.transform.localPosition;
            var rs=new List<Renderer>();int tris=0;
            foreach(var src in temp.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var reference=originals.FirstOrDefault(r=>r.name==src.name)??originals[0];var g=new GameObject("SM_"+src.name+"_LOD"+l);g.transform.SetParent(reference.transform.parent,false);g.transform.localPosition=reference.transform.localPosition;g.transform.localRotation=reference.transform.localRotation;g.transform.localScale=reference.transform.localScale;
                var mesh=UnityEngine.Object.Instantiate(src.sharedMesh);var conversion=reference.transform.worldToLocalMatrix*src.transform.localToWorldMatrix;
                mesh.vertices=mesh.vertices.Select(v=>conversion.MultiplyPoint3x4(v)).ToArray();mesh.normals=mesh.normals.Select(v=>conversion.inverse.transpose.MultiplyVector(v).normalized).ToArray();
                var bind=reference.bones.Select((b,i)=>new{b.name,i}).ToDictionary(x=>x.name,x=>reference.sharedMesh.bindposes[x.i]);mesh.bindposes=src.bones.Select(b=>bind.ContainsKey(b.name)?bind[b.name]:bones[b.name].worldToLocalMatrix*reference.transform.localToWorldMatrix).ToArray();mesh.RecalculateBounds();
                string meshPath="Assets/Enemies/Models/P19/LOD/"+model+"_"+src.name+"_LOD"+l+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(saved==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}
                var r=g.AddComponent<SkinnedMeshRenderer>();r.sharedMesh=mesh;r.bones=src.bones.Select(b=>bones[b.name]).ToArray();r.rootBone=bones[src.rootBone.name];r.localBounds=reference.localBounds;r.sharedMaterials=Enumerable.Repeat(material,src.sharedMaterials.Length).ToArray();rs.Add(r);tris+=mesh.triangles.Length/3;
            }
            UnityEngine.Object.DestroyImmediate(temp);counts.Add(tris);lods.Add(new LOD(l==1?.12f:.025f,rs.ToArray()));
        }
        var group=root.AddComponent<LODGroup>();group.SetLODs(lods.ToArray());group.RecalculateBounds();
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=7;
        var agent=root.AddComponent<NavMeshAgent>();agent.agentTypeID=-1372625422;agent.enabled=false;agent.height=height;agent.radius=.35f;agent.stoppingDistance=.15f;agent.angularSpeed=360;
        var collider=root.AddComponent<CapsuleCollider>();collider.height=height;collider.radius=.35f;collider.center=Vector3.up*height*.5f;
        Get<MonsterVitality>(root);Get<StatusEffectHost>(root);Get<MinionMotor>(root);var instance=Get<EnemyInstance>(root);Get<MinionBrain>(root);Get<EnemyAbilityRunner>(root);Get<EnemyAnimationDriver>(root).profile=profile;if(n==0)Get<EnemyFootPlant>(root);if(n==2)Get<FlyingMotor>(root);Get<ExpandedEnemyRuntime>(root);if(n==0)Get<EnemyConcealment>(root);
        var arch=Asset<EnemyArchetype>("Assets/Enemies/Data/"+id+".asset");arch.id=id;arch.displayName=resource[n];arch.displayNameVN=names[n];arch.baseHealth=new[]{80,120,40,150}[n];arch.baseDamage=new[]{14,6,8,16}[n];arch.baseSpeed=new[]{6.5f,3.5f,7,5}[n];arch.element=n<2?Element.Am:n==2?Element.Moc:Element.Hoa;arch.swordIntentWeight=n==1?2:1;arch.ranged=n==1;arch.preferredMin=9;arch.preferredMax=13;arch.attackRange=n==1?13:1.8f;arch.attackCooldown=1.6f;arch.isFlying=n==2;arch.prefabPending=false;instance.archetype=arch;
        arch.prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Enemies/Prefabs/"+resource[n]+".prefab");UnityEngine.Object.DestroyImmediate(root);EditorUtility.SetDirty(arch);
        var runtime=Asset<EnemyArchetype>("Assets/Enemies/Resources/P19/"+resource[n]+".asset");EditorUtility.CopySerialized(arch,runtime);EditorUtility.SetDirty(runtime);
        audit.Add(id+" height="+height+" sourceScale="+scale+" LOD triangles="+string.Join(",",counts)+" Generic/animated/rootMotionOFF");
    }
    static void Affixes()
    {
        string[] vn={"Cuồng Bạo","Kim Thân","Phân Liệt","Hấp Huyết","Tự Bạo","Ẩn Hình","Hộ Vệ","Hỏa Tâm"},en={"Berserk","Metal Body","Split","Vampiric","Explosion","Invisible","Guardian","Fire Heart"};
        Color[] colors={ComicTheme.Red,ComicTheme.Gold,ComicTheme.Purple,ComicTheme.Red,ComicTheme.Orange,ComicTheme.Muted,ComicTheme.Green,ComicTheme.Orange};
        string[] icon={"warning","currency","skill","heart","flame","eye","shield","flame"};
        for(int i=0;i<8;i++){string ip="Assets/Enemies/Data/Affixes/Icons/"+en[i].Replace(" ","")+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(ip);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=64;importer.SaveAndReimport();var a=Asset<EliteAffixDefinition>("Assets/Enemies/Data/Affixes/"+en[i].Replace(" ","")+".asset");a.kind=(EliteAffixKind)i;a.nameVN=vn[i];a.nameEN=en[i];a.color=colors[i];a.minLevel=i==7?8:6;a.icon=AssetDatabase.LoadAssetAtPath<Sprite>(ip);EditorUtility.SetDirty(a);var r=Asset<EliteAffixDefinition>("Assets/Enemies/Resources/P19/Affixes/"+a.name+".asset");EditorUtility.CopySerialized(a,r);EditorUtility.SetDirty(r);}
    }
    static void Data()
    {
        for(int level=6;level<=10;level++)
        {
            var d=LevelCatalog.Instance.Get(level);var table=d.spawnTable;table.roster.Clear();
            foreach(var id in ids.Take(level<8?10:11)){if(level<8&&id=="hoa-trung")continue;table.roster.Add(new SpawnWeight{archetype=Arch(id),weight=id=="tieu-yeu"?3:id=="trieu-hon-su"?.8f:1.2f,minPerWave=level>=8?1:0,maxPerWave=id=="thiet-giap-nguu"?3:id=="trieu-hon-su"?2:99});}
            int[] totals=level==6?new[]{5,6,6,9}:level==7?new[]{6,6,8,9}:level==8?new[]{32}:level==9?new[]{16,20}:new[]{13,15,17};
            table.elitesPerWave=level==6?new[]{0,1,1,1}:level==7?new[]{1,1,1,2}:level==8?new[]{3}:level==9?new[]{2,2}:new[]{2,2,3};
            table.finalWave=table.roster.Select(r=>new SpawnEntry{archetype=r.archetype,count=1}).ToList();
            for(int extra=table.roster.Count;extra<totals.Last();extra++){int i=extra%3==0?1:0;var entry=table.finalWave[i];entry.count++;table.finalWave[i]=entry;}
            for(int w=0;w<d.waves.Count;w++)d.waves[w].entries=w==d.waves.Count-1?table.finalWave.Select(e=>e).ToList():new List<SpawnEntry>{new SpawnEntry{archetype=Arch("tieu-yeu"),count=totals[w]}};
            if(level==6)d.stars[2]=new StarCondition{kind=StarKind.SummonerBeforeSecond,value=2,descriptionVN="Hạ mỗi Triệu Hồn Sư trước lần triệu hồi thứ 2",descriptionEN="Defeat each Summoner before its second summon"};
            EditorUtility.SetDirty(table);EditorUtility.SetDirty(d);
        }
        var ai=Resources.Load<AITierProfile>("AITierProfiles");var tier=ai.tiers[4];tier.dodgeChance=.6f;tier.adapt=true;ai.tiers[4]=tier;EditorUtility.SetDirty(ai);
    }
}
#endif
