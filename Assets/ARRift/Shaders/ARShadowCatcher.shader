Shader "Campus Rift/AR/Shadow Catcher"
{
 Properties { _ShadowStrength("Shadow strength",Range(0,1))=.65 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent" }
 Pass { Tags { "LightMode"="UniversalForward" } Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 float _ShadowStrength;
 struct A { float4 positionOS:POSITION; };struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;};
 V vert(A a){V o;o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);return o;}
 half4 frag(V i):SV_Target {Light l=GetMainLight(TransformWorldToShadowCoord(i.positionWS));return half4(.025,.018,.05,(1-l.shadowAttenuation)*_ShadowStrength);}
 ENDHLSL
 } }
}
