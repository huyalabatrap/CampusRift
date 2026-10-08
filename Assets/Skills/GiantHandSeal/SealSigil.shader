Shader "Campus Rift/Seal Sigil"
{
    Properties { _Tint("Tint",Color)=(1,0.65,0.15,1) _Opacity("Opacity",Range(0,1))=1 _Progress("Progress",Float)=0 _Mode("Mode",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off Offset -1,-1
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;float _Opacity,_Progress,_Mode;
            CBUFFER_END
            V vert(A i){V o;o.position=TransformObjectToHClip(i.vertex.xyz);o.uv=i.uv*2-1;return o;}
            float band(float r,float target,float width){return 1-smoothstep(width,width+0.009,abs(r-target));}
            half4 frag(V i):SV_Target
            {
                float r=length(i.uv),a=atan2(i.uv.y,i.uv.x),t=_Time.y;
                clip(1-r);
                float outer=band(r,0.94,0.008)+band(r,0.89,0.004);
                float ticks=step(0.86,cos(a*32))*step(0.75,r)*step(r,0.86);
                float inner=band(r,0.65,0.007)*step(0.05,sin(a*6+t*0.5));
                float angle=a+_Progress*0.2;
                float petals=band(r,0.47+0.09*cos(angle*6),0.007);
                float rays=step(0.992,abs(cos(angle*3)))*step(0.2,r)*step(r,0.68);
                float core=band(r,0.18,0.011)+band(r,0.24,0.004);
                float ink=saturate(outer+ticks+inner+petals+rays+core);
                float3 col=_Tint.rgb*(1.1+ink*0.5);
                float alpha=ink*0.9+(1-r)*0.055;
                if(_Mode>0.5 && _Mode<1.5)
                {
                    float spiral=pow(saturate(sin(a*5-r*22+t*2)),12)*smoothstep(0.2,0.88,r);
                    col=lerp(float3(0.022,0.008,0.065),_Tint.rgb*1.8,saturate(ink+spiral));
                    alpha=saturate(0.76+ink*0.2)*smoothstep(1,0.93,r);
                }
                if(_Mode>1.5 && _Mode<2.5)
                {
                    float crack=step(0.96,cos(a*9+sin(r*35)*0.28))*step(0.12,r)*step(r,0.83);
                    ink=saturate(crack+outer*0.7+petals*0.4);
                    alpha=ink;col=_Tint.rgb*1.8;
                }
                if(_Mode>2.5 && _Mode<3.5)
                {
                    float edge=0.84+sin(a*11+t*5)*0.012;
                    alpha=band(r,edge,0.018)*0.95+pow(saturate(1-abs(r-edge)/0.14),3)*0.32;
                    col=_Tint.rgb*2.2;
                }
                if(_Mode>3.5)
                {alpha=pow(saturate(1-r),2);col=_Tint.rgb*4;}
                clip(alpha*_Opacity-0.005);
                return half4(col,alpha*_Opacity);
            }
            ENDHLSL
        }
    }
}
