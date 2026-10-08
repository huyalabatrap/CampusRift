var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
var field=UnityEngine.Object.FindAnyObjectByType<CampusRift.AR.ARBattlefield>();var hud=field.GetComponent<CampusRift.AR.ARBattleHUD>();
((RectTransform)hud.GetType().GetField("content",flags).GetValue(hud)).gameObject.SetActive(true);
var tech=field.GetComponent<CampusRift.AR.ARTechHUD>();var dual=(RectTransform)tech.GetType().GetField("dual",flags).GetValue(tech);dual.anchoredPosition=new Vector2(-620,-235);dual.sizeDelta=new Vector2(450,160);
var texts=dual.GetComponentsInChildren<TMPro.TMP_Text>();texts[0].rectTransform.anchoredPosition=new Vector2(0,50);texts[0].rectTransform.sizeDelta=new Vector2(420,38);texts[0].fontSize=20;texts[0].margin=Vector4.zero;
var glyphs=dual.GetComponentsInChildren<CampusRift.AR.ARHandGraphic>();for(int i=0;i<glyphs.Length;i++){glyphs[i].rectTransform.anchoredPosition=new Vector2(-88+i*176,3);glyphs[i].rectTransform.sizeDelta=new Vector2(44,50);glyphs[i].color=CampusRift.UI.ComicTheme.Gold;}
texts[1].rectTransform.anchoredPosition=new Vector2(0,-46);texts[1].rectTransform.sizeDelta=new Vector2(420,46);texts[1].fontSize=15;
var combat=field.GetComponent<CampusRift.AR.ARCombatHUD>();var canvas=(RectTransform)combat.GetType().GetField("canvas",flags).GetValue(combat);canvas.GetComponentInParent<Canvas>(true).gameObject.SetActive(true);
foreach(var name in new[]{"warning","shieldBadge","ultimatePanel"})((RectTransform)combat.GetType().GetField(name,flags).GetValue(combat)).gameObject.SetActive(false);
((TMPro.TMP_Text)combat.GetType().GetField("sealText",flags).GetValue(combat)).text="LINH ẤN 100 / 100\nKẾT ẤN SẴN SÀNG";
((UnityEngine.UI.Image)combat.GetType().GetField("sealFill",flags).GetValue(combat)).fillAmount=1;
((TMPro.TMP_Text)combat.GetType().GetField("activeText",flags).GetValue(combat)).text="CHẾ ĐỘ GIÁ ĐỠ · TAY A / B";
Canvas.ForceUpdateCanvases();return "Final layout posed: new two-hand card below existing seals, no overlap";
