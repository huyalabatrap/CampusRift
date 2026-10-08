// P13 variant of P10FireBillow's rolling hot-core/dark-rim shading, using a single smoke puff.
Shader "Campus Rift/P13 Rolling Fire"
{
    Properties{_MainTex("Smoke puff",2D)="white"{}}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+3" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;v.c=a.c;return v;}
            half4 frag(V v):SV_Target
            {
                half4 puff=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv);
                float2 q=v.uv-.5;
                float roll=sin(q.x*18+sin(q.y*14-_Time.y*6)*1.2)*sin(q.y*16+_Time.y*5);
                float hot=saturate(1-length(q)*2.2+roll*.17);
                half3 rgb=lerp(half3(.075,.009,.003),half3(2.2,.30,.012),smoothstep(.08,.75,hot));
                rgb=lerp(rgb,half3(3.8,2.8,.75),smoothstep(.88,1,hot));
                float alpha=puff.a*v.c.a*saturate(puff.r*2.4);
                clip(alpha-.02);return half4(rgb,alpha);
            }
            ENDHLSL
        }
    }
}
