Shader "Campus Rift/P10 Alpha Stroke"
{
    Properties { _BaseMap("Texture",2D)="white"{} }
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-2" "RenderType"="Transparent"}
        Pass {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            CBUFFER_END
            struct A {float4 p:POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;};
            struct V {float4 p:SV_POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;};
            V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.c=a.c;v.uv=TRANSFORM_TEX(a.uv,_BaseMap);return v;}
            half4 frag(V v):SV_Target {return half4(v.c.rgb,v.c.a*SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv).a);}
            ENDHLSL
        }
    }
}
