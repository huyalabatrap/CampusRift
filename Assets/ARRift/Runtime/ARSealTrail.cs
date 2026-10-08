using UnityEngine;
using UnityEngine.UI;
using CampusRift.Progression;
namespace CampusRift.AR
{
    // Only a short-lived on-screen trail; neither images nor landmark coordinates are persisted.
    public sealed class ARSealTrail : MonoBehaviour
    {
        ARBattlefield field;ARSkillCaster caster;RectTransform canvas;Image[] dots;Vector2 latest;float freshUntil;
        void Start()
        {
            field=GetComponent<ARBattlefield>();caster=GetComponent<ARSkillCaster>();canvas=ARUI.Canvas(transform,"AR unlocked hand seal");canvas.GetComponentInParent<Canvas>(true).sortingOrder=5;
            dots=new Image[10];for(int i=0;i<dots.Length;i++){dots[i]=ARUI.Rect(canvas,"Seal glow "+i,0,0,18-i,18-i).gameObject.AddComponent<Image>();dots[i].sprite=CampusRift.UI.ComicTheme.Sprite("round-mask");dots[i].raycastTarget=false;dots[i].gameObject.SetActive(false);}
            caster.source.Result+=Frame;caster.source.Invalidated+=Invalidate;
        }
        void Frame(GestureFrame f){if(!f.handPresent)return;latest=GestureCoordinates.Palm(f)*new Vector2(Screen.width,Screen.height);freshUntil=Time.unscaledTime+.25f;}
        void Invalidate(){freshUntil=0;}
        void LateUpdate()
        {
            if(dots==null)return;var progress=ARProgression.Read(ProfileService.Instance?.Data);string id=progress.selectedSeal;
            bool visible=field.Root!=null&&!field.Paused&&caster.source.SamplingActive&&!caster.source.Recovering&&Time.unscaledTime<freshUntil&&progress.cosmetics!=null&&progress.cosmetics.Contains(id);
            Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,latest,null,out local);
            for(int i=0;i<dots.Length;i++){dots[i].gameObject.SetActive(visible);if(!visible)continue;var target=i==0?local:dots[i-1].rectTransform.anchoredPosition;dots[i].rectTransform.anchoredPosition=Vector2.Lerp(dots[i].rectTransform.anchoredPosition,target,1-Mathf.Exp(-Time.unscaledDeltaTime*(i==0?35:15)));var color=id==ARProgression.AzureSeal?new Color(.15f,.75f,1):CampusRift.UI.ComicTheme.Gold;color.a=1-i/(float)dots.Length;dots[i].color=color;}
        }
        void OnDestroy(){if(caster!=null&&caster.source!=null){caster.source.Result-=Frame;caster.source.Invalidated-=Invalidate;}if(canvas!=null)Destroy(canvas.GetComponentInParent<Canvas>(true).gameObject);}
    }
}

