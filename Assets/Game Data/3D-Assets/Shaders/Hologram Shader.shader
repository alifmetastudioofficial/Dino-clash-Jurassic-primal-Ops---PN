Shader "Custom/HoloGlowShader_Optimized"
{
    Properties
    {
        _Color ("Color", Color) = (1,0,0,1)
        _GlowColor ("Glow Color", Color) = (0.2, 0.5, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2.0
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150

        CGPROGRAM
        #pragma surface surf Lambert noforwardadd

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        half4 _Color;
        half4 _GlowColor;
        half _FresnelPower;

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Basic texture sampling
            half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Fresnel calculation simplified for mobile performance
            half fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), _FresnelPower);

            // Simple inner glow effect
            half4 glow = _GlowColor * fresnel;

            // Combine base color and glow
            o.Albedo = c.rgb + glow.rgb;
            o.Emission = glow.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
