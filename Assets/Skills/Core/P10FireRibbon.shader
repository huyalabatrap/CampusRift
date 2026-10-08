Shader "Campus Rift/P10 Scrolling Fire Ribbon"
{
    Properties { _MainTex("Fire atlas",2D)="white"{} }
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-2" "RenderType"="Transparent"}
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;v.c=a.c;return v;}
            half4 frag(V v):SV_Target {
                float f=floor(_Time.y*12)%16;
                float2 tile=float2(fmod(f,4),floor(f/4));
                float2 uv=(float2(frac(v.uv.x*2-_Time.y*3),v.uv.y)+tile)/4;
                half4 fire=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
                float d=abs(v.uv.y*2-1);
                half3 color=d>.75?half3(.16,.015,.006):lerp(half3(2,.12,.005),half3(3.5,2,.3),saturate((1-d)*fire.r));
                return half4(color,v.c.a*saturate((1-d)*10)*(.55+.45*fire.a));
            }
            ENDHLSL
        }
    }
}
