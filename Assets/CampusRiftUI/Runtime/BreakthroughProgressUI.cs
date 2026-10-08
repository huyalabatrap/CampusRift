using TMPro;
using UnityEngine;
namespace CampusRift.UI
{
    public sealed class BreakthroughProgressUI : MonoBehaviour
    {
        public CampusRiftUITheme Theme;
        public RiftGraphic MarkerTemplate;
        public Transform MarkerRoot;
        public TMP_Text Count, Available;
        public int Current {get;private set;}
        public int Required {get;private set;}=5;
        public void SetProgress(int current,int required)
        {
            Required=Mathf.Max(1,required);Current=Mathf.Clamp(current,0,Required);
            while(MarkerRoot.childCount<Required) Instantiate(MarkerTemplate,MarkerRoot).gameObject.SetActive(true);
            for(int i=0;i<MarkerRoot.childCount;i++)
            {
                var marker=MarkerRoot.GetChild(i).GetComponent<RiftGraphic>();marker.gameObject.SetActive(i<Required);
                marker.color=i<Current?Theme.SecondaryAccent:Theme.DisabledColor;
                var center=marker.transform.Find("Hollow");if(center!=null)center.gameObject.SetActive(i>=Current);
            }
            Count.text=$"{Current} / {Required}";Available.gameObject.SetActive(Current==Required);
        }
        // V2: the five markers are the tiers of the current realm; all five lit means the bottleneck (exam available).
        Progression.CultivationService cultivation;
        void Start()
        {
            cultivation=Progression.ProfileService.CultivationOrNull;
            if(cultivation!=null)cultivation.Changed+=Refresh;
            Refresh();
        }
        void Refresh()
        {
            if(cultivation==null){SetProgress(Current,Required);return;}
            int tiers=Progression.CultivationTable.TiersPerRealm;
            SetProgress(cultivation.IsBottleneck?tiers:cultivation.Tier-1,tiers);
        }
        void OnDestroy(){if(cultivation!=null)cultivation.Changed-=Refresh;}
    }
}
