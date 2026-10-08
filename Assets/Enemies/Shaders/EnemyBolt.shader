Shader "Campus Rift/Enemy Bolt Comic"
{
 Properties { _Tint("Tint",Color)=(1,1,1,1) }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-3" "RenderType"="Transparent"}
 Pass {Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial) float4 _Tint; CBUFFER_END
 struct A{float4 p:POSITION;half4 c:COLOR;};struct V{float4 p:SV_POSITION;half4 c:COLOR;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.c=i.c;return o;}
 half4 frag(V i):SV_Target{return _Tint*i.c;}
 ENDHLSL }
 }
}
