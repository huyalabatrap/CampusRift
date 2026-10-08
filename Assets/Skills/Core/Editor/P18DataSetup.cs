#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public static class P18DataSetup
    {
        public static void Install()
        {
            var config=AssetDatabase.LoadAssetAtPath<ReactionConfig>("Assets/Combat/Data/Reactions.asset");if(config==null||config.rules==null||config.rules.Length<5)throw new InvalidOperationException("P11 reaction data required.");
            var rules=new ReactionRule[9];Array.Copy(config.rules,rules,5);
            rules[5]=Rule(ReactionType.Wildfire,"phong-hoa-lieu-nguyen","Wildfire Wind","Phong Hỏa Liệu Nguyên","Burn + Kunpeng ghost spreads fire nearby","Bỏng + bóng Côn Bằng: lan lửa ra quái gần",Element.Hoa,Element.Moc,4,"impactSoft_heavy_000","thrusterFire_001","forceField_003");
            rules[6]=Rule(ReactionType.SwordSoul,"kiem-hon","Sword Soul","Kiếm Hồn","Soul ally inside Sword Array: +50% damage","Âm Binh trong Tru Tiên: +50% sát thương",Element.Am,Element.Kim,0,"impactMetal_heavy_001","laserSmall_001","impactBell_heavy_001");
            rules[7]=Rule(ReactionType.GuardDrain,"ho-the-hap-nguyen","Guarded Absorption","Hộ Thể Hấp Nguyên","Golden Bell + Northern Drain: healing x2","Kim Chung + Bắc Minh: máu hút về ×2",Element.Kim,Element.Thuy,0,"impactBell_heavy_001","forceField_003","laserSmall_001");
            rules[8]=Rule(ReactionType.DomainResonance,"lanh-dia-cong-huong","Domain Resonance","Lãnh Địa Cộng Hưởng","Cast a skill inside Domain: +20% damage","Dùng kỹ năng trong Lãnh Địa: +20% sát thương",Element.KhongGian,Element.None,0,"impactSoft_heavy_000","lowFrequency_explosion_001","forceField_003");
            config.rules=rules;EditorUtility.SetDirty(config);
            string[] ids={"dai-thu-an","hu-khong-ket-gioi","anh-phan-than","tich-lich-nhat-thiem","phat-no-hoa-lien","han-bang-phong-an","than-kiem-ngu-loi","kim-chung-trao","hac-dong-than-la","van-kiem-quyet"};
            string[] vi={"Ngũ Chỉ Sơn chắn đường 5 giây.","Tường phản lại đạn.","Hai phân thân.","Lướt lần thứ hai trong 2 giây, miễn Linh Lực; giữ hồi chiêu lần đầu.","Ba hoa sen nhỏ, mỗi hoa 150% Công.","Băng vỡ để lại vùng Chậm 2 m trong 3 giây.","Nảy thêm 3 lần (tối đa 9).","Chuông vỡ nổ 200% Công trong 4 m.","Hút cả đạn.","50 thanh kiếm."};
            string[] en={"Five Finger Mountain blocks the path for 5 s.","Wall reflects projectiles.","Two decoys.","A second free dash within 2 s; keeps the first cooldown.","Three small lotuses, each 150% attack.","Shattering ice leaves a 2 m Chill field for 3 s.","Three extra bounces (up to nine).","Broken bell explodes for 200% attack within 4 m.","Also consumes projectiles.","Fifty swords."};
            for(int i=0;i<ids.Length;i++){var d=AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Skills/Core/Data/"+ids[i]+".asset");if(d==null)throw new InvalidOperationException(ids[i]);d.descriptionVN=Trim(d.descriptionVN,"Viên Mãn:")+" Viên Mãn: "+vi[i];d.description=Trim(d.description,"Mastery:")+" Mastery: "+en[i];EditorUtility.SetDirty(d);}
            var catalog=AssetDatabase.LoadAssetAtPath<Localization.LocalizationCatalog>("Assets/Localization/Resources/LocalizationCatalog.asset");
            foreach(var r in rules){Translate(catalog,r.nameEN,r.nameVI);Translate(catalog,r.nameEN.ToUpperInvariant(),r.nameVI.ToUpperInvariant());Translate(catalog,r.hintEN,r.hintVI);}EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        static string Trim(string s,string marker){int at=(s??"").IndexOf(marker,StringComparison.Ordinal);return at<0?s:s.Substring(0,at).TrimEnd();}
        static void Translate(Localization.LocalizationCatalog c,string en,string vi){var entry=c.entries.Find(x=>x.en==en);if(entry==null)c.entries.Add(new Localization.TranslationEntry{en=en,vi=vi});else entry.vi=vi;}
        static ReactionRule Rule(ReactionType type,string id,string en,string vi,string hintEN,string hintVI,Element first,Element second,float radius,string a,string b,string c)
        {return new ReactionRule{type=type,id=id,nameEN=en,nameVI=vi,hintEN=hintEN,hintVI=hintVI,first=first,second=second,radius=radius,bonus=type==ReactionType.SwordSoul?.5f:type==ReactionType.DomainResonance?.2f:0,hitStop=.065f,impulse=.35f,transient=Clip(a),body=Clip(b),tail=Clip(c)};}
        static AudioClip Clip(string id){var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Combat/Audio/Reactions/"+id+".ogg");if(clip==null)throw new InvalidOperationException("Missing CC0 clip "+id);return clip;}
    }
}
#endif
