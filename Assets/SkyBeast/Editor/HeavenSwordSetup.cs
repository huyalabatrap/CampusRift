#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CampusRift.UI;
using CampusRift.Progression;
namespace CampusRift.SkyBeast
{
    public static class HeavenSwordSetup
    {
        [MenuItem("Campus Rift/V2/P15/Build config")]
        public static void Build()
        {
            const string dir="Assets/SkyBeast/Resources/P15/";System.IO.Directory.CreateDirectory(dir);
            var cfg=AssetDatabase.LoadAssetAtPath<HeavenSwordConfig>(dir+"HeavenSword.asset");if(cfg==null){cfg=ScriptableObject.CreateInstance<HeavenSwordConfig>();AssetDatabase.CreateAsset(cfg,dir+"HeavenSword.asset");}
            var gold=AssetDatabase.LoadAssetAtPath<Material>(dir+"HeavenGold.mat");if(gold==null){gold=new Material(Shader.Find("Campus Rift/Heaven Sword Gold"));AssetDatabase.CreateAsset(gold,dir+"HeavenGold.mat");}
            cfg.gold=gold;cfg.blade=AssetDatabase.LoadAssetAtPath<Combat.NguKiemConfig>("Assets/Combat/Data/NguKiemConfig.asset");cfg.sounds=AssetDatabase.LoadAssetAtPath<Skills.GiantHandConfig>("Assets/Skills/GiantHandSeal/GiantHandConfig.asset");cfg.ready=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Skills/GiantHandSeal/Audio/impactBell_heavy_000.ogg");cfg.victory=AssetDatabase.LoadAssetAtPath<AudioClip>(dir+"victory.wav");EditorUtility.SetDirty(cfg);AssetDatabase.SaveAssets();
        }
        [MenuItem("Campus Rift/DEV/P15/Level 8 Hoa Than")]
        public static void Level8()=>Begin(8,Realm.HoaThan);
        [MenuItem("Campus Rift/DEV/P15/Level 9 Luyen Hu")]
        public static void Level9()=>Begin(9,Realm.LuyenHu);
        [MenuItem("Campus Rift/DEV/P15/Level 10 Do Kiep")]
        public static void Level10()=>Begin(10,Realm.DoKiep);
        static void Begin(int level,Realm realm)
        {
            if(!Application.isPlaying)return;UIStateManager.Instance.EnterScene(true);
            // DEV uses a disposable save; it never promotes the user's real cultivation.
            var profile=ProfileService.Ensure();profile.UseTransient(new ProfileData{cultivation=new CultivationData{realm=(int)realm,tier=1}});
            Levels.LevelSession.Select(level);Levels.LevelDirector.Ensure().Begin(Levels.LevelCatalog.Instance.Get(level));
        }
        [MenuItem("Campus Rift/DEV/P15/Defeat current enemies")]
        public static void Defeat()
        {
            if(!Application.isPlaying)return;foreach(var e in new System.Collections.Generic.List<Enemies.EnemyInstance>(Enemies.EnemyDirector.Instance.Active))if(e!=null&&e.Alive&&e.countsForSwordIntent)
            {var info=Combat.DamageInfo.Create(1000000,Combat.Element.Kim,Combat.DamageSource.Skill,e.transform.position,Vector3.down);info.skillId="p15-dev";e.Vitality.ApplyDamage(info);}
        }
        [MenuItem("Campus Rift/DEV/P15/End disposable profile")]
        public static void EndDev(){Levels.LevelDirector.Instance?.End();ProfileService.Instance?.EndTransient();}
    }
}
#endif
