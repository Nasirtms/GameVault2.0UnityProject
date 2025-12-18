Shader "Custom/SpriteInnerGlow_BuiltIn"
{
    Properties
    {
        _MainTex        ("Sprite Texture", 2D) = "white" {}
        _Tint           ("Tint", Color) = (1,1,1,1)

        // Inner rim (bright near alpha edge, fades inward)
        _GlowColor      ("Glow Color", Color) = (1,0.95,0.8,1)
        _InnerWidth     ("Inner Rim Width", Range(0,10)) = 2
        _InnerStrength  ("Inner Rim Strength", Range(0,4)) = 1

        // Optional soft center glow
        _CenterStrength ("Center Strength", Range(0,4)) = 0
        _CenterRadius   ("Center Radius", Range(0,1)) = 0.35
        _CenterSoftness ("Center Softness", Range(0.001,1)) = 0.25
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0                 // for fwidth/derivatives
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
                float2 uv     : TEXCOORD0;
                float4 pos    : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Edge-near mask using alpha transition + screen-space width
            float InnerRimMask(float alpha, float width)
            {
                // fwidth ≈ |ddx|+|ddy|; scale by width control
                float fw = fwidth(alpha) * max(1.0, width);
                // bright where alpha rises from 0 to 1 (the *inside* edge)
                float rim = 1.0 - smoothstep(0.5, 0.5 + fw, alpha);
                // keep strictly inside sprite
                rim *= saturate(alpha);
                return rim;
            }

            // Radial center mask (stays inside alpha)
            float CenterMask(float2 uv, float alpha, float radius, float soft)
            {
                float2 p = uv - 0.5;
                // normalize to ~0..1 from center to far corner
                float d = length(p) / 0.7071;
                float m = 1.0 - smoothstep(radius, radius + soft, d);
                return m * saturate(alpha);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Tint;
                float  a = c.a;

                float rim = InnerRimMask(a, _InnerWidth);
                float cen = CenterMask(i.uv, a, _CenterRadius, _CenterSoftness);

                // Inner-only glow contribution
                float glowFactor = _InnerStrength * rim + _CenterStrength * cen;
                float3 glow = _GlowColor.rgb * glowFactor;

                // Add inside the sprite only (multiply by alpha to prevent bleed)
                float3 rgb = c.rgb + glow * a;

                return float4(rgb, a);
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}
