Shader "Custom/VertexColorPixelatedURP"
{
    Properties
    {
        _Brightness("Brightness", Range(0, 5)) = 1.0
        _Saturation("Saturation", Range(0, 2)) = 1.0
        _PixelSize("Pixel Size (Screen Pixels)", Float) = 8.0
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _Brightness;
            float _Saturation;
            float _PixelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 获取正确的屏幕UV坐标
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 screenSize = _ScreenParams.xy;

                // 计算像素化后的UV坐标
                float2 pixelatedUV = floor(screenUV * screenSize / _PixelSize) * _PixelSize / screenSize;

                // 使用原始顶点色（如果要基于UV采样纹理，这里需要修改）
                float3 color = input.color.rgb;

                // 根据像素大小调整量化级别（更大的像素块使用更粗糙的量化）
                float quantizationLevels = max(1, 8.0 / (_PixelSize * 0.125));
                float3 quantizedColor = floor(color * quantizationLevels) / quantizationLevels;

                // 加入亮度 & 饱和度调整
                float luminance = dot(quantizedColor, float3(0.299, 0.587, 0.114));
                float3 saturatedColor = lerp(float3(luminance, luminance, luminance), quantizedColor, _Saturation);
                float3 finalColor = saturatedColor * _Brightness;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}