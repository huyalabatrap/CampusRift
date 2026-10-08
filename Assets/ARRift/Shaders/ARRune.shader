Shader "Campus Rift/AR/Runic Circle"
{
    Properties { _Silhouette("Faint placement silhouette", Range(0,1)) = 0 _Valid("Valid placement", Range(0,1)) = 1 }
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
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            float _Silhouette, _Valid;
            V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;return o;}
            float ring(float r,float target,float width){return 1-smoothstep(width,width+.006,abs(r-target));}
            half4 frag(V i):SV_Target
            {
                if(_Silhouette>.5) return half4(.35,.3,.48,(1-smoothstep(.85,1,length(i.uv*2-1)))*.16);
                float2 p=i.uv*2-1;float r=length(p),a=atan2(p.y,p.x)+_Time.y*.12;
                float outer=ring(r,.91,.012),inner=ring(r,.72,.008);
                float sector=frac(a/6.2831853*18);float glyph=step(.38,sector)*step(sector,.62)*step(.77,r)*step(r,.86);
                glyph=max(glyph,step(.26,sector)*step(sector,.74)*ring(r,.80,.007));
                float ink=ring(r,.91,.028)+ring(r,.72,.021);
                float glow=exp(-abs(r-.91)*65)*.5+exp(-abs(r-.72)*60)*.35;
                float strokes=saturate(outer+inner+glyph);float3 gold=float3(1,.65,.015),violet=float3(.5,.15,.9);
                float3 c=lerp(violet,gold,saturate(outer+glyph));c=lerp(float3(.012,.008,.025),c,saturate(strokes+glow*.6));
                float shadow=(1-smoothstep(.1,.68,r))*.10;
                c=lerp(float3(.4,.4,.4),c,_Valid);
                return half4(c,saturate(strokes+ink*.7+glow+shadow));
            }
            ENDHLSL
        }
    }
}
