Shader "UI/Outline2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _lineWidth("Line Width", Range(0, 30)) = 20
        _lineColor("Outline Color", Color) = (1,1,0,1)
        _Color("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            Lighting Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _lineWidth;
            float4 _lineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 原图颜色
                fixed4 col = tex2D(_MainTex, i.uv);

                // 采样四周
                float2 offset = _lineWidth * _MainTex_TexelSize.xy;
                float aUp = tex2D(_MainTex, i.uv + float2(0, offset.y)).a;
                float aDown = tex2D(_MainTex, i.uv - float2(0, offset.y)).a;
                float aLeft = tex2D(_MainTex, i.uv - float2(offset.x, 0)).a;
                float aRight = tex2D(_MainTex, i.uv + float2(offset.x, 0)).a;

                float edge = aUp * aDown * aLeft * aRight;

                // 判断是否描边区域（透明度不满）
                if (edge < 1.0 && col.a > 0.01)
                {
                    return _lineColor; // 纯描边色，完全盖在原图上
                }

                // 原图 + tint
                return col * _Color;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
