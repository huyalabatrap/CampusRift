using UnityEngine;
using TMPro;
using CampusRift.UI;
namespace CampusRift.AR
{
    public sealed class ARBattleStatus : MonoBehaviour
    {
        ARBattlefield field;ARMonsterDirector director;RectTransform canvas,result;TMP_Text status,title;UnityEngine.UI.Image hpbar;
        void Start(){field=GetComponent<ARBattlefield>();director=GetComponent<ARMonsterDirector>();canvas=ARUI.Canvas(transform,"AR wave status");status=ARUI.Text(ARUI.Panel(canvas,"Shrine health",0,455,740,110),"",0,0,700,100,30);hpbar=ARUI.Rect(status.transform.parent,"Shrine HP",0,-36,650,12).gameObject.AddComponent<UnityEngine.UI.Image>();hpbar.sprite=ComicTheme.Sprite("round-mask");hpbar.type=UnityEngine.UI.Image.Type.Filled;hpbar.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;hpbar.color=ComicTheme.Gold;hpbar.raycastTarget=false;result=ARUI.Panel(canvas,"AR result",0,50,850,400);title=ARUI.Text(result,"",0,100,790,160,40);bool vn=LevelHUD.Vietnamese;ARUI.Button(result,vn?"CHƠI LẠI":"REPLAY",-270,-105,245,()=>director.StartBattle());ARUI.Button(result,vn?"ĐỔI VỊ TRÍ":"REPOSITION",0,-105,245,()=>field.placement.Reposition());ARUI.Button(result,vn?"THOÁT":"EXIT",270,-105,245,()=>UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));}
        void Update(){if(field==null)return;bool vn=LevelHUD.Vietnamese;hpbar.fillAmount=field.Shrine!=null?field.Shrine.CurrentHealth/field.Shrine.maxHealth:0;status.text=field.Shrine==null?"AR RIFT":(vn?"LINH TRẬN ":"SHRINE ")+Mathf.CeilToInt(field.Shrine.CurrentHealth)+" / "+field.Shrine.maxHealth+"     "+(vn?"ĐỢT ":"WAVE ")+director.Wave+" / 3";if(field.Root!=null&&field.Paused)status.text=vn?"Di chuyển chậm lại / thêm ánh sáng":"Move slowly / improve lighting";result.gameObject.SetActive(director.Finished);title.text=director.Won?(vn?"CHIẾN THẮNG!":"VICTORY!"):(vn?"LINH TRẬN ĐÃ VỠ":"SHRINE DEFEATED");}
    }
}
