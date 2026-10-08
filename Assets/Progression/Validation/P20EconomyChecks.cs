#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using CampusRift.Combat;
using CampusRift.Enemies;
using CampusRift.Monsters;
namespace CampusRift.Progression
{
    public static class P20EconomyChecks
    {
        static bool Close(float a,float b)=>Mathf.Abs(a-b)<.01f;
        public static void Run(GameObject player,ProfileService profile,Action<bool,string> check)
        {
            var cat=ItemCatalog.Instance;check(cat.items.Count==16&&cat.artifacts.Count==5,"P20 catalog16 items/5 artifacts");
            string[] ids={"thanh-tam-dan","cuu-chuyen-hoan-hon-dan","tu-khi-dan","bao-kich-dan","ngu-hanh-phu","tam-yeu-phu"};int[] prices={50,250,90,70,100,60},max={2,1,2,2,1,2},realms={2,3,2,2,3,1};
            for(int i=0;i<ids.Length;i++){var d=cat.Item(ids[i]);check(d!=null&&d.price==prices[i]&&d.maxPerLevel==max[i]&&(int)d.availableFrom==realms[i],ids[i]+" schema/price/cap/realm");check(UI.ContentImages.Item(ids[i])!=null,ids[i]+" supplied comic icon");}
            var stats=player.GetComponent<PlayerStats>();var buffs=player.GetComponent<BuffSystem>();var health=player.GetComponent<PlayerMonsterHealth>();var pi=player.GetComponent<PlayerItems>();var explorer=player.GetComponent<CampusExplorer>();var control=PlayerEnemyControl.Ensure(player);
            profile.Cultivation.SetState(Realm.HoaThan,1,0);profile.Wallet.Earn(30000,"QA");
            float clock=100;pi.Clock=()=>clock;
            ItemUseResult Use(string id){var item=cat.Item(id);profile.Inventory.Add(id,2);pi.Bag.Slots.Clear();pi.Bag.Slots.Add(new LevelBag.Slot{item=item,remaining=2});clock+=2;return pi.Use(0);}
            pi.BeginLevel();explorer.TrySpendEnergy(explorer.maxEnergy*.8f);control.Stun(2);
            check(Use(ids[0])==ItemUseResult.Used&&Close(explorer.Energy,explorer.maxEnergy)&&buffs.ControlImmune,"Clear Heart restores stamina and control immunity");
            control.Clear();control.Stun(3);check(!control.Stunned,"Control immunity prevents actual Stun");buffs.Tick(6);control.Stun(3);check(control.Stunned,"Control immunity expires after5seconds");
            health.Revive(.2f,0);check(Use(ids[1])==ItemUseResult.Used&&Close(health.CurrentHealth,health.maxHealth)&&!control.Stunned,"Full heal cleanses actual negative control");
            health.Revive(.2f,0);check(Use(ids[1])==ItemUseResult.Blocked,"Full heal limited to once per level");
            buffs.ClearAll();float cdr=stats.CooldownReduction;check(Use(ids[2])==ItemUseResult.Used&&Close(stats.CooldownReduction,cdr+.3f),"Qi pill30% CDR");buffs.Tick(46);check(Close(stats.CooldownReduction,cdr),"Qi pill expires45seconds");
            float crit=stats.CritChance;check(Use(ids[3])==ItemUseResult.Used&&Close(stats.CritChance,crit+.25f),"Critical pill25% chance");buffs.Tick(46);check(Close(stats.CritChance,crit),"Critical pill expires45seconds");
            var shop=new ShopService(profile);int cash=profile.Wallet.Balance;check(shop.Buy(cat.Item(ids[4]),1,Element.Hoa)==PurchaseResult.Ok&&profile.Wallet.Balance==cash-100&&profile.Inventory.Count("ngu-hanh-phu-hoa")==1,"Chosen element stored at purchase,100LT");
            var variant=cat.Item("ngu-hanh-phu-hoa");profile.Inventory.ClearCarry();check(profile.Inventory.SetCarry(variant,1)==1,"Chosen elemental talisman enters loadout");profile.Inventory.Add("ngu-hanh-phu-kim",1);check(profile.Inventory.SetCarry(cat.Item("ngu-hanh-phu-kim"),1)==0,"One elemental talisman type per level");
            pi.BeginLevel();check(Use(variant.id)==ItemUseResult.Used&&Close(buffs.ElementBonus(Element.Hoa),.4f)&&Close(buffs.ElementBonus(Element.Thuy),0),"Talisman40% for chosen element only");buffs.Tick(61);check(Close(buffs.ElementBonus(Element.Hoa),0),"Element buff expires60seconds");
            check(Use(ids[5])==ItemUseResult.Used&&buffs.RevealEnemies,"Tracking reveal flag on");buffs.Tick(31);check(!buffs.RevealEnemies,"Tracking expires30seconds");
            buffs.ClearAll();profile.Data.artifacts.RemoveAll(a=>a.key=="linh-luc-ho-lo"||a.key=="ngoc-boi-ngu-hanh");profile.Cultivation.SetState(Realm.LuyenKhi,1,0);
            var gourd=cat.Artifact("linh-luc-ho-lo");var pendant=cat.Artifact("ngoc-boi-ngu-hanh");cash=profile.Wallet.Balance;
            check(profile.Artifacts.TryUpgrade(gourd)&&profile.Artifacts.TryUpgrade(pendant)&&profile.Wallet.Balance==cash-650,"Artifact first levels cost250/400");check(!profile.Artifacts.TryUpgrade(gourd)&&!profile.Artifacts.TryUpgrade(pendant),"Artifact cap follows reached realms");
            stats.RemoveSource(StatSource.Artifact,ArtifactService.Key+":"+gourd.id);float spiritBefore=stats.MaxSpirit,regenBefore=stats.SpiritRegen;profile.Artifacts.ApplyTo(stats);check(Close(stats.MaxSpirit,spiritBefore+8)&&Close(stats.SpiritRegen,regenBefore+.4f),"Gourd +8spirit/+0.4regen");profile.Artifacts.ApplyTo(stats);check(Close(stats.MaxSpirit,spiritBefore+8),"Artifact apply does not stack");
            profile.Cultivation.SetState(Realm.HoaThan,1,0);cash=profile.Wallet.Balance;for(int i=0;i<4;i++){profile.Artifacts.TryUpgrade(gourd);profile.Artifacts.TryUpgrade(pendant);}profile.Artifacts.ApplyTo(stats);
            check(profile.Artifacts.Level(gourd.id)==5&&profile.Artifacts.Level(pendant.id)==5&&profile.Wallet.Balance==cash-11750,"Artifact full price tables and5level cap");
            check(Close(profile.Artifacts.CounterBonus,.15f),"Pendant5×3%=15% counter bonus");
            var target=new GameObject("P20 damage receiver").AddComponent<MonsterVitality>();target.Element=Element.Moc;target.SetMaxHealth(1000,true);
            var hit=DamageInfo.Create(150,Element.Kim,DamageSource.Skill,target.transform.position,Vector3.zero,player);target.ApplyDamage(hit);check(Close(1000-target.Health,172.5f),"Pendant actual counter hit +15%");target.ResetVitality();hit.element=Element.Thuy;hit.amount=100;target.ApplyDamage(hit);check(Close(1000-target.Health,100),"Pendant does not boost neutral element");UnityEngine.Object.Destroy(target.gameObject);
            check(UI.ContentImages.Artifact(gourd.id)!=null&&UI.ContentImages.Artifact(pendant.id)!=null,"Two artifact comic icons bound");pi.BeginLevel();control.Clear();
        }
    }
}
#endif
