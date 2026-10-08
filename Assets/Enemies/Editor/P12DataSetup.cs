using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using CampusRift.Enemies;
using CampusRift.Monsters;
using CampusRift.Combat;
using CampusRift.Levels;
using CampusRift.SkyBeast;
public static class P12DataSetup
{
    static T Asset<T>(string path) where T:ScriptableObject=>P12AssetSetup.Asset<T>(path);
    static readonly string[] ids={"tieu-yeu","doc-nhan","liem-hon","thiet-giap-nguu","bao-thi","quang-ma","hoa-trung"};
    static EnemyArchetype Arch(string id)=>AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/"+id+".asset");
    static T Ability<T>(string id,float warning,float range,float radius,string clip,float impact) where T:EnemyAbility
    {var a=Asset<T>("Assets/Enemies/Data/Abilities/"+id+".asset");a.id=id;a.telegraphSeconds=warning;a.range=range;a.radius=radius;a.clip=clip;a.impactSeconds=impact;a.cooldown=6;EditorUtility.SetDirty(a);return a;}
    [MenuItem("Campus Rift/P12/Install Gameplay Data")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        Directory.CreateDirectory("Assets/Enemies/Resources/P12");Directory.CreateDirectory("Assets/SkyBeast/Resources/P12");Directory.CreateDirectory("Assets/Resources/EnemyVfx");AssetDatabase.Refresh();
        var leap=Ability<LeapAbility>("leap",.6f,6,1.5f,"Happy_Bounce",.65f);
        var spread=Ability<SpreadShotAbility>("spread-shot",.8f,14,1,"Spore_Cast",.7f);
        var charge=Ability<ChargeAbility>("charge",1,9,.8f,"Roar_Both",1);
        var explode=Ability<ExplodeAbility>("explode",.4f,3.5f,3.5f,"Sniff",.4f);
        Arch("tieu-yeu").abilities=new List<AbilityUnlock>{new AbilityUnlock{minLevel=4,ability=leap}};
        Arch("doc-nhan").abilities=new List<AbilityUnlock>{new AbilityUnlock{minLevel=5,ability=spread}};
        Arch("thiet-giap-nguu").abilities=new List<AbilityUnlock>{new AbilityUnlock{minLevel=1,ability=charge}};
        Arch("bao-thi").abilities=new List<AbilityUnlock>{new AbilityUnlock{minLevel=1,ability=explode}};
        foreach(var id in ids)EditorUtility.SetDirty(Arch(id));
        var imp=Asset<EnemyArchetype>("Assets/Enemies/Resources/P12/TieuYeu.asset");EditorUtility.CopySerialized(Arch("tieu-yeu"),imp);EditorUtility.SetDirty(imp);
        var elite=Shaban("ShabanElite","Săn Hồn Giả",500,false);
        var boss5=Shaban("ShabanBoss5","Săn Hồn Giả",500,true);
        var boss7=Shaban("ShabanBoss7","Săn Hồn Thức Tỉnh",750,true);
        int[][] progression={new[]{0,2,6},new[]{0,1,2,6},new[]{0,1,2,3},new[]{0,1,2,3,4},new[]{0,1,2,3,4},new[]{0,1,2,3,4,5},new[]{0,1,2,3,4,5},new[]{0,1,2,3,4,5,6},new[]{0,1,2,3,4,5,6},new[]{0,1,2,3,4,5,6}};
        for(int level=1;level<=10;level++){
            var d=AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/Levels/Data/Level"+level.ToString("00")+".asset");
            // The 14 monsters in level 3 include the elite spawned by LevelDirector.
            if(level==3&&d.TotalMonsters==14){var last=d.waves.Last();int index=last.entries.FindIndex(e=>e.count>1);var entry=last.entries[index];entry.count--;last.entries[index]=entry;}
            var table=Asset<LevelSpawnTable>("Assets/Levels/Data/Spawn"+level.ToString("00")+".asset");table.roster.Clear();
            foreach(int i in progression[level-1]){
                string id=ids[i];float weight=id=="tieu-yeu"?3:id=="thiet-giap-nguu"?.65f:id=="bao-thi"?.7f:1.5f;
                if(level==2&&(id=="doc-nhan"||id=="hoa-trung"))weight*=2;if(level==4&&(id=="liem-hon"||id=="bao-thi"))weight*=2;if(level==6&&id=="quang-ma")weight*=2;
                table.roster.Add(new SpawnWeight{archetype=Arch(id),weight=weight,minPerWave=1,maxPerWave=id=="thiet-giap-nguu"?3:99});
            }
            table.maxHeavyConcurrent=2;table.maxBombersConcurrent=2;table.minRangedFraction=table.roster.Any(r=>r.archetype.ranged)?.15f:0;
            table.maxRangedFraction=.45f;table.maxBomberFraction=.2f;table.finalWave.Clear();
            int count=d.waves.Last().TotalCount;foreach(var r in table.roster)table.finalWave.Add(new SpawnEntry{archetype=r.archetype,count=1});
            for(int extra=table.roster.Count;extra<count;extra++){
                string add=extra%3==0&&table.roster.Any(r=>r.archetype.id=="doc-nhan")?"doc-nhan":"tieu-yeu";
                int idx=table.finalWave.FindIndex(e=>e.archetype.id==add);var e=table.finalWave[idx];e.count++;table.finalWave[idx]=e;
            }
            d.spawnTable=table;d.bosses.Clear();if(level==5)d.bosses.Add(boss5);if(level==7)d.bosses.Add(boss7);
            if(level<=7)d.aiTier=Mathf.Min(2,d.aiTier);
            if(level==6)d.stars[2]=new StarCondition{kind=StarKind.BeatParTime,value=660,descriptionVN="Hoàn thành dưới 11:00",descriptionEN="Finish under 11:00"};
            EditorUtility.SetDirty(d);EditorUtility.SetDirty(table);
        }
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/EnemyVfx/P12Telegraph.mat");if(material==null){material=new Material(Shader.Find("Campus Rift/P10 Alpha Stroke"));AssetDatabase.CreateAsset(material,"Assets/Resources/EnemyVfx/P12Telegraph.mat");}
        material.SetTexture("_BaseMap",Texture2D.whiteTexture);EditorUtility.SetDirty(material);
        var bolt=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/EnemyVfx/P12Bolt.mat");if(bolt==null){bolt=new Material(Shader.Find("Campus Rift/Enemy Bolt Comic"));AssetDatabase.CreateAsset(bolt,"Assets/Resources/EnemyVfx/P12Bolt.mat");}
        bolt.SetColor("_Tint",Color.white);EditorUtility.SetDirty(bolt);
        foreach(var row in new[]{new[]{"020","020_SilverCloudDragon"},new[]{"023","023_AzureSerpentDragon"},new[]{"026","026_LavaWingDragon"}}){
            var data=Asset<SkyBeastDefinition>("Assets/SkyBeast/Resources/P12/Dragon"+row[0]+".asset");data.id=row[0];data.displayName=row[1];data.prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SkyBeast/Prefabs/"+row[1]+".prefab");
            data.scale=9;data.altitude=row[0]=="020"?120:row[0]=="023"?110:110;data.orbitRadius=105;data.cruiseSpeed=12;
            data.roarState=row[0]=="026"?"Ground_Roar":"Air_Roar";data.roars=Enumerable.Range(1,4).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SkyBeast/Audio/Designed/"+row[0]+"-roar-"+i+".wav")).ToArray();
            data.rumble=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SkyBeast/Audio/Designed/dragon-rumble.wav");data.wind=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SkyBeast/Audio/Designed/dragon-wind.wav");EditorUtility.SetDirty(data);
        }
        foreach(var path in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/SkyBeast/Audio/Designed","Assets/Enemies/Resources/P12"}).Select(AssetDatabase.GUIDToAssetPath)){
            var importer=AssetImporter.GetAtPath(path) as AudioImporter;if(importer==null)continue;var s=importer.defaultSampleSettings;s.loadType=path.EndsWith(".ogg")?AudioClipLoadType.Streaming:AudioClipLoadType.CompressedInMemory;s.compressionFormat=AudioCompressionFormat.Vorbis;s.quality=.75f;importer.defaultSampleSettings=s;importer.forceToMono=!path.EndsWith(".ogg");importer.SaveAndReimport();}
        AssetDatabase.SaveAssets();Debug.Log("P12 gameplay data installed: 10 rosters, Shaban elite / boss5 / boss7, 3 dragon definitions");
    }
    static EnemyArchetype Shaban(string name,string vn,float hp,bool boss)
    {
        string path="Assets/Enemies/Prefabs/"+name+".prefab";
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MonsterShaban/Monster_Shaban.prefab"));go.name=name;
        P12AssetSetup.Get<MonsterVitality>(go);P12AssetSetup.Get<StatusEffectHost>(go);P12AssetSetup.Get<EnemyInstance>(go);P12AssetSetup.Get<ShabanEnemyBridge>(go);P12AssetSetup.Get<BossController>(go);
        foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=7;go.GetComponent<NavMeshAgent>().enabled=false;
        var prefab=PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);
        var a=Asset<EnemyArchetype>("Assets/Enemies/Resources/P12/"+name+".asset");a.id=name=="ShabanElite"?"shaban-elite":name=="ShabanBoss5"?"shaban-boss5":"shaban-boss7";
        a.prefab=prefab;a.prefabPending=false;a.displayName=name;a.displayNameVN=vn;a.baseHealth=hp;a.baseDamage=25;a.element=Element.Am;a.baseSpeed=7.5f;a.swordIntentWeight=4;a.isBoss=boss;a.resistHardControl=boss;a.defense=boss?(name=="ShabanBoss7"?.25f:.45f):0;a.lateHealthMultiplier=1;EditorUtility.SetDirty(a);return a;
    }
}
