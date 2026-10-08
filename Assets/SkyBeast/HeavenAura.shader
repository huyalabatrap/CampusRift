Shader "Campus Rift/Heaven Sword Aura"
{
    Properties { _Alpha("Alpha",Range(0,1))=1 _Clock("Unscaled time",Float)=0 _Soft("Soft haze",Float)=0 _RuneFlow("Flowing seal light",Float)=0 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent"}
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;float height:TEXCOORD1;};
            CBUFFER_START(UnityPerMaterial) float _Alpha,_Clock,_Soft,_RuneFlow; CBUFFER_END
            V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.c=a.c;o.height=a.p.y;return o;}
            half4 frag(V i):SV_Target
            {
                float edge=abs(i.uv.x-.5)*2;
                float a=pow(saturate(1-edge),lerp(.65,1.3,_Soft));
                half3 ink=half3(.18,.06,.008);
                half3 col=lerp(i.c.rgb,ink,smoothstep(.55,.94,edge)*(1-_Soft));
                col*=lerp(1,.45+.9*pow(.5+.5*sin(i.height*.22-_Clock*5),3),_RuneFlow);
                return half4(col,a*i.c.a*_Alpha);
            }
            ENDHLSL
        }
    }
}
