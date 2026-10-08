#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CampusRift.Levels;
using CampusRift.UI;
namespace CampusRift.SkyBeast
{
    public static class P14DevMenu
    {
        static void Level(int index)
        {if(!Application.isPlaying){Debug.LogWarning("P14 DEV: enter Play Mode first");return;}UIStateManager.Instance?.EnterScene(true);LevelDirector.Ensure().Begin(LevelCatalog.Instance.Get(index));}
        [MenuItem("Campus Rift/DEV/P14/Level 8")]static void Eight()=>Level(8);
        [MenuItem("Campus Rift/DEV/P14/Level 9")]static void Nine()=>Level(9);
        [MenuItem("Campus Rift/DEV/P14/Level 10")]static void Ten()=>Level(10);
        [MenuItem("Campus Rift/DEV/P14/Apply Sky Sword Hit")]static void Hit()=>SkyBeastScheduler.Instance?.ApplySkySwordHit();
        [MenuItem("Campus Rift/DEV/P14/Long No warning")]static void Fury()=>SkyBeastScheduler.Instance?.Fury?.Request();
        [MenuItem("Campus Rift/DEV/P14/Feather barrage")]static void Feathers()=>Object.FindAnyObjectByType<FeatherBarrage>()?.TryDrop();
        [MenuItem("Campus Rift/DEV/P14/Meteor shower")]static void Meteors()=>Object.FindAnyObjectByType<MeteorShower>()?.TryDrop();
        [MenuItem("Campus Rift/Validation/Sky Beast Smoke")]static void Smoke(){if(Application.isPlaying)new GameObject("P14 smoke").AddComponent<SkyBeastPlayTest>();}
    }
}
#endif
