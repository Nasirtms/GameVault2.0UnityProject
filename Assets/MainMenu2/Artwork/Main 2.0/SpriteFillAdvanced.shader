Shader "Custom/SpriteFillAdvanced"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0

        _FillAmount ("Fill Amount", Range(0,1)) = 1

        [Enum(Horizontal,0,Vertical,1,Radial90,2,Radial180,3,Radial360,4)]
        _FillMethod ("Fill Method", Float) = 0

        _FillOrigin ("Fill Origin", Float) = 0
        [MaterialToggle] _Clockwise ("Clockwise", Float) = 1

        // minX, minY, maxX, maxY in local sprite space
        [HideInInspector] _SpriteRect ("Sprite Rect", Vector) = (-0.5,-0.5,0.5,0.5)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            #define PI 3.14159265359
            #define HALF_PI 1.57079632679
            #define TWO_PI 6.28318530718

            sampler2D _MainTex;
            fixed4 _Color;
            float _FillAmount;
            float _FillMethod;
            float _FillOrigin;
            float _Clockwise;
            float4 _SpriteRect;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localPos : TEXCOORD1;
            };

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.localPos = IN.vertex.xy;
                OUT.color = IN.color * _Color;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            float2 GetFillUV(float2 localPos)
            {
                float2 minP = _SpriteRect.xy;
                float2 maxP = _SpriteRect.zw;
                float2 size = maxP - minP;
                size = max(size, float2(1e-6, 1e-6));
                return saturate((localPos - minP) / size);
            }

            float NormalizeAngle01(float angle)
            {
                angle = fmod(angle, TWO_PI);
                if (angle < 0.0) angle += TWO_PI;
                return angle / TWO_PI;
            }

            float Radial360Value(float2 uv, float origin, float clockwise)
            {
                float2 p = uv - float2(0.5, 0.5);

                if (abs(p.x) < 1e-6 && abs(p.y) < 1e-6)
                    return 0.0;

                float angle = atan2(p.y, p.x);
                float startAngle = 0.0;

                // 0 = Bottom, 1 = Right, 2 = Top, 3 = Left
                if (origin < 0.5) startAngle = -HALF_PI;
                else if (origin < 1.5) startAngle = 0.0;
                else if (origin < 2.5) startAngle = HALF_PI;
                else startAngle = PI;

                float delta = angle - startAngle;

                if (_Clockwise > 0.5)
                    delta = -delta;

                return NormalizeAngle01(delta);
            }

            float HorizontalMask(float2 uv, float fillAmount, float origin)
            {
                if (origin < 0.5)
                    return uv.x <= fillAmount ? 1.0 : 0.0;
                else
                    return uv.x >= (1.0 - fillAmount) ? 1.0 : 0.0;
            }

            float VerticalMask(float2 uv, float fillAmount, float origin)
            {
                if (origin < 0.5)
                    return uv.y <= fillAmount ? 1.0 : 0.0;
                else
                    return uv.y >= (1.0 - fillAmount) ? 1.0 : 0.0;
            }

            float Radial90Mask(float2 uv, float fillAmount, float origin, float clockwise)
            {
                float2 p;

                if (origin < 0.5)         p = float2(uv.x, uv.y);
                else if (origin < 1.5)    p = float2(uv.x, 1.0 - uv.y);
                else if (origin < 2.5)    p = float2(1.0 - uv.x, 1.0 - uv.y);
                else                      p = float2(1.0 - uv.x, uv.y);

                p = max(p, 1e-6);

                float angle01;
                if (clockwise > 0.5)
                    angle01 = saturate(atan2(p.x, p.y) / HALF_PI);
                else
                    angle01 = saturate(atan2(p.y, p.x) / HALF_PI);

                return angle01 <= fillAmount ? 1.0 : 0.0;
            }

            float Radial180Mask(float2 uv, float fillAmount, float origin, float clockwise)
            {
                float2 p;

                if (origin < 0.5)         p = float2(uv.x - 0.5, uv.y);
                else if (origin < 1.5)    p = float2(uv.y - 0.5, uv.x);
                else if (origin < 2.5)    p = float2(0.5 - uv.x, 1.0 - uv.y);
                else                      p = float2(0.5 - uv.y, 1.0 - uv.x);

                p.y = max(p.y, 1e-6);

                float a;
                if (clockwise > 0.5)
                    a = atan2(p.y, -p.x);
                else
                    a = atan2(p.y, p.x);

                float angle01 = saturate(a / PI);
                return angle01 <= fillAmount ? 1.0 : 0.0;
            }

            float Radial360Mask(float2 uv, float fillAmount, float origin, float clockwise)
            {
                float angle01 = Radial360Value(uv, origin, clockwise);
                return angle01 <= fillAmount ? 1.0 : 0.0;
            }

            float GetFillMask(float2 fillUV)
            {
                if (_FillAmount <= 0.0) return 0.0;
                if (_FillAmount >= 1.0) return 1.0;

                if (_FillMethod < 0.5)
                    return HorizontalMask(fillUV, _FillAmount, _FillOrigin);
                else if (_FillMethod < 1.5)
                    return VerticalMask(fillUV, _FillAmount, _FillOrigin);
                else if (_FillMethod < 2.5)
                    return Radial90Mask(fillUV, _FillAmount, _FillOrigin, _Clockwise);
                else if (_FillMethod < 3.5)
                    return Radial180Mask(fillUV, _FillAmount, _FillOrigin, _Clockwise);
                else
                    return Radial360Mask(fillUV, _FillAmount, _FillOrigin, _Clockwise);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                float2 fillUV = GetFillUV(IN.localPos);
                float mask = GetFillMask(fillUV);

                c.rgb *= c.a;
                c *= mask;

                return c;
            }
            ENDCG
        }
    }
}