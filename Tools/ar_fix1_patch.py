from pathlib import Path
def edit(path,old,new):
 p=Path(path);s=p.read_text(encoding='utf-8-sig');assert old in s,(path,old[:60]);p.write_text(s.replace(old,new),encoding='utf-8')
p='Assets/Skills/Core/Runtime/SkillVfxPool.cs'
edit(p,'var n=nodes[i];n.Live=true;n.kind=kind;', '''// Fit complete circular effects before rendering; positions and radii are world units.
                float worldRadius=radius*Scale;
                if(ar!=null && kind!=SkillVfxKind.Bolt && kind!=SkillVfxKind.ChainBolt && kind!=SkillVfxKind.ReactionBolt)
                {
                    worldRadius=ar.VisualRadius(worldRadius);
                    position=ar.ConstrainVisual(position,worldRadius*(kind==SkillVfxKind.Portal?1.15f:1));
                }
                var n=nodes[i];n.Live=true;n.kind=kind;''')
edit(p,'n.Radius=radius*Scale;','n.Radius=worldRadius;')
# World-space LineRenderer ignores parent scale. The pooled mesh roots already cancel it.
# Vortex spirals used the old nine-unit envelope even on a half-metre battlefield.
edit(p,'void VortexParticles(Node n)\n        {','float VortexScale=>ar!=null?Mathf.Min(Scale,ar.battlefield.placement.Radius*.85f/9):Scale;\n        void VortexParticles(Node n)\n        {')
s=Path(p).read_text(encoding='utf-8')
for start,end in [('void VortexParticles(Node n)','void LotusParticles'),('void VortexSpirals(Node n,float fade)','void RepulsionParticles')]:
 a=s.index(start);b=s.index(end,a);part=s[a:b].replace('*Scale','*VortexScale');s=s[:a]+part+s[b:]
Path(p).write_text(s,encoding='utf-8')
edit(p,'n.model.localPosition=Vector3.up*Mathf.Lerp(.8f,0,Mathf.Clamp01(n.age/.25f));','n.model.localPosition=Vector3.up*Mathf.Lerp(.8f,0,Mathf.Clamp01(n.age/.25f))*Scale;')
edit(p,'Vector3.up*(n.kind==SkillVfxKind.Bell?.55f:0)','Vector3.up*((n.kind==SkillVfxKind.Bell?.55f:0)*Scale)')
p='Assets/Combat/Runtime/DamageNumberPool.cs'
edit(p,'public bool reserved; public Element element;','public bool reserved,ar; public Element element;')
edit(p,'if(!e.reserved||Time.time-e.born>Lifetime)continue;','if(e.ar||!e.reserved||Time.time-e.born>Lifetime)continue;')
edit(p,'existing.text.text=UI.Accessibility.Symbol(existing.element)+" "+Mathf.CeilToInt(existing.amount);','existing.text.text=(existing.ar?"":UI.Accessibility.Symbol(existing.element)+" ")+Mathf.CeilToInt(existing.amount);')
edit(p,'ShowEntry(top, info.amount, info.element, info.critical, null);','ShowEntry(top, info.amount, info.element, info.critical, null,ar!=null);')
edit(p,'Entry ShowEntry(Vector3 position, float amount, Element element, bool critical, string label)','Entry ShowEntry(Vector3 position, float amount, Element element, bool critical, string label,bool ar=false)')
edit(p,'e.element=element;','e.element=element;e.ar=ar;')
edit(p,'UI.Accessibility.Symbol(element)+" "+Mathf.CeilToInt(amount)','(ar?"":UI.Accessibility.Symbol(element)+" ")+Mathf.CeilToInt(amount)')
edit(p,'e.born=Time.time+(stagger%8)*.04f;','if(ar)e.origin=position+side*((stagger%3-1)*.045f)+Vector3.up*((stagger/3)*.03f);\n            e.born=Time.time+(stagger%8)*.04f;')
edit(p,'distance * WorldSize * e.scale','distance * WorldSize * (e.ar?.8f:1) * e.scale')
p='Assets/ARRift/Runtime/ARBattlefield.cs'
s=Path(p).read_text(encoding='utf-8');a=s.index('            var gem=');b=s.index('            Rift=',a)
s=s[:a]+'            shrine.AddComponent<ARShrineVisual>().Initialize(Shrine,this);\n'+s[b:]
s=s.replace('Mesh mesh;Material shrineMaterial;','Mesh mesh;').replace('if(shrineMaterial!=null)Destroy(shrineMaterial);','')
Path(p).write_text(s,encoding='utf-8')
# Keep the old placement-only screen correct too; the battle scene already uses M6 ARBattleHUD.
p='Assets/ARRift/Runtime/ARPlacementHUD.cs'
edit(p,'TMP_Text message;','TMP_Text message;float anchoredAt=-1;bool wasAnchored;')
edit(p,'message.text=placement.Message;','message.text=placement.Message;bool anchored=placement.Root!=null;if(anchored&&!wasAnchored)anchoredAt=Time.unscaledTime;wasAnchored=anchored;canvas.gameObject.SetActive(!anchored||Time.unscaledTime-anchoredAt<2);foreach(var b in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))b.gameObject.SetActive(!anchored);')
print('fix1 patches applied')
