using System.Collections;
using UnityEngine;
namespace CampusRift.UI
{
    public sealed class MenuIntro : MonoBehaviour
    {
        public CanvasGroup[] Elements;
        public float Stagger=.065f, FadeDuration=.25f;
        void OnEnable(){foreach(var item in Elements)if(item!=null)item.alpha=0;StartCoroutine(Animate());}
        IEnumerator Animate()
        {
            float duration=FadeDuration+Stagger*(Elements.Length-1);
            for(float time=0;time<duration;time+=Time.unscaledDeltaTime)
            {
                for(int i=0;i<Elements.Length;i++)if(Elements[i]!=null)Elements[i].alpha=Mathf.SmoothStep(0,1,(time-i*Stagger)/FadeDuration);
                yield return null;
            }
            foreach(var item in Elements)if(item!=null)item.alpha=1;
        }
    }
}
