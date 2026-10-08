#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CampusRift.SkyBeast
{
    public static class P13Setup
    {
        [MenuItem("Campus Rift/V2/Setup P13 Fire Breath")]
        public static void Apply()
        {
            const string folder="Assets/SkyBeast/Resources/P13/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            foreach(var file in Directory.GetFiles(folder,"*.png"))
            {
                var i=(TextureImporter)AssetImporter.GetAtPath(file);i.textureType=TextureImporterType.Sprite;i.spritePixelsPerUnit=256;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.maxTextureSize=512;
                var a=i.GetPlatformTextureSettings("Android");a.overridden=true;a.format=TextureImporterFormat.ASTC_6x6;a.maxTextureSize=512;i.SetPlatformTextureSettings(a);i.SaveAndReimport();
            }
            foreach(var file in new[]{folder+"alarm.ogg",folder+"fire.wav"})
            {
                var importer=AssetImporter.GetAtPath(file) as AudioImporter;if(importer==null)continue;
                var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.CompressedInMemory;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.55f;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
            }
            foreach(var id in new[]{"flame","smoke","spark","scorch","line"})
                foreach(bool glow in new[]{false,true})
                {
                    string path=folder+id+"-"+(glow?"glow":"alpha")+".mat";
                    var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Campus Rift/Fire Sprite"));AssetDatabase.CreateAsset(m,path);}
                    m.SetTexture("_MainTex",id=="line"?Texture2D.whiteTexture:AssetDatabase.LoadAssetAtPath<Texture2D>(folder+id+".png"));m.SetFloat("_DstBlend",glow?1:10);m.SetColor("_Color",Color.white);EditorUtility.SetDirty(m);
                }
            for(int level=8;level<=10;level++)for(int phase=1;phase<=(level==10?3:level==9?2:1);phase++)
            {
                string path=folder+"Fire"+level+"Phase"+phase+".asset";var p=AssetDatabase.LoadAssetAtPath<FireBreathProfile>(path);
                if(p==null){p=ScriptableObject.CreateInstance<FireBreathProfile>();AssetDatabase.CreateAsset(p,path);}
                p.level=level;p.phase=phase;p.cycleSeconds=level==8?45:level==9?(phase==1?40:32):(phase==1?20:phase==2?28:25);
                p.warningSeconds=level==10&&phase==3?4:6;p.breathSeconds=4;p.afterfireSeconds=10;
                p.outdoorDamage=level==8?280:level==9?416:600;p.partialDamage=level==8?126:level==9?187:270;p.indoorDamage=level==8?34:level==9?50:72;p.recommendedHealth=level==8?500:level==9?650:820;EditorUtility.SetDirty(p);
            }
            var graph=AssetDatabase.LoadAssetAtPath<ShelterGraphReference>(folder+"ShelterGraph.asset");
            if(graph==null){graph=ScriptableObject.CreateInstance<ShelterGraphReference>();AssetDatabase.CreateAsset(graph,folder+"ShelterGraph.asset");}
            graph.graph=AssetDatabase.LoadAssetAtPath<Monsters.RoomGraph>("Assets/MonsterShaban/CampusRoomGraph.asset");EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();EnvironmentLayerTool.Assign();
        }
        [MenuItem("Campus Rift/DEV/Thiên Hỏa (level 8 damage)")]
        public static void Dev(){if(!Application.isPlaying){Debug.LogWarning("Enter Play Mode first");return;}FireBreathCycle.Ensure().StartDev(8);}
        [MenuItem("Campus Rift/DEV/Long Nộ (once)")]
        public static void Fury(){if(Application.isPlaying)FireBreathCycle.Instance?.StartFury();}
    }
}
#endif
