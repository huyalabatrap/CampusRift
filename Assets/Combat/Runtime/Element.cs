using UnityEngine;

namespace CampusRift.Combat
{
    // Ngũ hành plus Lôi, Âm and Không Gian (plan §7.2). Values are stored in assets: append only.
    public enum Element { None = 0, Kim = 1, Moc = 2, Thuy = 3, Hoa = 4, Tho = 5, Loi = 6, Am = 7, KhongGian = 8 }

    public static class ElementChart
    {
        public const float Advantage = 1.5f, Disadvantage = 0.75f, KimVersusAm = 0.8f;

        // Tương khắc: Kim ▶ Mộc ▶ Thổ ▶ Thủy ▶ Hỏa ▶ Kim.
        public static Element Overcomes(Element e)
        {
            switch (e)
            {
                case Element.Kim: return Element.Moc;
                case Element.Moc: return Element.Tho;
                case Element.Tho: return Element.Thuy;
                case Element.Thuy: return Element.Hoa;
                case Element.Hoa: return Element.Kim;
                default: return Element.None;
            }
        }

        // Tương sinh: Mộc → Hỏa → Thổ → Kim → Thủy → Mộc.
        public static Element Generates(Element e)
        {
            switch (e)
            {
                case Element.Moc: return Element.Hoa;
                case Element.Hoa: return Element.Tho;
                case Element.Tho: return Element.Kim;
                case Element.Kim: return Element.Thuy;
                case Element.Thuy: return Element.Moc;
                default: return Element.None;
            }
        }

        public static bool Generates(Element from, Element to) => from != Element.None && Generates(from) == to;

        public static float Multiplier(Element attack, Element target)
        {
            if (attack == Element.None || target == Element.None) return 1f;
            if (attack == Element.Loi && target == Element.Am) return Advantage;
            if (attack == Element.Kim && target == Element.Am) return KimVersusAm;
            if (Overcomes(attack) == target) return Advantage;
            if (Overcomes(target) == attack) return Disadvantage;
            return 1f;
        }

        public static Color ColorOf(Element e)
        {
            if (UI.Accessibility.ColorBlind) return UI.Accessibility.Color(e);
            switch (e)
            {
                case Element.Kim: return new Color(1f, .82f, .3f);
                case Element.Moc: return new Color(.35f, .9f, .45f);
                case Element.Thuy: return new Color(.45f, .8f, 1f);
                case Element.Hoa: return new Color(1f, .45f, .2f);
                case Element.Tho: return new Color(.8f, .6f, .3f);
                case Element.Loi: return new Color(.8f, .6f, 1f);
                case Element.Am: return new Color(.6f, .5f, .7f);
                case Element.KhongGian: return new Color(.55f, .3f, .9f);
                default: return Color.white;
            }
        }
    }
}
