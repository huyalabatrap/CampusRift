using System.Collections.Generic;
using UnityEngine;

namespace CampusRift.Progression
{
    // Every item of the game, in shop order.
    [CreateAssetMenu(menuName = "Campus Rift/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        public List<ItemDefinition> items = new List<ItemDefinition>();
        public List<ArtifactDefinition> artifacts = new List<ArtifactDefinition>();
        static ItemCatalog cached;
        public static ItemCatalog Instance
        {
            get
            {
                if (cached == null) cached = Resources.Load<ItemCatalog>("ItemCatalog");
                if (cached == null) cached = CreateInstance<ItemCatalog>();
                return cached;
            }
        }
        // Tests may install their own catalog.
        public static void Use(ItemCatalog catalog) { cached = catalog; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cached = null; }

        readonly Dictionary<string,ItemDefinition> variants=new Dictionary<string,ItemDefinition>();
        public static readonly CampusRift.Combat.Element[] TalismanElements={CampusRift.Combat.Element.Kim,CampusRift.Combat.Element.Moc,CampusRift.Combat.Element.Thuy,CampusRift.Combat.Element.Hoa,CampusRift.Combat.Element.Tho};
        static readonly string[] TalismanNamesVN={"Kim","Mộc","Thủy","Hỏa","Thổ"};
        static readonly string[] TalismanNamesEN={"Metal","Wood","Water","Fire","Earth"};
        public ItemDefinition ElementVariant(CampusRift.Combat.Element element)
        {
            if(System.Array.IndexOf(TalismanElements,element)<0)return null;
            string id="ngu-hanh-phu-"+element.ToString().ToLowerInvariant();
            if(variants.TryGetValue(id,out var value)&&value!=null)return value;
            var original=items.Find(i=>i.id=="ngu-hanh-phu");if(original==null)return null;
            value=Instantiate(original);value.hideFlags=HideFlags.DontSave;value.id=id;value.chosenElement=element;
            int index=System.Array.IndexOf(TalismanElements,element);
            value.nameVN+=" · "+TalismanNamesVN[index];value.nameEN+=" · "+TalismanNamesEN[index];
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
        }
        public ArtifactDefinition Artifact(string id) { foreach (var a in artifacts) if (a != null && a.id == id) return a; return null; }
    }
}
