Shader "Campus Rift/P10 Comic Energy"
{
    Properties {
        _InkMask("Authored energy stencil",Float)=0
        _BaseColor("Color",Color)=(1,.6,.1,1)
        [HDR] _Emission("Rim",Color)=(2,1,.2,1)
        _Alpha("Opacity",Range(0,1))=.8
        _Crack("Absorbed fraction",Range(0,1))=0
        _Progress("Charge",Range(0,1))=1
        _Distortion("PC refraction",Float)=0
        _FlatGlow("Sword core",Float)=0
        _OpaqueCore("Black hole core",Float)=0
        _ImpactRim("Impact ring",Float)=0
        _BellMetal("Transparent gold metal",Float)=0
        _LotusGlow("Petal ink and charge core",Float)=0
        _ParticleTint("Particle vertex color",Float)=0
    }
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Stencil { Ref [_InkMask] WriteMask [_InkMask] Comp Always Pass Replace }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor,_Emission;float _Alpha,_Crack,_Progress,_Distortion,_FlatGlow,_OpaqueCore,_ImpactRim,_BellMetal,_LotusGlow,_ParticleTint;
            CBUFFER_END
            struct A {float4 p:POSITION;float3 n:NORMAL;half4 c:COLOR;float2 uv:TEXCOORD0;};
            struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float3 o:TEXCOORD2;half4 c:TEXCOORD3;float2 uv:TEXCOORD4;};
            V vert(A a){V v;v.w=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(v.w);v.n=TransformObjectToWorldNormal(a.n);v.o=a.p.xyz;v.c=a.c;v.uv=a.uv;return v;}
            half4 frag(V v):SV_Target {
                float rim=pow(1-saturate(abs(dot(normalize(v.n),GetWorldSpaceNormalizeViewDir(v.w)))),2.5);
                float light=saturate(dot(normalize(v.n),normalize(float3(.4,1,.3))));
                float crack=(1-step(.045,abs(sin(v.o.y*7+v.o.x*14+sin(v.o.z*13+v.o.y*18)*.9))))*_Crack;
                float halftone=step(.82,frac(v.p.x*.22)*frac(v.p.y*.22))*.06;
                half3 color=_BaseColor.rgb*(.32+.68*step(.45,light)) + _Emission.rgb*(rim+_FlatGlow*.5)+crack*_Emission.rgb*2;
                if(_FlatGlow>0)color=lerp(color,lerp(half3(3.1,1.9,.10),half3(4.5,3.9,2.1),step(.8,v.c.g)*step(.5,v.o.z)),saturate(_FlatGlow));
                if(_FlatGlow>0&&v.o.z<-.2)color=half3(.9,.38,.018);
                if(_ParticleTint>0)color*=v.c.rgb;
                if(_LotusGlow>0){float edge=smoothstep(0,.12,min(v.uv.x,1-v.uv.x));float vein=1-smoothstep(.009,.023,abs(v.uv.x-.5));color=lerp(half3(.13,.008,.003),half3(1.6,.3,.014)+half3(2.5,1.9,.2)*pow(1-v.uv.y,3)*_Progress,edge);color+=vein*half3(.55,.19,.008)*sin(v.uv.y*3.14);}
                if(_Distortion>0){float2 uv=GetNormalizedScreenSpaceUV(v.p);color=lerp(color,SampleSceneColor(uv+normalize(v.n).xy*_Distortion),_OpaqueCore>0?rim*.08:.12);}
                color+=_BellMetal*pow(saturate(dot(reflect(-GetWorldSpaceNormalizeViewDir(v.w),normalize(v.n)),normalize(float3(.4,1,.3)))),24)*half3(3,2,.7);
                float opacity=lerp(.6+.4*rim,1,_OpaqueCore);
                opacity=lerp(opacity,.05+.85*pow(rim,.6),_ImpactRim);
                opacity=lerp(opacity,.06+.94*rim,_BellMetal);
                float alpha=_Alpha*opacity*lerp(1,v.c.a,_ParticleTint);clip(alpha-.025);
                return half4(color-halftone,alpha);
            }
            ENDHLSL
        }
    }
}
