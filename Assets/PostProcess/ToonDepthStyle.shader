Shader "Custom/URP/ToonDepthStyle"
{
    Properties
    {
        [Header(Toon Depth Banding)]
        _BandColor("Far Band Color", Color) = (0.4, 0.5, 0.8, 1)
        _BandSteps("Band Steps (Layer Count)", Float) = 5.0
        _BandInterval("Distance per Band (Meters)", Float) = 10.0
        _BandStrength("Band Color Strength", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ToonDepthPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);

                output.positionCS = pos;
                output.uv = uv;
                return output;
            }

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            
            // Banding Params
            float4 _BandColor;
            float _BandSteps;
            float _BandInterval;
            float _BandStrength;

            // 辅助函数：获取线性深度 (米)
            float GetLinearDepth(float2 uv)
            {
                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(uv);
                #else
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                #endif
                return LinearEyeDepth(depth, _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                float4 originalColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
                
                // --- 1. 计算线性深度 ---
                float centerDepth = GetLinearDepth(uv);

                // --- 2. 阶梯式深度染色 (Toon Depth Banding) ---
                // 将连续的深度值量化为整数层级
                // 例如：0-10m是第0层，10-20m是第1层...
                // 使用 max 避免除以 0
                float bandIndex = floor(centerDepth / max(_BandInterval, 1.0));
                
                // 限制最大层数，避免无限远处的层数过高
                bandIndex = min(bandIndex, _BandSteps);

                // 计算这一层的混合强度：层数越高，混合越多
                float bandFactor = saturate(bandIndex / _BandSteps);
                
                // 制造色块感：直接混合颜色，而不是平滑过渡
                float3 bandedColor = lerp(originalColor.rgb, _BandColor.rgb, bandFactor * _BandStrength);

                return half4(bandedColor, originalColor.a);
            }
            ENDHLSL
        }
    }
}