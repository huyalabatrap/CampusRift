using System;
using System.Collections.Generic;
using System.IO;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Localization;
using CampusRift.UI;
using UnityEditor;
using UnityEngine;

// P05-T01/T08: writes the ten LevelDefinition assets and the catalog from the tables of plan §2.1, and adds the
// level-result card to the gameplay scene. Safe to run again: existing assets are refreshed, not duplicated.
public static class LevelSetup
{
    const string Data = "Assets/Levels/Data/";

    sealed class Row
    {
        public string en, vn, briefEN, briefVN; public int realm, tier; public SkyPreset sky; public int[] waves; public int darts; // darts = Độc Nhãn share
        public float hp, dmg, speed; public int ai; public float par;
        public Vector3 spawn; public float yaw; public string[] zoneIds; public string[] star;
    }

    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    // Zones are listed once and referenced by id from each level.
    static readonly Dictionary<string, Vector3> Zones = new Dictionary<string, Vector3>
    {
        { "c-north", V(0, 0, 8) }, { "c-west", V(-12, 0, 4) }, { "c-east", V(12, 0, 4) }, { "c-far", V(0, 0, 20) },
        { "c-west2", V(-20, 0, 5) }, { "c-east2", V(20, 0, 5) }, { "c-ne", V(10, 0, 25) }, { "c-nw", V(-10, 0, 25) }, { "c-top", V(0, 0, 45) },
        { "b-out-e", V(30, 0, -35) }, { "b-out-w", V(10, 0, -35) }, { "b-in-e", V(31, 0, -30) }, { "b-in-n", V(19, 0, -24) },
        { "b-up-w", V(25, 3.7f, -30) }, { "b-up-e", V(31, 3.7f, -28) },
        { "a-in", V(36, 0, 28) }, { "a-up", V(33, 4, 30) }, { "a-out", V(40, 0, 25) },
        { "e-in-w", V(-2, 0, -26) }, { "e-in-e", V(11, 0, -26) }, { "e-up", V(2, 3.6f, -22) }, { "d-up", V(-16, 3.7f, -26) },
        { "d-out", V(-20, 0, -35) }, { "e-out", V(-10, 0, -35) },
        { "v-in", V(6, 0, -44) }, { "v-up", V(2, 3.5f, -42) }, { "v-out", V(10, 0, -55) },
        { "h-out-a", V(40, 0, -15) }, { "h-out-b", V(50, 0, -25) }, { "h-out-c", V(30, 0, -35) },
        { "far-w", V(-40, 0, 5) }, { "far-e", V(40, 0, -5) }, { "far-s", V(0, 0, -55) }, { "far-ne", V(50, 0, 35) }, { "far-sw", V(-50, 0, -35) },
    };

    static readonly string[] AllCampus = { "far-w", "far-e", "far-s", "far-ne", "far-sw", "c-top", "e-in-w", "a-in", "v-in", "c-east2" };

    static readonly Row[] Rows =
    {
        new Row { en = "The First Rift", vn = "Khe Nứt Đầu Tiên", realm = 0, tier = 1, sky = SkyPreset.Dusk, waves = new[] { 3, 3 }, darts = 0, hp = 1.0f, dmg = 1.0f, speed = 1.00f, ai = 0, par = 240,
            spawn = V(0, 0.13f, -10), yaw = 0, zoneIds = new[] { "c-north", "c-west", "c-east", "c-far" },
            briefEN = "Dusk in the central yard. The first rift opens in the middle of the campus.", briefVN = "Chiều tà ở sân trung tâm. Khe nứt đầu tiên mở ra giữa trường.",
            star = new[] { "KillsWithSkill|3|Defeat 3 monsters with Giant Hand|Hạ 3 quái bằng Đại Thủ Ấn" } },
        new Row { en = "Poison Corridor", vn = "Độc Vụ Hành Lang", realm = 0, tier = 3, sky = SkyPreset.Dusk, waves = new[] { 5, 5 }, darts = 3, hp = 1.3f, dmg = 1.25f, speed = 1.03f, ai = 0, par = 300,
            spawn = V(20, 0.1f, -8), yaw = 180, zoneIds = new[] { "b-out-e", "b-out-w", "b-in-e", "b-in-n", "b-up-w", "b-up-e" },
            briefEN = "Sunset around Block B and its first two floors. Archers spit poison from afar.", briefVN = "Hoàng hôn quanh khu B và hành lang tầng 1–2. Xạ thủ bắn độc từ xa.",
            star = new[] { "MaxProjectileHits|3|Take at most 3 poison hits|Trúng đạn độc không quá 3 lần" } },
        new Row { en = "The Soul Hunter", vn = "Kẻ Săn Hồn", realm = 1, tier = 1, sky = SkyPreset.Night, waves = new[] { 4, 5, 4 }, darts = 4, hp = 1.8f, dmg = 1.8f, speed = 1.06f, ai = 1, par = 420,
            spawn = V(10, 0.1f, -5), yaw = 0, zoneIds = new[] { "a-out", "a-in", "a-up", "b-out-e", "b-in-e", "b-up-w" },
            briefEN = "Night over Blocks A and B. Monsters begin to surround you.", briefVN = "Đêm trên khu A–B. Quái bắt đầu bao vây.",
            star = new[] { "BossTimeLimit|60|Defeat Shaban within 60 s of meeting him|Hạ Shaban trong 60 giây kể từ lúc chạm trán" } },
        new Row { en = "Blackout Night", vn = "Đêm Mất Điện", realm = 1, tier = 3, sky = SkyPreset.Night, waves = new[] { 6, 6, 6 }, darts = 5, hp = 2.2f, dmg = 2.1f, speed = 1.09f, ai = 1, par = 480,
            spawn = V(-10, 0.1f, -15), yaw = 180, zoneIds = new[] { "e-in-w", "e-in-e", "e-up", "d-up", "d-out", "e-out" },
            briefEN = "Night in Blocks D and E. One building has lost power; bring a flashlight.", briefVN = "Đêm ở khu D–E. Một tòa mất điện, nhớ mang đèn pin.",
            star = new[] { "ComboReactions|5|Trigger 5 Frost-Lightning reactions|Kích hoạt 5 lần phản ứng Băng Lôi Liệt" } },
        new Row { en = "The Soul Hunter's Prey", vn = "Săn Hồn Giả", realm = 2, tier = 1, sky = SkyPreset.Night, waves = new[] { 7, 7, 8 }, darts = 7, hp = 3.0f, dmg = 2.8f, speed = 1.12f, ai = 2, par = 600,
            spawn = V(0, 0.13f, -10), yaw = 0, zoneIds = new[] { "c-north", "c-west2", "c-east2", "c-ne", "c-nw", "c-top" },
            briefEN = "Midnight in the central campus. A hunter is watching.", briefVN = "Nửa đêm ở khu trung tâm. Có kẻ săn đang dõi theo.",
            star = new[] { "NoHealPotion|0|Defeat the boss without a healing pill|Hạ boss mà không dùng đan hồi máu" } },
        new Row { en = "Blood Moon", vn = "Huyết Nguyệt", realm = 2, tier = 3, sky = SkyPreset.BloodMoon, waves = new[] { 6, 7, 7, 6 }, darts = 8, hp = 3.5f, dmg = 3.2f, speed = 1.16f, ai = 2, par = 660,
            spawn = V(30, 0.1f, -5), yaw = 90, zoneIds = new[] { "v-in", "v-up", "v-out", "h-out-a", "h-out-b", "h-out-c" },
            briefEN = "The moon turns red over Blocks H and V.", briefVN = "Trăng chuyển đỏ trên khu H và V.",
            star = new[] { "KillsWithSkill|0|Defeat every summoner before its second summon|Hạ mỗi Triệu Hồn Sư trước khi nó triệu hồi lần thứ 2" } },
        new Row { en = "The Hunter Awakens", vn = "Săn Hồn Thức Tỉnh", realm = 3, tier = 1, sky = SkyPreset.BloodMoon, waves = new[] { 7, 8, 8, 7 }, darts = 9, hp = 4.6f, dmg = 4.2f, speed = 1.20f, ai = 3, par = 780,
            spawn = V(0, 0.13f, -10), yaw = 0, zoneIds = AllCampus,
            briefEN = "Blood moon over the whole campus. The final fight is in the tall Block X.", briefVN = "Trăng máu phủ toàn trường. Trận cuối ở tòa X cao tầng.",
            star = new[] { "BeatParTime|780|Finish in under 13:00|Hoàn thành dưới 13:00" } },
        new Row { en = "Crimson Flame Descends", vn = "Xích Hỏa Giáng Thế", realm = 4, tier = 1, sky = SkyPreset.Inferno, waves = new[] { 32 }, darts = 10, hp = 6.4f, dmg = 5.8f, speed = 1.25f, ai = 3, par = 720,
            spawn = V(0, 0.13f, -10), yaw = 0, zoneIds = AllCampus,
            briefEN = "The sky burns. A flame drake circles the campus.", briefVN = "Trời rực lửa. Giao long lửa lượn quanh trường.",
            star = new[] { "MaxFireHitsOutdoors|0|Never get hit by Sky Fire outdoors|Không trúng Thiên Hỏa lần nào khi ở ngoài trời" } },
        new Row { en = "Vermilion Bird's Wrath", vn = "Chu Tước Phần Thiên", realm = 5, tier = 1, sky = SkyPreset.Inferno, waves = new[] { 16, 20 }, darts = 11, hp = 8.4f, dmg = 7.6f, speed = 1.30f, ai = 4, par = 900,
            spawn = V(0, 0.13f, -10), yaw = 0, zoneIds = AllCampus,
            briefEN = "Ash rain, a corrupted firebird overhead.", briefVN = "Mưa tro, chim lửa tà hóa trên trời.",
            star = new[] { "SwordWithin|10|Summon the sword within 10 s of a full meter, both times|Triệu hồi Thiên Kiếm trong 10 giây sau khi Kiếm Ý đầy, cả 2 lần" } },
        new Row { en = "Three Beasts Above", vn = "Tam Thú Lâm Không", realm = 6, tier = 1, sky = SkyPreset.RedEclipse, waves = new[] { 13, 15, 17 }, darts = 14, hp = 10.5f, dmg = 9.6f, speed = 1.35f, ai = 4, par = 1080,
            spawn = V(0, 0.13f, -10), yaw = 0, zoneIds = AllCampus,
            briefEN = "A red eclipse. Three colossal beasts hang over the campus.", briefVN = "Nhật thực đỏ. Ba cự thú treo trên bầu trời trường.",
            star = new[] { "ComboReactions|3|Trigger 3 Generating Chain reactions|Kích hoạt ít nhất 3 lần Tương Sinh Liên Hoàn" } },
    };

    [MenuItem("Campus Rift/V2/Setup Levels")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var log = new List<string>();
        Directory.CreateDirectory(Data); Directory.CreateDirectory("Assets/Levels/Resources");
        var imp = AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/tieu-yeu.asset");
        var archer = AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/Enemies/Data/doc-nhan.asset");
        if (imp == null || archer == null) throw new InvalidOperationException("Run 'Campus Rift/V2/Setup Enemies' first.");

        var made = new LevelDefinition[Rows.Length];
        for (int i = 0; i < Rows.Length; i++)
        {
            var row = Rows[i]; string path = Data + "Level" + (i + 1).ToString("00") + ".asset";
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (level == null) { level = ScriptableObject.CreateInstance<LevelDefinition>(); AssetDatabase.CreateAsset(level, path); }
            level.id = "level-" + (i + 1).ToString("00"); level.index = i + 1;
            level.displayName = row.en; level.displayNameVN = row.vn; level.briefEN = row.briefEN; level.briefVN = row.briefVN;
            level.requiredRealm = row.realm; level.requiredTier = row.tier; level.sky = row.sky; level.aiTier = row.ai;
            level.healthMultiplier = row.hp; level.damageMultiplier = row.dmg; level.speedMultiplier = row.speed; level.parTimeSeconds = row.par; level.restSeconds = 15;
            level.spawnPoint = row.spawn; level.spawnYaw = row.yaw;
            level.zones = new List<RiftZoneData>();
            foreach (var id in row.zoneIds) level.zones.Add(new RiftZoneData { id = id, position = Zones[id], radius = id.Contains("-in") || id.Contains("-up") ? 2f : 3f });
            level.waves = BuildWaves(row, imp, archer);
            level.bosses = new List<EnemyArchetype>();
            level.stars = new StarCondition[3];
            level.stars[0] = new StarCondition { kind = StarKind.None, descriptionEN = "Complete the level", descriptionVN = "Hoàn thành màn" };
            level.stars[1] = new StarCondition { kind = StarKind.BeatParTime, value = row.par, descriptionEN = "Beat the par time without a Life Talisman", descriptionVN = "Dưới thời gian mốc, không dùng Hộ Mệnh Phù" };
            var third = row.star[0].Split('|');
            level.stars[2] = new StarCondition { kind = (StarKind)Enum.Parse(typeof(StarKind), third[0]), value = float.Parse(third[1]), descriptionEN = third[2], descriptionVN = third[3] };
            EditorUtility.SetDirty(level); made[i] = level;
        }

        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Levels/Resources/LevelCatalog.asset");
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<LevelCatalog>(); AssetDatabase.CreateAsset(catalog, "Assets/Levels/Resources/LevelCatalog.asset"); }
        catalog.levels = made; EditorUtility.SetDirty(catalog);

        int added = AddTranslations();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        log.Add("levels written: " + made.Length + ", catalog ok, translations added: " + added);
        return string.Join("\n", log);
    }

    // Archers are spread over the waves in proportion to wave size; the rest are Tiểu Yêu.
    static List<WaveDefinition> BuildWaves(Row row, EnemyArchetype imp, EnemyArchetype archer)
    {
        var waves = new List<WaveDefinition>(); int total = 0; foreach (var w in row.waves) total += w;
        int archersLeft = row.darts, totalLeft = total;
        for (int i = 0; i < row.waves.Length; i++)
        {
            int size = row.waves[i];
            int a = i == row.waves.Length - 1 ? archersLeft : Mathf.Min(archersLeft, Mathf.RoundToInt(row.darts * (size / (float)total)));
            archersLeft -= a; totalLeft -= size;
            var wave = new WaveDefinition();
            if (size - a > 0) wave.entries.Add(new SpawnEntry { archetype = imp, count = size - a });
            if (a > 0) wave.entries.Add(new SpawnEntry { archetype = archer, count = a });
            wave.riftZones = row.zoneIds;
            waves.Add(wave);
        }
        return waves;
    }

    static int AddTranslations()
    {
        var catalog = Resources.Load<LocalizationCatalog>("LocalizationCatalog");
        if (catalog == null) return 0;
        int added = 0;
        foreach (var pair in new[]
        {
            new[] { "REPLAY", "CHƠI LẠI" }, new[] { "NEXT LEVEL", "MÀN TIẾP" },
        })
        {
            if (catalog.entries.Exists(e => e.en == pair[0])) continue;
            catalog.entries.Add(new TranslationEntry { en = pair[0], vi = pair[1] }); added++;
        }
        EditorUtility.SetDirty(catalog); return added;
    }
}
