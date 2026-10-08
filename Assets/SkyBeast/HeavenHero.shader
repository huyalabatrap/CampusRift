Shader "Campus Rift/Heaven Sword Hero"
{
    Properties { _Alpha("Alpha",Range(0,1))=1 _Clock("Unscaled time",Float)=0 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Name "Faceted HDR gold"
            Cull Back ZWrite On Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 c:COLOR;};
            struct V {float4 p:SV_POSITION;float3 n:TEXCOORD0;float3 world:TEXCOORD1;float2 uv:TEXCOORD2;float4 c:COLOR;};
            CBUFFER_START(UnityPerMaterial) float _Alpha,_Clock; CBUFFER_END
            V vert(A a){V o;o.world=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;o.c=a.c;return o;}
            half4 frag(V i):SV_Target
            {
                float3 n=normalize(i.n),v=normalize(GetWorldSpaceViewDir(i.world));
                float face=saturate(dot(n,normalize(float3(-.7,.5,-.8))));
                float glint=pow(saturate(dot(reflect(-normalize(float3(-.4,.8,-.5)),n),v)),24);
                float rim=pow(1-saturate(dot(n,v)),3);
                half3 gold=lerp(half3(.9,.28,.018),half3(3.6,2.15,.24),face);
                gold+=half3(4.8,3.9,1.8)*(glint*.9+rim*.28);
                return half4(gold*i.c.rgb,_Alpha*i.c.a);
            }
            ENDHLSL
        }
    }
}
