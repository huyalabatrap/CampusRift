Shader "Campus Rift/P10 Ink Hull"
{
    Properties {_Color("Ink",Color)=(.012,.008,.028,1) _Alpha("Opacity",Range(0,1))=1 _Width("Ink width",Float)=.014}
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-1"}
        Pass {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Cull Front
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;half _Alpha;float _Width;
            CBUFFER_END
            struct A{float4 p:POSITION;float3 n:NORMAL;};
            float4 vert(A a):SV_POSITION{return TransformObjectToHClip(a.p.xyz+a.n*_Width);}
            half4 frag():SV_Target{return half4(_Color.rgb,_Alpha);}
            ENDHLSL
        }
    }
}
