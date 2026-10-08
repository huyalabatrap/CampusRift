using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Enemies;
using CampusRift.Levels;

namespace CampusRift.UI
{
    // "Tầm Yêu" (plan §2.2): when three or fewer monsters remain, every 20 seconds their positions are shown through
    // walls for 3 seconds, as a marker on the monster or an edge marker pointing towards it.
    public sealed class EnemyRevealMarker : MonoBehaviour
    {
        public const int Threshold = 3;
        public const float Interval = 20f, Duration = 3f;
        public bool Showing => Time.time < showUntil;
        public int ShownCount { get; private set; }
        public int RevealCount { get; private set; }
        public float NextReveal => nextReveal;
        readonly List<Marker> markers = new List<Marker>();
        RectTransform root; float showUntil, nextReveal = -1;
        float skillRevealUntil;
        public void RevealAll(float seconds){skillRevealUntil=Time.time+seconds;}
        public void EndSkillReveal(){skillRevealUntil=0;Hide();}
        struct Marker { public RectTransform rect; public RectTransform arrow; public TMP_Text label; }

        public static EnemyRevealMarker Create(Transform parent)
        {
            var go = new GameObject("Level Reveal", typeof(RectTransform)); go.layer = 5;
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var marker = go.AddComponent<EnemyRevealMarker>(); marker.root = rect;for(int i=0;i<64;i++)marker.Get(i);marker.Hide();return marker;
        }

        void Update()
        {
            if(CampusRift.Levels.EndgameHazard.Silenced){Hide();nextReveal=-1;return;}
            if(Time.time<skillRevealUntil || (CampusRift.Levels.LevelDirector.Instance?.PlayerTransform?.GetComponent<CampusRift.Progression.BuffSystem>()?.RevealEnemies??false)){PlaceAll();return;}
            var director = LevelDirector.Instance;
            bool hunting = director != null && director.State == LevelDirector.Phase.Wave && director.Remaining <= Threshold && director.Remaining > 0 && director.AliveCount > 0
                && director.AliveCount == director.Remaining;
            if (!hunting) { nextReveal = -1; Hide(); return; }
            if (nextReveal < 0) nextReveal = Time.time + Interval;
            if (Time.time >= nextReveal) { nextReveal = Time.time + Interval; showUntil = Time.time + Duration; RevealCount++; }
            if (!Showing) { Hide(); return; }
            Place(director);
        }

        void Hide()
        {
            ShownCount = 0;
            for (int i = 0; i < markers.Count; i++) if (markers[i].rect.gameObject.activeSelf) markers[i].rect.gameObject.SetActive(false);
        }
        void PlaceAll()
        {
            var camera=Camera.main;if(camera==null)return;int used=0;
            foreach(var enemy in CampusRift.Monsters.MonsterVitality.Active)
            {
                if(enemy==null||enemy.Defeated||used>=64)continue;var marker=Get(used++);var screen=camera.WorldToScreenPoint(enemy.transform.position+Vector3.up*1.8f);
                bool behind=screen.z<0;if(behind){screen.x=Screen.width-screen.x;screen.y=Screen.height-screen.y;}
                Vector2 center=new Vector2(Screen.width,Screen.height)*.5f,offset=new Vector2(screen.x,screen.y)-center;
                bool off=behind||Mathf.Abs(offset.x)>center.x-60||Mathf.Abs(offset.y)>center.y-160;
                if(off&&offset.sqrMagnitude>.01f)offset*=Mathf.Min((center.x-60)/Mathf.Max(.01f,Mathf.Abs(offset.x)),(center.y-160)/Mathf.Max(.01f,Mathf.Abs(offset.y)));
                Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(root,center+offset,null,out local);marker.rect.anchoredPosition=local;
                marker.arrow.gameObject.SetActive(off);if(off)marker.arrow.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(offset.y,offset.x)*Mathf.Rad2Deg);
                marker.label.SetText("{0}",enemy.Health);marker.rect.gameObject.SetActive(true);
            }
            for(int i=used;i<markers.Count;i++)markers[i].rect.gameObject.SetActive(false);ShownCount=used;
        }

        void Place(LevelDirector director)
        {
            var camera = Camera.main; var player = director.PlayerTransform;
            if (camera == null) return;
            int used = 0;
            foreach (var enemy in director.Alive)
            {
                if (enemy == null || !enemy.Alive) continue;
                var marker = Get(used++);
                Vector3 world = enemy.transform.position + Vector3.up * 1.2f;
                Vector3 screen = camera.WorldToScreenPoint(world);
                bool behind = screen.z < 0;
                if (behind) { screen.x = Screen.width - screen.x; screen.y = Screen.height - screen.y; }
                Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
                Vector2 offset = new Vector2(screen.x, screen.y) - center;
                float marginX = Screen.width * 0.5f - 60, marginY = Screen.height * 0.5f - 60;
                bool off = behind || Mathf.Abs(offset.x) > marginX || Mathf.Abs(offset.y) > marginY;
                if (off && offset.sqrMagnitude > 0.01f)
                {
                    float k = Mathf.Min(marginX / Mathf.Max(0.01f, Mathf.Abs(offset.x)), marginY / Mathf.Max(0.01f, Mathf.Abs(offset.y)));
                    offset *= k;
                }
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, center + offset, null, out var local);
                marker.rect.anchoredPosition = local;
                marker.arrow.gameObject.SetActive(off);
                if (off) marker.arrow.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
                if (player != null) marker.label.text = Mathf.RoundToInt(Vector3.Distance(player.position, enemy.transform.position)) + " m";
                marker.rect.gameObject.SetActive(true);
            }
            for (int i = used; i < markers.Count; i++) markers[i].rect.gameObject.SetActive(false);
            ShownCount = used;
        }

        Marker Get(int index)
        {
            while (markers.Count <= index)
            {
                var holder = NewRect("Marker", root, 0, 0);
                var diamond = NewRect("Diamond", holder, 30, 30); diamond.localRotation = Quaternion.Euler(0, 0, 45);
                var image = diamond.gameObject.AddComponent<Image>(); image.color = new Color(0.86f, 0.55f, 1f, 0.95f); image.raycastTarget = false;
                var core = NewRect("Core", holder, 12, 12); core.localRotation = Quaternion.Euler(0, 0, 45);
                var coreImage = core.gameObject.AddComponent<Image>(); coreImage.color = Color.white; coreImage.raycastTarget = false;
                var arrow = NewRect("Arrow", holder, 0, 0);
                var tail = NewRect("Tail", arrow, 34, 8); tail.anchoredPosition = new Vector2(34, 0);
                var tailImage = tail.gameObject.AddComponent<Image>(); tailImage.color = new Color(0.86f, 0.55f, 1f, 0.95f); tailImage.raycastTarget = false;
                var labelRect = NewRect("Distance", holder, 90, 26); labelRect.anchoredPosition = new Vector2(0, -32);
                var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
                label.fontSize = 20; label.alignment = TextAlignmentOptions.Center; label.color = Color.white; label.raycastTarget = false;
                ComicTheme.Text(label);
                markers.Add(new Marker { rect = holder, arrow = arrow, label = label });
            }
            return markers[index];
        }

        static RectTransform NewRect(string name, Transform parent, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var r = go.GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.sizeDelta = new Vector2(w, h); r.anchoredPosition = Vector2.zero; return r;
        }
    }
}
