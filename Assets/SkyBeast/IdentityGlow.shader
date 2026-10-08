Shader "Campus Rift/P21 Identity Glow"
{
    Properties { _Dissolve("Dissolve",Range(0,1))=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 positionOS:POSITION;half4 color:COLOR;};
            struct V {float4 positionCS:SV_POSITION;half4 color:COLOR;float3 pos:TEXCOORD0;};
            CBUFFER_START(UnityPerMaterial) float _Dissolve; CBUFFER_END
            V vert(A v){V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.pos=v.positionOS.xyz;o.color=v.color;return o;}
            half4 frag(V i):SV_Target {float n=frac(sin(dot(i.pos,float3(12.9,78.2,37.7)))*43758.5);clip(n-_Dissolve);return half4(i.color.rgb*2.2,1);}
            ENDHLSL
        }
    }
}
