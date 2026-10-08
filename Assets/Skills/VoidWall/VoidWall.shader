Shader "Campus Rift/Void Wall"
{
    Properties
    {
        [HDR] _Edge ("Electric edge", Color) = (0.12,1.8,2.5,1)
        [HDR] _Violet ("Rift violet", Color) = (1.0,0.12,2.5,1)
        _Age("Age",Float)=0
        _Integrity("Integrity",Range(0,1))=1
        _Reveal("Reveal",Range(0,1))=1
        _Fade("Dissolve",Range(0,1))=0
        _Preview("Preview: 1 valid, -1 invalid",Float)=0
        _Hit("Impact UV and time",Vector)=(0.5,0.5,-100,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 positionOS:POSITION; float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            CBUFFER_START(UnityPerMaterial)
            float4 _Edge,_Violet,_Hit;
            float _Age,_Integrity,_Reveal,_Fade,_Preview;
            CBUFFER_END
            V vert(A i) { V o; i.positionOS.y *= max(0.015,_Reveal); o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            half4 frag(V i):SV_Target
            {
                float2 uv=i.uv;
                float2 p=(uv-0.5)*2;
                float edge=min(1-abs(p.x),1-abs(p.y));
                edge=min(edge, (1.85-abs(p.x)-abs(p.y))*0.7);
                clip(edge);
                float rim=1-smoothstep(0.004,0.025,edge);
                float halo=exp(-edge*32)*0.35;
                float scan=pow(0.5+0.5*sin(uv.y*65-_Age*2.3+sin(uv.x*12)*0.45),18)*0.014;
                float drift=sin(uv.x*13+uv.y*8-_Age*0.8)*sin(uv.y*17-uv.x*7+_Age*0.35);
                float crack=0;
                [unroll] for(int k=0;k<5;k++)
                {
                    float y=uv.y*7+k*1.37;
                    float segment=floor(y),t=frac(y);
                    float jitter=lerp(hash(float2(segment,k)),hash(float2(segment+1,k)),t);
                    float fissureDistance=abs(uv.x-(0.12+k*0.19+(jitter-0.5)*0.18+(uv.y-0.5)*(k%2==0 ? 0.15:-0.22)));
                    float enabled=saturate((1-_Integrity)*1.8-0.12-k*0.11);
                    crack+=exp(-fissureDistance*420)*enabled;
                }
                float fissureY=uv.y*9;
                float fracture=lerp(hash(float2(floor(fissureY),13)),hash(float2(floor(fissureY)+1,13)),frac(fissureY));
                float slash=abs(p.x*0.74-p.y*0.24+(fracture-0.5)*0.23);
                float seam=exp(-slash*190)*(0.45+0.08*sin(_Age*3));
                float facet=exp(-abs(p.x*0.65+p.y*0.7-0.35)*180)+exp(-abs(p.x*0.4-p.y*0.8+0.62)*180);
                float dt=_Age-_Hit.z;
                float ripple=exp(-abs(length((uv-_Hit.xy)*float2(1.2,1))-dt*1.8)*65)*saturate(1-dt*2.1)*step(0,dt);
                float instability=(1-_Integrity)*step(_Integrity,0.35)*(0.5+0.5*sin(_Age*37));
                float rune=step(0.8,abs(p.y))*step(0.92,abs(p.x))*step(0.65,frac(p.y*12+p.x*7));
                float3 hue=lerp(_Violet.rgb,_Edge.rgb,saturate(uv.y+drift*0.2));
                float3 col=float3(0.012,0.004,0.038)+hue*(rim+halo+scan+crack+seam+facet*0.08+ripple*2+instability*0.16);
                col+=float3(1.4,0.7,0.12)*rune*0.8;
                float alpha=0.94+rim*0.06;
                if(abs(_Preview)>0.5)
                {
                    hue=_Preview>0 ? float3(0.12,1.2,1.6) : float3(1.8,0.09,0.12);
                    col=hue*(0.25+rim+seam*0.4+scan*2);
                    alpha=0.13+rim*0.72+scan;
                    float cross=1-smoothstep(0.02,0.045,min(abs(p.x-p.y),abs(p.x+p.y)));
                    if(_Preview<0){col+=hue*cross;alpha+=cross*0.4;}
                }
                float dissolve=saturate((1-_Fade)*1.3-uv.y*0.22-drift*0.04);
                alpha*=smoothstep(0,0.12,dissolve)*saturate(1-_Fade);
                return half4(col,alpha);
            }
            ENDHLSL
        }
    }
}
