using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace CampusRift.Localization
{
    public enum GameLanguage { Vietnamese=0, English=1 }
    [Serializable] public sealed class TranslationEntry { public string en; [TextArea] public string vi; }
    [CreateAssetMenu(menuName="Campus Rift/Localization/UI translations")]
    public sealed class LocalizationCatalog : ScriptableObject
    {
        public TMP_FontAsset vietnameseFont;
        public List<TranslationEntry> entries=new List<TranslationEntry>();
    }
}
