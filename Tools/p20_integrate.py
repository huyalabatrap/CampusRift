from pathlib import Path
root=Path(__file__).resolve().parents[1]
def edit(path,old,new):
    p=root/path;t=p.read_text(encoding='utf-8-sig');assert old in t,(path,old[:70]);p.write_text(t.replace(old,new),encoding='utf-8')
edit('Assets/Learning/Runtime/LearningUI.cs','else if(pending=="Reviews")ShowReviews();','else if(pending=="Cards")ShowCards();\n            else if(pending=="Notebook")ShowNotebook();\n            else if(pending=="Daily")ShowDaily();\n            else if(pending=="Shrine")OpenShrineQuestion();\n            else if(pending=="Reviews")ShowReviews();')
edit('Assets/Learning/Runtime/LearningUI.cs','void ShowQuestion()','public void ShowQuestion()')
edit('Assets/Learning/Runtime/LearningUI.cs','case QuizKind.Exam: title=', 'case QuizKind.Notebook: title=L("MISTAKE NOTEBOOK","SỔ TAY CÂU SAI")+"  /  "+(quiz.Answered+1)+"/"+quiz.Questions.Count;subtitle=L("Review at your own pace","Ôn theo nhịp của bạn");parent=ShowNotebook;break;\n                case QuizKind.Shrine: title=L("FORTUNE STELE","LINH BIA CƠ DUYÊN");subtitle=L("One question from a lesson you have read","Một câu từ bài đã đọc");parent=null;break;\n                case QuizKind.Exam: title=')
edit('Assets/Learning/Runtime/LearningUI.cs','switch(q.Data.type)\n            {','switch(q.Data.type)\n            {\n                case "multi-choice": case "matching": case "fill-blank": DrawExtraQuestion(q);break;')
edit('Assets/Learning/Runtime/LearningUI.cs','case QuizKind.Exam: FinishExam();return;','case QuizKind.Notebook: case QuizKind.Shrine: ExtraResult();return;\n                case QuizKind.Exam: FinishExam();return;')
edit('Assets/Learning/Runtime/LearningUI.cs','public void Close(){','public void Close(){CancelShrine();')
edit('Assets/Learning/Runtime/LearningUI.cs','back.onClick.RemoveListener(GoBack);close.onClick.RemoveListener(Close);','back.onClick.RemoveListener(GoBack);close.onClick.RemoveListener(Close);\n            CancelShrine();')
edit('Assets/Learning/Runtime/LearningUI.cs','int due=engine.DueReviewCount;','Button(L("FLASHCARDS","THẺ GHI NHỚ"),ShowCards);\n            Button(L("MISTAKE NOTEBOOK","SỔ TAY CÂU SAI"),ShowNotebook);\n            int due=engine.DueReviewCount;')
edit('Assets/CampusRiftUI/Runtime/HubPages.cs','kit.Text(content, L("Studying is for the Hub only: the world stops while you read.", "Học chỉ ở Sảnh: thế giới đứng yên khi bạn đọc."), 12, 690, 1600, 30, 18, UiKit.Muted, TextAlignmentOptions.TopLeft, false);','kit.Button(content,L("FLASHCARDS","THẺ GHI NHỚ"),8,682,560,68,()=>OpenStudy("Cards"),true,false,24);\n            kit.Button(content,L("MISTAKE NOTEBOOK","SỔ TAY CÂU SAI"),608,682,560,68,()=>OpenStudy("Notebook"),true,false,24);\n            var d=engine.Daily.State;\n            kit.Button(content,L("DAILY STUDY","NHIỆM VỤ NGÀY")+" · "+(d.streak%7)+"/7",1208,682,570,68,()=>OpenStudy("Daily"),true,false,24);')
edit('Assets/Progression/Runtime/ItemDefinition.cs','Stat = 4, Flag = 5','Stat = 4, Flag = 5, RestoreEnergy = 6, ClearNegative = 7, ElementBoost = 8')
edit('Assets/Progression/Runtime/ItemDefinition.cs','SwordUninterrupted = 3','SwordUninterrupted = 3, ControlImmune = 4, ControlGuard = 5, RevealEnemies = 6')
edit('Assets/Progression/Runtime/ItemDefinition.cs','public ItemFlag flag;','public ItemFlag flag;\n        public Element element;')
edit('Assets/Progression/Runtime/ItemDefinition.cs','public string id;','public string id;\n        public Element chosenElement;\n        public bool IsElementTalisman => id!=null && (id=="ngu-hanh-phu" || id.StartsWith("ngu-hanh-phu-"));')
edit('Assets/Progression/Runtime/ArtifactDefinition.cs','ItemSlots = 2','ItemSlots = 2, Spirit = 3, ElementCounter = 4')
edit('Assets/Progression/Runtime/ArtifactDefinition.cs','public float perLevel = 0.05f;','public float perLevel = 0.05f;\n        public float regenPerLevel;')
edit('Assets/Progression/Runtime/ArtifactDefinition.cs','case ArtifactEffect.ItemSlots:','case ArtifactEffect.Spirit: return "+"+(perLevel*level).ToString("0")+" Linh Lực · +"+(regenPerLevel*level).ToString("0.0")+"/s";\n                case ArtifactEffect.ElementCounter: return "+"+Mathf.RoundToInt(perLevel*level*100)+"% "+(vietnamese?"sát thương khắc hệ":"counter-element damage");\n                case ArtifactEffect.ItemSlots:')
edit('Assets/Progression/Runtime/ArtifactService.cs','case ArtifactEffect.BasicDamage:','case ArtifactEffect.Spirit: stats.SetModifier(StatSource.Artifact,Key+":"+a.id,StatType.MaxSpirit,value,0);stats.SetModifier(StatSource.Artifact,Key+":"+a.id,StatType.SpiritRegen,a.regenPerLevel*Level(a.id),0);break;\n                    case ArtifactEffect.BasicDamage:')
edit('Assets/Progression/Runtime/ArtifactService.cs','public int ItemSlots(ItemCatalog catalog = null)','public float CounterBonus {get {float value=0;foreach(var a in ItemCatalog.Instance.artifacts)if(a!=null&&a.effect==ArtifactEffect.ElementCounter)value+=a.perLevel*Level(a.id);return value;}}\n\n        public int ItemSlots(ItemCatalog catalog = null)')
edit('Assets/Progression/Runtime/BuffSystem.cs','e.type != ItemEffectType.HealOverTime','e.type != ItemEffectType.HealOverTime && e.type != ItemEffectType.ElementBoost')
edit('Assets/Progression/Runtime/BuffSystem.cs','if (e.type == ItemEffectType.Stat) PushStat','if(e.type==ItemEffectType.Flag&&e.flag==ItemFlag.ControlGuard)controlCharges=Mathf.Max(1,Mathf.RoundToInt(e.value));\n                if (e.type == ItemEffectType.Stat) PushStat')
edit('Assets/Progression/Runtime/BuffSystem.cs','active.Clear(); uninterruptedCharges = 0;','active.Clear(); uninterruptedCharges = controlCharges = 0;')
edit('Assets/Progression/Runtime/BuffSystem.cs','public bool EmberImmune =>','int controlCharges;\n        public bool ControlImmune => HasFlag(ItemFlag.ControlImmune);\n        public bool ConsumeControlGuard(){if(controlCharges<=0||!HasFlag(ItemFlag.ControlGuard))return false;controlCharges--;return true;}\n        public float ElementBonus(Element element){float best=0;foreach(var b in active)if(b.effect.type==ItemEffectType.ElementBoost&&b.effect.element==element)best=Mathf.Max(best,b.effect.value);return best;}\n        public bool RevealEnemies=>HasFlag(ItemFlag.RevealEnemies);\n        public bool EmberImmune =>')
edit('Assets/Progression/Runtime/PlayerItems.cs','bool reviveSpent;','bool reviveSpent,fullHealSpent,elementSpent;')
edit('Assets/Progression/Runtime/PlayerItems.cs','reviveSpent = false; readyAt = 0;','reviveSpent = fullHealSpent = elementSpent = false; readyAt = 0;')
edit('Assets/Progression/Runtime/PlayerItems.cs','if (item.IsPassive)','if((item.id=="cuu-chuyen-hoan-hon-dan"&&fullHealSpent)||(item.IsElementTalisman&&elementSpent))return Report(item,ItemUseResult.Blocked);\n            if (item.IsPassive)')
edit('Assets/Progression/Runtime/PlayerItems.cs','Effect(item);','if(item.id=="cuu-chuyen-hoan-hon-dan")fullHealSpent=true;if(item.IsElementTalisman)elementSpent=true;\n            Effect(item);')
edit('Assets/Progression/Runtime/PlayerItems.cs','case ItemEffectType.HealInstant: if','case ItemEffectType.RestoreEnergy: GetComponent<CampusExplorer>()?.RefillEnergy();break;\n                    case ItemEffectType.ClearNegative: GetComponent<Enemies.PlayerEnemyControl>()?.Clear();GetComponent<StatusEffectHost>()?.Clear();break;\n                    case ItemEffectType.HealInstant: if')
edit('Assets/Enemies/Runtime/PlayerEnemyControl.cs','bool Immune=>GetComponent<Skills.MartialAvatarRuntime>()?.ControlImmune??false;','bool Immune=> (GetComponent<Skills.MartialAvatarRuntime>()?.ControlImmune??false)||(GetComponent<Progression.BuffSystem>()?.ControlImmune??false);\n        bool BlockControl()=>Immune||(GetComponent<Progression.BuffSystem>()?.ConsumeControlGuard()??false);')
edit('Assets/Enemies/Runtime/PlayerEnemyControl.cs','if(!Immune)','if(!BlockControl())')
edit('Assets/Skills/GiantHandSeal/Runtime/MonsterVitality.cs','var original=info;','if(info.attacker!=null && (info.source==DamageSource.Skill||info.source==DamageSource.Melee||info.source==DamageSource.Projectile))\n            {\n                float extra=info.attacker.GetComponent<CampusRift.Progression.BuffSystem>()?.ElementBonus(info.element)??0;\n                if(info.attacker.GetComponent<PlayerStats>()!=null && ElementChart.Multiplier(info.element,Element)>1)extra+=CampusRift.Progression.ProfileService.Instance?.Artifacts.CounterBonus??0;\n                info.amount*=1+extra;\n            }\n            var original=info;')
edit('Assets/CampusRiftUI/Runtime/EnemyRevealMarker.cs','if(Time.time<skillRevealUntil){','if(Time.time<skillRevealUntil || (CampusRift.Levels.LevelDirector.Instance?.PlayerTransform?.GetComponent<CampusRift.Progression.BuffSystem>()?.RevealEnemies??false)){')
edit('Assets/Progression/Runtime/ItemCatalog.cs','public ItemDefinition Item(string id) { foreach (var i in items) if (i != null && i.id == id) return i; return null; }','''readonly Dictionary<string,ItemDefinition> variants=new Dictionary<string,ItemDefinition>();
        public static readonly CampusRift.Combat.Element[] TalismanElements={CampusRift.Combat.Element.Kim,CampusRift.Combat.Element.Moc,CampusRift.Combat.Element.Thuy,CampusRift.Combat.Element.Hoa,CampusRift.Combat.Element.Tho};
        public ItemDefinition ElementVariant(CampusRift.Combat.Element element)
        {
            if(System.Array.IndexOf(TalismanElements,element)<0)return null;
            string id="ngu-hanh-phu-"+element.ToString().ToLowerInvariant();
            if(variants.TryGetValue(id,out var value)&&value!=null)return value;
            var original=items.Find(i=>i.id=="ngu-hanh-phu");if(original==null)return null;
            value=Instantiate(original);value.hideFlags=HideFlags.DontSave;value.id=id;value.chosenElement=element;
            value.nameVN+=" · "+element;value.nameEN+=" · "+element;
            foreach(var e in value.effects)if(e.type==ItemEffectType.ElementBoost)e.element=element;
            variants[id]=value;return value;
        }
        public IEnumerable<ItemDefinition> Owned(Inventory inventory)
        {
            foreach(var i in items)if(i!=null&&inventory.Count(i.id)>0)yield return i;
            foreach(var element in TalismanElements){var i=ElementVariant(element);if(i!=null&&inventory.Count(i.id)>0)yield return i;}
        }
        public ItemDefinition Item(string id)
        {
            foreach(var i in items)if(i!=null&&i.id==id)return i;
            foreach(var element in TalismanElements)if(id=="ngu-hanh-phu-"+element.ToString().ToLowerInvariant())return ElementVariant(element);
            return null;
        }''')
edit('Assets/Progression/Runtime/ShopService.cs','public PurchaseResult Buy(ItemDefinition item, int quantity)','public PurchaseResult Buy(ItemDefinition item, int quantity, CampusRift.Combat.Element chosen = CampusRift.Combat.Element.None)')
edit('Assets/Progression/Runtime/ShopService.cs','var check = Check(item, quantity);','if(item!=null&&item.id=="ngu-hanh-phu"){item=catalog.ElementVariant(chosen);if(item==null)return PurchaseResult.NotFound;}\n            var check = Check(item, quantity);')
edit('Assets/Progression/Runtime/Inventory.cs','int count = Mathf.Clamp(wanted','if(item.IsElementTalisman&&wanted>0&&Data.carry.Exists(c=>c.key!=item.id&&c.key.StartsWith("ngu-hanh-phu")))return 0;\n            int count = Mathf.Clamp(wanted')
edit('Assets/CampusRiftUI/Runtime/ContentImages.cs','public static Sprite Item(string id) => Get("Items", id);','public static Sprite Item(string id) => Get("Items", id!=null&&id.StartsWith("ngu-hanh-phu-")?"ngu-hanh-phu":id);')
edit('Assets/CampusRiftUI/Runtime/LoadoutUI.cs','catalog.items.Where(i => inv.Count(i.id) > 0).ToList()','catalog.Owned(inv).ToList()')
edit('Assets/CampusRiftUI/Runtime/LoadoutUI.cs','catalog.items.FirstOrDefault(d=>d.id==entry.key)','catalog.Item(entry.key)')
edit('Assets/CampusRiftUI/Runtime/HubPages.cs','Shop.Catalog.items.FirstOrDefault(i => inv.Count(i.id) > 0 && inv.CarryCount(i.id) == 0)','Shop.Catalog.Owned(inv).FirstOrDefault(i => inv.CarryCount(i.id) == 0)')
edit('Assets/Learning/Runtime/LearningUI.Shop.cs','Shop.Catalog.items.Where(i=>inv.Count(i.id)>0).ToList()','Shop.Catalog.Owned(inv).ToList()')
edit('Assets/CampusRiftUI/Runtime/HubPages.cs','int buyQuantity = 1;','int buyQuantity = 1;\n        CampusRift.Combat.Element shopElement=CampusRift.Combat.Element.Kim;')
edit('Assets/CampusRiftUI/Runtime/HubPages.cs','Shop.Buy(item, buyQuantity);','Shop.Buy(item, buyQuantity,shopElement);')
edit('Assets/CampusRiftUI/Runtime/HubPages.cs','check=Shop.Check(item,buyQuantity);\n            kit.Button','if(item.IsElementTalisman)\n            {\n                int k=0;foreach(var e in ItemCatalog.TalismanElements){var selected=e;kit.Button(detail,e.ToString(),24+k*204,446,194,68,()=>{shopElement=selected;ShowTab(Tab.Shop);},true,shopElement==e,23);k++;}\n            }\n            check=Shop.Check(item.IsElementTalisman?Shop.Catalog.ElementVariant(shopElement):item,buyQuantity);\n            kit.Button')
edit('Assets/Learning/Runtime/LearningUI.Shop.cs','void ShowShopItem()','CampusRift.Combat.Element talismanElement=CampusRift.Combat.Element.Kim;\n        void ShowShopItem()')
edit('Assets/Learning/Runtime/LearningUI.Shop.cs','var check=Shop.Check(item,buyQuantity);','if(item.IsElementTalisman)foreach(var element in ItemCatalog.TalismanElements){var selected=element;Button(element.ToString()+(element==talismanElement?" ✓":""),()=>{talismanElement=selected;ShowShopItem();});}\n            var check=Shop.Check(item.IsElementTalisman?Shop.Catalog.ElementVariant(talismanElement):item,buyQuantity);')
edit('Assets/Learning/Runtime/LearningUI.Shop.cs','Shop.Buy(item,buyQuantity);','Shop.Buy(item,buyQuantity,talismanElement);')
print('P20 integration patched')
