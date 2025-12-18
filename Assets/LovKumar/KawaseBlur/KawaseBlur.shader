Shader "Custom/RenderFeature/KawaseBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Transparency ("Transparency", Range(0,2)) = 1
        // _offset ("Offset", float) = 0.5
    }

    SubShader
    {
        // Render as transparent so the alpha can blend with the destination
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // Standard alpha blending over the current render target
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;

            float _offset;
            float _Transparency;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f input) : SV_Target
            {
                float2 res = _MainTex_TexelSize.xy;
                float i = _offset;

                fixed3 col = tex2D(_MainTex, input.uv).rgb;
                col += tex2D(_MainTex, input.uv + float2( i,  i) * res).rgb;
                col += tex2D(_MainTex, input.uv + float2( i, -i) * res).rgb;
                col += tex2D(_MainTex, input.uv + float2(-i,  i) * res).rgb;
                col += tex2D(_MainTex, input.uv + float2(-i, -i) * res).rgb;
                col /= 5.0f;

                // Use _Transparency as output alpha for blending
                return fixed4(col, _Transparency);
            }
            ENDCG
        }
    }
}
