Shader "Campus Rift/Character Afterimage"
{
    Properties
    {
        _BaseMap ("Silhouette alpha", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0
        [HDR] _GhostColor ("Ghost color", Color) = (0.1,1.4,2,1)
        _GhostAlpha ("Opacity", Range(0,1)) = 0.48
        _RimPower ("Edge softness", Range(0.5,5)) = 1.7
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Ghost"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _GhostColor;
                half _GhostAlpha;
                half _Cutoff;
                half _RimPower;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half silhouette = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                clip(silhouette - max(_Cutoff, 0.001h));
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = pow(1 - saturate(abs(dot(normalize(input.normalWS), view))), _RimPower);
                half alpha = _GhostAlpha * lerp(0.42h, 1.0h, rim) * silhouette;
                return half4(_GhostColor.rgb * lerp(0.7h, 1.25h, rim), alpha);
            }
            ENDHLSL
        }
    }
}
