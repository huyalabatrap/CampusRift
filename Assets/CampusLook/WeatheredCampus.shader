Shader "Campus Rift/Weathered URP Lit"
{
    Properties
    {
        _BaseMap("Photographed base color",2D)="white"{}
        _BumpMap("Normal (OpenGL)",2D)="bump"{}
        _MaskMap("AO Roughness Metallic",2D)="white"{}
        _BaseColor("Faded paint tint",Color)=(1,1,1,1)
        _WorldScale("Repeats per metre",Float)=0.5
        _NormalScale("Normal strength",Range(0,2))=0.5
        _Metallic("Metallic",Range(0,1))=0
        _Smoothness("Smoothness multiplier",Range(0,1))=0.6
        _Age("Weathering",Range(0,1))=0.3
        _Wall("Wall rain/moss",Float)=0
        _Decals("Sparse weathered notice stamps",Float)=0
        [HideInInspector] _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "UniversalMaterialType"="Lit" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float _WorldScale,_NormalScale,_Metallic,_Smoothness,_Age,_Wall,_Decals,_Cull;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);
        float _CampusLowQuality;
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 lightmap:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half fog:TEXCOORD2;DECLARE_LIGHTMAP_OR_SH(lightmapUV,vertexSH,3);UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes i)
            {
                Varyings o=(Varyings)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;
                o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.fog=ComputeFogFactor(p.positionCS.z);
                OUTPUT_LIGHTMAP_UV(i.lightmap,unity_LightmapST,o.lightmapUV);OUTPUT_SH(o.normalWS,o.vertexSH);return o;
            }
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 geom=normalize(i.normalWS);float3 p=i.positionWS*_WorldScale;
                half3 weights=pow(abs(geom),8);weights/=max(dot(weights,1),0.001);
                float2 uvX=p.zy,uvY=p.xz,uvZ=p.xy;
                half3 base,mask;half3 n=geom;
                if(_CampusLowQuality>.5)
                {
                    float2 uv=weights.x>weights.y&&weights.x>weights.z?uvX:weights.y>weights.z?uvY:uvZ;
                    base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uv).rgb;
                }
                else
                {
                    base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uvX).rgb*weights.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uvY).rgb*weights.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uvZ).rgb*weights.z;
                    mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uvX).rgb*weights.x+SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uvY).rgb*weights.y+SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uvZ).rgb*weights.z;
                    half3 nx=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uvX));half3 ny=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uvY));half3 nz=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uvZ));
                    n=normalize(geom+_NormalScale*(half3(0,nx.y,nx.x)*weights.x+half3(ny.x,0,ny.y)*weights.y+half3(nz.x,nz.y,0)*weights.z));
                }
                float macro=noise(i.positionWS.xz*.09+i.positionWS.y*.012);
                float streak=noise(float2((i.positionWS.x+i.positionWS.z)*1.7,i.positionWS.y*.06));
                float moss=saturate(1-i.positionWS.y*.85)*_Wall;
                float rain=smoothstep(.35,.83,streak)*_Wall*(.55+.45*saturate(frac(i.positionWS.y*.25)*3));
                float damp=smoothstep(.48,.78,noise(i.positionWS.xz*.21+i.positionWS.y*.16))*_Wall;
                base*=_BaseColor.rgb*lerp(.88,1.08,macro);
                base=lerp(base,base*half3(.43,.40,.31),_Age*(.16*macro+.7*rain+.35*damp));
                base=lerp(base,base*half3(.36,.47,.26),moss*_Age*.8);
                // Painted skirting and a narrow floor seam break up broad wall planes.
                float wallFacing=1-smoothstep(.1,.35,abs(geom.y));
                float floorHeight=frac(max(0,i.positionWS.y)/4.2)*4.2;
                float skirt=(1-smoothstep(.68,.84,floorHeight))*_Wall*wallFacing;
                float seam=(1-smoothstep(.025,.07,abs(floorHeight-3.95)))*_Wall*wallFacing;
                base=lerp(base,base*half3(.64,.67,.63),skirt*.65);
                base=lerp(base,base*.58,seam*.55);
                // In-shader projections: no decal objects, mesh, collider or extra draw passes.
                if(_CampusLowQuality<.5&&_Decals>.5&&abs(geom.y)<.25)
                {
                    float2 wall=float2(i.positionWS.x+i.positionWS.z,i.positionWS.y);
                    float2 cell=floor(wall/float2(11,4));float2 local=frac(wall/float2(11,4))*float2(11,4);
                    float notice=step(.87,hash(cell))*step(1.15,local.x)*step(local.x,1.58)*step(1.35,local.y)*step(local.y,1.95);
                    float torn=step(.26,noise(wall*31));float lines=step(.5,frac(local.y*32))*step(1.23,local.x)*step(local.x,1.50);
                    base=lerp(base,lerp(half3(.62,.58,.43),half3(.25,.27,.25),lines*.7),notice*torn*.72);
                }
                SurfaceData surface=(SurfaceData)0;surface.albedo=base;surface.alpha=1;surface.metallic=_Metallic;
                surface.specular=.5;surface.smoothness=saturate((1-mask.g)*_Smoothness);surface.occlusion=lerp(1,mask.r,.8);surface.normalTS=half3(0,0,1);
                InputData data=(InputData)0;data.positionWS=i.positionWS;data.normalWS=n;data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);data.fogCoord=i.fog;data.vertexLighting=VertexLighting(i.positionWS,n);
                data.bakedGI=SAMPLE_GI(i.lightmapUV,i.vertexSH,n);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=SAMPLE_SHADOWMASK(i.lightmapUV);
                half4 col=UniversalFragmentPBR(data,surface);col.rgb=MixFog(col.rgb,i.fog);return col;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection,_LightPosition;
            struct A {float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            float4 ShadowVert(A i):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(i);float3 p=TransformObjectToWorld(i.p.xyz),n=TransformObjectToWorldNormal(i.n);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 dir=normalize(_LightPosition-p);
                #else
                float3 dir=_LightDirection;
                #endif
                float4 c=TransformWorldToHClip(ApplyShadowBias(p,n,dir));
                #if UNITY_REVERSED_Z
                c.z=min(c.z,UNITY_NEAR_CLIP_VALUE*c.w);
                #else
                c.z=max(c.z,UNITY_NEAR_CLIP_VALUE*c.w);
                #endif
                return c;
            }
            half4 DepthFrag():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags {"LightMode"="DepthNormals"} ZWrite On Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex NormalVert
            #pragma fragment NormalFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            struct NA {float4 p:POSITION;float3 n:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct NV {float4 p:SV_POSITION;half3 n:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
            NV NormalVert(NA i)
            {
                NV o=(NV)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.p=TransformObjectToHClip(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);return o;
            }
            half4 NormalFrag(NV i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n=normalize(i.n);
                #if defined(_GBUFFER_NORMALS_OCT)
                float2 oct=saturate(PackNormalOctQuadEncode(n)*.5+.5);
                return half4(PackFloat2To888(oct),0);
                #else
                return half4(n,0);
                #endif
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            struct A {float4 p:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
            float4 DepthVert(A i):SV_POSITION{UNITY_SETUP_INSTANCE_ID(i);return TransformObjectToHClip(i.p.xyz);}
            half4 DepthFrag():SV_Target{return 0;}
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
