Shader "Custom/VertexColor_ClipByPlaneWithHighlight"
{
    Properties
    {
        _ClipPlanePos("Clip Plane Position", Vector) = (0,0,0,0)
        _ClipPlaneNormal("Clip Plane Normal", Vector) = (0,1,0,0)
        _HighlightWidth("Highlight Width", Float) = 0.02
        _HighlightIntensity("Highlight Intensity", Float) = 2.0
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

            float4 _ClipPlanePos;
            float4 _ClipPlaneNormal;
            float _HighlightWidth;
            float _HighlightIntensity;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.worldPos = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float d = dot(input.worldPos - _ClipPlanePos.xyz, normalize(_ClipPlaneNormal.xyz));

            // ✅ 裁剪
            if (d < 0) discard;

            // ✅ 高光（d 越接近 0 越亮）
            float highlight = exp(-pow(d / _HighlightWidth, 2.0)) * _HighlightIntensity;

            half4 finalColor = input.color;
            finalColor.rgb *= (1.0 + highlight);
            return finalColor;
        }

        ENDHLSL
    }
    }
}
