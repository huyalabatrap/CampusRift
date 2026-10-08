using System.Collections;
using CampusRift.Monsters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CampusRift.UI
{
    public sealed class PlayerHealthUI : MonoBehaviour
    {
        public PlayerMonsterHealth Source;
        public Image Fill, DelayedFill;
        public TMP_Text Value;
        public float DamageDelay=.3f;
        Coroutine trail;
        bool recolored;Color fillOriginal,trailOriginal;
        void Update(){if(Fill==null)return;if(Accessibility.ColorBlind){if(!recolored){fillOriginal=Fill.color;trailOriginal=DelayedFill!=null?DelayedFill.color:Color.white;recolored=true;}Fill.color=new Color(.34f,.70f,.91f);if(DelayedFill!=null)DelayedFill.color=ComicTheme.Gold;}else if(recolored){Fill.color=fillOriginal;if(DelayedFill!=null)DelayedFill.color=trailOriginal;recolored=false;}}
        void OnEnable() { if(Source!=null) {Source.HealthChanged.AddListener(SetHealth);SetHealth(Source.CurrentHealth,Source.maxHealth,true);} }
        void OnDisable() { if(Source!=null) Source.HealthChanged.RemoveListener(SetHealth);if(trail!=null)StopCoroutine(trail);trail=null; }
        public void Bind(PlayerMonsterHealth source)
        {
            if(Source!=null)Source.HealthChanged.RemoveListener(SetHealth);Source=source;
            if(isActiveAndEnabled && Source!=null){Source.HealthChanged.AddListener(SetHealth);SetHealth(Source.CurrentHealth,Source.maxHealth,true);}
        }
        public void SetHealth(float current,float max) { SetHealth(current,max,false); }
        void SetHealth(float current,float max,bool instant)
        {
            float fraction=max>0?Mathf.Clamp01(current/max):0;
            Fill.fillAmount=fraction;Value.text=$"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            if(trail!=null)StopCoroutine(trail);
            if(instant || fraction>=DelayedFill.fillAmount)DelayedFill.fillAmount=fraction;
            else trail=StartCoroutine(Delay(fraction));
        }
        IEnumerator Delay(float target)
        {
            yield return new WaitForSeconds(DamageDelay);
            while(DelayedFill.fillAmount>target) {DelayedFill.fillAmount=Mathf.MoveTowards(DelayedFill.fillAmount,target,Time.deltaTime*.65f);yield return null;}
            trail=null;
        }
    }
}
