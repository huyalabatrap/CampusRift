using System.Collections.Generic;
using TMPro;
using UnityEngine;
using CampusRift.Monsters;

namespace CampusRift.Combat
{
    // Pooled billboard health bars above monsters. A bar shows for 4 s after a hit, or while the monster
    // is locked on (P03-T05). Elites and bosses show their name.
    public sealed class EnemyHealthBars : MonoBehaviour
    {
        const float VisibleAfterHit = 4f, Width = 1.1f, Height = 0.12f, WorldSize = 0.018f;
        sealed class Bar
        {
            public Transform root, fill; public SpriteRenderer back, front, frame, namePlate, nameBorder; public SpriteRenderer[] icons; public TextMeshPro label;
            public MonsterVitality target; public float until; public float height; public SkinnedMeshRenderer[] renderers;
            public SpriteRenderer[] divisions;
        }
        readonly List<Bar> bars = new List<Bar>();
        readonly Dictionary<MonsterVitality, Bar> byTarget = new Dictionary<MonsterVitality, Bar>();
        readonly Dictionary<MonsterVitality, string> names = new Dictionary<MonsterVitality, string>();
        MonsterVitality locked;
        Sprite pixel;
        TMP_FontAsset font;
        public static EnemyHealthBars Instance { get; private set; }
        public int VisibleCount { get { int n = 0; foreach (var b in bars) if (b.root.gameObject.activeSelf) n++; return n; } }
        public int CreatedCount => bars.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("Enemy Health Bars"); DontDestroyOnLoad(go);
            go.AddComponent<EnemyHealthBars>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0, .5f), 4);
            var catalog = Resources.Load<Localization.LocalizationCatalog>("LocalizationCatalog");
            font = catalog != null ? catalog.vietnameseFont : null;
            MonsterVitality.AnyDamaged += OnDamaged;
        }
        void OnDestroy() { if (Instance == this) { MonsterVitality.AnyDamaged -= OnDamaged; Instance = null; } }

        // Elite/boss nameplate (P04, P12). Null or empty removes the name.
        public void SetName(MonsterVitality target, string displayName)
        {
            if (target == null) return;
            if (string.IsNullOrEmpty(displayName)) names.Remove(target); else { names[target] = displayName; Show(target, float.PositiveInfinity); }
        }

        // Lock-on keeps the bar visible (P03-T05).
        public void SetLocked(MonsterVitality target)
        {
            locked = target;
            if (target != null && !target.Defeated) Show(target, 0);
        }

        public bool IsShowing(MonsterVitality target) => target != null && byTarget.TryGetValue(target, out var b) && b.root.gameObject.activeSelf;

        void OnDamaged(MonsterVitality monster, DamageInfo info) => Show(monster, VisibleAfterHit);

        void Show(MonsterVitality target, float seconds)
        {
            if (!byTarget.TryGetValue(target, out var bar))
            {
                bar = Take(); bar.target = target; byTarget[target] = bar;
                bar.renderers=target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var r = target.GetComponentInChildren<Renderer>();
                var ar=target.GetComponent<AR.ARCombatContext>();float units=ar!=null?ar.scale:1;
                bar.height = r != null ? r.bounds.max.y - target.transform.position.y + 0.45f*units : 2.4f*units;
            }
            bar.until = Mathf.Max(bar.until, Time.time + seconds);
            bar.root.gameObject.SetActive(true);
        }

        Bar Take()
        {
            foreach (var b in bars) if (!b.root.gameObject.activeSelf && b.target == null) return b;
            var root = new GameObject("Enemy Health Bar").transform; root.SetParent(transform, false);
            var frame=new GameObject("Comic affix border").AddComponent<SpriteRenderer>();frame.transform.SetParent(root,false);frame.sprite=pixel;frame.color=UI.ComicTheme.Ink;frame.transform.localPosition=new Vector3(-Width/2-.035f,0,.01f);frame.transform.localScale=new Vector3(Width+.07f,Height+.07f,1);frame.sortingOrder=-1;
            var back = new GameObject("Back").AddComponent<SpriteRenderer>(); back.transform.SetParent(root, false);
            back.sprite = pixel; back.color = new Color(0, 0, 0, .65f); back.transform.localPosition = new Vector3(-Width / 2, 0, 0);
            back.transform.localScale = new Vector3(Width, Height, 1);
            var fill = new GameObject("Fill").transform; fill.SetParent(root, false); fill.localPosition = new Vector3(-Width / 2, 0, -0.001f);
            var front = fill.gameObject.AddComponent<SpriteRenderer>(); front.sprite = pixel; front.sortingOrder = 1;
            var label = new GameObject("Name").AddComponent<TextMeshPro>(); label.transform.SetParent(root, false);
            if (UI.ComicTheme.Font != null) label.font = UI.ComicTheme.Font;else if (font != null) label.font = font;
            label.fontSize = 2.2f; label.alignment = TextAlignmentOptions.Center; label.textWrappingMode = TextWrappingModes.NoWrap;
            label.transform.localPosition = new Vector3(0, 0.59f, -.02f);label.rectTransform.sizeDelta=new Vector2(4.4f,.98f);label.textWrappingMode=TextWrappingModes.Normal;
            var nameBorder=new GameObject("Comic name border").AddComponent<SpriteRenderer>();nameBorder.transform.SetParent(root,false);nameBorder.sprite=pixel;nameBorder.color=UI.ComicTheme.Ink;nameBorder.transform.localPosition=new Vector3(-2.28f,.59f,.035f);nameBorder.transform.localScale=new Vector3(4.56f,1.05f,1);
            var namePlate=new GameObject("Comic name plate").AddComponent<SpriteRenderer>();namePlate.transform.SetParent(root,false);namePlate.sprite=pixel;namePlate.color=UI.ComicTheme.Navy;namePlate.transform.localPosition=new Vector3(-2.23f,.59f,.025f);namePlate.transform.localScale=new Vector3(4.46f,.95f,1); label.outlineWidth = 0.2f; label.outlineColor = new Color32(0, 0, 0, 200);
            var icons=new SpriteRenderer[2];for(int i=0;i<2;i++){icons[i]=new GameObject("Affix icon "+i).AddComponent<SpriteRenderer>();icons[i].transform.SetParent(root,false);icons[i].transform.localPosition=new Vector3((i-.5f)*.48f,-.35f,-.01f);icons[i].transform.localScale=Vector3.one*.42f;icons[i].sortingOrder=3;}
            var bar = new Bar { root = root, fill = fill, back = back, front = front, frame=frame,namePlate=namePlate,nameBorder=nameBorder,icons=icons,label = label };
            bar.divisions=new SpriteRenderer[4];for(int i=0;i<4;i++){var tick=new GameObject("Health division "+i).AddComponent<SpriteRenderer>();tick.transform.SetParent(root,false);tick.sprite=pixel;tick.color=Color.black;tick.sortingOrder=3;tick.transform.localPosition=new Vector3(-Width*.5f+Width*(i+1)/5,0,-.015f);tick.transform.localScale=new Vector3(.018f,Height,1);bar.divisions[i]=tick;}
            root.gameObject.SetActive(false); bars.Add(bar); return bar;
        }

        void Release(Bar bar)
        {
            if (bar.target != null) byTarget.Remove(bar.target);
            bar.target = null; bar.until = 0; bar.root.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            foreach (var bar in bars)
            {
                if (!bar.root.gameObject.activeSelf) continue;
                var t = bar.target;
                if (t == null || t.Defeated || !t.isActiveAndEnabled || (Time.time > bar.until && t != locked)) { Release(bar); continue; }
                var ar=t.GetComponent<AR.ARCombatContext>();float units=ar!=null?ar.scale:1;
                float fraction = Mathf.Clamp01(t.Health / Mathf.Max(1, t.maxHealth));
                bar.fill.localScale = new Vector3(Width * fraction, Height, 1);
                bar.front.color = Color.Lerp(new Color(1f, .25f, .2f), ElementChart.ColorOf(t.Element == Element.None ? Element.Moc : t.Element), fraction);
                if(UI.Accessibility.ColorBlind)bar.front.color=new Color(.34f,.70f,.91f);
                var elite=t.GetComponent<Enemies.EliteAffix>();bool isElite=elite!=null&&elite.IsElite;
                var conceal=t.GetComponent<Enemies.EnemyConcealment>();bool hidden=conceal!=null&&conceal.Hidden;
                bar.front.enabled=bar.back.enabled=bar.frame.enabled=!hidden;bar.frame.color=isElite&&elite.Affixes.Count>0?elite.Affixes[0].color:UI.ComicTheme.Ink;
                foreach(var tick in bar.divisions)tick.enabled=!hidden&&UI.Accessibility.ColorBlind;
                for(int i=0;i<2;i++){bar.icons[i].enabled=!hidden&&isElite&&i<elite.Affixes.Count;if(bar.icons[i].enabled){bar.icons[i].sprite=elite.Affixes[i].icon;bar.icons[i].color=elite.Affixes[i].color;}}
                bool named = names.TryGetValue(t, out var n);
                // Spawn poses and elite scale change after the bar is first created.
                if((named||ar!=null)&&bar.renderers!=null){float top=t.transform.position.y;foreach(var r in bar.renderers)if(r!=null&&r.enabled&&!r.forceRenderingOff)top=Mathf.Max(top,r.bounds.max.y);bar.height=top-t.transform.position.y+.45f*units;}
                bool close=cam!=null&&Vector3.Distance(cam.transform.position,t.transform.position)<=18;
                bool full=named&&!hidden&&(!isElite||close||t==locked);
                bar.label.gameObject.SetActive(full);bar.namePlate.enabled=bar.nameBorder.enabled=full;
                if(full){string display=isElite?n.Replace(" · ","\n"):n;if(bar.label.text!=display)bar.label.text=display;}
                bar.label.color=UI.ComicTheme.Gold;bar.label.fontSize=isElite?3.1f:2.6f;
                bar.root.position = t.transform.position + Vector3.up * bar.height;
                if (cam != null)
                {
                    bar.root.rotation = cam.transform.rotation;
                    float distance = Vector3.Distance(cam.transform.position, bar.root.position);
                    bar.root.localScale = Vector3.one * (ar!=null?Mathf.Clamp(distance*.05f,.4f*units,.8f*units):Mathf.Clamp(distance * WorldSize, 0.6f, 3f)) * UI.Accessibility.TextScale;
                }
            }
        }
    }
}
