Shader "Campus Rift/P10 Comic Flipbook" {
 Properties { _MainTex("Atlas",2D)="white"{} _Intensity("Core HDR",Float)=1.8 _WhiteSmoke("White frost",Float)=0 }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float _Intensity,_WhiteSmoke;
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
 V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;v.c=a.c;return v;}
 half4 frag(V v):SV_Target{half4 t=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv);return half4(lerp(t.rgb,half3(1,1,1),_WhiteSmoke)*v.c.rgb*_Intensity,t.a*v.c.a);}
 ENDHLSL }
 }
}
