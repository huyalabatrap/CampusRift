#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.Localization;
namespace CampusRift.Combat
{
    public static class ReactionSetup
    {
        [MenuItem("Campus Rift/V2/Install Reactions and Generation")]
        public static void Install()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before installing P11.");
            const string data="Assets/Combat/Data/Reactions.asset";
            var config=AssetDatabase.LoadAssetAtPath<ReactionConfig>(data);
            if(config==null){config=ScriptableObject.CreateInstance<ReactionConfig>();AssetDatabase.CreateAsset(config,data);}
            config.internalCooldown=1;config.chainWindow=6;config.chainDamageBonus=.3f;config.chainSpirit=20;
            var previousRules=config.rules;
            config.rules=new[]{
                Rule(ReactionType.IceLightning,"bang-loi-liet","Ice Lightning Rupture","Băng Lôi Liệt","Freeze + Lightning = Ice Lightning Rupture","Đóng băng + Lôi = Băng Lôi Liệt",Element.Thuy,Element.Loi,3,1.5f,0,0,.08f,.7f,"impactGlass_heavy_001","laserLarge_000","forceField_003"),
                Rule(ReactionType.ElectricFlow,"dien-luu","Electric Flow","Điện Lưu","Chill or Wet + Lightning = Electric Flow","Chậm hoặc Ướt + Lôi = Điện Lưu",Element.Thuy,Element.Loi,5,0,.5f,0,.075f,.45f,"laserSmall_001","laserLarge_000","forceField_003"),
                Rule(ReactionType.FireExplosion,"bao-viem","Fire Detonation","Bạo Viêm","Burn + Fire = Fire Detonation","Bỏng + Hỏa = Bạo Viêm",Element.Hoa,Element.Hoa,4,.8f,0,0,.08f,.75f,"impactPlate_heavy_000","explosionCrunch_001","thrusterFire_001"),
                Rule(ReactionType.Convergence,"tu-sat","Convergence","Tụ Sát","Black Hole Pull + Area Hit = Convergence","Hắc Động hút + Đòn vùng = Tụ Sát",Element.KhongGian,Element.Hoa,0,.4f,0,0,.08f,.65f,"impactSoft_heavy_000","lowFrequency_explosion_001","forceField_003"),
                Rule(ReactionType.ArmorShatter,"pha-giap","Armor Shatter","Phá Giáp","Stun + Metal = Armor Shatter","Choáng + Kim = Phá Giáp",Element.Tho,Element.Kim,0,0,8,.3f,.08f,.6f,"impactMetal_heavy_001","impactPlate_heavy_000","impactBell_heavy_001")};
            if(previousRules!=null&&previousRules.Length==9){var all=new ReactionRule[9];Array.Copy(config.rules,all,5);Array.Copy(previousRules,5,all,5,4);config.rules=all;}
            EditorUtility.SetDirty(config);Directory.CreateDirectory("Assets/Combat/Resources");AssetDatabase.Refresh();
            const string linkPath="Assets/Combat/Resources/ReactionConfigReference.asset";
            var link=AssetDatabase.LoadAssetAtPath<ReactionConfigReference>(linkPath);
            if(link==null){link=ScriptableObject.CreateInstance<ReactionConfigReference>();AssetDatabase.CreateAsset(link,linkPath);}link.config=config;EditorUtility.SetDirty(link);
            var catalog=AssetDatabase.LoadAssetAtPath<LocalizationCatalog>("Assets/Localization/Resources/LocalizationCatalog.asset");
            foreach(var r in config.rules){Translation(catalog,r.nameEN,r.nameVI);Translation(catalog,r.nameEN.ToUpperInvariant(),r.nameVI.ToUpperInvariant());Translation(catalog,r.hintEN,r.hintVI);}
            Translation(catalog,"GENERATION","TƯƠNG SINH");Translation(catalog,"GENERATION!","TƯƠNG SINH!");Translation(catalog,"3 LINKED ELEMENTS","3 HỆ LIÊN TIẾP");Translation(catalog,"+20 SPIRIT","+20 LINH LỰC");
            foreach(var pair in new[]{new[]{"WATER","THỦY"},new[]{"BOLT","LÔI"},new[]{"FIRE","HỎA"},new[]{"METAL","KIM"},new[]{"EARTH","THỔ"},new[]{"VOID","KHÔNG"}})Translation(catalog,pair[0],pair[1]);
            EditorUtility.SetDirty(catalog);
            const string prefab="Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
            var root=PrefabUtility.LoadPrefabContents(prefab);Configure(root,config);PrefabUtility.SaveAsPrefabAsset(root,prefab);PrefabUtility.UnloadPrefabContents(root);
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");Configure(UnityEngine.Object.FindAnyObjectByType<CampusExplorer>().gameObject,config);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();Debug.Log("P11 reactions, chain and localization installed.");
        }
        static void Configure(GameObject root,ReactionConfig config)
        {var tracker=root.GetComponent<GenerationChainTracker>()??root.AddComponent<GenerationChainTracker>();tracker.config=config;}
        static ReactionRule Rule(ReactionType type,string id,string en,string vi,string hintEN,string hintVI,Element first,Element second,float radius,float bonus,float duration,float magnitude,float stop,float impulse,string transient,string body,string tail)
        {return new ReactionRule{type=type,id=id,nameEN=en,nameVI=vi,hintEN=hintEN,hintVI=hintVI,first=first,second=second,radius=radius,bonus=bonus,statusDuration=duration,statusMagnitude=magnitude,hitStop=stop,impulse=impulse,transient=Clip(transient),body=Clip(body),tail=Clip(tail)};}
        static AudioClip Clip(string name)
        {var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Combat/Audio/Reactions/"+name+".ogg");if(clip==null)throw new InvalidOperationException("Missing P11 audio "+name);return clip;}
        static void Translation(LocalizationCatalog catalog,string en,string vi)
        {var existing=catalog.entries.Find(e=>e.en==en);if(existing==null)catalog.entries.Add(new TranslationEntry{en=en,vi=vi});else existing.vi=vi;}
    }
}
#endif
