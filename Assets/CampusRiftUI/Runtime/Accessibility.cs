using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Combat;

namespace CampusRift.UI
{
    public enum AccessiblePalette { Default, ColorBlind }
    // Symbols have different silhouettes even in a monochrome screenshot. No combat rule changes.
    public static class Accessibility
    {
        public static float TextScale => SettingsManager.Instance == null ? 1 : Scale(SettingsManager.Instance.Current.TextSize);
        public static float Scale(int size) => size == 2 ? 1.3f : size == 1 ? 1.15f : 1;
        public static bool ColorBlind => SettingsManager.Instance?.Current.AccessibleColors == AccessiblePalette.ColorBlind;
        public static float ReadingTime(QuizKindProxy kind) => kind == QuizKindProxy.Exam ? 1 : SettingsManager.Instance?.Current.SlowReading == true ? 1.5f : 1;
        public static string Symbol(Element e)
        {
            switch(e) {case Element.Kim:return "<>";case Element.Moc:return "Y";case Element.Thuy:return "~";case Element.Hoa:return "^";case Element.Tho:return "#";case Element.Loi:return "Z";case Element.Am:return "(";case Element.KhongGian:return "@";default:return "o";}
        }
        public static Color Color(Element e)
        {
            // Blue/orange/luminance palette; shape and names remain the primary distinction.
            switch(e) {case Element.Kim:return new Color32(240,228,66,255);case Element.Moc:return new Color32(0,158,115,255);case Element.Thuy:return new Color32(86,180,233,255);case Element.Hoa:return new Color32(230,159,0,255);case Element.Tho:return new Color32(215,192,148,255);case Element.Loi:return new Color32(204,121,167,255);case Element.Am:return new Color32(181,181,181,255);case Element.KhongGian:return new Color32(129,159,245,255);default:return UnityEngine.Color.white;}
        }
        public static Sprite SymbolSprite(Element e)
        {
            return Resources.Load<Sprite>("P23/Elements/"+e);
        }
    }
    public enum QuizKindProxy { Study, Exam }

    // Observe generated and authored TMP widgets, including those made after opening a page.
    // Scale their authored range, never scale a previously scaled value again.
    [DefaultExecutionOrder(32700)]
    public sealed class AccessibleText : MonoBehaviour
    {
        TMP_Text text; float authoredSize, authoredMin, authoredMax, lastSize, lastMin, lastMax, factor=1;
        void Awake(){text=GetComponent<TMP_Text>();Capture();}
        void Capture(){if(text==null)return;authoredSize=text.fontSize;authoredMin=text.fontSizeMin;authoredMax=text.fontSizeMax;lastSize=text.fontSize;lastMin=text.fontSizeMin;lastMax=text.fontSizeMax;}
        void LateUpdate()
        {
            if(text==null)return;
            if(!text.enableAutoSizing&&Mathf.Abs(text.fontSize-lastSize)>.01f)authoredSize=text.fontSize;
            if(Mathf.Abs(text.fontSizeMin-lastMin)>.01f)authoredMin=text.fontSizeMin;
            if(Mathf.Abs(text.fontSizeMax-lastMax)>.01f)authoredMax=text.fontSizeMax;
            factor=Accessibility.TextScale;
            if(text.enableAutoSizing){text.fontSizeMin=authoredMin*factor;text.fontSizeMax=authoredMax*factor;}
            else text.fontSize=authoredSize*factor;
            lastSize=text.fontSize;lastMin=text.fontSizeMin;lastMax=text.fontSizeMax;
        }
    }
    public sealed class AccessibilityDirector : MonoBehaviour
    {
        float nextScan;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
        {var go=new GameObject("P23 accessibility");DontDestroyOnLoad(go);go.AddComponent<AccessibilityDirector>();}
        void Update()
        {
            if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+.25f;
            foreach(var t in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                if(t.GetComponentInParent<Canvas>()!=null && t.GetComponent<AccessibleText>()==null)t.gameObject.AddComponent<AccessibleText>();
        }
    }
}
