from pathlib import Path
p=Path('Assets/CampusRiftUI/Runtime/HubPages.cs')
text=p.read_text(encoding='utf-8-sig')
start=text.index('        void DrawItemDetail(')
end=text.index('        // ================================================================ Library',start)
replacement='''        void DetailHeading(RectTransform detail, string title, string rarity)
        {
            var strip=kit.Panel(detail,"Item Title Banner",20,14,1028,78,true);
            var name=kit.Text(strip,title,20,6,988,62,38,ComicTheme.Ink,TextAlignmentOptions.Center,false);
            name.fontStyle=FontStyles.Bold|FontStyles.Italic|FontStyles.UpperCase;
            name.fontSharedMaterial=ComicTheme.Font.material;
            var badge=kit.Panel(detail,"Rarity",810,96,228,44);ComicTheme.Frame(badge.gameObject,"rarity");
            kit.Text(badge,rarity,14,2,200,38,20,ComicTheme.Paper,TextAlignmentOptions.Center,false);
        }
        void DetailPrice(RectTransform detail, int price)
        {
            var label=kit.Panel(detail,"Currency",432,270,606,72);
            kit.Gem(label,24,16,38,UiKit.Gold);
            kit.Text(label,price.ToString("N0")+" LINH THẠCH",84,10,490,54,30,UiKit.GoldHi,TextAlignmentOptions.Center,false);
        }
        void DrawItemDetail(RectTransform detail)
        {
            var list = ItemsOf(shopCategory).ToList();
            if (shopItem == null || !list.Contains(shopItem)) shopItem = list.FirstOrDefault(i => Shop.IsUnlocked(i)) ?? list.FirstOrDefault();
            var item = shopItem; if (item == null) return;
            var inv = ProfileService.Instance.Inventory; bool open = Shop.IsUnlocked(item);
            string rarity=(int)item.availableFrom>=3?L("Epic / Quý","Epic / Quý"):(int)item.availableFrom>=1?L("Rare / Hiếm","Rare / Hiếm"):L("Common / Thường","Common / Thường");
            DetailHeading(detail,item.Name(Vn),rarity);
            kit.Image(detail, "Art", 24, 104, 380, 244, open ? Color.white : new Color(.4f, .4f, .45f, 1), ItemIcons.Get(item));
            for (int i = 0; i < list.Count && i < 8; i++)
            {
                var chip = list[i]; var box = kit.Panel(detail, "Chip " + i, 24 + i * 94, 356, 88, 76, chip == item, .6f);
                kit.Image(box, "Icon", 16, 10, 56, 54, Shop.IsUnlocked(chip) ? Color.white : new Color(.4f, .4f, .45f, 1), ItemIcons.Get(chip));
                kit.Clickable(box, () => { shopItem = chip; buyQuantity = 1; ShowTab(Tab.Shop); });
            }
            var stats=kit.Panel(detail,"Item Stats",432,146,606,112);ComicTheme.Frame(stats.gameObject,"paper");
            kit.Text(stats,item.Description(Vn),22,10,562,60,25,ComicTheme.Ink,TextAlignmentOptions.TopLeft,true).fontStyle=FontStyles.Normal;
            var check=Shop.Check(item,buyQuantity);
            string info=open?$"{L("Carry per level", "Mang tối đa/màn")}: {item.maxPerLevel}  ·  {L("Owned", "Đang có")}: {inv.Count(item.id)}":Shop.LockReason(item,Vn);
            if(open&&check==PurchaseResult.NotEnough)info=L($"Need {Shop.Missing(item,buyQuantity)} more Linh Thạch.",$"Cần thêm {Shop.Missing(item,buyQuantity)} Linh Thạch.");
            kit.Text(stats,info,22,74,562,30,18,open?ComicTheme.Ink:UiKit.Danger,TextAlignmentOptions.TopLeft,false);
            int max = Shop.MaxPurchasable(item); buyQuantity = Mathf.Clamp(buyQuantity, 1, Math.Max(1, max));
            DetailPrice(detail,Shop.Price(item,buyQuantity));
            Stepper(detail, "-", 432, 356, () => { buyQuantity--; ShowTab(Tab.Shop); }, open&&buyQuantity > 1);
            kit.Text(detail, buyQuantity.ToString(), 524, 356, 76, 68, 34, null, TextAlignmentOptions.Center, false);
            Stepper(detail, "+", 606, 356, () => { buyQuantity++; ShowTab(Tab.Shop); }, open&&buyQuantity < max);
            check=Shop.Check(item,buyQuantity);
            kit.Button(detail, L("EXCHANGE", "TRAO ĐỔI"), 716, 350, 322, 82, () => { Shop.Buy(item, buyQuantity); buyQuantity = 1; ShowTab(Tab.Shop); }, open&&check == PurchaseResult.Ok, true, 30);
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

'''
text=text[:start]+replacement+text[end:]
text=text.replace('118 + (i / 3) * 236','138 + (i / 3) * 236')
text=text.replace('"Badge Panel", 0, 118, 560, 620','"Badge Panel", 0, 138, 560, 602').replace('"Stats Panel", 580, 118, 1208, 620','"Stats Panel", 580, 138, 1208, 602')
p.write_text(text,encoding='utf-8')
print('Updated shop detail layout')
