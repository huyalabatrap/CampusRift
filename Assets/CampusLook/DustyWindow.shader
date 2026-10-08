Shader "Campus Rift/Dusty Window"
{
    Properties{_BaseMap("Dust",2D)="white"{} _BaseColor("Cool dusty glass",Color)=(.4,.52,.55,.24) _WorldScale("World detail",Float)=.4}
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial) float4 _BaseColor;float _WorldScale; CBUFFER_END
            struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;};
            V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.world=TransformObjectToWorld(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);return o;}
            half4 frag(V i):SV_Target
            {
                float2 uv=abs(i.n.x)>abs(i.n.z)?i.world.zy:i.world.xy;half3 dust=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv*_WorldScale).rgb;
                float fresnel=pow(1-saturate(abs(dot(normalize(i.n),normalize(GetWorldSpaceViewDir(i.world))))),4);
                return half4(_BaseColor.rgb*(.75+dust*.5)+fresnel*.16,saturate(_BaseColor.a+fresnel*.22));
            }
            ENDHLSL
        }
    }
}
