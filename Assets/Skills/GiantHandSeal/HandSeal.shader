Shader "Campus Rift/Giant Hand Seal"
{
    Properties { _Tint("Tint",Color)=(1,0.58,0.12,1) _Reveal("Reveal",Range(0,1))=1 _Dissolve("Dissolve",Range(0,1))=0 }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 vertex:POSITION;float3 normal:NORMAL;};
            struct V{float4 position:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float3 local:TEXCOORD2;};
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;float _Reveal,_Dissolve;
            CBUFFER_END
            V vert(A i){V o;o.position=TransformObjectToHClip(i.vertex.xyz);o.world=TransformObjectToWorld(i.vertex.xyz);o.normal=TransformObjectToWorldNormal(i.normal);o.local=i.vertex.xyz;return o;}
            half4 frag(V i):SV_Target
            {
                float noise=frac(sin(dot(floor(i.local*18),float3(13.1,71.7,31.9)))*43758.5453);
                float reveal=(_Reveal*5.5-2.5)-i.local.z+noise*0.18;
                clip(reveal);clip(noise-_Dissolve);
                float3 n=normalize(i.normal),v=normalize(GetWorldSpaceViewDir(i.world));
                float rim=pow(1-saturate(dot(n,v)),2.4);
                float light=saturate(dot(n,normalize(float3(-0.4,0.9,-0.25))));
                float seam=pow(saturate(1-abs(sin((i.local.x+i.local.z*0.57)*19+i.local.y*31))),18);
                float3 col=_Tint.rgb*(0.22+light*0.85)+rim*float3(1.5,0.8,0.22)+seam*float3(0.06,0.65,0.85);
                col+=step(reveal,0.12)*float3(1.4,0.5,1.8)+step(noise-_Dissolve,0.075)*_Dissolve*float3(1.5,0.9,0.3);
                return half4(col,1);
            }
            ENDHLSL
        }
    }
}
