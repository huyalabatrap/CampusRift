from pathlib import Path
def edit(path,old,new,count=1):
    p=Path(path);s=p.read_text(encoding='utf-8-sig')
    assert s.count(old)==count,(path,old[:70],s.count(old))
    p.write_text(s.replace(old,new),encoding='utf-8')

# Rain: visual dimensions/tail only. No changes to flight time, landing point, cadence or hits.
f='Assets/Combat/Runtime/FlyingSword.cs'
edit(f,'trail.time=.045f;','trail.time=.15f;')
edit(f,'visual.localScale=Vector3.one*1.25f;','visual.localScale=Vector3.one*1.95f;',2)
edit(f,'MobileQuality?.09f:.14f','MobileQuality?.14f:.22f')
edit(f,'rainInk.SetPropertyBlock(rainBlock);','rainBlock.SetFloat("_Width",.03f);rainInk.SetPropertyBlock(rainBlock);',2)
f='Assets/Skills/SwordRain/Runtime/SwordRainRuntime.cs'
edit(f,'int danger;','int danger;\n        readonly SkillVfxPool.Node[] portals=new SkillVfxPool.Node[5];')
edit(f,'vfx.Spawn(SkillVfxKind.Portal,p,Accent,3.7f,1.1f);','portals[i]=vfx.Spawn(SkillVfxKind.Portal,p,Accent,3.7f,1.1f);')
edit(f,'while(SwordsLaunched<30','for(int i=0;i<portals.Length;i++)if(portals[i]!=null&&portals[i].Live)portals[i].Progress=1+Mathf.Clamp01(elapsed-2)*2;\n            while(SwordsLaunched<30')
edit(f,'SkillVfxKind.Scorch,destination,new Color(.32f,.17f,.025f),1.8f,.4f','SkillVfxKind.CopperMark,destination,new Color(.7f,.43f,.06f),1.8f,.6f')
old='if(hit)vfx.Burst(destination+Vector3.up*.15f,Accent,1.45f,vfx.config.sword,false);else{var spark=vfx.Spawn(SkillVfxKind.Burst,destination+Vector3.up*.15f,Accent,.7f,.3f,vfx.config.sword);if(spark!=null)vfx.Emit(spark,SkillVfxPool.MobileQuality?8:20);}'
edit(f,old,'var spark=vfx.Spawn(SkillVfxKind.SwordImpact,destination+Vector3.up*.2f,Accent,.5f,.85f,vfx.config.sword);if(spark!=null)vfx.Emit(spark,SkillVfxPool.MobileQuality?6:14);')
f='Assets/Skills/Core/P10Ink.shader'
edit(f,'_Alpha("Opacity",Range(0,1))=1','_Alpha("Opacity",Range(0,1))=1 _Width("Ink width",Float)=.014')
edit(f,'half _Alpha;','half _Alpha;float _Width;')
edit(f,'a.n*.014','a.n*_Width')
f='Assets/Skills/Core/P10Surface.shader'
edit(f,'half3(1.45,.55,.025),half3(2.2,1.9,1.1)','half3(3.1,1.9,.10),half3(4.5,3.9,2.1)')

# Materials and mesh helpers are all warmed once and scoped to their own effects.
f='Assets/Skills/Core/Runtime/SkillSet1VfxConfig.cs'
edit(f,'public Material accretion;','public Material accretion;\n        public Material copperMark,impactStar,slashStroke,reactionSurface,reactionStroke,reactionIce;')
f='Assets/Skills/Core/Editor/SkillSet1Setup.cs'
marker='        static void ConfigureFlash('
new='''        [MenuItem("Campus Rift/V2/Upgrade Fix3 Visuals")]
        public static void UpgradeFix3Visuals()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
            var c=Resources.Load<SkillSet1VfxConfig>("SkillSet1Vfx");
            c.copperMark=Material("P10CopperMark","Campus Rift/P10 Ground Mark");c.copperMark.SetFloat("_Metal",1);c.copperMark.enableInstancing=true;
            c.impactStar=Material("P10ImpactStar","Campus Rift/P10 Impact Star");
            c.slashStroke=Material("P10CrescentSlash","Campus Rift/P10 Layered Stroke");c.slashStroke.SetFloat("_Core",.54f);c.slashStroke.SetFloat("_Glow",.82f);c.slashStroke.SetFloat("_ZTest",8);
            c.reactionSurface=Material("P11PriorityEnergy","Campus Rift/P10 Comic Energy");c.reactionSurface.renderQueue=3120;
            c.reactionStroke=Material("P11PriorityStroke","Campus Rift/P10 Layered Stroke");c.reactionStroke.SetFloat("_Core",.12f);c.reactionStroke.SetFloat("_Glow",.70f);c.reactionStroke.SetFloat("_Lightning",1);c.reactionStroke.renderQueue=3130;
            c.reactionIce=Material("P11PriorityIce","Campus Rift/P10 Comic Energy");c.reactionIce.SetFloat("_ParticleTint",1);c.reactionIce.renderQueue=3120;
            foreach(var m in new[]{c.copperMark,c.impactStar,c.slashStroke,c.reactionSurface,c.reactionStroke,c.reactionIce})EditorUtility.SetDirty(m);
            EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();
        }
'''
edit(f,marker,new+marker)
f='Assets/Skills/Core/P10GroundMark.shader'
edit(f,'_BaseColor("Legacy Ink"','_Metal("Copper cracks",Float)=0 _BaseColor("Legacy Ink"')
edit(f,'half4 _BaseColor;float _Alpha;','half4 _BaseColor;float _Alpha,_Metal;')
edit(f,'return half4(UNITY_ACCESS_INSTANCED_PROP(P10Marks,_MarkColor).rgb,','half3 color=UNITY_ACCESS_INSTANCED_PROP(P10Marks,_MarkColor).rgb;\n                if(_Metal>.5){float cracks=1-smoothstep(.03,.055,abs(sin(a*7+d*16+sin(d*11)*.5)));color=lerp(color,half3(.008,.006,.002),cracks*edge);}\n                return half4(color,')
f='Assets/Skills/Core/Runtime/SkillVfxMeshes.cs'
edit(f,'        static Mesh Build(','''        public static Mesh ImpactStar()
        {
            var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();
            v.Add(Vector3.zero);colors.Add(new Color(4,3.6f,2,1));
            for(int i=0;i<=24;i++){float a=i/24f*Mathf.PI*2,r=i%2==0?(i%6==0?1:.7f):.22f;v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0));colors.Add(i%2==0?new Color(.2f,.07f,.003f,1):new Color(2,1.4f,.03f,1));if(i>0){t.Add(0);t.Add(i);t.Add(i+1);}}
            var m=Build("Golden star impact",v,t);m.SetColors(colors);return m;
        }
        static Mesh Build(''')
Path('Assets/Skills/Core/P10ImpactStar.shader').write_text('''Shader "Campus Rift/P10 Impact Star" {
 Properties { _Alpha("Opacity",Range(0,1))=1 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+4"} Pass {
 Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float _Alpha;
 struct A{float4 p:POSITION;half4 c:COLOR;};struct V{float4 p:SV_POSITION;half4 c:COLOR;};
 V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.c=a.c;return v;}
 half4 frag(V v):SV_Target{return half4(v.c.rgb,_Alpha*v.c.a);}
 ENDHLSL
 } }
}
''',encoding='utf-8')
print('Rain visual edits applied')
