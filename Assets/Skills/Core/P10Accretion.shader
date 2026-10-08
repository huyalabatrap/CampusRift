Shader "Campus Rift/P10 Accretion Disc"
{
    Properties { _Alpha("Opacity",Range(0,1))=1 _Progress("Expansion",Float)=1 }
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Alpha,_Progress;
            CBUFFER_END
            struct A {float4 p:POSITION;};
            struct V {float4 p:SV_POSITION;float2 o:TEXCOORD0;};
            V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.o=a.p.xz;return v;}
            half4 frag(V v):SV_Target {
                float r=length(v.o),a=atan2(v.o.y,v.o.x);
                float flow=a*7-r*22+_Time.y*8;
                float bands=pow(saturate(.5+.5*sin(flow)),5);
                float threads=pow(saturate(.5+.5*sin(a*19-r*49+_Time.y*13)),12);
                float edge=smoothstep(.43,.5,r)*(1-smoothstep(.91,1,r));
                half3 ink=half3(.035,.004,.075);
                half3 glow=lerp(half3(.3,.015,.8),half3(2.5,.18,1.6),bands);
                return half4(lerp(ink,glow,saturate(bands+threads*.5)),edge*_Alpha*(.45+.5*bands));
            }
            ENDHLSL
        }
    }
}
