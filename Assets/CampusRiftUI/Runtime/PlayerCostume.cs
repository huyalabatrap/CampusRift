using System;
using System.Linq;
using UnityEngine;
using CampusRift.Progression;
namespace CampusRift.UI
{
    public sealed class CostumeDefinition
    {
        public string id,vn,en,achievement;public Color cloth,accent;
        public CostumeDefinition(string id,string vn,string en,string achievement,Color cloth,Color accent)
        {this.id=id;this.vn=vn;this.en=en;this.achievement=achievement;this.cloth=cloth;this.accent=accent;}
    }
    public sealed class PlayerCostume:MonoBehaviour
    {
        public static readonly CostumeDefinition[] Catalog={
            new CostumeDefinition("default","Đồng phục","School Uniform",null,Color.white,Color.white),
            new CostumeDefinition("jade","Thanh Ngọc","Jade Scholar","gold",new Color(.18f,.65f,.58f),new Color(.55f,1,.88f)),
            new CostumeDefinition("dawn","Bình Minh","Dawn Warden","rift",new Color(.85f,.48f,.24f),new Color(1,.8f,.3f)),
            new CostumeDefinition("cloud","Lăng Vân","Cloud Walker","tower10",new Color(.3f,.48f,.92f),new Color(.55f,.8f,1)),
            new CostumeDefinition("dream","Dạ Mộng","Night Dream","night1",new Color(.55f,.2f,.72f),new Color(.95f,.5f,1))
        };
        public string Applied {get;private set;}
        public static bool Open(ProfileData p,CostumeDefinition c)=>DevMode.Active || c.achievement==null||EndgameService.State(p).achievements.Contains(c.achievement);
        public static bool Equip(ProfileService p,string id)
        {var c=Catalog.FirstOrDefault(x=>x.id==id);if(c==null||!Open(p.Data,c))return false;EndgameService.State(p.Data).costume=id;p.MarkDirty();foreach(var v in FindObjectsByType<PlayerCostume>(FindObjectsInactive.Include,FindObjectsSortMode.None))v.Apply(id);return true;}
        public static void Attach(GameObject player){if(player!=null)(player.GetComponent<PlayerCostume>()??player.AddComponent<PlayerCostume>()).Apply(EndgameService.State(ProfileService.Ensure().Data).costume);}
        void Start(){Apply(EndgameService.State(ProfileService.Ensure().Data).costume);}
        public void Apply(string id)
        {
            var c=Catalog.FirstOrDefault(x=>x.id==id)??Catalog[0];Applied=c.id;
            foreach(var r in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                for(int i=0;i<r.sharedMaterials.Length;i++)
                {
                    var m=r.sharedMaterials[i];if(m==null)continue;
                    bool cloth=m.name.StartsWith("cloth",StringComparison.OrdinalIgnoreCase);
                    bool accessory=m.name.StartsWith("HatAnd",StringComparison.OrdinalIgnoreCase);
                    if(!cloth&&!accessory)continue;
                    var block=new MaterialPropertyBlock();r.GetPropertyBlock(block,i);
                    var tint=c.id=="default"?(m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.white):cloth?c.cloth:c.accent;
                    block.SetColor("_BaseColor",tint);block.SetColor("_Color",tint);r.SetPropertyBlock(block,i);
                }
            }
        }
    }
}
