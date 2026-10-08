Shader "Campus Rift/Real Campus Sky"
{
    Properties
    {
        _MainTex("CC0 photographed sky",2D)="white"{}
        _Tint("Sky tint",Color)=(.5,.5,.5,1)
        _Exposure("Exposure",Range(0,8))=1
        _Rotation("Rotation",Range(0,360))=0
        _Celestial("0 none, 1 moon, 2 blood moon, 3 eclipse",Float)=0
        [HideInInspector] _StormTex("Fire storm sky",2D)="white"{}
        [HideInInspector] _FireStorm("Fire storm blend",Range(0,1))=0
        [HideInInspector] _DragonFury("Dragon fury sky",Range(0,1))=0
    }
    SubShader
    {
        Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"}
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            TEXTURE2D(_StormTex);SAMPLER(sampler_StormTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;float _Exposure,_Rotation,_Celestial,_FireStorm,_DragonFury;
            CBUFFER_END
            struct A {float4 p:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 p:SV_POSITION;float3 dir:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.p=TransformObjectToHClip(i.p.xyz);o.dir=i.p.xyz;return o;}
            half4 Frag(V i):SV_Target
            {
                float3 dir=normalize(i.dir);float a=radians(_Rotation);float2 xz=mul(float2x2(cos(a),-sin(a),sin(a),cos(a)),dir.xz);
                float2 uv=float2(atan2(xz.x,xz.y)/(2*PI)+.5,acos(clamp(dir.y,-1,1))/PI);
                half3 col=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb*_Tint.rgb*2*_Exposure;
                // Blend the actual inferno cloud image during Warning; the level preset stays intact.
                half3 storm=SAMPLE_TEXTURE2D(_StormTex,sampler_StormTex,uv).rgb*half3(1.65,.23,.09)*_Exposure;
                col=lerp(col,storm,saturate(_FireStorm)*.92);
                if(_Celestial>.5)
                {
                    float3 center=normalize(float3(.16,.34,1));float angle=acos(clamp(dot(dir,center),-1,1));
                    float disc=1-smoothstep(.074,.079,angle);
                    if(_Celestial>2.5)
                    {
                        float corona=exp(-abs(angle-.08)*115)*.8;
                        col+=half3(1,.56,.12)*corona;col=lerp(col,half3(.004,.003,.003),disc);
                    }
                    else
                    {
                        float crater=.82+.12*sin(dir.x*310)*sin(dir.y*245)+.06*sin(dir.x*690+dir.y*430);
                        float limb=sqrt(saturate(1-pow(angle/.079,2)));
                        half3 moon=_Celestial>1.5?half3(.73,.12,.045):half3(.7,.73,.79);
                        col+=moon*crater*lerp(.35,1,limb)*disc;
                        col+=moon*.12*exp(-angle*24);
                    }
                }
                col*=lerp(half3(1,1,1),half3(.65,.12,.08),saturate(_DragonFury));
                return half4(col,1);
            }
            ENDHLSL
        }
    }
}
