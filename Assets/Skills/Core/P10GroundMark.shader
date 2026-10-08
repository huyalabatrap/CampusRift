Shader "Campus Rift/P10 Ground Mark"
{
    Properties { _Metal("Copper cracks",Float)=0 _BaseColor("Legacy Ink",Color)=(.04,.008,.07,1) _Alpha("Legacy Opacity",Range(0,1))=.8 _MarkColor("Ink",Color)=(.04,.008,.07,1) _MarkAlpha("Opacity",Range(0,1))=.8 }
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-3" "RenderType"="Transparent"}
        Pass {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Offset -1,-1
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;float _Alpha,_Metal;
            CBUFFER_END
            UNITY_INSTANCING_BUFFER_START(P10Marks)
                UNITY_DEFINE_INSTANCED_PROP(float4,_MarkColor)
                UNITY_DEFINE_INSTANCED_PROP(float,_MarkAlpha)
            UNITY_INSTANCING_BUFFER_END(P10Marks)
            struct A {float4 p:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};struct V{float4 p:SV_POSITION;float2 o:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            V vert(A a){V v;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,v);v.p=TransformObjectToHClip(a.p.xyz);v.o=a.p.xz;return v;}
            half4 frag(V v):SV_Target {
                UNITY_SETUP_INSTANCE_ID(v);
                float d=length(v.o);float a=atan2(v.o.y,v.o.x);
                float edge=1-smoothstep(.55+.12*sin(a*13),1,d);
                float grooves=step(.84,sin(a*11+d*18))*.4;
                float dots=step(.52,frac(v.o.x*21)*frac(v.o.y*21))*.15;
                half3 color=UNITY_ACCESS_INSTANCED_PROP(P10Marks,_MarkColor).rgb;
                if(_Metal>.5){float cracks=1-smoothstep(.03,.055,abs(sin(a*7+d*16+sin(d*11)*.5)));color=lerp(color,half3(.008,.006,.002),cracks*edge);}
                return half4(color,UNITY_ACCESS_INSTANCED_PROP(P10Marks,_MarkAlpha)*saturate(edge*(.72+grooves)-dots));
            }
            ENDHLSL
        }
    }
}
