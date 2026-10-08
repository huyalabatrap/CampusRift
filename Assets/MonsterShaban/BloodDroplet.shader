Shader "Campus Rift/Blood Droplet"
{
    Properties { _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.color = input.color * _Color; o.uv = input.uv; return o;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2 - 1;
                p.x *= 1.15 + 0.28 * p.y;
                float edge = length(p) + 0.045 * sin(p.x * 21) * sin(p.y * 17);
                half alpha = 1 - smoothstep(0.64, 0.96, edge);
                half highlight = (1 - smoothstep(0, 0.55, length(p - float2(-0.2,0.24)))) * 0.12;
                return half4(input.color.rgb + half3(highlight,highlight * 0.12,highlight * 0.08), input.color.a * alpha);
            }
            ENDHLSL
        }
    }
}
