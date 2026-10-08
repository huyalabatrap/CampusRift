using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CampusRift.Enemies;
namespace CampusRift.UI
{
    public sealed class BossHealthBarUI : MonoBehaviour
    {
        BossController boss;Image fill;TMP_Text label;RectTransform panel;
        public static void Attach(BossController controller){var ui=FindAnyObjectByType<BossHealthBarUI>();if(ui==null)ui=new GameObject("Boss Health UI").AddComponent<BossHealthBarUI>();ui.boss=controller;}
        void Awake(){var c=ComboUIFactory.Canvas("Boss Comic HUD",transform,65);panel=ComboUIFactory.Rect("Boss panel",c.transform,new Vector2(780,84),Vector2.zero);panel.anchorMin=panel.anchorMax=new Vector2(.5f,1);panel.anchoredPosition=new Vector2(0,-206);
            ComicTheme.Frame(panel.gameObject);
            var track=ComboUIFactory.Rect("Blood track",panel,new Vector2(724,28),new Vector2(0,-18));var ink=track.gameObject.AddComponent<Image>();ink.sprite=ComicTheme.Sprite("round-mask");ink.type=Image.Type.Sliced;ink.color=ComicTheme.Ink;ink.raycastTarget=false;
            var r=ComboUIFactory.Rect("Blood bar",track,new Vector2(708,16),Vector2.zero);fill=r.gameObject.AddComponent<Image>();fill.sprite=ComicTheme.Sprite("round-mask");fill.type=Image.Type.Sliced;fill.color=new Color(1,.13f,.22f);fill.raycastTarget=false;fill.rectTransform.pivot=new Vector2(0,.5f);fill.rectTransform.anchorMin=fill.rectTransform.anchorMax=new Vector2(0,.5f);fill.rectTransform.anchoredPosition=new Vector2(8,0);
            label=ComboUIFactory.Text("Boss name",panel,new Vector2(724,32),new Vector2(0,16),24);ComicTheme.Text(label,true);label.color=ComicTheme.Gold;}
        void Update(){bool live=boss!=null&&boss.ActiveBoss&&boss.GetComponent<EnemyInstance>().Alive;panel.gameObject.SetActive(live);if(!live)return;
            var owner=boss.GetComponent<EnemyInstance>();fill.rectTransform.sizeDelta=new Vector2(708*Mathf.Clamp01(owner.Vitality.Health/owner.MaxHealth),16);
            label.text=owner.archetype.LocalizedName(LevelHUD.Vietnamese)+(boss.PhaseTwo?"  ·  II":"")+"   "+Mathf.CeilToInt(owner.Vitality.Health)+" / "+Mathf.CeilToInt(owner.MaxHealth);}
    }
}
