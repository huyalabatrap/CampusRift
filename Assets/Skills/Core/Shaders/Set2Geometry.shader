Shader "Campus Rift/P18 Comic Geometry"
{
    Properties { _Intensity("Emission", Float)=1 _ZTest("Depth", Float)=4 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off ZTest [_ZTest]
            Stencil { Ref 8 Comp Always Pass Replace WriteMask 8 }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input {float4 vertex:POSITION;float4 color:COLOR;};
            struct Output {float4 position:SV_POSITION;float4 color:COLOR;};
            float _Intensity;
            Output vert(Input v){Output o;o.position=TransformObjectToHClip(v.vertex.xyz);o.color=v.color;return o;}
            half4 frag(Output i):SV_Target {return half4(i.color.rgb*_Intensity,i.color.a);}
            ENDHLSL
        }
    }
}
