Shader "Campus Rift/P19 Elite Outline"
{
 Properties{_OutlineColor("Color",Color)=(1,0.5,0,1) _Width("Width",Float)=0.025}
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Geometry-1"}
 Pass{
 Cull Front ZWrite On
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _OutlineColor;float _Width;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;};
 V vert(A i){V o;o.p=TransformWorldToHClip(TransformObjectToWorld(i.p.xyz)+TransformObjectToWorldNormal(i.n)*_Width);return o;}
 half4 frag(V i):SV_Target{return _OutlineColor;}
 ENDHLSL
 }
 }
}
