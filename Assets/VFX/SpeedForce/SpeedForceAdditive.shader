Shader "Campus Rift/Speed Force Additive"
{
    // Additive glow for lightning lines and particles. Vertex color carries hue/alpha;
    // _Intensity pushes it into HDR so the scene Bloom turns the core white-hot.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("HDR intensity", Range(0,12)) = 3
        _CoreBoost ("White core", Range(0,2)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Additive"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _Intensity;
                half _CoreBoost;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                // The brightest part of the texture drifts toward white, like a real arc.
                half3 hue = lerp(input.color.rgb, half3(1,1,1), saturate(mask * mask * _CoreBoost));
                return half4(hue * _Intensity, mask * input.color.a);
            }
            ENDHLSL
        }
    }
}
