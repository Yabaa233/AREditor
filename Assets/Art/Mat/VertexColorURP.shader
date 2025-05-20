Shader "Custom/VertexColorURP"
{
    Properties
    {
        _Brightness("Brightness", Range(0, 5)) = 1.0
        _Saturation("Saturation", Range(0, 2)) = 1.0
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 color = input.color.rgb;

                // 灰度值计算（加权平均法）
                float luminance = dot(color, float3(0.299, 0.587, 0.114));

                // 饱和度调整：1 = 原色, 0 = 灰色, >1 = 增强
                float3 saturatedColor = lerp(float3(luminance, luminance, luminance), color, _Saturation);

                // 亮度调整
                saturatedColor *= _Brightness;

                return half4(saturatedColor, input.color.a);
            }
            ENDHLSL
        }
    }
}
