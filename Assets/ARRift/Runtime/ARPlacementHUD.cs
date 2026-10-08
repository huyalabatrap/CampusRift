using UnityEngine;
using TMPro;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARPlacementHUD : MonoBehaviour
    {
        RiftPlacementService placement;RectTransform canvas;TMP_Text message;float anchoredAt=-1;bool wasAnchored;
        void Start(){placement=GetComponent<RiftPlacementService>();canvas=ARUI.Canvas(transform,"AR placement guidance");var panel=ARUI.Panel(canvas,"Placement",0,-370,1100,150);message=ARUI.Text(panel,"",0,0,1060,130,32);bool vn=LevelHUD.Vietnamese;ARUI.Button(canvas,vn?"BÀN":"TABLE",-130,-480,230,()=>placement.SetFloor(false));ARUI.Button(canvas,vn?"SÀN":"FLOOR",130,-480,230,()=>placement.SetFloor(true));}
        void Update(){if(placement==null)return;placement.English=!LevelHUD.Vietnamese;message.text=placement.Message;bool anchored=placement.Root!=null;if(anchored&&!wasAnchored)anchoredAt=Time.unscaledTime;wasAnchored=anchored;canvas.gameObject.SetActive(!anchored||Time.unscaledTime-anchoredAt<2);foreach(var b in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))b.gameObject.SetActive(!anchored);}
        void OnDestroy(){if(canvas!=null)Destroy(canvas.gameObject);}
    }
}
