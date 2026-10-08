from pathlib import Path
def edit(path,old,new,count=1):
    p=Path(path);s=p.read_text(encoding='utf-8-sig');assert s.count(old)==count,(path,old[:60],s.count(old));p.write_text(s.replace(old,new),encoding='utf-8')

f='Assets/Skills/Core/Runtime/SkillVfxPool.cs'
edit(f,'SkillVfxKind kind=SkillVfxKind.Shard)','SkillVfxKind kind=SkillVfxKind.Shard,bool priority=false)')
edit(f,'if(n!=null)n.End=Random.onUnitSphere*Random.Range(1,3)+Vector3.up*1.2f;','if(n!=null){n.End=Random.onUnitSphere*Random.Range(1,3)+Vector3.up*1.2f;if(priority)Priority(n,true);}')
f='Assets/Skills/IceSeal/Runtime/IceSealRuntime.cs'
edit(f,'        protected override void OnCast()','''        public bool ShatterForReaction(MonsterVitality target)
        {for(int i=0;i<count;i++)if(frozen[i]==target&&shells[i]!=null){Shatter(i,true);return true;}return false;}
        protected override void OnCast()''')
edit(f,'void Shatter(int i)','void Shatter(int i,bool reaction=false)')
edit(f,'shell.Fade(.4f);vfx.Fragments(shell.Position+Vector3.up,Accent,SkillVfxPool.MobileQuality?3:6,.35f);','shell.Fade(reaction?.08f:.4f);if(reaction)shell.Opacity=.12f;vfx.Fragments(shell.Position+Vector3.up,Accent,SkillVfxPool.MobileQuality?3:6,reaction?.45f:.35f,SkillVfxKind.Shard,reaction);')
f='Assets/Combat/Runtime/ReactionFeedback.cs'
edit(f,'if(ev.type==ReactionType.ArmorShatter)TrackArmor(ev.target);','''if(ev.type==ReactionType.ArmorShatter)TrackArmor(ev.target);
            if(ev.type==ReactionType.IceLightning){pool.DimIce(ev.point,3.5f,.55f);GetComponent<IceSealRuntime>()?.ShatterForReaction(ev.target);}''')
edit(f,'var ice=GetComponent<IceSealRuntime>();if(ice==null||!ice.HasShell(ev.target))pool.Fragments(p,cyan,pieces,.4f);','var ice=GetComponent<IceSealRuntime>();if(ice==null||!ice.IsCasting)pool.Fragments(p,cyan,pieces,.45f,SkillVfxKind.Shard,true);')
edit(f,'var bolt=pool.Spawn(SkillVfxKind.ReactionBolt,p,gold,.24f,1);','var bolt=pool.Priority(pool.Spawn(SkillVfxKind.ReactionBolt,p,gold,.38f,1.1f));')
edit(f,'var bolt=pool.Spawn(SkillVfxKind.ChainBolt,p,gold,.35f,.6f);','var bolt=pool.Priority(pool.Spawn(SkillVfxKind.ReactionBolt,p,gold,.45f,.8f));')
edit(f,'var body=pool.Spawn(SkillVfxKind.Bolt,m.transform.position+Vector3.up*.45f,purple,.5f,.7f);','var body=pool.Priority(pool.Spawn(SkillVfxKind.Bolt,m.transform.position+Vector3.up*.45f,purple,.5f,.9f));')
edit(f,'pool.Spawn(SkillVfxKind.FireBloom,ev.point+Vector3.up*.4f,fire,.45f,1.5f);','pool.Priority(pool.Spawn(SkillVfxKind.FireBloom,ev.point+Vector3.up*.8f,fire,.6f,2));')
edit(f,'var ring=pool.Spawn(SkillVfxKind.Ring,ev.point+Vector3.up*(.15f+i*.55f),purple,.55f,3-i*.35f);','var ring=pool.Priority(pool.Spawn(SkillVfxKind.Ring,ev.point+Vector3.up*(.15f+i*.55f),purple,.55f,3-i*.35f));')
edit(f,'pool.Fragments(p,gold,pieces,.35f,SkillVfxKind.BellShard);','pool.Fragments(p,gold,pieces,.45f,SkillVfxKind.BellShard,true);')
edit(f,'pool.Fragments(p,new Color(.65f,.72f,.8f),pieces/2,.3f);','pool.Fragments(p,new Color(.65f,.72f,.8f),pieces/2,.4f,SkillVfxKind.Shard,true);')
# The same warmed flash node, promoted to the reaction render pass.
edit(f,'pool.Burst(p,cyan,1.45f,null,false);','ReactionFlash(p,cyan,1.65f);')
edit(f,'pool.Burst(p,gold,1.15f,null,false);','ReactionFlash(p,gold,1.3f);')
edit(f,'pool.Burst(p,fire,1.55f,null,false);','ReactionFlash(p,fire,1.8f);')
edit(f,'pool.Burst(p,new Color(1,.1f,.35f),1.6f,null,false);','ReactionFlash(p,new Color(1,.1f,.35f),1.8f);')
edit(f,'pool.Burst(p,gold,1.4f,null,false);','ReactionFlash(p,gold,1.65f);')
edit(f,'        void TrackTighten(','''        void ReactionFlash(Vector3 p,Color color,float size)
        {var node=pool.Priority(pool.Spawn(SkillVfxKind.Burst,p,color,.6f,size));if(node!=null)pool.Emit(node,SkillVfxPool.MobileQuality?10:28);}
        void TrackTighten(''')

f='Assets/CampusRiftUI/Runtime/ReactionHintUI.cs'
edit(f,'new Vector2(60,42),Vector2.zero,14','new Vector2(46,38),Vector2.zero,12',2)
edit(f,'firstName.fontStyle=secondName.fontStyle=FontStyles.Bold;','firstName.fontStyle=secondName.fontStyle=FontStyles.Bold;firstName.fontSizeMin=secondName.fontSizeMin=8;firstName.margin=secondName.margin=new Vector4(2,0,2,0);')
f='Assets/CampusRiftUI/Runtime/GenerationChainUI.cs'
edit(f,'RectTransform panel;','RectTransform panel,completionBurst;CanvasGroup completionGroup;')
edit(f,'complete=ComboUIFactory.Text("Generation completion",canvas.transform,new Vector2(470,70),new Vector2(555,-125),34);complete.gameObject.SetActive(false);','''completionBurst=ComboUIFactory.Rect("Generation comic burst",canvas.transform,new Vector2(470,105),new Vector2(555,-125));
            ComboUIFactory.Graphic("Generation ink burst",completionBurst,completionBurst.sizeDelta,Vector2.zero,ComboGraphic.Shape.Burst,ComicTheme.Paper);
            completionGroup=completionBurst.gameObject.AddComponent<CanvasGroup>();completionGroup.blocksRaycasts=false;
            complete=ComboUIFactory.Text("Generation completion",completionBurst,new Vector2(375,68),Vector2.zero,34);completionBurst.gameObject.SetActive(false);''')
edit(f,'complete.gameObject.SetActive(true);','completionBurst.gameObject.SetActive(true);')
edit(f,'complete.rectTransform.anchoredPosition=mobile?','completionBurst.anchoredPosition=mobile?')
edit(f,'complete.gameObject.SetActive(t<1.1f);complete.color=new Color(1,.92f,.4f,Mathf.Clamp01((1.1f-t)/.3f));','completionBurst.gameObject.SetActive(t<1.1f);completionGroup.alpha=Mathf.Clamp01((1.1f-t)/.3f);complete.color=ComicTheme.Purple;')
edit(f,'complete.transform.localScale=','completionBurst.localScale=')

f='Assets/Combat/Runtime/DamageNumberPool.cs'
edit(f,'Material comicMaterial;','Material comicMaterial;readonly Material[] reactionMaterials=new Material[5];')
edit(f,'for(int i=0;i<Capacity;i++)CreateEntry();','''if(comicMaterial!=null)for(int i=0;i<reactionMaterials.Length;i++)
            {
                var rule=ReactionConfig.Current.Rule((ReactionType)i);var outline=ElementChart.ColorOf(rule.first);
                reactionMaterials[i]=new Material(comicMaterial){name="Reaction damage outline "+rule.id};
                reactionMaterials[i].SetColor(ShaderUtilities.ID_OutlineColor,outline);reactionMaterials[i].SetFloat(ShaderUtilities.ID_OutlineWidth,.35f);
                reactionMaterials[i].EnableKeyword("UNDERLAY_ON");reactionMaterials[i].SetColor("_UnderlayColor",Color.black);reactionMaterials[i].SetFloat("_UnderlayDilate",.3f);reactionMaterials[i].SetFloat("_UnderlaySoftness",.04f);
            }
            for(int i=0;i<Capacity;i++)CreateEntry();''')
edit(f,'if(comicMaterial!=null)Destroy(comicMaterial);','if(comicMaterial!=null)Destroy(comicMaterial);foreach(var material in reactionMaterials)if(material!=null)Destroy(material);')
edit(f,'float reactionScale=info.skillId=="tu-sat"?1.25f:1;','int reaction=ReactionIndex(info);float reactionScale=reaction>=0?(info.skillId=="tu-sat"?1.55f:1.45f):1;')
edit(f,'MergedHits++;return;','if(reaction>=0)StyleReaction(existing,reaction);MergedHits++;ReflowBelowReactions();return;')
edit(f,'entry.scale=Mathf.Max(entry.scale,reactionScale);','entry.scale=Mathf.Max(entry.scale,reactionScale);if(reaction>=0)StyleReaction(entry,reaction);ReflowBelowReactions();')
edit(f,'        // label: reaction name', '''        int ReactionIndex(DamageInfo info)
        {if(info.source!=DamageSource.Reaction||ReactionConfig.Current==null)return -1;for(int i=0;i<5;i++)if(ReactionConfig.Current.Rule((ReactionType)i).id==info.skillId)return i;return -1;}
        void StyleReaction(Entry entry,int index)
        {entry.text.fontSharedMaterial=reactionMaterials[index];entry.text.color=Color.white;}
        // label: reaction name''')
edit(f,'e.text.fontStyle = FontStyles.Bold;','e.text.fontSharedMaterial=comicMaterial;e.text.fontStyle = FontStyles.Bold;')
edit(f,'*125+Random.Range(-5f,5f)','*145+Random.Range(-3f,3f)')
edit(f,'(index/columns)*52','(index/columns)*64')

# New captures never overwrite historical images/results.
f='Assets/Skills/Core/Validation/SkillVisualCapture.cs'
edit(f,'task/p10/screens/fix2','task/p10/screens/fix3')
edit(f,'Artifacts/Skills/fix2','Artifacts/Skills/fix3')
edit(f,'revision="fix2-final"','revision="fix3"')
f='Assets/Combat/Validation/ReactionVisualCapture.cs'
edit(f,'task/p11/screens/','task/p11/screens/fix3/')
edit(f,'Artifacts/Reactions','Artifacts/Reactions/fix3',5)
f='Assets/Skills/Core/Validation/SkillSet1Performance.cs'
edit(f,'"tich-lich-nhat-thiem","phat-no-hoa-lien","han-bang-phong-an","than-kiem-ngu-loi","kim-chung-trao","hac-dong-than-la","van-kiem-quyet"','"van-kiem-quyet","than-kiem-ngu-loi","phat-no-hoa-lien","tich-lich-nhat-thiem"')
edit(f,'Artifacts/Skills/fix2','Artifacts/Skills/fix3',2)
print('P11 and new capture roots applied')
