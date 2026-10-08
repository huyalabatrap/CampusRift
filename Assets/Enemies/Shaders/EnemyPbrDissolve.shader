Shader "Campus Rift/Enemy PBR Dissolve"
{
 Properties
 {
  _BaseMap("Albedo",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1)
  _BumpMap("Normal",2D)="bump"{} _BumpScale("Normal strength",Float)=1
  _MetallicGlossMap("Metallic (R), Smoothness (A)",2D)="white"{}
  _Metallic("Metallic",Range(0,1))=1 _Smoothness("Smoothness",Range(0,1))=1
  _EmissionMap("Emissive mask",2D)="black"{} [HDR]_EmissionColor("Emissive",Color)=(0,0,0,1)
  [HDR]_RimColor("Element rim",Color)=(0,0,0,1) [HDR]_EdgeColor("Dissolve edge",Color)=(.5,.2,1,1)
  _Dissolve("Dissolve",Range(0,1))=0
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
  TEXTURE2D(_MetallicGlossMap);SAMPLER(sampler_MetallicGlossMap);TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
  CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST,_BaseColor,_EmissionColor,_RimColor,_EdgeColor;float _BumpScale,_Metallic,_Smoothness,_Dissolve;
  CBUFFER_END
  struct A {float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;float3 o:TEXCOORD4;UNITY_VERTEX_INPUT_INSTANCE_ID};
  V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);o.p=TransformObjectToHClip(i.p.xyz);o.w=TransformObjectToWorld(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.t=float4(TransformObjectToWorldDir(i.t.xyz),i.t.w*GetOddNegativeScale());o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.o=i.p.xyz;return o;}
  float cut(V i){if(_Dissolve<=0)return 1;float3 q=floor(i.o*22);float d=frac(sin(dot(q,float3(12.9898,78.233,37.719)))*43758.5453)-_Dissolve;clip(d);return d;}
  ENDHLSL
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile_fog
   half4 frag(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
   {
    UNITY_SETUP_INSTANCE_ID(i);float d=cut(i);float3 n=normalize(i.n),t=normalize(i.t.xyz),b=cross(n,t)*i.t.w;
    n=normalize(mul(UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale),float3x3(t,b,n)))*IS_FRONT_VFACE(face,1.0,-1.0);
    half4 ms=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,i.uv);
    SurfaceData surface=(SurfaceData)0;surface.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
    surface.metallic=ms.r*_Metallic;surface.smoothness=ms.a*_Smoothness;surface.normalTS=half3(0,0,1);surface.occlusion=1;surface.alpha=1;
    surface.emission=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).rgb*_EmissionColor.rgb+_RimColor.rgb*pow(1-saturate(dot(n,normalize(GetWorldSpaceViewDir(i.w)))),3);
    if(_Dissolve>0&&d<.095)surface.emission+=d<.025?float3(.018,.01,.025):_EdgeColor.rgb*2.8;
    InputData input=(InputData)0;input.positionWS=i.w;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);input.shadowCoord=TransformWorldToShadowCoord(i.w);input.bakedGI=max(SampleSH(n),half3(.04,.04,.05));input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);input.shadowMask=half4(1,1,1,1);
    half4 color=UniversalFragmentPBR(input,surface);color.rgb=MixFog(color.rgb,ComputeFogFactor(i.p.z));return color;
   }
   ENDHLSL
  }
  Pass {Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex shadowVert
   #pragma fragment shadow
   #pragma multi_compile_instancing
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   float3 _LightDirection;float3 _LightPosition;
   V shadowVert(A i){V o=vert(i);float3 direction=_LightDirection;
   #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    direction=normalize(_LightPosition-o.w);
   #endif
    o.p=TransformWorldToHClip(ApplyShadowBias(o.w,normalize(o.n),direction));
   #if UNITY_REVERSED_Z
    o.p.z=min(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
   #else
    o.p.z=max(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
   #endif
    return o;}
   half4 shadow(V i):SV_Target{cut(i);return 0;}
   ENDHLSL
  }
  Pass {Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment depth
   #pragma multi_compile_instancing
   half4 depth(V i):SV_Target{cut(i);return 0;}
   ENDHLSL
  }
  Pass {Tags {"LightMode"="DepthNormalsOnly"} ZWrite On
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment normals
   #pragma multi_compile_instancing
   half4 normals(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target{cut(i);return half4(normalize(i.n)*IS_FRONT_VFACE(face,1.0,-1.0),0);}
   ENDHLSL
  }
 }
}
