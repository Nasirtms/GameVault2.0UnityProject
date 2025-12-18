Shader "Custom/SpriteInnerGlow_BuiltIn_UIFix"
{
    Properties
    {
        _MainTex        ("Sprite Texture", 2D) = "white" {}
        _Tint           ("Tint", Color) = (1,1,1,1)

        // Inner rim
        _GlowColor      ("Glow Color", Color) = (1,0.95,0.8,1)
        _InnerWidth     ("Inner Rim Width", Range(0,10)) = 2
        _InnerStrength  ("Inner Rim Strength", Range(0,4)) = 1

        // Center glow
        _CenterStrength ("Center Strength", Range(0,4)) = 0
        _CenterRadius   ("Center Radius", Range(0,1)) = 0.35
        _CenterSoftness ("Center Softness", Range(0.001,1)) = 0.25

        // --- UI Stencil Support ---
        _Stencil ("Stencil ID", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Lighting Off

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;

            float4 _Tint;
            float4 _GlowColor;
            float  _InnerWidth;
            float  _InnerStrength;
            float  _CenterStrength;
            float  _CenterRadius;
            float  _CenterSoftness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv  : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float InnerRimMask(float alpha, float width)
            {
                float fw = fwidth(alpha) * max(1.0, width);
                float rim = 1.0 - smoothstep(0.5, 0.5 + fw, alpha);
                rim *= saturate(alpha);
                return rim;
            }

            float CenterMask(float2 uv, float alpha, float radius, float soft)
            {
                float2 p = uv - 0.5;
                float d = length(p) / 0.7071;
                float m = 1.0 - smoothstep(radius, radius + soft, d);
                return m * saturate(alpha);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Tint;
                float a = c.a;

                float rim = InnerRimMask(a, _InnerWidth);
                float cen = CenterMask(i.uv, a, _CenterRadius, _CenterSoftness);

                float glowFactor = _InnerStrength * rim + _CenterStrength * cen;
                float3 glow = _GlowColor.rgb * glowFactor;

                float3 rgb = c.rgb + glow * a;
                return float4(rgb, a);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
