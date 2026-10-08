Shader "Campus Rift/Comic Ink"
{
    Properties { _InkStrength("Ink strength", Range(0,1)) = 0.88 _Saturation("Saturation", Range(1,1.3)) = 1.12 _Contrast("Contrast", Range(1,1.15)) = 1.035 }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Comic Ink"
            ZTest Always ZWrite Off Cull Off
            Stencil { Ref 8 ReadMask 8 Comp NotEqual Pass Keep }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            float _InkStrength, _Saturation, _Contrast;
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float thickness = 2.0 * _ScreenParams.y / 1080.0;
                float2 dx = float2(thickness / _ScreenParams.x,0), dy = float2(0,thickness / _ScreenParams.y);
                float raw = SampleSceneDepth(uv);
                float depth = LinearEyeDepth(raw,_ZBufferParams);
                float right = LinearEyeDepth(SampleSceneDepth(uv+dx),_ZBufferParams);
                float up = LinearEyeDepth(SampleSceneDepth(uv+dy),_ZBufferParams);
                float depthEdge = max(abs(depth-right),abs(depth-up)) / max(depth,1.0);
                float deviceDepth = raw;
                #if !UNITY_REVERSED_Z
                    deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE,1.0,raw);
                #endif
                float3 position = ComputeWorldSpacePosition(uv,deviceDepth,UNITY_MATRIX_I_VP);
                float3 faceNormal = cross(ddx(position),ddy(position));
                float3 normal = faceNormal / max(length(faceNormal),0.00001);
                float normalEdge = max(length(ddx(normal)),length(ddy(normal))) * thickness;
                normalEdge *= depth < _ProjectionParams.z * .99 ? 1.0 : 0.0;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv);
                half3 rightColor = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+dx).rgb;
                half3 upColor = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+dy).rgb;
                float colorEdge = max(length(color.rgb-rightColor),length(color.rgb-upColor));
                // Colour catches material/lighting seams; depth catches silhouettes. Avoid a costly
                // additional normals prepass over the entire campus, especially its alpha foliage.
                // Derivatives of reconstructed depth normals create speckles on dense skinned meshes.
                // Depth silhouettes and colour seams already supply the comic ink without that unstable term.
                float edge = max(smoothstep(0.018,0.055,depthEdge),smoothstep(0.16,0.42,colorEdge));
                float luma = dot(color.rgb,float3(.2126,.7152,.0722));
                color.rgb = max(0,lerp(luma.xxx,color.rgb,_Saturation));
                color.rgb = saturate((color.rgb-.18)*_Contrast+.18);
                color.rgb = lerp(color.rgb,float3(.007,.010,.018),edge*_InkStrength);
                return color;
            }
            ENDHLSL
        }
    }
}
