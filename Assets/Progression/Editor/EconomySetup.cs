#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CampusRift.Combat;

namespace CampusRift.Progression
{
    // Creates (or refreshes) the economy assets of plan §9: EconomyConfig, the 10 MVP items, the 3 MVP artifacts and the catalog.
    // Safe to run again: existing assets keep their GUIDs and only get the table values back.
    public static class EconomySetup
    {
        const string Data = "Assets/Progression/Data", Resources = "Assets/Progression/Resources";

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path); return created;
        }

        static ItemEffect Heal(float f) => new ItemEffect { type = ItemEffectType.HealInstant, value = f };
        static ItemEffect HealOver(float f, float s) => new ItemEffect { type = ItemEffectType.HealOverTime, value = f, duration = s };
        static ItemEffect Spirit(float f) => new ItemEffect { type = ItemEffectType.RestoreSpirit, value = f };
        static ItemEffect Revive(float f, float invulnerable) => new ItemEffect { type = ItemEffectType.Revive, value = f, duration = invulnerable };
        static ItemEffect Stat(StatType t, float v, float s) => new ItemEffect { type = ItemEffectType.Stat, stat = t, value = v, duration = s };
        static ItemEffect Flag(ItemFlag f, float v, float s) => new ItemEffect { type = ItemEffectType.Flag, flag = f, value = v, duration = s };

        [MenuItem("Campus Rift/V2/Install Economy Data")]
        public static void Install()
        {
            var config = Asset<EconomyConfig>(Resources + "/EconomyConfig.asset"); EditorUtility.SetDirty(config);
            var catalog = Asset<ItemCatalog>(Resources + "/ItemCatalog.asset");
            catalog.items.Clear(); catalog.artifacts.Clear();

            void Item(string id, string en, string vn, string descEN, string descVN, ItemKind kind, int price, int max, Realm from, ItemShape shape, Color tint, params ItemEffect[] effects)
            {
                var item = Asset<ItemDefinition>(Data + "/Items/" + id + ".asset");
                item.id = id; item.nameEN = en; item.nameVN = vn; item.descriptionEN = descEN; item.descriptionVN = descVN;
                item.kind = kind; item.price = price; item.maxPerLevel = max; item.availableFrom = from; item.shape = shape; item.tint = tint;
                item.effects = new List<ItemEffect>(effects);
                EditorUtility.SetDirty(item); catalog.items.Add(item);
            }
            var green = new Color(.35f, .9f, .5f); var jade = new Color(.3f, .85f, .75f); var blue = new Color(.4f, .6f, 1f);
            var gold = new Color(1f, .8f, .3f); var red = new Color(1f, .4f, .35f); var violet = new Color(.7f, .5f, 1f); var ice = new Color(.6f, .9f, 1f);
            // Recovery
            Item("hoi-khi-dan", "Qi Recovery Pill", "Hồi Khí Đan", "Restores 20% of your health at once.", "Hồi ngay 20% máu.", ItemKind.Heal, 25, 5, Realm.LuyenKhi, ItemShape.Pill, green, Heal(.20f));
            Item("hoi-xuan-dan", "Spring Renewal Pill", "Hồi Xuân Đan", "Restores 45% of your health over 3 seconds.", "Hồi 45% máu trong 3 giây.", ItemKind.Heal, 60, 3, Realm.TrucCo, ItemShape.Pill, jade, HealOver(.45f, 3f));
            Item("tu-linh-dan", "Spirit Gathering Pill", "Tụ Linh Đan", "Restores 50% of your spirit.", "Hồi 50% Linh Lực.", ItemKind.Heal, 40, 3, Realm.LuyenKhi, ItemShape.Pill, blue, Spirit(.5f));
            Item("ho-menh-phu", "Life-Guarding Talisman", "Hộ Mệnh Phù", "Carried automatically: revives you once with 40% health when you would fall.", "Tự hồi sinh 1 lần với 40% máu khi sắp gục.", ItemKind.Revive, 200, 1, Realm.KetDan, ItemShape.Talisman, gold, Revive(.4f, 2f));
            // Power
            Item("cuong-luc-dan", "Fury Pill", "Cuồng Lực Đan", "+30% damage for 60 seconds.", "+30% sát thương trong 60 giây.", ItemKind.Buff, 80, 2, Realm.TrucCo, ItemShape.Pill, red, Stat(StatType.DamageDealt, .30f, 60f));
            Item("kim-cuong-phu", "Diamond Talisman", "Kim Cương Phù", "Take 35% less damage for 45 seconds.", "Giảm 35% sát thương nhận vào trong 45 giây.", ItemKind.Buff, 80, 2, Realm.TrucCo, ItemShape.Talisman, gold, Stat(StatType.DamageTaken, -.35f, 45f));
            Item("than-hanh-phu", "Swift Step Talisman", "Thần Hành Phù", "+30% run speed for 40 seconds.", "+30% tốc chạy trong 40 giây.", ItemKind.Buff, 50, 2, Realm.LuyenKhi, ItemShape.Talisman, jade, Stat(StatType.MoveSpeed, .30f, 40f));
            // Levels 8-10
            Item("ti-hoa-chau", "Fire-Ward Pearl", "Tị Hỏa Châu", "Sky-fire deals 50% less damage for 90 seconds.", "Giảm 50% sát thương Thiên Hỏa trong 90 giây.", ItemKind.FireWard, 120, 2, Realm.HoaThan, ItemShape.Pearl, red, Stat(StatType.FireResistance, .5f, 90f));
            Item("bang-tam-phu", "Ice-Heart Talisman", "Băng Tâm Phù", "Immune to embers and 25% less sky-fire damage for 120 seconds.", "Miễn nhiễm Dư Hỏa, giảm 25% sát thương Thiên Hỏa trong 120 giây.", ItemKind.FireWard, 90, 2, Realm.HoaThan, ItemShape.Talisman, ice, Stat(StatType.FireResistance, .25f, 120f), Flag(ItemFlag.EmberImmune, 1f, 120f));
            Item("kiem-tam-dan", "Sword-Heart Pill", "Kiếm Tâm Đan", "Channel the Heavenly Sword 40% faster and resist one interruption.", "Niệm Thiên Kiếm nhanh hơn 40% và không bị ngắt 1 lần.", ItemKind.Utility, 150, 1, Realm.HoaThan, ItemShape.Pill, violet, Flag(ItemFlag.SwordChannelSpeed, .4f, 180f), Flag(ItemFlag.SwordUninterrupted, 1f, 180f));

            Item("thanh-tam-dan", "Clear Heart Pill", "Thanh Tâm Đan", "Full stamina; control immunity for 5 seconds.", "Hồi đầy Thể Lực; miễn khống chế 5 giây.", ItemKind.Heal,50,2,Realm.KetDan,ItemShape.Pill,jade,new ItemEffect{type=ItemEffectType.RestoreEnergy,value=1},Flag(ItemFlag.ControlImmune,1,5));
            Item("cuu-chuyen-hoan-hon-dan", "Ninefold Renewal Pill", "Cửu Chuyển Hoàn Hồn Đan", "Full health; cleanse negative effects. Once per level.", "Hồi đầy máu, xóa hiệu ứng xấu. Tối đa 1/màn.", ItemKind.Heal,250,1,Realm.NguyenAnh,ItemShape.Pill,red,Heal(1),new ItemEffect{type=ItemEffectType.ClearNegative});
            Item("tu-khi-dan", "Qi Condensing Pill", "Tụ Khí Đan", "30% cooldown reduction for 45 seconds.", "Giảm 30% hồi chiêu trong 45 giây.", ItemKind.Buff,90,2,Realm.KetDan,ItemShape.Pill,blue,Stat(StatType.CooldownReduction,.3f,45));
            Item("bao-kich-dan", "Critical Pill", "Bạo Kích Đan", "+25% critical chance for 45 seconds.", "+25% tỉ lệ chí mạng trong 45 giây.", ItemKind.Buff,70,2,Realm.KetDan,ItemShape.Pill,red,Stat(StatType.CritChance,.25f,45));
            Item("ngu-hanh-phu", "Five Element Talisman", "Ngũ Hành Phù", "Choose an element when buying; +40% damage with it for 60 seconds.", "Chọn hệ khi mua; hệ đó +40% sát thương trong 60 giây.", ItemKind.Buff,100,1,Realm.NguyenAnh,ItemShape.Talisman,gold,new ItemEffect{type=ItemEffectType.ElementBoost,value=.4f,duration=60});
            Item("tam-yeu-phu", "Demon Tracking Talisman", "Tầm Yêu Phù", "Reveal all enemy locations for 30 seconds.", "Hiện vị trí mọi quái trong 30 giây.", ItemKind.Buff,60,2,Realm.TrucCo,ItemShape.Talisman,violet,Flag(ItemFlag.RevealEnemies,1,30));

            void Artifact(string id, string en, string vn, string dEN, string dVN, ArtifactEffect effect, float perLevel, params int[] prices)
            {
                var a = Asset<ArtifactDefinition>(Data + "/Artifacts/" + id + ".asset");
                a.id = id; a.nameEN = en; a.nameVN = vn; a.descriptionEN = dEN; a.descriptionVN = dVN; a.effect = effect; a.perLevel = perLevel; a.prices = prices;
                EditorUtility.SetDirty(a); catalog.artifacts.Add(a);
            }
            Artifact("phi-kiem", "Green Bamboo Flying Sword", "Phi Kiếm Thanh Trúc", "+6% basic attack damage per level.", "+6% sát thương đánh thường mỗi cấp.", ArtifactEffect.BasicDamage, .06f, 300, 600, 1000, 1600, 2500);
            Artifact("ho-tam-kinh", "Heart-Guard Mirror", "Hộ Tâm Kính", "+5% maximum health per level.", "+5% máu tối đa mỗi cấp.", ArtifactEffect.MaxHealth, .05f, 300, 600, 1000, 1600, 2500);
            Artifact("tui-can-khon", "Cosmos Pouch", "Túi Càn Khôn", "+1 item slot per level.", "+1 ô vật phẩm mỗi cấp.", ArtifactEffect.ItemSlots, 1f, 800, 2000);
            Artifact("linh-luc-ho-lo","Spirit Gourd","Linh Lực Hồ Lô","+8 spirit and +0.4 spirit/second per level.","+8 Linh Lực và hồi +0,4/giây mỗi cấp.",ArtifactEffect.Spirit,8,250,500,850,1300,2000);
            var gourd=catalog.Artifact("linh-luc-ho-lo");gourd.regenPerLevel=.4f;EditorUtility.SetDirty(gourd);
            Artifact("ngoc-boi-ngu-hanh","Five Element Pendant","Ngọc Bội Ngũ Hành","+3% damage against a countered element per level.","+3% sát thương khi đánh vào hệ mình khắc mỗi cấp.",ArtifactEffect.ElementCounter,.03f,400,800,1300,2000,3000);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("Economy data installed: " + catalog.items.Count + " items, " + catalog.artifacts.Count + " artifacts.");
        }
    }
}
#endif
