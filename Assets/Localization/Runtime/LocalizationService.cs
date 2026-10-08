using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CampusRift.Learning;
using CampusRift.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CampusRift.Localization
{
    [DefaultExecutionOrder(-100)]
    public sealed class LocalizationService : MonoBehaviour
    {
        public static LocalizationService Instance {get;private set;}
        public GameLanguage Language {get;private set;}=GameLanguage.Vietnamese;
        public TMP_FontAsset VietnameseFont=>catalog!=null?catalog.vietnameseFont:null;
        public event Action Changed;
        LocalizationCatalog catalog;
        readonly Dictionary<string,string> translations=new Dictionary<string,string>(StringComparer.Ordinal);
        readonly Dictionary<string,string> cache=new Dictionary<string,string>(StringComparer.Ordinal);
        Regex phrases;
        float discoverAt;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot(){if(Instance==null)new GameObject("Localization EN-VN").AddComponent<LocalizationService>();}
        void Awake()
        {
            Instance=this;DontDestroyOnLoad(gameObject);catalog=Resources.Load<LocalizationCatalog>("LocalizationCatalog");
            RebuildDictionary();SceneManager.sceneLoaded+=SceneLoaded;
        }
        void Start()
        {
            if(SettingsManager.Instance!=null){SettingsManager.Instance.Changed+=Applied;Preview(SettingsManager.Instance.Current.Language);}
            Discover();
        }
        void Applied(GameSettings settings)=>Preview(settings.Language);
        public void Restore()=>Preview(SettingsManager.Instance!=null?SettingsManager.Instance.Current.Language:GameLanguage.Vietnamese);
        public void Preview(GameLanguage language)
        {Language=language;Changed?.Invoke();Discover();}
        void SceneLoaded(Scene _,LoadSceneMode mode){Discover();}
        void Update(){if(Time.unscaledTime>=discoverAt){discoverAt=Time.unscaledTime+.4f;Discover();}}
        public static void Bind(TMP_Text text)
        {
            if(text==null || text.GetComponentInParent<TMP_InputField>()!=null)return;
            var binding=text.GetComponent<LocalizedText>()??text.gameObject.AddComponent<LocalizedText>();binding.Refresh();
        }
        public void Discover()
        {foreach(var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))Bind(text);}
        public static string T(string english)=>Instance!=null?Instance.Translate(english):english;
        public string Translate(string english)
        {
            if(Language==GameLanguage.English || string.IsNullOrEmpty(english))return english;
            if(translations.TryGetValue(english,out var exact))return exact;
            if(cache.TryGetValue(english,out var saved))return saved;
            string value=phrases!=null?phrases.Replace(english,m=>translations[m.Value]):english;
            // Dynamic HUD counters must not cause unbounded cache growth.
            if(cache.Count>=2048)cache.Clear();cache[english]=value;return value;
        }
        void Add(string en,string vi){if(!string.IsNullOrWhiteSpace(en) && !string.IsNullOrWhiteSpace(vi) && en!=vi)translations[en]=vi;}
        public void RebuildDictionary()
        {
            translations.Clear();cache.Clear();
            if(catalog!=null)foreach(var row in catalog.entries)Add(row.en,row.vi);
            var learning=Resources.Load<LearningCatalog>("LearningCatalog");
            if(learning!=null)foreach(var course in learning.courses)
            {
                Add(course.title,course.titleVN);Add(course.description,course.descriptionVN);
                foreach(var lesson in course.lessons)
                {
                    Add(lesson.title,lesson.titleVN);
                    foreach(var page in lesson.pages){Add(page.title,page.titleVN);Add(page.content,page.contentVN);Add(page.example,page.exampleVN);Add(page.takeaway,page.takeawayVN);}
                    if(lesson.questionBank!=null)foreach(var q in lesson.questionBank.questions)
                    {Add(q.prompt,q.promptVN);Add(q.explanation,q.explanationVN);foreach(var a in q.options)Add(a.text,a.textVN);}
                }
                foreach(var tier in course.skillTiers)if(tier.skill!=null){Add(tier.skill.displayName,tier.skill.displayNameVN);Add(tier.skill.description,tier.skill.descriptionVN);}
            }
            // Longest phrases first; boundaries prevent translating inside unrelated words.
            phrases=translations.Count==0?null:new Regex(@"(?<![\p{L}\p{N}_])(?:"+
                string.Join("|",translations.Keys.OrderByDescending(s=>s.Length).Select(Regex.Escape))+@")(?![\p{L}\p{N}_])",RegexOptions.CultureInvariant);
        }
        void OnDestroy()
        {
            SceneManager.sceneLoaded-=SceneLoaded;
            if(SettingsManager.Instance!=null)SettingsManager.Instance.Changed-=Applied;
            if(Instance==this)Instance=null;
        }
    }
}
