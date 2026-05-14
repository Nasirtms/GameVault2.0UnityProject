Shader "Custom/SimpleCausticsOverlay"
{
    Properties
    {
        _CausticsTex ("Caustics Texture", 2D) = "white" {}
        _Color ("Caustics Color", Color) = (0.4, 0.8, 1, 1)
        _Intensity ("Intensity", Range(0, 5)) = 1.5
        _Alpha ("Alpha", Range(0, 1)) = 0.35
        _Tiling ("Tiling", Vector) = (3, 3, 0, 0)
        _Speed ("Speed", Vector) = (0.03, 0.02, 0, 0)
        _Speed2 ("Speed 2", Vector) = (-0.02, 0.015, 0, 0)
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

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
                fixed4 color : COLOR;
            };

            sampler2D _CausticsTex;
            float4 _CausticsTex_ST;
            fixed4 _Color;
            float _Intensity;
            float _Alpha;
            float4 _Tiling;
            float4 _Speed;
            float4 _Speed2;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;

                float2 uv1 = i.uv * _Tiling.xy + _Speed.xy * t;
                float2 uv2 = i.uv * (_Tiling.xy * 1.4) + _Speed2.xy * t;

                fixed c1 = tex2D(_CausticsTex, uv1).r;
                fixed c2 = tex2D(_CausticsTex, uv2).r;

                fixed caustics = saturate((c1 + c2) * 0.5) * _Intensity;

                fixed3 rgb = _Color.rgb * caustics;
                fixed a = caustics * _Alpha;

                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
}