Shader "Campus Rift/P13 Mouth Flame Cone"
{
    Properties{_MainTex("Rolling smoke",2D)="white"{}}
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
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;return v;}
            half4 frag(V v):SV_Target
            {
                float curl=sin(v.uv.y*34-_Time.y*13+sin(v.uv.x*18+_Time.y*4));
                float cross=abs(v.uv.x-.5)*2;
                float hot=saturate(1-cross+curl*.18);
                half3 rgb=lerp(half3(.12,.008,.002),half3(2.5,.34,.012),smoothstep(.08,.75,hot));
                rgb=lerp(rgb,half3(4,2.9,.9),smoothstep(.88,1.05,hot));
                float2 flow=float2(v.uv.x+sin(v.uv.y*15-_Time.y*6)*.08,frac(v.uv.y*3-_Time.y*.8));
                float puff=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,flow).r;
                float edge=1-smoothstep(.72,1,cross+curl*.045);
                float ends=smoothstep(0,.05,v.uv.y)*(1-smoothstep(.82,1,v.uv.y));
                return half4(rgb,edge*ends*(.55+puff*.45)*.94);
            }
            ENDHLSL
        }
    }
}
