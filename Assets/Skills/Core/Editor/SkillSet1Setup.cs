#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public static class SkillSet1Setup
    {
        const string Prefab="Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
        const string Data="Assets/Skills/Core/Data/";
        const string ResourcesPath="Assets/Skills/Core/Resources/";
        [MenuItem("Campus Rift/V2/Install Lightning Flash")]
        public static void InstallFlash()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
            Directory.CreateDirectory(ResourcesPath);
            var config=AssetDatabase.LoadAssetAtPath<SkillSet1VfxConfig>(ResourcesPath+"SkillSet1Vfx.asset");
            if(config==null){config=ScriptableObject.CreateInstance<SkillSet1VfxConfig>();AssetDatabase.CreateAsset(config,ResourcesPath+"SkillSet1Vfx.asset");}
            var mixer=AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/CampusRiftUI/CampusRiftAudio.mixer");if(mixer!=null){var groups=mixer.FindMatchingGroups("SFX");if(groups.Length>0)config.audioOutput=groups[0];}
            config.surface=Material("P10Energy","Campus Rift/P10 Comic Energy");config.ink=Material("P10Ink","Campus Rift/P10 Ink Hull");
            config.stroke=Material("P10Stroke","Campus Rift/P10 Alpha Stroke");config.groundMark=Material("P10GroundMark","Campus Rift/P10 Ground Mark");
            config.groundMark.enableInstancing=true;EditorUtility.SetDirty(config.groundMark);
            config.accretion=Material("P10Accretion","Campus Rift/P10 Accretion Disc");
            config.vortexDebris=Material("P10VortexDebris","Campus Rift/P10 Comic Energy");config.vortexDebris.SetColor("_BaseColor",new Color(.15f,.04f,.23f));config.vortexDebris.SetColor("_Emission",new Color(.6f,.08f,1));config.vortexDebris.SetFloat("_Alpha",.9f);config.vortexDebris.SetFloat("_ParticleTint",1);EditorUtility.SetDirty(config.vortexDebris);
            config.layeredWide=Material("P10LayeredWide","Campus Rift/P10 Layered Stroke");config.layeredSmall=Material("P10LayeredSmall","Campus Rift/P10 Layered Stroke");config.layeredRing=Material("P10LayeredRing","Campus Rift/P10 Layered Stroke");
            config.layeredWide.SetFloat("_Core",1/1.85f);config.layeredWide.SetFloat("_Glow",1.45f/1.85f);config.layeredSmall.SetFloat("_Core",.055f/.42f);config.layeredSmall.SetFloat("_Glow",.28f/.42f);config.layeredRing.SetFloat("_Core",0);config.layeredRing.SetFloat("_Glow",.07f/.15f);EditorUtility.SetDirty(config.layeredWide);EditorUtility.SetDirty(config.layeredSmall);EditorUtility.SetDirty(config.layeredRing);
            config.layeredSlash=Material("P10LayeredSlash","Campus Rift/P10 Layered Stroke");config.layeredSlash.SetFloat("_Core",1/1.85f);config.layeredSlash.SetFloat("_Glow",1.45f/1.85f);config.layeredSlash.SetFloat("_ZTest",8);EditorUtility.SetDirty(config.layeredSlash);
            var popupFont=Resources.Load<TMPro.TMP_FontAsset>("Comic/ComicVietnamese");
            if(popupFont!=null)
            {
                var popup=Material("P10DamageNumbers",popupFont.material.shader.name,true);
                popup.CopyPropertiesFromMaterial(popupFont.material);popup.EnableKeyword("OUTLINE_ON");
                popup.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth,.3f);popup.SetColor(TMPro.ShaderUtilities.ID_OutlineColor,Color.black);popup.SetFloat(TMPro.ShaderUtilities.ID_FaceDilate,.12f);EditorUtility.SetDirty(popup);
            }
            config.fireFlipbook=Material("P10FireFlipbook","Campus Rift/P10 Comic Flipbook");config.smokeFlipbook=Material("P10SmokeFlipbook","Campus Rift/P10 Comic Flipbook");
            config.fireFlipbook.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Data+"P10FireFlipbook.png");config.smokeFlipbook.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Data+"P10SmokeFlipbook.png");config.smokeFlipbook.SetFloat("_Intensity",1);EditorUtility.SetDirty(config.fireFlipbook);EditorUtility.SetDirty(config.smokeFlipbook);
            config.fireRibbon=Material("P10FireRibbon","Campus Rift/P10 Scrolling Fire Ribbon");config.fireRibbon.mainTexture=config.fireFlipbook.mainTexture;EditorUtility.SetDirty(config.fireRibbon);
            config.frostMist=Material("P10FrostMist","Campus Rift/P10 Comic Flipbook");config.frostMist.mainTexture=config.smokeFlipbook.mainTexture;config.frostMist.SetFloat("_WhiteSmoke",1);config.frostMist.SetFloat("_Intensity",1.2f);EditorUtility.SetDirty(config.frostMist);
            config.layeredLightning=Material("P10LayeredLightning","Campus Rift/P10 Layered Stroke");config.layeredLightning.SetFloat("_Core",.10f);config.layeredLightning.SetFloat("_Glow",.68f);config.layeredLightning.SetFloat("_Lightning",1);EditorUtility.SetDirty(config.layeredLightning);
            config.solidEmission=Material("P10SolidEmission","Campus Rift/Speed Force Additive");config.solidEmission.SetFloat("_Intensity",1.8f);config.solidEmission.SetFloat("_CoreBoost",0);EditorUtility.SetDirty(config.solidEmission);
            config.additive=AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/SpeedForce/SF_Bolt.mat");config.fork=AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/SpeedForce/SF_Fork.mat");config.particles=AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/SpeedForce/SF_Flare.mat");config.ghost=AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Afterimage/Afterimage.mat");
            config.lightningCast=Clip("Assets/VFX/SpeedForce/Audio/laserSmall_000.ogg");config.lightningHit=Clip("Assets/VFX/SpeedForce/Audio/laserSmall_002.ogg");
            config.fireCast=Clip("Assets/Skills/GiantHandSeal/Audio/thrusterFire_000.ogg");config.fireHit=Clip("Assets/Skills/GiantHandSeal/Audio/explosionCrunch_000.ogg");config.iceCast=Clip("Assets/Skills/VoidWall/Audio/forceField_004.ogg");config.iceHit=Clip("Assets/Skills/VoidWall/Audio/impactGlass_heavy_000.ogg");config.bell=Clip("Assets/Skills/GiantHandSeal/Audio/impactBell_heavy_000.ogg");config.voidCast=Clip("Assets/Skills/VoidWall/Audio/forceField_004.ogg");config.voidHit=Clip("Assets/Skills/VoidWall/Audio/lowFrequency_explosion_000.ogg");config.sword=Clip("Assets/Skills/VoidWall/Audio/impactGlass_medium_000.ogg");EditorUtility.SetDirty(config);
            var d=Definition("tich-lich-nhat-thiem","Thunderclap Flash","Tích Lịch Nhất Thiểm",Element.Loi,SkillRole.Mobility,CastType.Instant,0,3,8,20,"Dash 10 m; 180% attack, Shock 0.5 s, invulnerable 0.2 s.","Lướt 10 m; 180% Công, Sốc 0,5 giây, miễn thương 0,2 giây.");
            var root=PrefabUtility.LoadPrefabContents(Prefab);ConfigureFlash(root,config,d);PrefabUtility.SaveAsPrefabAsset(root,Prefab);PrefabUtility.UnloadPrefabContents(root);
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");var player=UnityEngine.Object.FindAnyObjectByType<CampusExplorer>();ConfigureFlash(player.gameObject,config,d);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            SkillCatalogSetup.Install();AssetDatabase.SaveAssets();Debug.Log("P10 T01/T02 installed.");
        }
        [MenuItem("Campus Rift/V2/Upgrade Fix3 Visuals")]
        public static void UpgradeFix3Visuals()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
            var c=Resources.Load<SkillSet1VfxConfig>("SkillSet1Vfx");
            c.lotusSurface=Material("P10LotusSurface","Campus Rift/P10 Comic Energy");c.lotusSurface.SetFloat("_InkMask",8);EditorUtility.SetDirty(c.lotusSurface);
            c.fireBillow=Material("P10FireBillow","Campus Rift/P10 Rolling Fire Billow");c.fireBillow.mainTexture=c.smokeFlipbook.mainTexture;EditorUtility.SetDirty(c.fireBillow);
            c.copperMark=Material("P10CopperMark","Campus Rift/P10 Ground Mark");c.copperMark.SetFloat("_Metal",1);c.copperMark.enableInstancing=true;
            c.impactStar=Material("P10ImpactStar","Campus Rift/P10 Impact Star");
            c.slashStroke=Material("P10CrescentSlash","Campus Rift/P10 Layered Stroke");c.slashStroke.SetFloat("_Core",.54f);c.slashStroke.SetFloat("_Glow",.82f);c.slashStroke.SetFloat("_ZTest",8);
            c.reactionSurface=Material("P11PriorityEnergy","Campus Rift/P10 Comic Energy");c.reactionSurface.renderQueue=3120;
            c.reactionStroke=Material("P11PriorityStroke","Campus Rift/P10 Layered Stroke");c.reactionStroke.SetFloat("_Core",.12f);c.reactionStroke.SetFloat("_Glow",.70f);c.reactionStroke.SetFloat("_Lightning",1);c.reactionStroke.renderQueue=3130;
            c.reactionIce=Material("P11PriorityIce","Campus Rift/P10 Comic Energy");c.reactionIce.SetFloat("_ParticleTint",1);c.reactionIce.renderQueue=3120;
            foreach(var m in new[]{c.slashStroke,c.layeredLightning,c.reactionStroke,c.reactionSurface}){m.SetFloat("_InkMask",8);EditorUtility.SetDirty(m);}
            foreach(var m in new[]{c.copperMark,c.impactStar,c.slashStroke,c.reactionSurface,c.reactionStroke,c.reactionIce})EditorUtility.SetDirty(m);
            EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();
        }
        static void ConfigureFlash(GameObject player,SkillSet1VfxConfig config,SkillDefinition d)
        {
            Ensure<SkillVfxPool>(player).config=config;Ensure<GroundAimIndicator>(player);Ensure<SkillImpact>(player);Ensure<SkillCastPose>(player);Ensure<LightningFlashRuntime>(player).definition=d;
        }
        internal static T Ensure<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
        [MenuItem("Campus Rift/V2/Install Skill Set 1")]
        public static void InstallAll()
        {
            InstallFlash();
            var defs=new[] {
                Definition("phat-no-hoa-lien","Wrathful Fire Lotus","Phật Nộ Hỏa Liên",Element.Hoa,SkillRole.Burst,CastType.Aimed,1,1,20,45,"Charge 1.2 s; throw up to 18 m; 7 m blast, 450% attack, Burn 3 s, fire field 4 s.","Tụ lực 1,2 giây; ném tối đa 18 m; nổ 7 m, 450% Công, Bỏng 3 giây, vùng lửa 4 giây."),
                Definition("han-bang-phong-an","Ice Seal","Hàn Băng Phong Ấn",Element.Thuy,SkillRole.Control,CastType.Aimed,1,1,14,30,"90 degree cone, 10 m; 120% attack, Freeze 2.5 s. Bosses: 50% Chill.","Nón 90°, dài 10 m; 120% Công, Đóng Băng 2,5 giây. Boss: Chậm 50%."),
                Definition("than-kiem-ngu-loi","Lightning Sword","Thần Kiếm Ngự Lôi Chân Quyết",Element.Loi,SkillRole.Burst,CastType.Instant,1,1,12,35,"Six targets, 8 m per bounce; 200% attack, 10% less each bounce, clear line of sight.","Sáu mục tiêu, mỗi lần nảy tối đa 8 m; 200% Công, giảm 10% mỗi lần, cần đường nhìn thông suốt."),
                Definition("kim-chung-trao","Golden Bell","Kim Chung Tráo",Element.Kim,SkillRole.Defense,CastType.Instant,2,1,30,40,"Shield for 40% max health, 6 s; reflects 20% melee damage; 60% less sky-fire while active.","Khiên bằng 40% máu tối đa, 6 giây; phản 20% cận chiến; giảm 60% Thiên Hỏa khi còn khiên."),
                Definition("hac-dong-than-la","Black Hole Repulsion","Hắc Động Thần La",Element.KhongGian,SkillRole.Control,CastType.Aimed,3,1,22,50,"Place within 15 m; pull 9 m radius for 3 s; repel 4 m and deal 250% attack. Bosses resist pull.","Đặt trong 15 m; hút vùng 9 m trong 3 giây; hất 4 m, 250% Công. Boss kháng hút."),
                Definition("van-kiem-quyet","Sword Rain","Vạn Kiếm Quyết",Element.Kim,SkillRole.Burst,CastType.Aimed,4,1,18,50,"Thirty golden swords over 3 s in an 8 m area; each deals 40% attack.","Ba mươi kiếm vàng trong 3 giây, vùng 8 m; mỗi kiếm 40% Công.")};
            var config=AssetDatabase.LoadAssetAtPath<SkillSet1VfxConfig>(ResourcesPath+"SkillSet1Vfx.asset");
            config.rainConfig=AssetDatabase.LoadAssetAtPath<NguKiemConfig>(Data+"P10RainConfig.asset");
            if(config.rainConfig==null){config.rainConfig=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<NguKiemConfig>("Assets/Combat/Data/NguKiemConfig.asset"));AssetDatabase.CreateAsset(config.rainConfig,Data+"P10RainConfig.asset");}
            config.rainConfig.bladeColor=new Color(1,.65f,.1f);config.rainConfig.tipColor=Color.white;config.rainConfig.bladeMaterial=config.surface;config.rainConfig.trailMaterial=config.additive;EditorUtility.SetDirty(config.rainConfig);EditorUtility.SetDirty(config);
            var root=PrefabUtility.LoadPrefabContents(Prefab);ConfigureAll(root,defs);PrefabUtility.SaveAsPrefabAsset(root,Prefab);PrefabUtility.UnloadPrefabContents(root);
            var scene=EditorSceneManager.GetActiveScene();var player=UnityEngine.Object.FindAnyObjectByType<CampusExplorer>();ConfigureAll(player.gameObject,defs);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            SkillCatalogSetup.Install();AssetDatabase.SaveAssets();Debug.Log("P10: all ten MVP skills installed.");
        }
        static void ConfigureAll(GameObject go,SkillDefinition[] defs)
        {
            Ensure<FireLotusRuntime>(go).definition=defs[0];Ensure<IceSealRuntime>(go).definition=defs[1];Ensure<ChainLightningRuntime>(go).definition=defs[2];Ensure<GoldenBellRuntime>(go).definition=defs[3];Ensure<BlackHoleRuntime>(go).definition=defs[4];Ensure<SwordRainRuntime>(go).definition=defs[5];
            Ensure<CampusRift.Progression.BuffSystem>(go);
            Ensure<SkillProgressBridge>(go);
        }
        static AudioClip Clip(string path){var c=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(c==null)throw new InvalidOperationException("Missing SFX "+path);return c;}
        static Material Material(string name,string shader,bool resource=false)
        {
            string path=(resource?ResourcesPath:Data)+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find(shader)){name=name};AssetDatabase.CreateAsset(m,path);}return m;
        }
        internal static SkillDefinition Definition(string id,string en,string vi,Element element,SkillRole role,CastType cast,int realm,int tier,float cooldown,float spirit,string description,string descriptionVN)
        {
            string path=Data+id+".asset";var d=AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);if(d==null){d=ScriptableObject.CreateInstance<SkillDefinition>();AssetDatabase.CreateAsset(d,path);}
            d.id=id;d.displayName=en;d.displayNameVN=vi;d.shortName=en.ToUpperInvariant();d.element=element;d.role=role;d.castType=cast;d.starter=false;d.unlockRealm=realm;d.unlockTier=tier;d.cooldown=cooldown;d.spiritCost=spirit;d.description=description;d.descriptionVN=descriptionVN;
            string iconPath="Assets/Resources/ContentImages/Skills/"+id+".png";
            var importer=AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if(importer!=null&&importer.textureType!=TextureImporterType.Sprite){importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.SaveAndReimport();}
            d.icon=AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);if(d.icon==null)throw new InvalidOperationException("Missing comic icon "+id);
            d.ranks=new SkillRank[5];int[] costs={0,150,400,900,1600};for(int i=0;i<5;i++)d.ranks[i]=new SkillRank{effectMultiplier=1+.12f*i,cooldownMultiplier=1-.05f*i,upgradeCost=costs[i],requiredRealm=Mathf.Min(6,realm+i)};
            EditorUtility.SetDirty(d);return d;
        }
    }
}
#endif
