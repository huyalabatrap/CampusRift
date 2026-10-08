using System.Collections.Generic;
using UnityEngine;

namespace CampusRift.Progression
{
    // Tu Vi per tier and the stat table of the seven realms (plan §8.1, §8.4). Lives in Resources so a build has it;
    // CreateDefault() holds the same numbers so tools and tests never depend on the asset being present.
    [CreateAssetMenu(menuName = "Campus Rift/Cultivation Table")]
    public sealed class CultivationTable : ScriptableObject
    {
        public const int TiersPerRealm = 5;
        public RealmRow[] rows = new RealmRow[0];
        [Range(0, 0.05f)] public float runBonusPerRealm = 0.015f;
        [Range(0, 0.3f)] public float runBonusCap = 0.10f;

        static CultivationTable cached;
        public static CultivationTable Instance
        {
            get
            {
                if (cached == null) cached = Resources.Load<CultivationTable>("CultivationTable");
                if (cached == null) { cached = CreateDefault(); cached.hideFlags = HideFlags.HideAndDontSave; }
                return cached;
            }
        }
        public static void ResetCache() { cached = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cached = null; }

        public int RealmCount => rows.Length;
        public RealmRow Row(Realm realm)
        {
            int i = Mathf.Clamp((int)realm, 0, rows.Length - 1);
            return rows[i];
        }
        public int TuViPerTier(Realm realm) => Row(realm).tuViPerTier;
        public int TuViOfRealm(Realm realm) => Row(realm).tuViPerTier * TiersPerRealm;

        static float Lerp(float a, float b, int tier) => Mathf.Lerp(a, b, Mathf.Clamp01((tier - 1) / (float)(TiersPerRealm - 1)));
        public float Health(Realm r, int tier) { var row = Row(r); return Lerp(row.health1, row.health5, tier); }
        public float Attack(Realm r, int tier) { var row = Row(r); return Lerp(row.attack1, row.attack5, tier); }
        public float Spirit(Realm r, int tier) { var row = Row(r); return Lerp(row.spirit1, row.spirit5, tier); }
        public float Defense(Realm r) => Row(r).defense;
        public float RunBonus(Realm r) => Mathf.Min(runBonusCap, (int)r * runBonusPerRealm);

        public static CultivationTable CreateDefault()
        {
            var t = CreateInstance<CultivationTable>();
            RealmRow R(Realm realm, string en, string vn, int tuVi, float h1, float h5, float a1, float a5, float s1, float s5, float def, string skEN, string skVN) =>
                new RealmRow { realm = realm, nameEN = en, nameVN = vn, tuViPerTier = tuVi, health1 = h1, health5 = h5, attack1 = a1, attack5 = a5, spirit1 = s1, spirit5 = s5, defense = def, skillsEN = skEN, skillsVN = skVN };
            t.rows = new[]
            {
                R(Realm.LuyenKhi, "Qi Refining", "Luyện Khí", 100, 100, 140, 20, 28, 100, 120, 0f, "Giant Hand, Void Barrier, Phantom Clone; Lightning Flash (tier 3)", "Đại Thủ Ấn, Hư Không Kết Giới, Ảnh Phân Thân; Tích Lịch Nhất Thiểm (tầng 3)"),
                R(Realm.TrucCo, "Foundation", "Trúc Cơ", 150, 170, 220, 34, 44, 130, 150, 0.03f, "Buddha's Wrath Fire Lotus, Frost Seal, Thunder Sword", "Phật Nộ Hỏa Liên, Hàn Băng Phong Ấn, Thần Kiếm Ngự Lôi"),
                R(Realm.KetDan, "Golden Core", "Kết Đan", 225, 260, 320, 52, 64, 160, 180, 0.06f, "Golden Bell, Dragon-Subduing Palms, Samadhi True Fire", "Kim Chung Tráo, Hàng Long Thập Bát Chưởng, Tam Muội Chân Hỏa"),
                R(Realm.NguyenAnh, "Nascent Soul", "Nguyên Anh", 340, 370, 440, 74, 88, 190, 210, 0.09f, "Black Hole Net, Northern Sea Art, Peng Speed", "Hắc Động Thần La, Bắc Minh Thần Công, Côn Bằng Cực Tốc"),
                R(Realm.HoaThan, "Spirit Transformation", "Hóa Thần", 510, 500, 580, 100, 116, 220, 240, 0.12f, "Ten Thousand Swords, Sky Thunder, Wood Spirit Renewal, Divine Sight, Heaven Sword", "Vạn Kiếm Quyết, Thiên Lôi Dẫn, Mộc Linh Hồi Xuân, Thần Thức Linh Nhãn, Thiên Kiếm"),
                R(Realm.LuyenHu, "Void Refining", "Luyện Hư", 760, 650, 740, 130, 148, 250, 270, 0.15f, "Immortal-Slaying Sword Array, Yin Soldiers", "Tru Tiên Kiếm Trận, Âm Binh Quy Hồn"),
                R(Realm.DoKiep, "Tribulation", "Độ Kiếp", 1140, 820, 920, 164, 184, 280, 300, 0.18f, "Expanding Domain, Martial Soul True Form", "Bành Trướng Lãnh Địa, Võ Hồn Chân Thân"),
            };
            return t;
        }

        public string RealmName(Realm r, bool vietnamese) { var row = Row(r); return vietnamese ? row.nameVN : row.nameEN; }
        public string TierName(Realm r, int tier, bool vietnamese) => RealmName(r, vietnamese) + " " + tier;
        public IEnumerable<RealmRow> All() => rows;
    }
}
