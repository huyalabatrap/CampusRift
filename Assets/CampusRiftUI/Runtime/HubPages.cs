using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Learning;
using CampusRift.Progression;

namespace CampusRift.UI
{
    // The Alchemy Pavilion, Library and Realm pages of the Hub (P09 restyle): framed panels, gold titles, cards with art,
    // instead of lists of plain buttons. Reading and quizzes still happen in the study panel; these pages lead into it.
    public sealed partial class HubUI
    {
        int shopCategory;
        ItemDefinition shopItem;
        ArtifactDefinition shopArtifact;
        int buyQuantity = 1;
        CampusRift.Combat.Element shopElement=CampusRift.Combat.Element.Kim;
        ShopService shopService;
        ShopService Shop => shopService ?? (shopService = new ShopService(ProfileService.Instance));
        static readonly Color Green = new Color(.5f, 1f, .8f);

        void PageTitle(string title, string subtitle)
        {
            kit.Image(content, "Title Shade", -30, -16, 1850, 134, new Color(.01f, .015f, .04f, .62f));
            kit.Title(content, title, 8, -8, 1100, 54);
            kit.Text(content, subtitle, 12, 82, 1240, 34, 21, UiKit.Muted, TextAlignmentOptions.TopLeft, false);
            kit.Rule(content, 8, 122, 1780);
        }

        // Opens the study panel on one of its screens.
        void OpenStudy(string screen)
        {
            LearningUI.PendingScreen = screen; UIStateManager.Instance.OpenCourse();
        }

        // ================================================================ Alchemy Pavilion
        static readonly string[] CategoryEN = { "RECOVERY", "POWER", "ARTIFACTS", "HIGH-TIER ITEMS" }, CategoryVN = { "HỒI PHỤC", "TĂNG SỨC MẠNH", "PHÁP BẢO", "VẬT PHẨM CẤP CAO" };
        static readonly string[] CategoryHintEN = { "Pills that heal, restore spirit or guard your life", "Buffs to damage, speed and defence", "Permanent upgrades", "Recommended for levels 8-10" };
        static readonly string[] CategoryHintVN = { "Đan dược hồi máu, linh lực, hộ mệnh", "Buff sát thương, tốc độ, phòng thủ", "Nâng cấp vĩnh viễn", "Khuyến nghị cho màn 8–10" };
        static readonly string[] CategoryArt = { "hoi-khi-dan", "cuong-luc-dan", "phi-kiem-thanh-truc", "ti-hoa-chau" };

        IEnumerable<ItemDefinition> ItemsOf(int category)
        {
            var items = Shop.Catalog.items;
            switch (category)
            {
                case 0: return items.Where(i => i.kind == ItemKind.Heal || i.kind == ItemKind.Revive);
                case 1: return items.Where(i => i.kind == ItemKind.Buff);
                case 3: return items.Where(i => i.kind == ItemKind.FireWard || i.kind == ItemKind.Utility);
                default: return Enumerable.Empty<ItemDefinition>();
            }
        }
        Sprite CategorySprite(int c) => c == 2 ? ContentImages.Get("Artifacts", CategoryArt[2]) : ContentImages.Get("Items", CategoryArt[c]);

        void BuildShop()
        {
            var profile = ProfileService.Instance; var inv = profile.Inventory;
            PageTitle(L("ALCHEMY PAVILION", "ĐAN CÁC"), L("Knowledge turns into Linh Thạch.", "Tri thức hóa thành Linh Thạch."));
            // currency
            var pill = kit.Panel(content, "Currency", 1430, 4, 358, 94);
            kit.Gem(pill, 20, 20, 48, new Color(.6f, .55f, 1f));
            kit.Text(pill, profile.Wallet.BalanceText, 74, 12, 106, 62, 38, UiKit.GoldHi, TextAlignmentOptions.MidlineLeft, false).fontStyle = FontStyles.Bold | FontStyles.Italic;
            kit.Text(pill, "LINH THẠCH", 182, 28, 152, 38, 20, UiKit.Gold, TextAlignmentOptions.MidlineLeft, false);

            // categories
            for (int c = 0; c < 4; c++)
            {
                int cat = c; bool selected = shopCategory == c; int count = c == 2 ? Shop.Catalog.artifacts.Count : ItemsOf(c).Count();
                var card = kit.Panel(content, "Category " + c, 0, 138 + c * 114, 700, 104, selected);
                kit.Image(card, "Icon", 18, 12, 84, 84, Color.white, CategorySprite(c));
                kit.Text(card, Vn ? CategoryVN[c] : CategoryEN[c], 122, 16, 480, 42, 32, selected ? UiKit.GoldHi : (Color?)null, TextAlignmentOptions.TopLeft, false).fontStyle = FontStyles.Bold | FontStyles.Italic | FontStyles.UpperCase;
                kit.Text(card, Vn ? CategoryHintVN[c] : CategoryHintEN[c], 122, 54, 480, 32, 20, UiKit.Muted, TextAlignmentOptions.TopLeft, false);
                kit.Text(card, ">", 642, 30, 36, 48, 36, selected ? ComicTheme.Navy : UiKit.Muted, TextAlignmentOptions.MidlineRight, false);
                kit.Text(card, count.ToString(), 596, 34, 38, 40, 22, selected ? ComicTheme.Navy : UiKit.Muted, TextAlignmentOptions.MidlineRight, false);
                kit.Clickable(card, () => { shopCategory = cat; shopItem = null; shopArtifact = null; buyQuantity = 1; ShowTab(Tab.Shop); });
            }

            // detail
            var detail = kit.Panel(content, "Detail", 720, 138, 1068, 442);
            if (shopCategory == 2) DrawArtifactDetail(detail); else DrawItemDetail(detail);

            // carry
            var carry = kit.Panel(content, "Carry", 0, 596, 1788, 148);
            kit.Diamond(carry, 24, 22, 14, UiKit.GoldHi);
            kit.Text(carry, L("EQUIPMENT FOR THE MISSION", "TRANG BỊ CHO NHIỆM VỤ"), 50, 12, 700, 34, 26, UiKit.GoldHi, TextAlignmentOptions.TopLeft, false).fontStyle = FontStyles.Bold | FontStyles.Italic | FontStyles.UpperCase;
            kit.Text(carry, $"{inv.CarriedKinds} / {inv.SlotCount} " + L("slots used", "ô đã sử dụng"), 1250, 14, 490, 34, 22, UiKit.Muted, TextAlignmentOptions.TopRight, false);
            kit.Text(carry, L("Tap a slot to choose what to take; tap again to change the amount.", "Chạm một ô để chọn món mang theo; chạm nữa để đổi số lượng."), 760, 62, 1000, 60, 20, UiKit.Muted, TextAlignmentOptions.TopLeft, true);
            var carried = inv.Carry.ToList();
            for (int i = 0; i < inv.SlotCount; i++)
            {
                int slot = i;
                var box = kit.Panel(carry, "Slot " + i, 28 + i * 124, 52, 108, 84, false, .6f);
                if (i < carried.Count)
                {
                    var def = Shop.Catalog.Item(carried[i].key);
                    if (def != null)
                    {
                        kit.Image(box, "Icon", 16, 8, 70, 62, Color.white, ItemIcons.Get(def));
                        kit.Text(box, "×" + carried[i].count, 44, 48, 52, 28, 18, UiKit.GoldHi, TextAlignmentOptions.BottomRight, false);
                    }
                }
                else {var t=kit.Text(box, "+", 12, 6, 84, 70, 44, UiKit.Muted, TextAlignmentOptions.Center, false);t.margin=new Vector4(5,8,9,10);}
                kit.Clickable(box, () => CycleCarry(slot));
            }
        }

        // Empty slot: takes the next owned item that is not carried yet. Filled slot: raises the amount, then empties.
        void CycleCarry(int slot)
        {
            var inv = ProfileService.Instance.Inventory; var carried = inv.Carry.ToList();
            if (slot < carried.Count)
            {
                var def = Shop.Catalog.Item(carried[slot].key); int limit = Math.Min(def.maxPerLevel, inv.Count(def.id));
                inv.SetCarry(def, carried[slot].count >= limit ? 0 : carried[slot].count + 1);
            }
            else
            {
                var next = Shop.Catalog.Owned(inv).FirstOrDefault(i => inv.CarryCount(i.id) == 0);
                if (next != null) inv.SetCarry(next, 1);
            }
            ShowTab(Tab.Shop);
        }

        void Stepper(RectTransform parent, string sign, float x, float y, Action click, bool enabled)
        {
            var box = kit.Panel(parent, "Step " + sign, x, y, 88, 68, false, .7f);
            kit.Text(box, sign, 18, 5, 52, 48, 30, enabled ? UiKit.GoldHi : new Color(.35f, .35f, .45f), TextAlignmentOptions.Center, false);
            kit.Clickable(box, click, enabled);
        }

        void DetailHeading(RectTransform detail, string title, string rarity)
        {
            var strip=kit.Panel(detail,"Item Title Banner",20,14,1028,78,true);
            var name=kit.Text(strip,title,20,5,988,62,38,ComicTheme.Ink,TextAlignmentOptions.Center,false);
            name.fontStyle=FontStyles.Bold|FontStyles.Italic|FontStyles.UpperCase;
            name.fontSharedMaterial=ComicTheme.Font.material;
            var probe=kit.Text(detail,rarity,0,0,500,44,20,ComicTheme.Paper,TextAlignmentOptions.Center,false);
            float badgeWidth=Mathf.Clamp(probe.GetPreferredValues(rarity,500,44).x+64,228,390);
            var badge=kit.Panel(detail,"Rarity",1038-badgeWidth,96,badgeWidth,48);ComicTheme.Frame(badge.gameObject,"rarity");
            probe.rectTransform.SetParent(badge,false);probe.rectTransform.anchoredPosition=new Vector2(26,-3);probe.rectTransform.sizeDelta=new Vector2(badgeWidth-52,40);
        }
        void DetailPrice(RectTransform detail, int price, bool quantityControls = false)
        {
            float width=quantityControls?310:606;
            var label=kit.Panel(detail,"Currency",432,270,width,72);
            kit.Gem(label,24,16,38,UiKit.Gold);
            kit.Text(label,price.ToString("N0")+" LINH THẠCH",84,10,width-116,54,quantityControls?24:30,UiKit.GoldHi,TextAlignmentOptions.Center,false);
        }
        void DrawItemDetail(RectTransform detail)
        {
            var list = ItemsOf(shopCategory).ToList();
            if (shopItem == null || !list.Contains(shopItem)) shopItem = list.FirstOrDefault(i => Shop.IsUnlocked(i)) ?? list.FirstOrDefault();
            var item = shopItem; if (item == null) return;
            var inv = ProfileService.Instance.Inventory; bool open = Shop.IsUnlocked(item);
            string rarity=(int)item.availableFrom>=3?L("Epic / Quý","Epic / Quý"):(int)item.availableFrom>=1?L("Rare / Hiếm","Rare / Hiếm"):L("Common / Thường","Common / Thường");
            DetailHeading(detail,item.Name(Vn),rarity);
            kit.Image(detail, "Art", 24, 104, 380, item.IsElementTalisman?148:244, open ? Color.white : new Color(.4f, .4f, .45f, 1), ItemIcons.Get(item));
            for (int i = 0; i < list.Count && i < 8; i++)
            {
                var chip = list[i]; var box = kit.Panel(detail, "Chip " + i, 24 + i * 94, 356, 88, 76, chip == item, .6f);
                kit.Image(box, "Icon", 16, 10, 56, 54, Shop.IsUnlocked(chip) ? Color.white : new Color(.4f, .4f, .45f, 1), ItemIcons.Get(chip));
                kit.Clickable(box, () => { shopItem = chip; buyQuantity = 1; ShowTab(Tab.Shop); });
            }
            var stats=kit.Panel(detail,"Item Stats",432,146,606,112);ComicTheme.Frame(stats.gameObject,"paper");
            kit.Text(stats,item.Description(Vn),22,10,562,60,25,ComicTheme.Ink,TextAlignmentOptions.TopLeft,true).fontStyle=FontStyles.Normal;
            var purchaseItem=item.IsElementTalisman?Shop.Catalog.ElementVariant(shopElement):item;
            var check=Shop.Check(purchaseItem,buyQuantity);
            string info=open?$"{L("Carry per level", "Mang tối đa/màn")}: {item.maxPerLevel}  ·  {L("Owned", "Đang có")}: {inv.CountText(purchaseItem.id)}":Shop.LockReason(item,Vn);
            if(open&&check==PurchaseResult.NotEnough)info=L($"Need {Shop.Missing(item,buyQuantity)} more Linh Thạch.",$"Cần thêm {Shop.Missing(item,buyQuantity)} Linh Thạch.");
            kit.Text(stats,info,22,64,562,30,18,open?ComicTheme.Ink:UiKit.Danger,TextAlignmentOptions.TopLeft,false);
            int max = Shop.MaxPurchasable(purchaseItem); buyQuantity = Mathf.Clamp(buyQuantity, 1, Math.Max(1, max));
            DetailPrice(detail,Shop.Price(item,buyQuantity),true);
            Stepper(detail, "-", 754, 270, () => { buyQuantity--; ShowTab(Tab.Shop); }, open&&buyQuantity > 1);
            kit.Text(detail, buyQuantity.ToString(), 842, 270, 76, 68, 30, null, TextAlignmentOptions.Center, false);
            Stepper(detail, "+", 930, 270, () => { buyQuantity++; ShowTab(Tab.Shop); }, open&&buyQuantity < max);
            if(item.IsElementTalisman)
            {
                int k=0;foreach(var e in ItemCatalog.TalismanElements)
                {
                    var selected=e;
                    var button=kit.Button(detail,e.ToString(),24+k*76,270,72,68,()=>{shopElement=selected;ShowTab(Tab.Shop);},true,shopElement==e,17);
                    var text=button.GetComponentInChildren<TMP_Text>();text.text=ElementName(e,Vn);
                    text.rectTransform.anchoredPosition=new Vector2(4,0);text.rectTransform.sizeDelta=new Vector2(64,68);
                    text.margin=new Vector4(1,4,1,4);text.fontStyle=FontStyles.Bold;text.enableAutoSizing=false;text.fontSize=17;
                    k++;
                }
            }
            check=Shop.Check(item.IsElementTalisman?Shop.Catalog.ElementVariant(shopElement):item,buyQuantity);
            kit.Button(detail, L("EXCHANGE", "TRAO ĐỔI"), 716, 350, 322, 82, () => { Shop.Buy(item, buyQuantity,shopElement); buyQuantity = 1; ShowTab(Tab.Shop); }, open&&check == PurchaseResult.Ok, true, 30);
        }

        void DrawArtifactDetail(RectTransform detail)
        {
            var list = Shop.Catalog.artifacts; var service = ProfileService.Instance.Artifacts;
            if (shopArtifact == null || !list.Contains(shopArtifact)) shopArtifact = list.FirstOrDefault();
            var a = shopArtifact; if (a == null) return;
            int level = service.Level(a.id);
            DetailHeading(detail,a.Name(Vn),L("Epic / Artifact","Quý / Pháp bảo"));
            kit.Image(detail, "Art", 24, 104, 380, 244, Color.white, ContentImages.Artifact(a.id));
            for (int i = 0; i < list.Count && i < 8; i++)
            {
                var chip = list[i]; var box = kit.Panel(detail, "Chip " + i, 24 + i * 94, 356, 88, 76, chip == a, .6f);
                kit.Image(box, "Icon", 16, 10, 56, 54, Color.white, ContentImages.Artifact(chip.id));
                kit.Clickable(box, () => { shopArtifact = chip; ShowTab(Tab.Shop); });
            }
            bool can = service.CanUpgrade(a, out var en, out var vn);
            var stats=kit.Panel(detail,"Artifact Stats",432,146,606,112);ComicTheme.Frame(stats.gameObject,"paper");
            kit.Text(stats,a.Description(Vn),22,8,562,52,23,ComicTheme.Ink,TextAlignmentOptions.TopLeft,true);
            kit.Text(stats,$"{L("Level","Cấp")} {level}/{a.MaxLevel} · {a.EffectText(level,Vn)}",22,62,562,38,18,ComicTheme.Ink,TextAlignmentOptions.TopLeft,true);
            DetailPrice(detail,level<a.MaxLevel?service.Price(a):0);
            kit.Button(detail, level<a.MaxLevel?L("UPGRADE","NÂNG CẤP"):L("MAXIMUM LEVEL","CẤP TỐI ĐA"),716,350,322,82,()=>{service.TryUpgrade(a);ShowTab(Tab.Shop);},can&&level<a.MaxLevel,true,26);
            if(!can&&level<a.MaxLevel)kit.Text(detail,Vn?vn:en,432,354,264,76,17,UiKit.Muted,TextAlignmentOptions.TopLeft,true);
        }

        // ================================================================ Library
        void BuildLibrary()
        {
            var learning = LearningService.Instance; if (learning == null) return;
            var engine = learning.Engine; var cultivation = engine.Cultivation;
            PageTitle(L("LIBRARY", "THƯ VIỆN"), L("Study to grow stronger. Each chapter belongs to one realm.", "Học để mạnh lên. Mỗi chương gắn với một cảnh giới."));
            var courses = engine.Catalog.courses;
            for (int i = 0; i < courses.Count && i < 6; i++)
            {
                int index = i; var course = courses[i]; int done = course.lessons.Count(l => engine.Progress.Lesson(l.id).rewarded);
                bool current = engine.ChapterRealm(course) == cultivation.RealmIndex;
                var card = kit.Panel(content, "Chapter " + i, (i % 3) * 600, 138 + (i / 3) * 236, 580, 220, current);
                kit.Image(card, "Badge", 16, 20, 150, 150, Color.white, ContentImages.Realm(engine.ChapterRealm(course)));
                kit.Text(card, L("CHAPTER ", "CHƯƠNG ") + (i + 1), 184, 16, 380, 30, 20, UiKit.Gold, TextAlignmentOptions.TopLeft, false).fontStyle = FontStyles.Bold | FontStyles.Italic | FontStyles.UpperCase;
                kit.Text(card, course.title, 184, 48, 380, 100, 24, null, TextAlignmentOptions.TopLeft, true);
                kit.Bar(card, 184, 156, 376, 12, course.lessons.Count > 0 ? (float)done / course.lessons.Count : 0, done == course.lessons.Count ? Green : new Color(.55f, .45f, 1f));
                kit.Text(card, $"{done}/{course.lessons.Count} " + L("mastered", "đã thành thạo") + (current ? "   ·   " + L("CURRENT REALM", "CẢNH GIỚI HIỆN TẠI") : ""), 184, 170, 380, 36, 18, current ? UiKit.GoldHi : UiKit.Muted, TextAlignmentOptions.TopLeft, false);
                kit.Clickable(card, () => OpenStudy("Lessons:" + index));
            }
            int due = engine.DueReviewCount; var chapter = engine.CurrentExamCourse;
            int chapterIndex = chapter != null ? courses.IndexOf(chapter) : 0;
            kit.Button(content, L("SCHEDULED REVIEW", "ÔN TẬP THEO LỊCH") + "  " + due, 8, 600, 560, 68, () => OpenStudy("Reviews"), due > 0, false, 24);
            kit.Button(content, L("QUICK PRACTICE", "LUYỆN TẬP NHANH"), 608, 600, 560, 68, () => OpenStudy("Practice"), engine.PracticePool().Count > 0, false, 24);
            bool exam = chapter != null && engine.CanTakeExam(chapter, out _);
            kit.Button(content, L("BREAKTHROUGH EXAM", "THI ĐỘT PHÁ"), 1208, 600, 570, 68, () => OpenStudy("Exam:" + chapterIndex), exam, true, 24);
            kit.Button(content,L("FLASHCARDS","THẺ GHI NHỚ"),8,682,560,68,()=>OpenStudy("Cards"),true,false,24);
            kit.Button(content,L("MISTAKE NOTEBOOK","SỔ TAY CÂU SAI"),608,682,560,68,()=>OpenStudy("Notebook"),true,false,24);
            var d=engine.Daily.State;
            kit.Button(content,L("DAILY STUDY","NHIỆM VỤ NGÀY")+" · "+(d.streak%7)+"/7",1208,682,570,68,()=>OpenStudy("Daily"),true,false,24);
        }

        // ================================================================ Realm
        void BuildRealm()
        {
            var cultivation = ProfileService.Instance.Cultivation; var info = CultivationUI.Describe(cultivation, Vn);
            PageTitle(L("REALM", "CẢNH GIỚI"), L("Your cultivation and what it unlocks.", "Tu vi của bạn và những gì nó mở ra."));
            var art = kit.Panel(content, "Badge Panel", 0, 138, 560, 602);
            kit.Image(art, "Badge", 40, 60, 480, 480, Color.white, ContentImages.Realm(cultivation.RealmIndex));
            var stats = kit.Panel(content, "Stats Panel", 580, 138, 1208, 602);
            kit.Text(stats, info.title, 40, 24, 1100, 64, 46, UiKit.GoldHi, TextAlignmentOptions.TopLeft, false).fontStyle = FontStyles.Bold | FontStyles.Italic | FontStyles.UpperCase;
            kit.Rule(stats, 40, 100, 1120);
            for (int t = 1; t <= CultivationTable.TiersPerRealm; t++) kit.Diamond(stats, 52 + (t - 1) * 46, 132, 24, t <= cultivation.Tier ? UiKit.GoldHi : new Color(.25f, .25f, .35f, 1));
            kit.Bar(stats, 40, 180, 1120, 22, info.fraction, info.bottleneck ? UiKit.GoldHi : new Color(.55f, .45f, 1f));
            kit.Text(stats, info.bar, 40, 210, 1120, 34, 22, UiKit.Muted, TextAlignmentOptions.TopLeft, false);
            kit.Text(stats, info.stats, 40, 262, 1120, 120, 26, null, TextAlignmentOptions.TopLeft, true);
            kit.Text(stats, L("NEXT UNLOCKS", "MỞ KHÓA TIẾP THEO"), 40, 396, 600, 30, 20, UiKit.Gold, TextAlignmentOptions.TopLeft, false).fontStyle = FontStyles.Bold | FontStyles.Italic | FontStyles.UpperCase;
            kit.Text(stats, info.nextUnlock, 40, 428, 1120, 90, 22, null, TextAlignmentOptions.TopLeft, true);
            kit.Text(stats, info.breakthroughHint, 40, 520, 780, 60, 20, UiKit.Muted, TextAlignmentOptions.TopLeft, true);
            var engine = LearningService.Instance.Engine; var chapter = engine.CurrentExamCourse;
            bool can = chapter != null && engine.CanTakeExam(chapter, out _);
            int chapterIndex = chapter != null ? engine.Catalog.courses.IndexOf(chapter) : 0;
            kit.Button(stats, L("BREAKTHROUGH EXAM", "THI ĐỘT PHÁ"), 850, 526, 320, 68, () => OpenStudy("Exam:" + chapterIndex), can, true, 24);
        }
    }
}
