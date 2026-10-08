Shader "Campus Rift/Enemy Comic Dissolve"
{
 Properties
 {
  _BaseMap("Texture",2D)="white"{} _BaseColor("Color",Color)=(1,1,1,1)
  _BumpMap("Normal",2D)="bump"{} _BumpScale("Normal Strength",Float)=1
  _EdgeColor("Element edge",Color)=(.5,1,.2,1) _EmissionColor("Rim",Color)=(0,0,0,1)
  _Dissolve("Dissolve",Range(0,1))=0
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  // Supplied meshes contain thin/inward-facing facets, especially after LOD collapse.
  // Back-face culling exposes the sky through them as bright speckles even at dissolve0.
  // Render both sides consistently in colour, depth and shadows, keeping the source/LODs intact.
  Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap); TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
  CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST,_BaseColor,_EdgeColor,_EmissionColor;float _Dissolve,_BumpScale;
  CBUFFER_END
  struct A {float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
  struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;float3 o:TEXCOORD4;UNITY_VERTEX_INPUT_INSTANCE_ID};
  V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);o.p=TransformObjectToHClip(i.p.xyz);o.w=TransformObjectToWorld(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.t=float4(TransformObjectToWorldDir(i.t.xyz),i.t.w*GetOddNegativeScale());o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.o=i.p.xyz;return o;}
  float noise(float3 p){float3 q=floor(p*22);return frac(sin(dot(q,float3(12.9898,78.233,37.719)))*43758.5453);}
  float cut(V i){if(_Dissolve<=0)return 1;float d=noise(i.o)-_Dissolve;clip(d);return d;}
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
   half4 frag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target{
    UNITY_SETUP_INSTANCE_ID(i);float d=cut(i);float3 n=normalize(i.n),t=normalize(i.t.xyz),b=cross(n,t)*i.t.w;
    n=normalize(mul(UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale),float3x3(t,b,n)))*IS_FRONT_VFACE(face,1.0,-1.0);
    Light l=GetMainLight(TransformWorldToShadowCoord(i.w));float diff=saturate(dot(n,l.direction));
    float3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
    float3 c=base*(max(SampleSH(n),float3(.13,.13,.17))+l.color*diff*l.shadowAttenuation)+_EmissionColor.rgb*pow(1-saturate(dot(n,normalize(GetWorldSpaceViewDir(i.w)))),2);
    if(_Dissolve>0 && d<.095)c=d<.025?float3(.018,.01,.025):_EdgeColor.rgb*2.8;
    return half4(c,1);
   }
   ENDHLSL
  }
  Pass {Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex shadowVert
   #pragma fragment shadow
   #pragma multi_compile_instancing
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   float3 _LightDirection;
   float3 _LightPosition;
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
   half4 normals(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target{cut(i);return half4(normalize(i.n)*IS_FRONT_VFACE(face,1.0,-1.0),0);}
   ENDHLSL
  }
 }
}
