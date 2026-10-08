#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CampusRift.Combat;
namespace CampusRift.Skills
{
    public static class SkillSet2Setup
    {
        const string Prefab="Assets/Characters/SchoolGirl/Prefabs/CampusExplorer.prefab";
        public static void InstallUntil(int count)
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before installing P18.");
            Directory.CreateDirectory("Assets/Skills/Core/Resources/P18");
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Skills/Core/Resources/P18/geometry.mat");
            if(material==null){material=new Material(Shader.Find("Campus Rift/P18 Comic Geometry"));AssetDatabase.CreateAsset(material,"Assets/Skills/Core/Resources/P18/geometry.mat");}
            var d=SkillSet1Setup.Definition("hang-long-thap-bat-chuong","Eighteen Dragon Palms","Hàng Long Thập Bát Chưởng",Element.Kim,SkillRole.Burst,CastType.Aimed,2,1,10,35,"Golden dragon pierces 15 m, 350% attack; knockback 3 m. Mastery: two dragons in a V.","Rồng vàng xuyên 15 m, 350% Công; đẩy lùi 3 m. Viên Mãn: hai luồng rồng chữ V.");d.shortName="HÀNG LONG";
            var defs=new SkillDefinition[count];defs[0]=d;
            if(count>=2){defs[1]=SkillSet1Setup.Definition("tam-muoi-chan-hoa","Samadhi True Fire","Tam Muội Chân Hỏa",Element.Hoa,SkillRole.Burst,CastType.Aimed,2,1,14,40,"8 m flame cone for 3 s, 90% attack every 0.25 s + Burn. Move at 35% speed. Mastery: fire wall for 4 s.","Phun lửa nón 8 m trong 3 giây, 90% Công mỗi 0,25 giây + Bỏng; đi chậm 35%. Viên Mãn: tường lửa 4 giây.");defs[1].shortName="CHÂN HỎA";}
            if(count>=3){defs[2]=SkillSet1Setup.Definition("bac-minh-than-cong","Northern Sea Absorption","Bắc Minh Thần Công",Element.Thuy,SkillRole.Support,CastType.Channel,3,1,16,30,"Channel 3 s; three closest visible targets within 12 m, 70% attack every 0.5 s + Wet. Heal 50% of actual damage. Dodge/stun interrupts. Mastery drains spirit.","Vận công 3 giây: hút 3 mục tiêu gần nhất trong 12 m, 70% Công mỗi 0,5 giây + Ướt; hồi 50% sát thương thực tế. Né/choáng ngắt chiêu. Viên Mãn: hút Linh Lực.");defs[2].shortName="BẮC MINH";}
            if(count>=4){defs[3]=SkillSet1Setup.Definition("con-bang-cuc-toc","Kunpeng Swiftness","Côn Bằng Cực Tốc",Element.Moc,SkillRole.Mobility,CastType.Instant,3,1,25,25,"For 6 s: +40% speed (movement cap applies), free stamina dodges, afterimages. Mastery: ghosts deal 60% attack on contact.","Trong 6 giây: +40% tốc độ (giữ trần), né không tốn Thể Lực, bóng ảo. Viên Mãn: bóng chạm quái gây 60% Công.");defs[3].shortName="CÔN BẰNG";}
            if(count>=5){defs[4]=SkillSet1Setup.Definition("thien-loi-dan","Heavenly Thunder","Thiên Lôi Dẫn",Element.Loi,SkillRole.Burst,CastType.Aimed,4,1,20,45,"Mark a 5 m area; after 1.2 s, five thunderbolts (2.5 m impact), 220% attack each; Lightning vs Yin +50%. Mastery: eight bolts.","Báo vùng 5 m; sau 1,2 giây giáng 5 tia (nổ 2,5 m), mỗi tia220% Công; +50% với Âm. Viên Mãn: 8 tia.");defs[4].shortName="THIÊN LÔI";}
            if(count>=6){defs[5]=SkillSet1Setup.Definition("moc-linh-hoi-xuan","Wood Spirit Renewal","Mộc Linh Hồi Xuân",Element.Moc,SkillRole.Support,CastType.Instant,4,1,35,40,"Heal 25% max HP over 5 s. A 5 m field remains 8 s, healing allies for 25% max HP over its duration. Mastery: cleanse negative effects.","Hồi25% máu tối đa trong5 giây; vùng5m tồn tại8 giây, hồi đồng minh tổng25% máu. Viên Mãn: xóa hiệu ứng xấu.");defs[5].shortName="HỒI XUÂN";}
            if(count>=7){defs[6]=SkillSet1Setup.Definition("than-thuc-linh-nhan","Spirit Sight","Thần Thức Linh Nhãn",Element.None,SkillRole.Utility,CastType.Instant,4,1,30,20,"Reveal every enemy through walls for 10 s; +25% critical damage against them. Breaks stealth. Mastery: the three weakest take +30% damage.","Thấy quái xuyên tường10giây; +25% sát thương chí mạng, phá ẩn thân. Viên Mãn: đánh dấu3quái yếu nhất, chúng nhận+30% sát thương.");defs[6].shortName="LINH NHÃN";}
            if(count>=8){defs[7]=SkillSet1Setup.Definition("tru-tien-kiem-tran","Immortal Slaying Sword Array","Tru Tiên Kiếm Trận",Element.Kim,SkillRole.Control,CastType.Aimed,5,1,28,60,"8 m sword formation for 10 s; slash every 0.5 s, 60% attack. Stand inside for +20 percentage points critical chance. Mastery: 15 s.","Kiếm trận8m/10giây; mỗi0,5giây chém60% Công. Đứng trong trận:+20 điểm% chí mạng. Viên Mãn:15giây.");defs[7].shortName="TRU TIÊN";}
            if(count>=9){defs[8]=SkillSet1Setup.Definition("am-binh-quy-hon","Soul Army Resurrection","Âm Binh Quy Hồn",Element.Am,SkillRole.Summon,CastType.Instant,5,1,40,60,"Raise up to three normal enemies killed in the last 8 s as allies for 20 s, with 60% original stats. No boss/elite or second sword intent. Mastery: five allies.","Dựng tối đa3quái thường chết trong8giây thành đồng minh20giây với60% chỉ số gốc. Không dựng boss/tinh anh; chết lần hai không tính Kiếm Ý. Viên Mãn:5đồng minh.");defs[8].shortName="ÂM BINH";}
            if(count>=10){defs[9]=SkillSet1Setup.Definition("banh-truong-lanh-dia","Domain Expansion","Bành Trướng Lãnh Địa",Element.KhongGian,SkillRole.Control,CastType.Instant,6,1,60,80,"12 m domain for 8 s: enemies inside move 40% slower; caster's cooldowns advance twice as fast while inside. Mastery: enemy abilities are suppressed.","Lãnh địa12m/8giây: quái bên trong chậm40%; người chơi trong vùng hồi kỹ năng nhanh gấp đôi. Viên Mãn: khóa kỹ năng quái.");defs[9].shortName="LÃNH ĐỊA";}
            if(count>=11){defs[10]=SkillSet1Setup.Definition("vo-hon-chan-than","Martial Soul Avatar","Võ Hồn Chân Thân",Element.Tho,SkillRole.Burst,CastType.Instant,6,1,75,100,"Giant spirit for 10 s: +50% damage, take 30% less; basic attacks sweep a 4 m radius; camera pulls back. Mastery: immune to control.","Pháp tướng10giây:+50% sát thương, giảm30% sát thương nhận; đánh thường quét4m, máy quay lùi xa. Viên Mãn: miễn khống chế.");defs[10].shortName="VÕ HỒN";}
            var root=PrefabUtility.LoadPrefabContents(Prefab);Configure(root,defs);PrefabUtility.SaveAsPrefabAsset(root,Prefab);PrefabUtility.UnloadPrefabContents(root);
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");Configure(Object.FindAnyObjectByType<CampusExplorer>().gameObject,defs);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            SkillCatalogSetup.Install();if(count==11)P18DataSetup.Install();AssetDatabase.SaveAssets();Debug.Log("P18 installed through T"+count);
        }
        static void Configure(GameObject go,SkillDefinition[] d){SkillSet1Setup.Ensure<DragonPalmRuntime>(go).definition=d[0];SkillSet1Setup.Ensure<Enemies.PlayerEnemyControl>(go);SkillSet1Setup.Ensure<SkillMasteryFields>(go);SkillSet1Setup.Ensure<ReactionResolver>(go);if(d.Length>=2)SkillSet1Setup.Ensure<TrueFireRuntime>(go).definition=d[1];if(d.Length>=3)SkillSet1Setup.Ensure<NorthernDrainRuntime>(go).definition=d[2];if(d.Length>=4)SkillSet1Setup.Ensure<KunpengSpeedRuntime>(go).definition=d[3];if(d.Length>=5)SkillSet1Setup.Ensure<HeavenThunderRuntime>(go).definition=d[4];if(d.Length>=6)SkillSet1Setup.Ensure<WoodRenewalRuntime>(go).definition=d[5];if(d.Length>=7)SkillSet1Setup.Ensure<SpiritSightRuntime>(go).definition=d[6];if(d.Length>=8)SkillSet1Setup.Ensure<ImmortalSwordArrayRuntime>(go).definition=d[7];if(d.Length>=9)SkillSet1Setup.Ensure<SoulSummonRuntime>(go).definition=d[8];if(d.Length>=10)SkillSet1Setup.Ensure<DomainRuntime>(go).definition=d[9];if(d.Length>=11)SkillSet1Setup.Ensure<MartialAvatarRuntime>(go).definition=d[10];}
        [MenuItem("Campus Rift/V2/Install Skill Set 2")]
        public static void Install(){InstallUntil(11);}
    }
}
#endif
