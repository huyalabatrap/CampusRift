using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Progression;
using CampusRift.UI;
namespace CampusRift.Learning
{
    // "Đan Các": the shop, the carry list and the artifacts (P08-T06). Part of the study panel so it opens from the same
    // Courses screen and works with the same pause, back and close behaviour. Linh Thạch is earned only by studying and
    // playing; nothing on these screens sells it (plan §10).
    public sealed partial class LearningUI
    {
        ProfileService Profile=>ProfileService.Instance;
        ShopService shop;
        ItemDefinition shopItem;
        int buyQuantity=1;

        ShopService Shop=>shop??(shop=new ShopService(Profile));
        static readonly Color Gold=new Color(1,.8f,.4f);

        void AddLinhLines()
        {
            var linh=engine.LastLinhThach;
            if(linh==null)return;
            if(linh.total>0)Text($"+{linh.total} Linh Thạch"+(linh.lesson>0?"  ("+L("lesson ","bài học ")+linh.lesson+")":"")+(linh.review>0?"  ("+L("review ","ôn tập ")+linh.review+")":""),Gold);
            if(linh.retryCapReached)Text(L("Today's limit of repeat rewards (300 Linh Thạch) is reached.","Đã đạt trần thưởng luyện lại hôm nay (300 Linh Thạch)."),Gold);
            if(!string.IsNullOrEmpty(linh.notice))Text(linh.notice+"  —  "+L("rewards pause for 2 minutes.","tạm dừng thưởng 2 phút."),new Color(1,.55f,.4f));
        }

        void Picture(Sprite sprite,float height=200)
        {
            if(sprite==null||imageTemplate==null)return;
            var img=Instantiate(imageTemplate,content);img.gameObject.SetActive(true);img.sprite=sprite;img.preserveAspect=true;
            var layout=img.GetComponent<LayoutElement>();if(layout!=null)layout.preferredHeight=height;
        }
        string ItemName(ItemDefinition i)=>i.Name(Vn);
        string Balance=>Profile.Wallet.BalanceText+" Linh Thạch";

        // ---------------------------------------------------------------- shop home
        public void ShowShop()
        {
            Clear("Shop",L("ALCHEMY PAVILION","ĐAN CÁC"),Balance,ShowCourses);
            Text(L("Spend Linh Thạch earned by studying. Nothing here is sold for real money.","Dùng Linh Thạch kiếm được nhờ học. Ở đây không bán gì bằng tiền thật."));
            var cat=Shop.Catalog;
            Button(L("RECOVERY","HỒI PHỤC")+"  /  "+cat.items.Count(i=>i.kind==ItemKind.Heal||i.kind==ItemKind.Revive),()=>ShowShopCategory(0));
            Button(L("POWER","TĂNG SỨC MẠNH")+"  /  "+cat.items.Count(i=>i.kind==ItemKind.Buff),()=>ShowShopCategory(1));
            Button(L("LEVELS 8-10","MÀN 8–10")+"  /  "+cat.items.Count(i=>i.kind==ItemKind.FireWard||i.kind==ItemKind.Utility),()=>ShowShopCategory(2));
            Button(L("ARTIFACTS","PHÁP BẢO")+"  /  "+cat.artifacts.Count,ShowArtifacts);
            Button(L("TAKE INTO THE LEVEL","MANG VÀO MÀN")+"  /  "+Profile.Inventory.CarriedKinds+"/"+Profile.Inventory.SlotCount+" "+L("slots","ô"),ShowCarry);
        }

        static bool InCategory(ItemDefinition i,int category)=>category==0?(i.kind==ItemKind.Heal||i.kind==ItemKind.Revive):category==1?i.kind==ItemKind.Buff:(i.kind==ItemKind.FireWard||i.kind==ItemKind.Utility);
        void ShowShopCategory(int category)
        {
            string[] en={"RECOVERY","POWER","LEVELS 8-10"},vn={"HỒI PHỤC","TĂNG SỨC MẠNH","MÀN 8–10"};
            Clear("ShopCategory",L(en[category],vn[category]),Balance,ShowShop);
            foreach(var item in Shop.Catalog.items.Where(i=>InCategory(i,category)))
            {
                var captured=item;bool open=Shop.IsUnlocked(item);
                string state=open?$"{Shop.Price(item,1)} LT  /  {L("owned","đang có")} {Profile.Inventory.CountText(item.id)}":Shop.LockReason(item,Vn);
                Button($"{ItemName(item)}  /  {state}",()=>{shopItem=captured;buyQuantity=1;ShowShopItem();},true);
            }
        }

        CampusRift.Combat.Element talismanElement=CampusRift.Combat.Element.Kim;
        void ShowShopItem()
        {
            var item=shopItem;var purchaseItem=item.IsElementTalisman?Shop.Catalog.ElementVariant(talismanElement):item;int category=item.kind==ItemKind.Heal||item.kind==ItemKind.Revive?0:item.kind==ItemKind.Buff?1:2;
            Clear("ShopItem",ItemName(item),Balance,()=>ShowShopCategory(category));
            Picture(ItemIcons.Get(item));
            Text(item.Description(Vn));
            Text($"{L("Price","Giá")}: {Shop.Price(item,1)} Linh Thạch   /   {L("Carry per level","Mang tối đa/màn")}: {item.maxPerLevel}   /   {L("Owned","Đang có")}: {Profile.Inventory.CountText(purchaseItem.id)}");
            if(!Shop.IsUnlocked(item)){Text(Shop.LockReason(item,Vn),Gold);return;}
            int max=Shop.MaxPurchasable(purchaseItem);buyQuantity=Mathf.Clamp(buyQuantity,1,Math.Max(1,max));
            Text($"{L("Quantity","Số lượng")}: {buyQuantity}   /   {L("Total","Tổng")}: {Shop.Price(item,buyQuantity)} Linh Thạch");
            Button(L("MORE  (+1)","THÊM  (+1)"),()=>{buyQuantity++;ShowShopItem();},buyQuantity<max);
            Button(L("LESS  (-1)","BỚT  (-1)"),()=>{buyQuantity--;ShowShopItem();},buyQuantity>1);
            if(item.IsElementTalisman)foreach(var element in ItemCatalog.TalismanElements){var selected=element;Button(element.ToString()+(element==talismanElement?" ✓":""),()=>{talismanElement=selected;ShowShopItem();});}
            var check=Shop.Check(item.IsElementTalisman?Shop.Catalog.ElementVariant(talismanElement):item,buyQuantity);
            if(check==PurchaseResult.NotEnough)
            {
                int missing=Shop.Missing(item,buyQuantity);
                Text(L($"You need {missing} more Linh Thạch. Study more to earn it.",$"Cần thêm {missing} Linh Thạch — học thêm để tích lũy."),new Color(1,.55f,.4f));
                Button(L("OPEN THE LIBRARY","MỞ THƯ VIỆN"),ShowCourses);
            }
            Button(L("CONFIRM PURCHASE","XÁC NHẬN MUA"),()=>{Shop.Buy(item,buyQuantity,talismanElement);buyQuantity=1;ShowShopItem();status.text=L("Purchased.","Đã mua.");},check==PurchaseResult.Ok);
        }

        // ---------------------------------------------------------------- carry list
        void ShowCarry()
        {
            var inv=Profile.Inventory;
            Clear("Carry",L("TAKE INTO THE LEVEL","MANG VÀO MÀN"),$"{inv.CarriedKinds}/{inv.SlotCount} {L("slots","ô")}",ShowShop);
            Text(L("Tap an item to change how many you take. Unused items stay in your inventory.","Chạm một vật phẩm để đổi số lượng mang theo. Món chưa dùng vẫn còn trong kho."));
            var owned=Shop.Catalog.Owned(inv).ToList();
            if(owned.Count==0)Text(L("Your inventory is empty. Buy items in the shop first.","Kho đang trống. Hãy mua vật phẩm ở cửa hàng trước."),Gold);
            foreach(var item in owned)
            {
                var captured=item;int carry=inv.CarryCount(item.id),limit=Math.Min(item.maxPerLevel,inv.Count(item.id));
                bool canAdd=carry>0||inv.CarriedKinds<inv.SlotCount;
                Button($"{ItemName(item)}  /  {L("take","mang")} {carry}/{limit}  /  {L("owned","có")} {inv.CountText(item.id)}",()=>
                {
                    int next=carry>=limit?0:carry+1;
                    if(inv.SetCarry(captured,next)==0 && next>0)status.text=L("No free slot.","Hết ô.");
                    ShowCarry();
                },canAdd||carry>0);
            }
            Button(L("TAKE NOTHING","KHÔNG MANG GÌ"),()=>{inv.ClearCarry();ShowCarry();},inv.CarriedKinds>0);
        }

        // ---------------------------------------------------------------- artifacts
        void ShowArtifacts()
        {
            var service=Profile.Artifacts;
            Clear("Artifacts",L("ARTIFACTS","PHÁP BẢO"),Balance,ShowShop);
            Text(L("Artifacts are permanent. Their level cannot exceed the number of realms you have reached.","Pháp bảo là vĩnh viễn. Cấp pháp bảo không vượt quá số cảnh giới đã đạt."));
            foreach(var a in Shop.Catalog.artifacts)
            {
                var captured=a;int level=service.Level(a.id);
                Picture(ContentImages.Artifact(a.id),150);
                Text($"{a.Name(Vn)}  /  {L("level","cấp")} {level}/{a.MaxLevel}\n{a.Description(Vn)}\n{a.EffectText(level,Vn)}");
                bool can=service.CanUpgrade(a,out var en,out var vn);
                string label=level>=a.MaxLevel?L("MAX LEVEL","CẤP TỐI ĐA"):$"{L("UPGRADE","NÂNG CẤP")}  /  {service.Price(a)} LT"+(can?"":"  /  "+L(en,vn));
                Button(label,()=>{service.TryUpgrade(captured);ShowArtifacts();},can);
            }
        }
    }
}
