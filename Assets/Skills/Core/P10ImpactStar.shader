Shader "Campus Rift/P10 Impact Star" {
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
