from pathlib import Path
import shutil

def edit(path, old, new):
    p=Path(path); backup=Path('Backups/P10-fix3-resume-20261002')/p
    if not backup.exists():
        backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,backup)
    s=p.read_text(encoding='utf-8-sig')
    assert s.count(old)==1,(path,old,s.count(old))
    p.write_text(s.replace(old,new),encoding='utf-8-sig')

# URP user stencil bit 3. Gameplay UGUI masks use the lower bits; the effect only
# runs in SampleScene, before Overlay UI. Write only covered visible VFX pixels.
edit('Assets/CampusRiftUI/Runtime/ComicInkFeature.cs',
     '            fetchColorBuffer = true;',
     '            fetchColorBuffer = true;\n            bindDepthStencilAttachment = true;')
edit('Assets/CampusRiftUI/Comic/ComicInk.shader',
     '            ZTest Always ZWrite Off Cull Off',
     '            ZTest Always ZWrite Off Cull Off\n            Stencil { Ref 8 ReadMask 8 Comp NotEqual Pass Keep }')
edit('Assets/CampusRiftUI/Comic/ComicInk.shader',
     '''                float energyCore=smoothstep(.75,.88,min(color.r,color.g))*smoothstep(.4,.65,color.b);
                float edge = max(max(smoothstep(0.018,0.055,depthEdge),smoothstep(0.35,0.70,normalEdge))*(1-energyCore),smoothstep(0.16,0.42,colorEdge));''',
     '''                float edge = max(max(smoothstep(0.018,0.055,depthEdge),smoothstep(0.35,0.70,normalEdge)),smoothstep(0.16,0.42,colorEdge));''')
edit('Assets/Skills/Core/P10LayeredStroke.shader',
     'Properties { _Core', 'Properties { _InkMask("Authored energy stencil",Float)=0 _Core')
edit('Assets/Skills/Core/P10LayeredStroke.shader',
     '            ZTest [_ZTest]',
     '            ZTest [_ZTest]\n            Stencil { Ref [_InkMask] WriteMask [_InkMask] Comp Always Pass Replace }')
edit('Assets/Skills/Core/P10LayeredStroke.shader',
     '                half d=abs(v.uv.y*2-1);',
     '                clip(v.c.a-.025);\n                half d=abs(v.uv.y*2-1);')
edit('Assets/Skills/Core/P10Surface.shader',
     '        _BaseColor("Color",Color)',
     '        _InkMask("Authored energy stencil",Float)=0\n        _BaseColor("Color",Color)')
edit('Assets/Skills/Core/P10Surface.shader',
     '            ZWrite Off',
     '            ZWrite Off\n            Stencil { Ref [_InkMask] WriteMask [_InkMask] Comp Always Pass Replace }')
edit('Assets/Skills/Core/P10Surface.shader',
     '                return half4(color-halftone,_Alpha*opacity*lerp(1,v.c.a,_ParticleTint));',
     '                float alpha=_Alpha*opacity*lerp(1,v.c.a,_ParticleTint);clip(alpha-.025);\n                return half4(color-halftone,alpha);')
edit('Assets/Skills/Core/P10FireBillow.shader',
     ' Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off',
     ' Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off\n Stencil { Ref 8 WriteMask 8 Comp Always Pass Replace }')
edit('Assets/Skills/Core/P10FireBillow.shader',
     '     return half4(rgb,puff.a*v.c.a);',
     '     float alpha=puff.a*v.c.a;clip(alpha-.025);return half4(rgb,alpha);')
edit('Assets/Skills/Core/Runtime/SkillSet1VfxConfig.cs',
     'public Material fireBillow,', 'public Material lotusSurface,fireBillow,')
edit('Assets/Skills/Core/Runtime/SkillVfxPool.cs',
     '                if(kind==SkillVfxKind.Accretion)n.surface.sharedMaterial=config.accretion;',
     '                if(kind==SkillVfxKind.Lotus)n.surface.sharedMaterial=config.lotusSurface;\n                if(kind==SkillVfxKind.Accretion)n.surface.sharedMaterial=config.accretion;')
edit('Assets/Skills/Core/Editor/SkillSet1Setup.cs',
     '            c.fireBillow=Material',
     '            c.lotusSurface=Material("P10LotusSurface","Campus Rift/P10 Comic Energy");c.lotusSurface.SetFloat("_InkMask",8);EditorUtility.SetDirty(c.lotusSurface);\n            c.fireBillow=Material')
edit('Assets/Skills/Core/Editor/SkillSet1Setup.cs',
     '            foreach(var m in new[]{c.copperMark,',
     '''            foreach(var m in new[]{c.slashStroke,c.layeredLightning,c.reactionStroke,c.reactionSurface}){m.SetFloat("_InkMask",8);EditorUtility.SetDirty(m);}
            foreach(var m in new[]{c.copperMark,''')
print('Authored energy stencil applied; background ink shader restored to original edge computation.')
