from pathlib import Path
import shutil
def edit(path,old,new,count=1):
    p=Path(path);s=p.read_text(encoding='utf-8-sig');assert s.count(old)==count,(path,old[:60],s.count(old));p.write_text(s.replace(old,new),encoding='utf-8')
root=Path('Artifacts/Reactions/fix3/before-pool-and-foreground-fix');root.mkdir(parents=True,exist_ok=True)
for p in Path('Artifacts/Reactions/fix3').glob('Visual*'):shutil.copy2(p,root/p.name)
shutil.copytree('task/p11/screens/fix3',root/'screens',dirs_exist_ok=True)
f='Assets/Combat/Runtime/ReactionFeedback.cs'
edit(f,'pool.DimIce(ev.point,3.5f,.55f)','pool.DimIce(ev.point,10,.55f)')
edit(f,'case ReactionType.IceLightning:\n                    ReactionFlash','case ReactionType.IceLightning:\n                    p=ev.point+Vector3.up*1.6f;\n                    ReactionFlash')
edit(f,'Mathf.Cos(a)*rule.radius,.4f,Mathf.Sin(a)*rule.radius','Mathf.Cos(a)*rule.radius,.7f,Mathf.Sin(a)*rule.radius')
edit(f,'i<MonsterVitality.Active.Count&&arcs<8','i<MonsterVitality.Active.Count&&arcs<4')
f='Assets/Skills/Core/Runtime/SkillVfxPool.cs'
edit(f,'            Draw(n.line,9,n.Color,fade,width*1.85f);','''            if(n.age>=.3f){n.line.enabled=n.core.enabled=n.detail.enabled=false;Stream(n,MobileQuality?15:40);n.particles.transform.position=Vector3.Lerp(n.Position,n.End,Random.value);return;}
            Draw(n.line,9,n.Color,fade,width*1.85f);''')
edit(f,'Mathf.Lerp(1,.18f,(n.age-.23f)/.07f)','Mathf.Lerp(1,0,(n.age-.23f)/.07f)')
f='Assets/Skills/Core/Validation/SkillVisualCapture.cs'
edit(f,'.55f,.65f,.95f,1.75f','.55f,.76f,.95f,1.75f')
print('Foreground/readability and pool correction applied; raw FAIL retained')
