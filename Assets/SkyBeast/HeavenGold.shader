Shader "Campus Rift/Heaven Sword Gold"
{
    Properties { _Alpha("Alpha",Range(0,1))=1 _Clock("Unscaled time",Float)=0 _Tint("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            CBUFFER_START(UnityPerMaterial) float _Alpha,_Clock;float4 _Tint; CBUFFER_END
            V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color;return o;}
            half4 frag(V i):SV_Target
            {
                float edge=abs(i.uv.x-.5)*2;
                half3 ink=half3(.095,.035,.008),gold=half3(1.9,1.02,.08),core=half3(3.6,2.9,.95);
                half3 col=lerp(gold,ink,smoothstep(.67,.83,edge));
                col=lerp(col,core,(1-smoothstep(.04,.22,edge))*.7);
                float rune=step(.79,frac(i.uv.y*24-_Clock*.6))*step(.14,edge)*step(edge,.4);
                col=lerp(col,core,rune);
                return half4(col*i.color.rgb*_Tint.rgb,_Alpha*i.color.a*_Tint.a);
            }
            ENDHLSL
        }
    }
}
