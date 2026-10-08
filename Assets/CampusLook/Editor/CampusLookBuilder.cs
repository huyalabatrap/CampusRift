#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;

namespace CampusRift.Look
{
    public static class CampusLookBuilder
    {
        public const string Model="Assets/Models/Comic_Vibrant_Elevator_System_T77/Comic_Vibrant_Elevator_System_T77.fbx";
        const string Root="Assets/CampusLook/";
        [Serializable] public sealed class Audit {public List<string> assignments=new List<string>();public int oldSlots,newSlots,renderers,oldMaterials,newMaterials;}
        static Texture Tex(string id,string kind)=>AssetDatabase.LoadAssetAtPath<Texture>(Root+"Textures/"+id+"_"+kind+".png");
        static Material Surface(string id,string source,Color tint,float scale,float age,float wall=0,float metal=0,float smooth=.55f)
        {
            string path=Root+"Materials/"+id+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Campus Rift/Weathered URP Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetTexture("_BaseMap",Tex(source,"base"));m.SetTexture("_BumpMap",Tex(source,"normal"));m.SetTexture("_MaskMap",Tex(source,"arm"));
            m.SetColor("_BaseColor",tint);m.SetFloat("_WorldScale",scale);m.SetFloat("_Age",age);m.SetFloat("_Wall",wall);
            m.SetFloat("_Decals",wall);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.SetFloat("_NormalScale",.42f);m.enableInstancing=true;
            EditorUtility.SetDirty(m);return m;
        }
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Exit Play first");
            Directory.CreateDirectory(Root+"Materials");Directory.CreateDirectory(Root+"Resources/CampusLook");AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(Root+"Textures"))
            {
                if(file.EndsWith(".meta"))continue;var importer=AssetImporter.GetAtPath(file) as TextureImporter;if(importer==null)continue;
                importer.textureType=file.Contains("_normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=file.Contains("_base")||file.Contains("sky");importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
                importer.maxTextureSize=file.Contains("sky")?2048:1024;importer.anisoLevel=file.Contains("sky")?1:4;importer.isReadable=false;
                var android=importer.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=importer.maxTextureSize;
                android.format=TextureImporterFormat.ASTC_6x6;android.compressionQuality=50;importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();
            }
            var cream=Surface("Faded-ivory-plaster","plastered_wall_04",Color.white,.45f,.6f,1);
            var yellow=Surface("Faded-ochre-plaster","plastered_wall_04",new Color(1,.94f,.78f),.45f,.55f,1);
            var concrete=Surface("Weathered-concrete","concrete_floor_worn_001",new Color(.92f,.9f,.83f),.5f,.26f);
            var paving=Surface("Red-brick-courtyard","brick_pavement",new Color(1.15f,1.03f,.91f),.5f,.16f);
            var terrazzo=Surface("Old-terrazzo-corridor","concrete_floor_worn_001",new Color(1,.98f,.91f),1,.15f);
            var metal=Surface("Chipped-iron-frames","rusty_painted_metal",new Color(.62f,.66f,.61f),.8f,.23f,0,.45f,.4f);
            var blue=Surface("Faded-blue-steel","rusty_painted_metal",new Color(.58f,.65f,.63f),.7f,.25f,0,.35f,.45f);
            var roof=Surface("Faded-rust-zinc-roof","rusty_corrugated_iron",new Color(.85f,.8f,.7f),.5f,.15f,0,.5f,.4f);
            var wood=Surface("Old-wood","wood_planks_grey",new Color(.88f,.75f,.56f),.6f,.15f);
            var soil=Surface("Planter-earth","brown_mud_03",Color.white,.6f,.1f);
            var grass=Surface("Muted-campus-grass","sparse_grass",new Color(.87f,.93f,.72f),.6f,.1f);
            var glass=Surface("Dusty-window-glass","dusty_window",new Color(.27f,.35f,.36f),.4f,.06f,0,.15f,.9f);
            glass.SetFloat("_NormalScale",0);
            var importerModel=(ModelImporter)AssetImporter.GetAtPath(Model);
            var names=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Material>().Select(m=>m.name).ToArray();
            var audit=new Audit();
            foreach(var name in names)
            {
                Material target=null;var n=name.ToLowerInvariant();
                if(n.Contains("sign")||n.Contains("logo")||n.Contains("letter")||n.Contains("led")||n.Contains("screen")||n.Contains("car_")||n.Contains("scooter")||n.Contains("street_lamp"))continue;
                if(n.Contains("glass"))target=glass;
                else if(n=="lift_pale_lobby")target=terrazzo;
                else if(n=="cm_paper"||n=="k_mat_white"||n=="mat_lobby_warminterior"||n=="lift_comic_indigo_shaft")target=cream;
                else if(n=="lift_door_silver"||n=="mat_panic_bar_red"||n=="mat_aluminum_darkcharcoal")target=metal;
                else if(n=="cm_blue"||n=="lift_cyan_accent")target=blue;
                else if(n=="cm_red"||n.Contains("roof"))target=roof;
                else if(n.Contains("soil"))target=soil;
                else if(n.Contains("lawn")||n.Contains("grass"))target=grass;
                else if(n.Contains("wall_accent")||n.Contains("blue_steel")||n.Contains("steel_iuhblue")||n.Contains("teal_acp"))target=blue;
                else if(n.Contains("wall_concrete")||n.Contains("concrete_pier")||n.Contains("concrete_trim")||n.Contains("curb")||n.Contains("concrete_struct"))target=concrete;
                else if(n.Contains("wall")||n.Contains("plaster")||n.Contains("ceiling")||n.Contains("acp_white")||n.Contains("paint_white"))target=n.Contains("campus")||n.Contains("corewall")?yellow:cream;
                else if(n.Contains("paving")||n.Contains("pavement"))target=paving;
                else if(n.Contains("floor")||n.Contains("granite")||n.Contains("concrete"))target=terrazzo;
                else if(n.Contains("wood")||n.Contains("desk_top")||n.Contains("trunk")||n.Contains("bark")||n.Contains("branches"))target=wood;
                else if(n.Contains("steel")||n.Contains("stainless")||n.Contains("metal")||n.Contains("frame")||n.Contains("railing")||n.Contains("iron")||n.Contains("downspout"))target=metal;
                if(target==null)continue;
                importerModel.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),target);audit.assignments.Add(name+" -> "+target.name);
            }
            var campus=GameObject.Find("Comic_Vibrant_Elevator_System_T77");var rs=campus.GetComponentsInChildren<Renderer>(true);
            audit.renderers=rs.Length;audit.oldSlots=rs.Sum(r=>r.sharedMaterials.Length);audit.oldMaterials=rs.SelectMany(r=>r.sharedMaterials).Distinct().Count();
            var old=rs.ToDictionary(r=>r,r=>r.sharedMaterials.Select(m=>m!=null?m.name:"").ToArray());
            importerModel.SaveAndReimport();
            var maps=importerModel.GetExternalObjectMap();
            foreach(var pair in old)
            {
                var mats=pair.Key.sharedMaterials;
                for(int i=0;i<mats.Length;i++)
                {var key=new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Value[i]);UnityEngine.Object obj;if(maps.TryGetValue(key,out obj))mats[i]=obj as Material;}
                pair.Key.sharedMaterials=mats;EditorUtility.SetDirty(pair.Key);
            }
            audit.newSlots=rs.Sum(r=>r.sharedMaterials.Length);audit.newMaterials=rs.SelectMany(r=>r.sharedMaterials).Distinct().Count();
            // Existing vertex/UV/lightmap and renderer properties remain intact. Only slots change.
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            profile.components.RemoveAll(component=>component==null);
            ColorAdjustments color;if(!profile.TryGet(out color))color=profile.Add<ColorAdjustments>(true);
            if(!AssetDatabase.Contains(color))AssetDatabase.AddObjectToAsset(color,profile);
            color.saturation.Override(-7);color.contrast.Override(3);color.postExposure.Override(.12f);
            WhiteBalance wb;if(!profile.TryGet(out wb))wb=profile.Add<WhiteBalance>(true);wb.temperature.Override(3);wb.tint.Override(0);
            if(!AssetDatabase.Contains(wb))AssetDatabase.AddObjectToAsset(wb,profile);
            Vignette vig;if(profile.TryGet(out vig))vig.intensity.Override(.12f);
            EditorUtility.SetDirty(profile);
            var sun=RenderSettings.sun;if(sun==null)sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l=>l.type==LightType.Directional);
            if(sun!=null){sun.color=new Color(1,.965f,.91f);sun.shadows=LightShadows.Soft;EditorUtility.SetDirty(sun);}
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.53f,.55f,.58f);
            RenderSettings.ambientEquatorColor=new Color(.39f,.38f,.35f);
            RenderSettings.ambientGroundColor=new Color(.26f,.25f,.23f);
            foreach(string skyName in new[]{"day","dusk","night","blood","inferno","eclipse"})
            {
                string path=Root+"Resources/CampusLook/sky-"+skyName+".mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(sky==null){sky=new Material(Shader.Find("Campus Rift/Real Campus Sky"));AssetDatabase.CreateAsset(sky,path);}
                sky.shader=Shader.Find("Campus Rift/Real Campus Sky");
                sky.SetFloat("_Celestial",skyName=="night"?1:skyName=="blood"?2:skyName=="eclipse"?3:0);
                sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture>(Root+"Textures/campus-real-sky.jpg"));sky.SetFloat("_Exposure",skyName=="night"?.4f:1);
                Color tint=skyName=="night"?new Color(.24f,.32f,.55f):skyName=="blood"?new Color(.7f,.25f,.22f):skyName=="eclipse"?new Color(.46f,.3f,.14f):skyName=="inferno"?new Color(.8f,.44f,.27f):skyName=="dusk"?new Color(.65f,.52f,.4f):new Color(.5f,.5f,.5f);
                sky.SetColor("_Tint",tint);EditorUtility.SetDirty(sky);
            }
            RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>(Root+"Resources/CampusLook/sky-day.mat");
            // Strong full-screen comic outlines default off; user can enable the reduced PC pass.
            var ink=AssetDatabase.LoadAssetAtPath<Material>("Assets/CampusRiftUI/Comic/Resources/Comic/ComicInk.mat");
            ink.SetFloat("_InkStrength",.12f);ink.SetFloat("_Saturation",1);ink.SetFloat("_Contrast",1);EditorUtility.SetDirty(ink);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(campus.scene);EditorSceneManager.SaveScene(campus.scene);
            File.WriteAllText("task/look/material-audit.json",JsonUtility.ToJson(audit,true));
        }
    }
}
#endif
