from pathlib import Path
import shutil,json
def edit(path,old,new,count=1):
    p=Path(path);s=p.read_text(encoding='utf-8-sig');assert s.count(old)==count,(path,old[:60],s.count(old));p.write_text(s.replace(old,new),encoding='utf-8')
root=Path('Artifacts/Skills/fix3/visual-before-billows-soft-hit')
shutil.copytree('task/p10/screens/fix3',root/'screens',dirs_exist_ok=True)
for p in Path('Artifacts/Skills/fix3').glob('*-visual.json'):shutil.copy2(p,root/p.name)
f='Assets/Skills/GiantHandSeal/Runtime/MonsterVitality.cs'
edit(f,'new Color(.32f,.25f,.42f)','new Color(.78f,.69f,.86f)',2)
f='Assets/Skills/Core/Runtime/SkillSet1VfxConfig.cs'
edit(f,'public Material copperMark,','public Material fireBillow,copperMark,')
f='Assets/Skills/Core/Editor/SkillSet1Setup.cs'
edit(f,'c.copperMark=Material','c.fireBillow=Material("P10FireBillow","Campus Rift/P10 Rolling Fire Billow");c.fireBillow.mainTexture=c.smokeFlipbook.mainTexture;EditorUtility.SetDirty(c.fireBillow);\n            c.copperMark=Material')
f='Assets/Skills/Core/Runtime/SkillVfxPool.cs'
edit(f,'kind==SkillVfxKind.FireField||kind==SkillVfxKind.FireBloom?config.fireFlipbook:','kind==SkillVfxKind.FireBloom?config.fireBillow:kind==SkillVfxKind.FireField?config.fireFlipbook:')
edit(f,'e.startColor=new Color(1,1,1,fade*.85f);','e.startColor=new Color(1,1,1,fade*.68f);')
edit(f,'side*Mathf.Sin(t*Mathf.PI)*.8f','side*Mathf.Sin(t*Mathf.PI)*2.2f')
edit(f,'Mathf.Sin(t*Mathf.PI)*.8f+(edge==0','Mathf.Sin(t*Mathf.PI)*2.2f+(edge==0')
Path('Assets/Skills/Core/P10FireBillow.shader').write_text('''Shader "Campus Rift/P10 Rolling Fire Billow" {
 Properties { _MainTex("Rolling smoke atlas",2D)="white"{} }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+3"} Pass {
 Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
 V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;v.c=a.c;return v;}
 half4 frag(V v):SV_Target {
     half4 puff=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv);
     float2 q=frac(v.uv*4)-.5;
     float roll=sin(q.x*15+sin(q.y*18-_Time.y*8)*1.5)*sin(q.y*17+_Time.y*6);
     float hot=saturate(1-length(q)*2.3+roll*.2);
     half3 rgb=lerp(half3(.20,.024,.003),half3(3.2,1.15,.025),smoothstep(.05,.65,hot));
     rgb=lerp(rgb,half3(4.2,3.6,1.7),smoothstep(.65,.98,hot));
     return half4(rgb,puff.a*v.c.a);
 }
 ENDHLSL
 } }
}
''',encoding='utf-8')
# Depth-derived lines from the enemy behind transparent HDR energy looked like rock cracks.
# Keep colour outlines and authored dark borders, suppress background-depth ink inside HDR cores.
p=Path('Assets/CampusRiftUI/Comic/ComicInk.shader');backup=Path(json.loads(Path('Artifacts/Skills/fix3/PreEdit.json').read_text())['backup']);(backup/p.parent).mkdir(parents=True,exist_ok=True);shutil.copy2(p,backup/p)
f=str(p)
edit(f,'float edge = max(max(smoothstep(0.018,0.055,depthEdge),smoothstep(0.35,0.70,normalEdge)),smoothstep(0.16,0.42,colorEdge));','float energyCore=smoothstep(.91,.99,min(color.r,color.g))*smoothstep(.3,.7,color.b);\n                float edge = max(max(smoothstep(0.018,0.055,depthEdge),smoothstep(0.35,0.70,normalEdge))*(1-energyCore),smoothstep(0.16,0.42,colorEdge));')
print('Visual issues corrected; before-images archived')
