Shader "Campus Rift/P13 Meteor Batch"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _CampusReduceFlashes;
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float2 seed:TEXCOORD1;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float seed:TEXCOORD2;};
            V vert(A a){V v;v.p=TransformWorldToHClip(a.p.xyz);v.world=a.p.xyz;v.uv=a.uv;v.seed=a.seed.x;return v;}
            half4 frag(V v):SV_Target
            {
                float time=_Time.y; float _comfort=1-saturate(_CampusReduceFlashes);
                half flicker=.92+.08*sin(time*17+v.seed)*_comfort;
                half3 rgb;half alpha;
                if(v.uv.y<0)
                {
                    half2 radialUv=half2(v.uv.x*2-1,(v.uv.y+1.5)*2);
                    half radius=length(radialUv);
                    radius*=1+.045*sin(radialUv.x*13+radialUv.y*17-time*21+v.seed);
                    half hot=1-smoothstep(.12,.34,radius);
                    half gold=1-smoothstep(.30,.63,radius);
                    half halo=1-smoothstep(.48,1,radius);
                    rgb=(half3(2.4,.20,.015)+half3(1.3,1.1,.12)*gold+half3(2.2,2.1,1.6)*hot)*flicker;
                    // Small bright centre with a soft halo, without a hard cap perimeter.
                    alpha=saturate(hot+gold*.75+halo*.26)*(1-smoothstep(.88,1,radius));
                }
                else
                {
                    half y=saturate(v.uv.y);
                    half flow=sin(y*37-time*19+v.seed)*sin(y*17+time*7+v.seed);
                    half cross=abs(v.uv.x*2-1+flow*.075*y);
                    half grain=.78+.22*sin(y*83-time*27+v.seed+v.uv.x*11);
                    half core=(1-smoothstep(.12,.34,cross))*(1-smoothstep(.03,.48,y));
                    half3 gold=half3(3.1,1.5,.18),orange=half3(2.3,.25,.018),red=half3(.40,.018,.004);
                    rgb=lerp(gold,orange,smoothstep(0,.36,y));
                    rgb=lerp(rgb,red,smoothstep(.34,1,y));
                    rgb=rgb*grain+half3(2.0,1.6,.9)*core;
                    // A thin charcoal edge keeps the comic outline readable.
                    rgb=lerp(rgb,half3(.11,.016,.007),smoothstep(.84,.93,cross));
                    half edge=1-smoothstep(.92,1,cross);
                    alpha=edge*(1-smoothstep(.30,1,y))*(.8+.2*grain)*flicker;
                }
                // Nearby fragments fade as well as the pooled geometry shrinking inside 8 m.
                alpha*=smoothstep(1.5,8,distance(v.world,_WorldSpaceCameraPos));
                return half4(rgb,alpha);
            }
            ENDHLSL
        }
    }
}
