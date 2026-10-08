Shader "Campus Rift/Cinematic Mist"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+6" "RenderType"="Transparent" }
        Pass
        {
            Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 p:POSITION; float4 c:COLOR; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float4 c:COLOR; };
            V vert(A a) { V o; o.world=TransformObjectToWorld(a.p.xyz); o.p=TransformWorldToHClip(o.world); o.c=a.c; return o; }
            half4 frag(V i):SV_Target
            {
                float cloud=.5+.5*sin(i.world.x*.035+sin(i.world.z*.041))*sin(i.world.z*.026);
                float fade=1-smoothstep(500,670,distance(i.world,_WorldSpaceCameraPos));
                return half4(i.c.rgb*lerp(.85,1.15,cloud),i.c.a*fade);
            }
            ENDHLSL
        }
    }
}
