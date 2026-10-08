from pathlib import Path
def edit(p,a,b):
 p=Path(p);s=p.read_text(encoding='utf-8');assert a in s,(p,a);p.write_text(s.replace(a,b),encoding='utf-8')
edit('Assets/CampusRiftUI/Runtime/SwordIntentUI.cs',
 'meter.text=(vi?"KIẾM Ý":"SWORD INTENT")+" · "+Mathf.FloorToInt(value*100)+"%  · "+(intent!=null?intent.DefeatedWeight:0)+"/"+(intent!=null?intent.TotalWeight:0);',
 'meter.text=intent==null||intent.TotalWeight==0?(vi?"KIẾM Ý · CHỜ ĐỢT QUÁI":"SWORD INTENT · WAITING FOR WAVE"):(vi?"KIẾM Ý":"SWORD INTENT")+" · "+Mathf.FloorToInt(value*100)+"%  · "+intent.DefeatedWeight+"/"+intent.TotalWeight;')
edit('Assets/CampusRiftUI/Runtime/RestLoadoutUI.cs',
 'kit.Text(card,def!=null?def.LocalizedName(LevelHUD.Vietnamese):L("TRỐNG","EMPTY"),x,294,268,46,21,null,TextAlignmentOptions.Center,false);',
 'var caption=kit.Text(card,def!=null?def.LocalizedName(LevelHUD.Vietnamese):L("TRỐNG","EMPTY"),x,280,268,68,21,null,TextAlignmentOptions.Center,true);caption.enableAutoSizing=false;caption.fontSize=21;')
edit('Assets/CampusRiftUI/Runtime/RestLoadoutUI.cs','float height=mobile?254:304;','float height=mobile?232:304;')
edit('Assets/CampusRiftUI/Runtime/RestLoadoutUI.cs',
 'for(int i=0;i<skills.Length;i++)\n            {',
 '''if(mobile&&list.rect.height>height)
            {
                var scroll=list.GetComponentInParent<ScrollRect>();
                var cue=kit.Button(card,L("VUỐT ĐỂ XEM THÊM ↓","SWIPE FOR MORE ↓"),390,590,470,38,()=>{scroll.verticalNormalizedPosition=Mathf.Max(0,scroll.verticalNormalizedPosition-height/Mathf.Max(height,list.rect.height-height));},true,false,18);
                // Button's standard68px minimum would intrude into the footer; this compact cue
                // supplements the large swipe target and remains an actual clickable page-down.
                ((RectTransform)cue.transform).sizeDelta=new Vector2(470,38);
                var cueText=cue.GetComponentInChildren<TMP_Text>();cueText.rectTransform.sizeDelta=new Vector2(434,38);cueText.enableAutoSizing=false;cueText.fontSize=18;
                var rail=kit.Image(card,"Scroll rail",1203,top,7,height,ComicTheme.Ink);
                var thumb=kit.Image(rail.transform,"Scroll thumb",0,0,7,height*height/list.rect.height,ComicTheme.Gold);
                scroll.onValueChanged.AddListener(value=>{thumb.rectTransform.anchoredPosition=new Vector2(0,-(height-thumb.rectTransform.sizeDelta.y)*(1-value.y));cueText.text=value.y<.02f?L("VUỐT LÊN ĐỂ XEM LẠI ↑","SWIPE UP TO RETURN ↑"):L("VUỐT ĐỂ XEM THÊM ↓","SWIPE FOR MORE ↓");});
            }
            for(int i=0;i<skills.Length;i++)
            {''')
edit('Assets/CampusRiftUI/Runtime/RestLoadoutUI.cs',
 'kit.Text(list,def.LocalizedName(LevelHUD.Vietnamese),x+116,y+10,430,50,22,chosen?ComicTheme.Gold:(Color?)null,TextAlignmentOptions.MidlineLeft,false);',
 'var name=kit.Text(list,def.LocalizedName(LevelHUD.Vietnamese),x+116,y+4,430,58,22,chosen?ComicTheme.Gold:(Color?)null,TextAlignmentOptions.MidlineLeft,true);name.enableAutoSizing=false;name.fontSize=22;')
edit('Assets/SkyBeast/Runtime/HeavenSwordCinematic.cs','glow.intensity.Override(.6f);glow.threshold.Override(1.3f);glow.scatter.Override(.72f);','glow.intensity.Override(.32f);glow.threshold.Override(1.8f);glow.scatter.Override(.55f);')
edit('Assets/SkyBeast/Runtime/HeavenSwordCinematic.cs','center+new Vector3(75,34,-95),focus+new Vector3(70,100,-245)','center+new Vector3(85,42,-195),center+new Vector3(70,48,-270)')
edit('Assets/SkyBeast/Runtime/HeavenSwordCinematic.cs','Vector3.Lerp(center+Vector3.up*25,focus,.46f),focus+Vector3.up*95','Vector3.Lerp(center+Vector3.up*22,focus,.62f),focus+Vector3.up*45')
edit('Assets/SkyBeast/Runtime/HeavenSwordCinematic.cs','focus+new Vector3(37,12,-68),close);look=Vector3.Lerp(look,focus+Vector3.up*5,close);','center+new Vector3(85,40,-195),close);look=Vector3.Lerp(look,Vector3.Lerp(center+Vector3.up*22,focus,.62f),close);')
edit('Assets/SkyBeast/Runtime/HeavenSwordCinematic.cs','falling+new Vector3(47,15,-73),follow);look=Vector3.Lerp(look,falling,follow);','center+new Vector3(85,32,-195),follow);look=Vector3.Lerp(look,Vector3.Lerp(center+Vector3.up*18,falling,.65f),follow);')
edit('Assets/SkyBeast/Runtime/HeavenSwordCinematic.cs','flash.color=new Color(1,.93f,.68f,pulse?(reduced?.035f:.22f):0);lines.color=new Color(1,.9f,.55f,pulse?(reduced?.15f:.85f):0);','flash.color=new Color(1,.74f,.05f,pulse?(reduced?.02f:.08f):0);lines.color=new Color(1,.72f,.09f,pulse?(reduced?.10f:.48f):0);')
edit('Assets/SkyBeast/Runtime/HeavenSwordImpactVisual.cs','new Color(4.8f,3.1f,.48f,ring<3?.85f:.55f)','new Color(3.2f,1.45f,.08f,ring<3?.75f:.45f)')
edit('Assets/SkyBeast/Runtime/HeavenSwordImpactVisual.cs','new Color(6,4.5f,1.1f,.9f)','new Color(3.8f,1.65f,.12f,.8f)')
edit('Assets/Combat/Runtime/EnemyHealthBars.cs','public SpriteRenderer back, front, frame;','public SpriteRenderer back, front, frame, namePlate, nameBorder;')
edit('Assets/Combat/Runtime/EnemyHealthBars.cs','label.transform.localPosition = new Vector3(0, 0.22f, 0);',
 'label.transform.localPosition = new Vector3(0, 0.49f, -.02f);label.rectTransform.sizeDelta=new Vector2(3.3f,.82f);label.textWrappingMode=TextWrappingModes.Normal;\n            var nameBorder=new GameObject("Comic name border").AddComponent<SpriteRenderer>();nameBorder.transform.SetParent(root,false);nameBorder.sprite=pixel;nameBorder.color=UI.ComicTheme.Ink;nameBorder.transform.localPosition=new Vector3(-1.78f,.49f,.035f);nameBorder.transform.localScale=new Vector3(3.56f,.89f,1);\n            var namePlate=new GameObject("Comic name plate").AddComponent<SpriteRenderer>();namePlate.transform.SetParent(root,false);namePlate.sprite=pixel;namePlate.color=UI.ComicTheme.Navy;namePlate.transform.localPosition=new Vector3(-1.73f,.49f,.025f);namePlate.transform.localScale=new Vector3(3.46f,.79f,1);')
edit('Assets/Combat/Runtime/EnemyHealthBars.cs','(i-.5f)*.32f,-.32f','(i-.5f)*.48f,-.35f')
edit('Assets/Combat/Runtime/EnemyHealthBars.cs','Vector3.one*.28f','Vector3.one*.42f')
edit('Assets/Combat/Runtime/EnemyHealthBars.cs','frame=frame,icons=icons,label = label','frame=frame,namePlate=namePlate,nameBorder=nameBorder,icons=icons,label = label')
edit('Assets/Combat/Runtime/EnemyHealthBars.cs',
 'bar.label.gameObject.SetActive(named&&!hidden); if (named && bar.label.text != n) bar.label.text = n;bar.label.color=UI.ComicTheme.Gold;bar.label.fontSize=isElite?1.7f:2.2f;',
 '''bool close=cam!=null&&Vector3.Distance(cam.transform.position,t.transform.position)<=18;
                bool full=named&&!hidden&&(!isElite||close||t==locked);
                bar.label.gameObject.SetActive(full);bar.namePlate.enabled=bar.nameBorder.enabled=full;
                if(full){string display=isElite?n.Replace(" · ","\\n"):n;if(bar.label.text!=display)bar.label.text=display;}
                bar.label.color=UI.ComicTheme.Gold;bar.label.fontSize=isElite?3.1f:2.6f;''')
edit('Assets/Enemies/Runtime/EliteAffix.cs',
 'if(Has(EliteAffixKind.Invisible)&&GetComponent<EnemyConcealment>()==null)',
 'if(Has(EliteAffixKind.FireHeart)){var flames=GetComponent<EliteFireHeartVisual>()??gameObject.AddComponent<EliteFireHeartVisual>();flames.Configure();}\n            if(Has(EliteAffixKind.Invisible)&&GetComponent<EnemyConcealment>()==null)')
edit('Assets/Enemies/Runtime/EliteAffix.cs','block.SetColor("_OutlineColor",Affixes.Count>0?Affixes[0].color*2:Color.yellow);',
 'block.SetColor("_OutlineColor",Has(EliteAffixKind.FireHeart)?new Color(.85f,.29f,.035f,1):Affixes.Count>0?Affixes[0].color*2:Color.yellow);block.SetFloat("_Width",Has(EliteAffixKind.FireHeart)?.012f:.025f);')
edit('Assets/Enemies/Runtime/EliteAffix.cs','transform.forward,.8f,.8f,color:Affixes.Count>0?Affixes[0].color*1.7f:Color.yellow',
 'transform.forward,Has(EliteAffixKind.FireHeart)?GetComponent<EliteFireHeartVisual>().FootRadius:.8f,.8f,color:Has(EliteAffixKind.FireHeart)?new Color(1,.35f,.035f,.45f):Affixes.Count>0?Affixes[0].color*1.7f:Color.yellow')
