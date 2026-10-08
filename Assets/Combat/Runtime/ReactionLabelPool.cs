using TMPro;
using UnityEngine;
using CampusRift.UI;
namespace CampusRift.Combat
{
    // DamageNumberPool owns these comic reaction labels, rendered above its numeric world popups.
    public sealed class ReactionLabelPool : MonoBehaviour
    {
        public const int Capacity = 16;
        sealed class Entry { public RectTransform root; public TMP_Text text; public CanvasGroup group; public ReactionType type; public float born, latest; public int count; public Vector2 origin; public bool live; }
        readonly Entry[] entries = new Entry[Capacity];
        AR.ARCombatContext ar;float SessionNow=>ar!=null?ar.Now:Time.time;
        Canvas canvas;
        ReactionConfig config;
        public int Merged { get; private set; }
        public int ActiveCount { get { int count=0; foreach(var e in entries)if(e!=null&&e.live)count++;return count; } }
        public int CreatedCount => entries.Length;
        public float LowestBottom
        {
            get {float bottom=450;foreach(var e in entries)if(e.live)bottom=Mathf.Min(bottom,e.origin.y-62);return bottom;}
        }
        void Awake()
        {
            config=ReactionConfig.Current;canvas=ComboUIFactory.Canvas("Reaction comic labels",transform,35);
            for(int i=0;i<entries.Length;i++)
            {
                var r=ComboUIFactory.Rect("Pooled reaction burst "+i,canvas.transform,new Vector2(410,98),Vector2.zero);
                ComboUIFactory.Graphic("Ink burst",r,r.sizeDelta,Vector2.zero,ComboGraphic.Shape.Burst,ComicTheme.Paper);
                var text=ComboUIFactory.Text("Reaction name",r,new Vector2(340,65),Vector2.zero,30);
                var group=r.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts=false;
                entries[i]=new Entry{root=r,text=text,group=group};r.gameObject.SetActive(false);
            }
        }
        public void Show(ReactionEvent ev)
        {
            if(config==null||Camera.main==null)return;
            var context=ev.attacker!=null?ev.attacker.GetComponent<AR.ARCombatContext>():null;if(context!=ar){foreach(var old in entries){old.live=false;old.root.gameObject.SetActive(false);}ar=context;}
            foreach(var e in entries)if(e.live&&e.type==ev.type&&SessionNow-e.latest<=.2f)
            {e.count++;e.latest=SessionNow;SetLabel(e);Merged++;return;}
            Entry slot=null;foreach(var e in entries)if(!e.live){slot=e;break;}
            if(slot==null){slot=entries[0];foreach(var e in entries)if(e.born<slot.born)slot=e;}
            var screen=Camera.main.WorldToScreenPoint(ev.point+Vector3.up*(3.4f*(ar!=null?ar.scale:1)));
            if(screen.z<=0)return;
            // Six reserved slots clear the HP, hint card, and mobile controls. Spacing includes
            // the 1.25 punch and the full rise, rather than only the unscaled text rectangle.
            Vector2 p=Vector2.zero;bool placed=false;
            for(int position=0;position<6;position++)
            {
                float x=position%3==0?0:position%3==1?-550:550;
                p=new Vector2(x,220-position/3*170);bool occupied=false;
                foreach(var e in entries)if(e!=slot&&e.live&&e.origin==p){occupied=true;break;}
                if(!occupied){placed=true;break;}
            }
            if(!placed)
            {
                slot=entries[0];foreach(var e in entries)if(e.live&&e.born<slot.born)slot=e;
                p=slot.origin;
            }
            slot.type=ev.type;slot.born=slot.latest=SessionNow;slot.count=1;slot.origin=p;slot.live=true;slot.group.alpha=1;slot.root.gameObject.SetActive(true);slot.root.localScale=Vector3.zero;
            SetLabel(slot);
        }
        void SetLabel(Entry e)
        {
            var rule=config.Rule(e.type);e.text.text=(LevelHUD.Vietnamese?rule.nameVI:rule.nameEN).ToUpperInvariant()+"!"+(e.count>1?" ×"+e.count:"");
            var first=ElementChart.ColorOf(rule.first);var second=ElementChart.ColorOf(rule.second);
            e.text.enableVertexGradient=true;e.text.color=Color.white;e.text.colorGradient=new VertexGradient(first,second,first,second);
        }
        void LateUpdate()
        {
            bool gameplay=ar!=null||UIStateManager.Instance!=null&&(UIStateManager.Instance.State==UIState.Gameplay||UIStateManager.Instance.State==UIState.Modal);
            canvas.enabled=gameplay;
            foreach(var e in entries)
            {
                if(!e.live)continue;float t=SessionNow-e.born;
                if(t>=1.05f){e.live=false;e.root.gameObject.SetActive(false);continue;}
                float scale=t<.08f?Mathf.Lerp(0,1.25f,t/.08f):t<.15f?Mathf.Lerp(1.25f,1,(t-.08f)/.07f):1;
                e.root.localScale=Vector3.one*scale;e.root.anchoredPosition=e.origin+Vector2.up*(Mathf.Max(0,t-.15f)*42);
                e.group.alpha=t<.75f?1:1-(t-.75f)/.3f;
            }
        }
        public void Clear(){foreach(var e in entries)if(e!=null){e.live=false;e.root.gameObject.SetActive(false);}}
    }
}
