using UnityEngine;

namespace CampusRift.AR
{
    // One occupied HUD rectangle; nested graphics are counted once.
    public sealed class ARHUDRegion : MonoBehaviour
    {
        public int Priority;
        public bool Shown { get; private set; } = true;
        [SerializeField] CanvasGroup group;
        public static void Add(RectTransform rect, int priority=100)
        {
            var region=rect.gameObject.AddComponent<ARHUDRegion>();region.Priority=priority;
            region.group=rect.GetComponent<CanvasGroup>();if(region.group==null)region.group=rect.gameObject.AddComponent<CanvasGroup>();
        }
        public void Show(bool value){if(group==null){group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();}Shown=value;group.alpha=value?1:0;group.blocksRaycasts=value;}
        public float Area()
        {
            var corners=new Vector3[4];((RectTransform)transform).GetWorldCorners(corners);
            return Mathf.Abs((corners[2].x-corners[0].x)*(corners[2].y-corners[0].y));
        }
    }
    [DefaultExecutionOrder(31000)]
    public sealed class ARHUDBudget : MonoBehaviour
    {
        ARHUDRegion[] regions;float nextScan;
        public float OccupiedPercent {get;private set;}
        void LateUpdate()
        {
            if(regions==null||Time.unscaledTime>=nextScan){regions=GetComponentsInChildren<ARHUDRegion>(true);System.Array.Sort(regions,(a,b)=>b.Priority.CompareTo(a.Priority));nextScan=Time.unscaledTime+1;}
            var field=GetComponent<ARBattlefield>();var hud=GetComponent<ARBattleHUD>();bool fighting=field.Root!=null&&!field.placement.Adjusting&&!hud.ModalOpen;
            float used=0,limit=Screen.width*(float)Screen.height*.12f;
            foreach(var region in regions)
            {
                if(region==null)continue;
                if(!fighting||!region.gameObject.activeInHierarchy){region.Show(true);continue;}
                float area=region.Area();bool fits=used+area<=limit;region.Show(fits);if(fits)used+=area;
            }
            OccupiedPercent=Screen.width>0&&Screen.height>0?100*used/(Screen.width*(float)Screen.height):0;
        }
    }
}
