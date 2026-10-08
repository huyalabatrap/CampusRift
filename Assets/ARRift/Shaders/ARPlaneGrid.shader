Shader "Campus Rift/AR/Plane Grid"
{
    Properties { _Color("Color", Color) = (0.63,0.4,0.85,0.2) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            V vert(A i) { V o; o.world=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); return o; }
            half4 frag(V i):SV_Target
            {
                float2 grid=abs(frac(i.world.xz*10-.5)-.5)/max(fwidth(i.world.xz*10),.001);
                half gridAlpha=1-saturate(min(grid.x,grid.y));
                return half4(_Color.rgb,_Color.a*(.1+.9*gridAlpha));
            }
            ENDHLSL
        }
    }
}
