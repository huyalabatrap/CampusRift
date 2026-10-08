Shader "Campus Rift/P10 Layered Stroke"
{
    Properties { _InkMask("Authored energy stencil",Float)=0 _Core("White core fraction",Range(0,1))=.54 _Glow("Energy fraction",Range(0,1))=.784 _Lightning("Soft electric aura",Float)=0 [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Depth test",Float)=4 }
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-2" "RenderType"="Transparent"}
        Pass {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Stencil { Ref [_InkMask] WriteMask [_InkMask] Comp Always Pass Replace }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half _Core, _Glow, _Lightning;
            CBUFFER_END
            struct A {float4 p:POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;};
            struct V {float4 p:SV_POSITION;half4 c:COLOR;float2 uv:TEXCOORD0;};
            V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.c=a.c;v.uv=a.uv;return v;}
            half4 frag(V v):SV_Target {
                clip(v.c.a-.025);
                half d=abs(v.uv.y*2-1);
                half3 ink=v.c.b>v.c.r*.8 ? (v.c.r>v.c.g?half3(.055,.007,.1):half3(.008,.035,.09)) : (v.c.g<v.c.r*.5?half3(.16,.015,.006):half3(.09,.012,.12));
                // LineRenderer vertex colours arrive in linear space: metal blue .2 becomes .033.
                if(v.c.r>.8&&v.c.g>.35&&v.c.b>.02)ink=half3(.12,.055,.012);
                half3 rgb=d<_Core?half3(2,2,2):d<_Glow?v.c.rgb*1.8:ink;
                if(_Lightning>.5){
                    // A fine filament with translucent energy avoids an opaque flat ribbon.
                    half filament=1-smoothstep(_Core*.45,_Core,d);
                    half gold=exp(-d*d*8);
                    half purple=exp(-pow((d-.5)*3,2));
                    rgb=lerp(half3(.045,.003,.11)+half3(.28,.009,.65)*purple,v.c.rgb*2.2,gold);
                    rgb=lerp(rgb,half3(3,2.8,2.2),filament);
                    half alpha=max(filament,max(gold*.95,purple*.65))*(1-smoothstep(.82,1,d));
                    return half4(rgb,v.c.a*alpha);
                }
                return half4(rgb,v.c.a*saturate((1-d)*80));
            }
            ENDHLSL
        }
    }
}
