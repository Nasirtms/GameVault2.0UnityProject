Shader "Custom/ProceduralCausticsOverlay_Refraction"
{
    Properties
    {
        _Color ("Caustics Color", Color) = (0.45, 0.85, 1.0, 1.0)
        _Intensity ("Intensity", Range(0, 5)) = 1.5
        _Alpha ("Alpha", Range(0, 1)) = 0.35
        _Tiling ("Tiling", Range(1, 20)) = 6
        _TilingMultiplier ("Tiling Multiplier", Vector) = (1.0, 1.0, 0.0, 0.0)
        _Speed ("Speed", Range(0, 5)) = 1.0
        _Sharpness ("Sharpness", Range(0.5, 8)) = 3.0
        _Distortion ("Distortion", Range(0, 2)) = 0.6

        _RefractionStrength ("Refraction Strength", Range(0, 0.05)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        GrabPass { "_GrabTexture" }

        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _GrabTexture;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                fixed4 color : COLOR;
            };

            fixed4 _Color;
            float _Intensity;
            float _Alpha;
            float _Tiling;
            float4 _TilingMultiplier;
            float _Speed;
            float _Sharpness;
            float _Distortion;
            float _RefractionStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.vertex);
                o.color = v.color;
                return o;
            }

            float causticLayer(float2 uv, float t, float scale, float offset)
            {
                uv *= scale;

                float2 p = uv;

                p.x += sin((uv.y + offset) * 4.3 + t * 1.7) * 0.18 * _Distortion;
                p.y += cos((uv.x + offset) * 5.1 - t * 1.3) * 0.18 * _Distortion;

                float a = sin(p.x * 6.0 + t * 1.9);
                float b = sin(p.y * 7.0 - t * 1.5);
                float c = sin((p.x + p.y) * 4.0 + t * 1.2);
                float d = sin(length(p - 0.5) * 10.0 - t * 2.1);

                float v = (a + b + c + d) * 0.25;
                v = saturate(v * 0.5 + 0.5);
                v = pow(v, _Sharpness);

                return v;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv * _Tiling * _TilingMultiplier;
                float t = _Time.y * _Speed;

                float c1 = causticLayer(uv, t, 1.0, 0.0);
                float c2 = causticLayer(uv, -t * 0.8, 1.37, 2.4);
                float c3 = causticLayer(uv, t * 1.2, 0.73, 5.1);

                float caustics = (c1 + c2 + c3) / 3.0;
                caustics = saturate((caustics - 0.35) * 2.2);

                // 👇 Refraction distortion
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                float2 distortionOffset = (float2(c1, c2) - 0.5) * _RefractionStrength;

                fixed4 background = tex2D(_GrabTexture, screenUV + distortionOffset);

                // 👇 Add caustics lighting on top
                background.rgb += _Color.rgb * caustics * _Intensity;

                // final alpha controls overlay strength
                background.a = _Alpha * caustics;

                return background;
            }
            ENDCG
        }
    }
}