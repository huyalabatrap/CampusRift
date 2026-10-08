Shader "Campus Rift/P10 Rolling Fire Billow" {
 Properties { _MainTex("Rolling smoke atlas",2D)="white"{} }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+3"} Pass {
 Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Stencil { Ref 8 WriteMask 8 Comp Always Pass Replace }
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
 V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;v.c=a.c;return v;}
 half4 frag(V v):SV_Target {
     half4 puff=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv);
     float2 q=frac(v.uv*4)-.5;
     float roll=sin(q.x*15+sin(q.y*18-_Time.y*8)*1.5)*sin(q.y*17+_Time.y*6);
     float hot=saturate(1-length(q)*2.3+roll*.2);
     half3 rgb=lerp(half3(.20,.024,.003),half3(3.2,1.15,.025),smoothstep(.05,.65,hot));
     rgb=lerp(rgb,half3(4.2,3.6,1.7),smoothstep(.65,.98,hot));
     float alpha=puff.a*v.c.a;clip(alpha-.025);return half4(rgb,alpha);
 }
 ENDHLSL
 } }
}
