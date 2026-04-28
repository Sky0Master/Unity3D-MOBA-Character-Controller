Shader "Custom/EdgeDetection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EdgeWidth ("Edge Width", Range(0.0, 5.0)) = 1.0
        _EdgeColor ("Edge Color", Color) = (0, 0, 0, 1)
        _SensitivityDepth ("Sensitivity Depth", Float) = 1.5
        _SensitivityNormals ("Sensitivity Normals", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off ZTest Always

        Pass
        {
            Name "EdgeDetection"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            // 改名以避免与 Blit.hlsl 中的定义冲突
            struct AttributesEdge
            {
                uint vertexID : SV_VertexID;
            };

            struct VaryingsEdge
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
            };

            float _EdgeWidth;
            float4 _EdgeColor;
            float _SensitivityDepth;
            float _SensitivityNormals;
            //float4 _BlitTexture_TexelSize; // 声明纹理尺寸变量

            // 屏幕空间偏移数组 (Sobel 3x3)
            static float2 sobelSamplePoints[9] = {
                float2(-1, 1), float2(0, 1), float2(1, 1),
                float2(-1, 0), float2(0, 0), float2(1, 0),
                float2(-1, -1), float2(0, -1), float2(1, -1)
            };

            // Sobel 权重 X
            static float sobelX[9] = {
                -1, 0, 1,
                -2, 0, 2,
                -1, 0, 1
            };

            // Sobel 权重 Y
            static float sobelY[9] = {
                -1, -2, -1,
                 0,  0,  0,
                 1,  2,  1
            };

            VaryingsEdge vert (AttributesEdge input)
            {
                VaryingsEdge output;
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
                output.positionCS = pos;
                output.texcoord   = uv;
                return output;
            }

            half4 frag (VaryingsEdge input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 originalColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

                // 修复：使用 _BlitTexture_TexelSize 替代 _MainTex_TexelSize
                float2 texelSize = _BlitTexture_TexelSize.xy;

                // --- Sobel 深度检测 ---
                float depthGradientX = 0;
                float depthGradientY = 0;
                
                // --- Sobel 法线检测 ---
                float2 normalGradientX = 0;
                float2 normalGradientY = 0;

                // 采样中心深度，用于缩放灵敏度 (远处物体灵敏度低一些，近处高一些)
                float centerDepth = Linear01Depth(SampleSceneDepth(uv), _ZBufferParams);

                // 循环采样 3x3 区域
                for(int i = 0; i < 9; i++)
                {
                    float2 sampleUV = uv + sobelSamplePoints[i] * texelSize * _EdgeWidth;
                    
                    // 1. 深度
                    float d = Linear01Depth(SampleSceneDepth(sampleUV), _ZBufferParams);
                    depthGradientX += d * sobelX[i];
                    depthGradientY += d * sobelY[i];

                    // 2. 法线 (只检测 R和G 通道即可大致判断方向)
                    float3 n = SampleSceneNormals(sampleUV);
                    normalGradientX += n.xy * sobelX[i];
                    normalGradientY += n.xy * sobelY[i];
                }

                // 计算梯度的模 (Magnitude)
                float depthEdge = sqrt(depthGradientX * depthGradientX + depthGradientY * depthGradientY);
                float normalEdge = sqrt(dot(normalGradientX, normalGradientX) + dot(normalGradientY, normalGradientY));

                // 应用灵敏度
                // 深度边缘通常是非线性的，用 pow 让它更锐利，但 smoothstep 来做抗锯齿
                depthEdge = depthEdge * _SensitivityDepth * 100.0; // 缩放以便于控制
                normalEdge = normalEdge * _SensitivityNormals;

                // 组合边缘强度
                // 使用 max 混合，取深度或法线中最强的边缘
                float edge = max(depthEdge, normalEdge);

                // --- 核心：平滑抗锯齿 ---
                // 不使用 step(threshold, edge)，而是使用 smoothstep。
                // 这会生成 0.0 到 1.0 之间的灰度值，而不是非 0 即 1 的锯齿值。
                // 0.05 是下限，0.2 是上限，这之间的值会呈现渐变
                float edgeStrength = smoothstep(0.05, 0.2, edge);

                // 限制最大强度 (可选，如果想要非常黑的线就去掉)
                // edgeStrength = clamp(edgeStrength, 0.0, 1.0);

                // 混合颜色：根据边缘强度，在原色和边缘色之间插值
                return lerp(originalColor, _EdgeColor, edgeStrength);
            }
            ENDHLSL
        }
    }
}