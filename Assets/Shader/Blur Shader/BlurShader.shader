Shader "Custom/BlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Slider(0, 1)] _BlurAmount ("Blur Amount", Range(0, 1)) = 0.5
        [Slider(0, 10)] _BlurSize ("Blur Size", Range(0, 10)) = 5
        _TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.4)
        [Slider(0, 1)] _TintStrength ("Tint Strength", Range(0, 1)) = 0.5
        [Toggle] _UseGrayOverlay ("Use Gray Overlay", Float) = 0
        [Slider(0, 1)] _GrayIntensity ("Gray Intensity", Range(0, 1)) = 0.3
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
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
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _BlurAmount;
            float _BlurSize;
            float4 _TintColor;
            float _TintStrength;
            float _UseGrayOverlay;
            float _GrayIntensity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                float4 col;
                
                // Always apply blur for complete glass effect
                if (_BlurAmount <= 0)
                {
                    col = tex2D(_MainTex, i.uv);
                }
                else
                {
                    col = float4(0, 0, 0, 0);
                    float totalWeight = 0;
                    
                    // Use fixed texel size for uniform blur across entire surface
                    float2 texelSize = 1.0 / 512.0; // Fixed resolution for consistent blur
                    float blurRadius = _BlurSize * _BlurAmount;
                    
                    // Larger sample grid for complete glass effect
                    for (int x = -6; x <= 6; x++)
                    {
                        for (int y = -6; y <= 6; y++)
                        {
                            float2 offset = float2(x, y) * texelSize * blurRadius;
                            float2 sampleUV = i.uv + offset;
                            
                            // Clamp UVs to prevent edge artifacts
                            sampleUV = clamp(sampleUV, 0.0, 1.0);
                            
                            // Gaussian weight for smooth blur
                            float weight = exp(-(x*x + y*y) / (2.0 * 3.0 * 3.0));
                            
                            col += tex2D(_MainTex, sampleUV) * weight;
                            totalWeight += weight;
                        }
                    }
                    
                    col /= totalWeight;
                }
                
                // Apply gray overlay for better blur visibility
                if (_UseGrayOverlay > 0.5)
                {
                    float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                    col.rgb = lerp(col.rgb, float3(gray, gray, gray), _GrayIntensity);
                }
                
                // Apply tint color overlay
                if (_TintStrength > 0)
                {
                    // Blend the tint color with the blurred image
                    col.rgb = lerp(col.rgb, _TintColor.rgb, _TintColor.a * _TintStrength);
                    // Optionally adjust alpha for overlay effect
                    col.a = lerp(col.a, _TintColor.a, _TintStrength * 0.5);
                }
                
                return col;
            }
            ENDCG
        }
    }
}
