from pathlib import Path
def edit(path,old,new,count=1):
    p=Path(path);s=p.read_text(encoding='utf-8-sig');assert s.count(old)==count,(path,old[:60],s.count(old));p.write_text(s.replace(old,new),encoding='utf-8')
f='Assets/Skills/Core/Validation/SkillSet1Performance.cs'
edit(f,'public bool postProcessing,bloom,comicInk,hdr;','public string error="";public int renderedFrames;public bool postProcessing,bloom,comicInk,hdr;')
edit(f,'string Output=>Application.isEditor?"Artifacts/Skills/SkillSet1-Performance":"Artifacts/Skills/SkillSet1-Performance-Player";','string Output=>Application.isEditor?"Artifacts/Skills/fix3/Performance-Editor":"Artifacts/Skills/fix3/Performance-Player";')
edit(f,'if(onlySkill.Length>0&&skill.Id!=onlySkill)continue;','if(onlySkill.Length>0&&skill.Id!=onlySkill)continue;\n                if(onlySkill.Length==0&&skill.Id!="van-kiem-quyet"&&skill.Id!="than-kiem-ngu-loi"&&skill.Id!="phat-no-hoa-lien"&&skill.Id!="tich-lich-nhat-thiem")continue;')
edit(f,'Directory.CreateDirectory("Artifacts/Skills");','Directory.CreateDirectory("Artifacts/Skills/fix3");')
edit(f,'world.End();bool pass=report.postProcessing&&report.bloom&&report.comicInk&&report.hdr;','''var render=ReactionGpuBenchRender.Instance;report.renderedFrames=render!=null?render.RenderedFrames:0;report.error=render!=null?render.Error:"";
            if(!Application.isEditor)report.environment+=" D3D12 offscreen URP StandardRequest renders full HDR camera stack; GPU fence each frame; overlay HUD routed to camera; display Present excluded.";
            File.WriteAllText(Output+".json",JsonUtility.ToJson(report,true));
            world.End();bool pass=report.postProcessing&&report.bloom&&report.comicInk&&report.hdr&&report.error.Length==0&&(Application.isEditor||report.renderedFrames>0);''')
edit(f,'var bloom=UnityEngine.Rendering.VolumeManager.instance.stack.GetComponent<UnityEngine.Rendering.Universal.Bloom>();','var stack=data!=null&&data.volumeStack!=null?data.volumeStack:UnityEngine.Rendering.VolumeManager.instance.stack;var bloom=stack.GetComponent<UnityEngine.Rendering.Universal.Bloom>();')
f='Assets/Skills/Core/Validation/SkillSet1StandaloneBench.cs'
edit(f,'for(int i=0;i<30;i++)yield return null;','var rendering=gameObject.AddComponent<CampusRift.Combat.ReactionGpuBenchRender>();rendering.Initialize(FindAnyObjectByType<CampusExplorer>().followCamera);\n            for(int i=0;i<30;i++)yield return null;')
edit(f,'string shot=System.IO.Path.GetFullPath("Artifacts/Skills/P10-Player-render.png");\n            ScreenCapture.CaptureScreenshot(shot);float captureDeadline=Time.realtimeSinceStartup+3;\n            while(!System.IO.File.Exists(shot)&&Time.realtimeSinceStartup<captureDeadline)yield return null;','rendering.Snapshot(System.IO.Path.GetFullPath("Artifacts/Skills/fix3/Player-render.png"));')
p=Path('Tools/p10_build_bench.cs');s=p.read_text(encoding='utf-8');s=s.replace('Builds/P10Benchmark','Builds/P10Fix3Benchmark').replace('"P10_BENCH"','"P10_BENCH","P11_BENCH"').replace('Artifacts/Skills/P10-Build-status.txt','Artifacts/Skills/fix3/Build-status.txt');Path('Tools/p10_fix3_build.cs').write_text(s,encoding='utf-8')
p=Path('Tools/p10_build_invoke.cs');Path('Tools/p10_fix3_build_invoke.cs').write_text(p.read_text().replace('Artifacts/Skills/P10-Build-status.txt','Artifacts/Skills/fix3/Build-status.txt'),encoding='utf-8')
# Preserve the accepted black-hole body flash. Only Chain uses the softer impact tint.
f='Assets/Skills/GiantHandSeal/Runtime/MonsterVitality.cs'
edit(f,'bool comicSkillFlash;','bool comicSkillFlash,softComicFlash;')
edit(f,'comicSkillFlash=info.source==DamageSource.Skill&&ComicSkill(info.skillId);','softComicFlash=info.skillId=="than-kiem-ngu-loi";comicSkillFlash=info.source==DamageSource.Skill&&ComicSkill(info.skillId);')
edit(f,'if(comicSkillFlash)Paint(new Color(.32f,.25f,.42f),new Color(.32f,.25f,.42f));','if(comicSkillFlash)Paint(softComicFlash?new Color(.32f,.25f,.42f):new Color(.025f,.01f,.04f),softComicFlash?new Color(.32f,.25f,.42f):new Color(.04f,.015f,.06f));')
f='Assets/Skills/Core/P10Surface.shader'
edit(f,'if(_ParticleTint>0)color*=v.c.rgb;','if(_FlatGlow>0&&v.o.z<-.2)color=half3(.9,.38,.018);\n                if(_ParticleTint>0)color*=v.c.rgb;')
print('Native GPU measurement and four-skill benchmark prepared')
