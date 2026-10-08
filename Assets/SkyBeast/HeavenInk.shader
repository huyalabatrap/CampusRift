Shader "Campus Rift/Heaven Sword Ink"
{
    Properties { _Alpha("Alpha",Range(0,1))=1 _Clock("Unscaled time",Float)=0 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-1" "RenderType"="Transparent"}
        Pass
        {
            Cull Front ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 p:POSITION;float3 n:NORMAL;};struct V {float4 p:SV_POSITION;};
            CBUFFER_START(UnityPerMaterial) float _Alpha,_Clock; CBUFFER_END
            V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz+a.n*.9);return o;}
            half4 frag(V i):SV_Target{return half4(.085,.025,.005,_Alpha);}
            ENDHLSL
        }
    }
}
