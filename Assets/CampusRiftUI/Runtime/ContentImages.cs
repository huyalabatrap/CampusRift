using System.Collections.Generic;
using UnityEngine;

namespace CampusRift.UI
{
    // The 2D art supplied in Content/Images, copied (and downsized) to Assets/Resources/ContentImages/<group>/<name>.png.
    // Every lookup may return null: callers keep a generated or text fallback so a missing image never breaks a screen.
    public static class ContentImages
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        public static Sprite Get(string group, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if(group=="Elements" && Accessibility.ColorBlind)
            {string[] ids={"vo-he","kim","moc","thuy","hoa","tho","loi","am","khong-gian"};int index=System.Array.IndexOf(ids,name);if(index>=0)return Accessibility.SymbolSprite((Combat.Element)index);}
            string key = group + "/" + name;
            if (cache.TryGetValue(key, out var sprite)) return sprite;
            sprite = group == "Scenes" && name == "hub-background" ? ComicTheme.Sprite("academy-background") : null;
            if (sprite == null) sprite = Resources.Load<Sprite>("ContentImages/" + key);
            cache[key] = sprite; return sprite;
        }
        public static Sprite Item(string id) {id=id!=null&&id.StartsWith("ngu-hanh-phu-")?"ngu-hanh-phu":id;return Get("P20/Items",id)??Get("Items",id);}
        public static Sprite Artifact(string id) => Get("P20/Artifacts",id)??Get("Artifacts", id == "phi-kiem" ? "phi-kiem-thanh-truc" : id);
        public static Sprite Realm(int realmIndex)
        {
            string[] names = { "luyen-khi", "truc-co", "ket-dan", "nguyen-anh", "hoa-than", "luyen-hu", "do-kiep" };
            return Get("Realms", realmIndex >= 0 && realmIndex < names.Length ? names[realmIndex] : null);
        }
        public static Sprite Mastery(int badge) => Get("Badges", badge == 1 ? "mastery-dong" : badge == 2 ? "mastery-bac" : badge == 3 ? "mastery-vang" : null);
        public static Sprite Star(bool on) => Get("Badges", on ? "star-on" : "star-off");
        public static Sprite Level(int index) => Get("Scenes", "level-" + index.ToString("00"));
    }
}
