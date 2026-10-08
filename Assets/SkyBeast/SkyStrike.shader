Shader "Campus Rift/Sky Strike"
{
 Properties { _Feather("Feather",Float)=0 _Opacity("Opacity",Float)=1 _Charred("Charred",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial) float _Feather; float _Opacity; float _Charred; CBUFFER_END
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;float3 normalOS:NORMAL;};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2;};
   V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.normal=TransformObjectToWorldNormal(i.normalOS);o.world=TransformObjectToWorld(i.positionOS.xyz);return o;}
   half4 frag(V i):SV_Target
   {
    float3 dark=float3(.09,.014,.006),hot=float3(3.5,.72,.025),core=float3(5,3,.7);
    if(_Feather>.5){float edge=abs(i.uv.x-.5)*2;float barb=sin(i.uv.y*105-edge*24);float shaft=1-smoothstep(.035,.13,edge);float3 col=lerp(float3(1.8,.25,.015),float3(3.2,1.4,.12),saturate(barb*.5+.5));col=lerp(col,core,shaft);col=lerp(col,dark,smoothstep(.72,.97,edge));float alpha=(1-smoothstep(.86,1,edge+max(0,barb)*.10))*smoothstep(0,.05,i.uv.y);return half4(col,alpha*_Opacity*smoothstep(1.2,4,distance(i.world,_WorldSpaceCameraPos)));}
    float crack=pow(saturate(sin(i.world.x*5+i.world.z*4)*sin(i.world.y*8-_Time.y*2)),5);
    float light=.3+.7*saturate(dot(normalize(i.normal),normalize(float3(.3,1,.2))));float3 rock=lerp(dark*light,hot,crack*.9)+float3(.16,.035,.01);rock=lerp(rock,float3(.003,.002,.001)+float3(.28,.028,.002)*crack*.15,_Charred);return half4(rock,_Opacity);
   }
   ENDHLSL
  }
 }
}
