using System.Collections.Generic;
using TMPro;
using UnityEngine;
using CampusRift.Monsters;

namespace CampusRift.Combat
{
    // Pooled floating numbers above damaged monsters: element colour, bigger crits, optional reaction label.
    // Created automatically in any scene that has monsters; it listens to MonsterVitality.AnyDamaged.
    public sealed class DamageNumberPool : MonoBehaviour
    {
        public const int Capacity = 64;
        const float Lifetime = 0.9f, Rise = .85f, WorldSize = 0.023f;
        sealed class Entry { public TextMeshPro text; public Transform transform; public float born,latest,amount; public Vector3 origin; public float scale; public MonsterVitality target;public bool reserved,ar; public Element element; }
        readonly List<Entry> entries = new List<Entry>(Capacity);
        int next;
        TMP_FontAsset font;
        Material comicMaterial;readonly Material[] reactionMaterials=new Material[9];
        float groupAt=-1;
        int groupIndex;
        public int MergedHits {get;private set;}
        public ReactionLabelPool ReactionLabels {get;private set;}
        public void ShowReaction(ReactionEvent reaction)
        {
            ReactionLabels?.Show(reaction);
            ReflowBelowReactions();
        }
        void ReflowBelowReactions()
        {
            // Reserve a numeric band below every live burst, including crowded merged hits.
            var camera=Camera.main;if(camera==null||ReactionLabels==null||ReactionLabels.ActiveCount==0)return;
            float ceiling=ReactionLabels.LowestBottom+540-90;int index=0;
            foreach(var e in entries)
            {
                if(e.ar||!e.reserved||Time.time-e.born>Lifetime)continue;
                var screen=camera.WorldToScreenPoint(e.origin);if(screen.z<=0)continue;
                bool mobile=Controls.CampusInput.Mobile;
                int columns=mobile?6:8;
                float x=(mobile?850:960)+(index%columns-(columns-1)*.5f)*145*UI.Accessibility.TextScale+Random.Range(-3f,3f);
                float y=ceiling-(index/columns)*64*UI.Accessibility.TextScale;
                screen.x=x/1920*Screen.width;screen.y=y/1080*Screen.height;e.origin=camera.ScreenToWorldPoint(screen);index++;
            }
        }
        public static DamageNumberPool Instance { get; private set; }
        public int ActiveCount { get { int n = 0; foreach (var e in entries) if (e.text.gameObject.activeSelf) n++; return n; } }
        public int CreatedCount => entries.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("Damage Numbers"); DontDestroyOnLoad(go);
            go.AddComponent<DamageNumberPool>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ReactionLabels=gameObject.AddComponent<ReactionLabelPool>();
            var catalog = Resources.Load<Localization.LocalizationCatalog>("LocalizationCatalog");
            font = UI.ComicTheme.Font!=null?UI.ComicTheme.Font:catalog != null ? catalog.vietnameseFont : null;
            if(font!=null)
            {
                var outlined=Resources.Load<Material>("P10DamageNumbers");
                comicMaterial=new Material(outlined!=null?outlined:font.material){name="Comic damage numbers (pooled)"};
                comicMaterial.EnableKeyword("OUTLINE_ON");
                comicMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth,.3f);comicMaterial.SetColor(ShaderUtilities.ID_OutlineColor,Color.black);
                comicMaterial.SetFloat(ShaderUtilities.ID_FaceDilate,.12f);
                // Numeric popups retain their readable screen band in front of spike/fragment meshes.
                comicMaterial.SetFloat(ShaderUtilities.ShaderTag_ZTestMode,8);comicMaterial.renderQueue=4000;
            }
            if(comicMaterial!=null)for(int i=0;i<reactionMaterials.Length;i++)
            {
                var rule=ReactionConfig.Current.Rule((ReactionType)i);var outline=ElementChart.ColorOf(rule.first);
                reactionMaterials[i]=new Material(comicMaterial){name="Reaction damage outline "+rule.id};
                reactionMaterials[i].SetColor(ShaderUtilities.ID_OutlineColor,outline);reactionMaterials[i].SetFloat(ShaderUtilities.ID_OutlineWidth,.35f);
                reactionMaterials[i].EnableKeyword("UNDERLAY_ON");reactionMaterials[i].SetColor("_UnderlayColor",Color.black);reactionMaterials[i].SetFloat("_UnderlayDilate",.3f);reactionMaterials[i].SetFloat("_UnderlaySoftness",.04f);
            }
            for(int i=0;i<Capacity;i++)CreateEntry();
            MonsterVitality.AnyDamaged += OnDamaged;
        }
        void OnDestroy() { if (Instance == this) { MonsterVitality.AnyDamaged -= OnDamaged; Instance = null; }if(comicMaterial!=null)Destroy(comicMaterial);foreach(var material in reactionMaterials)if(material!=null)Destroy(material); }

        void OnDamaged(MonsterVitality monster, DamageInfo info)
        {
            // Start above the health bar and nameplate (EnemyHealthBars sits 0.45 m over the model).
            var ar=monster.GetComponent<AR.ARCombatContext>();float units=ar!=null?ar.scale:1;
            Vector3 top = monster.transform.position + Vector3.up * (2.7f*units);
            var r = monster.GetComponentInChildren<Renderer>();
            if (r != null) top = new Vector3(monster.transform.position.x, r.bounds.max.y + 0.9f*units, monster.transform.position.z);
            int reaction=ReactionIndex(info);float reactionScale=reaction>=0?(info.skillId=="tu-sat"?1.55f:1.45f):1;
            foreach(var existing in entries)if(existing.reserved&&existing.target==monster&&Time.time-existing.latest<=.1f)
            {existing.amount+=info.amount;existing.latest=Time.time;existing.scale=Mathf.Max(existing.scale,info.critical?1.4f:reactionScale);existing.text.text=(existing.ar?"":UI.Accessibility.Symbol(existing.element)+" ")+Mathf.CeilToInt(existing.amount);if(reaction>=0)StyleReaction(existing,reaction);MergedHits++;ReflowBelowReactions();return;}
            var entry=ShowEntry(top, info.amount, info.element, info.critical, null,ar!=null);entry.target=monster;entry.scale=Mathf.Max(entry.scale,reactionScale);if(reaction>=0)StyleReaction(entry,reaction);ReflowBelowReactions();
        }

        int ReactionIndex(DamageInfo info)
        {if(info.source!=DamageSource.Reaction||ReactionConfig.Current==null)return -1;for(int i=0;i<reactionMaterials.Length;i++)if(ReactionConfig.Current.Rule((ReactionType)i).id==info.skillId)return i;return -1;}
        void StyleReaction(Entry entry,int index)
        {entry.text.fontSharedMaterial=reactionMaterials[index];entry.text.color=Color.white;}
        // label: reaction name shown above the number (P11), e.g. "BĂNG LÔI LIỆT!".
        public void Show(Vector3 position, float amount, Element element, bool critical, string label)
        {ShowEntry(position,amount,element,critical,label);}
        Entry ShowEntry(Vector3 position, float amount, Element element, bool critical, string label,bool ar=false)
        {
            var e = Take();
            e.element=element;e.ar=ar;
            if(string.IsNullOrEmpty(label))e.text.text=(ar?"":UI.Accessibility.Symbol(element)+" ")+Mathf.CeilToInt(amount);else e.text.text="<size=70%>"+label+"</size>\n"+(ar?"":UI.Accessibility.Symbol(element)+" ")+Mathf.CeilToInt(amount);
            e.text.color = element==Element.Loi&&!UI.Accessibility.ColorBlind?new Color(1,.82f,.12f):ElementChart.ColorOf(element);
            e.text.fontSharedMaterial=comicMaterial;e.text.fontStyle = FontStyles.Bold;
            e.scale = critical ? 1.4f : 1f;
            if(Time.time-groupAt>.1f){groupAt=Time.time;groupIndex=0;}int stagger=groupIndex++;
            var cam=Camera.main;Vector3 side=cam!=null?cam.transform.right:Vector3.right;
            e.origin=position+side*((stagger%3-1)*1.1f+Random.Range(-.12f,.12f))+Vector3.up*((stagger/3)*.75f+Random.Range(0,.1f));
            if(cam!=null){Vector3 screen=cam.WorldToScreenPoint(position);screen.x+=(stagger%3-1)*110+Random.Range(-4f,4f);screen.y+=(stagger/3)*50+Random.Range(0f,4f);
                for(int attempt=0;attempt<10;attempt++){bool overlap=false;for(int i=0;i<entries.Count;i++){var other=entries[i];if(other==e||!other.reserved||Time.time-other.born>Lifetime)continue;Vector3 occupied=cam.WorldToScreenPoint(other.origin);float size=Mathf.Max(e.scale,other.scale);if(Mathf.Abs(screen.x-occupied.x)<104*size&&Mathf.Abs(screen.y-occupied.y)<46*size){overlap=true;break;}}if(!overlap)break;screen.y+=50*e.scale;}
                e.origin=cam.ScreenToWorldPoint(screen);
            }
            if(ar)e.origin=position+side*((stagger%3-1)*.045f)+Vector3.up*((stagger/3)*.03f);
            e.born=Time.time+(stagger%8)*.04f;e.latest=Time.time;e.amount=amount;e.target=null;e.reserved=true;
            e.text.gameObject.SetActive(false);ReflowBelowReactions();return e;
        }

        Entry Take()
        {
            foreach (var e in entries) if (!e.reserved) return e;
            var oldest = entries[next]; next = (next + 1) % entries.Count; return oldest;
        }
        void CreateEntry()
        {
            var go=new GameObject("Damage Number");go.transform.SetParent(transform,false);var text=go.AddComponent<TextMeshPro>();
            if(font!=null)text.font=font;if(comicMaterial!=null)text.fontSharedMaterial=comicMaterial;
            text.alignment=TextAlignmentOptions.Center;text.fontSize=10;text.fontStyle=FontStyles.Bold;text.textWrappingMode=TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta=new Vector2(24,12);text.text="888";text.ForceMeshUpdate();
            entries.Add(new Entry{text=text,transform=go.transform});go.SetActive(false);
        }

        void LateUpdate()
        {
            foreach (var e in entries)
            {
                if (!e.reserved) continue;
                if(Time.time<e.born)continue;
                if(!e.text.gameObject.activeSelf)e.text.gameObject.SetActive(true);
                float t = (Time.time - e.born) / Lifetime;
                if (t >= 1) { e.text.gameObject.SetActive(false);e.reserved=false;e.target=null;continue; }
                Place(e, t);
            }
        }

        void Place(Entry e, float t)
        {
            var cam = Camera.main;
            float rise=cam!=null?Vector3.Distance(cam.transform.position,e.origin)*.06f:Rise;
            e.transform.position = e.origin + Vector3.up * rise * t;
            if (cam != null)
            {
                e.transform.rotation = Quaternion.LookRotation(e.transform.position - cam.transform.position);
                // Constant on-screen size: readable on phones at any distance (≈28 px at 1080p).
                float distance = Vector3.Distance(cam.transform.position, e.transform.position);
                e.transform.localScale = Vector3.one * distance * WorldSize * (e.ar?.8f:1) * e.scale * UI.Accessibility.TextScale * (1f + 0.3f * (1f - t));
            }
            var c = e.text.color; c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f; e.text.color = c;
        }
    }
}
