// Combines:
//   - Alpha Cutout  (Standard shader style — clean hard edges, no sorting bugs)
//   - Bumped Specular lighting (Legacy style — rich color & albedo)
//   - Two-pass rendering (fixes eyes/inside faces showing through)
//   - Detail Albedo + Detail Normal (extra surface detail up close)

Shader "Custom/Dino_BumpedSpecular_Cutout"
{
    Properties {
        [Header(Main)]
        _MainTex        ("Albedo (RGB) Alpha (A)",  2D)     = "white" {}
        _Color          ("Color Tint",              Color)  = (1,1,1,1)
        _Cutoff         ("Alpha Cutoff",            Range(0.0, 1.0)) = 0.5

        [Header(Normal Map)]
        _BumpMap        ("Normal Map",              2D)     = "bump" {}
        _BumpScale      ("Normal Intensity",        Float)  = 1.0

        [Header(Specular)]
        _SpecColor      ("Specular Color",          Color)  = (0.5,0.5,0.5,1)
        _Shininess      ("Shininess",               Range(0.01, 1.0)) = 0.3

        [Header(Detail)]
        _DetailAlbedo   ("Detail Albedo (RGB)",     2D)     = "grey" {}  // grey = neutral (0.5,0.5,0.5)
        _DetailNormal   ("Detail Normal Map",       2D)     = "bump" {}
        _DetailScale    ("Detail Intensity",        Range(0.0, 2.0)) = 1.0
    }

    SubShader {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 400

        // ── PASS 1: Back faces ────────────────────────────────────────────────────────
        Cull Front

        CGPROGRAM
        #pragma surface surf BlinnPhong alphatest:_Cutoff
        #pragma target 2.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _DetailAlbedo;
        sampler2D _DetailNormal;
        fixed4    _Color;
        half      _BumpScale;
        half      _Shininess;
        half      _DetailScale;

        struct Input {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_DetailAlbedo;   // Unity auto-grabs tiling/offset from detail tex
            float2 uv_DetailNormal;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            // Main albedo
            fixed4 tex      = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Detail albedo — sampled at its own tiling, multiplied x2 so grey = no change
            // x2 multiply blend: 0.5 grey = neutral, darker = darken, lighter = lighten
            fixed3 detail   = tex2D(_DetailAlbedo, IN.uv_DetailAlbedo).rgb * 2.0;
            // Lerp between plain main color and detail blend using _DetailScale
            tex.rgb        *= lerp(fixed3(1,1,1), detail, _DetailScale);

            o.Albedo        = tex.rgb;
            o.Alpha         = tex.a;
            o.Gloss         = tex.a;
            o.Specular      = _Shininess;

            // Blend main normal + detail normal
            fixed3 n        = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            fixed3 dn       = UnpackNormal(tex2D(_DetailNormal, IN.uv_DetailNormal));
            n.xy           *= _BumpScale;
            dn.xy          *= _DetailScale;
            o.Normal        = normalize(fixed3(n.xy + dn.xy, n.z));
        }
        ENDCG

        // ── PASS 2: Front faces ───────────────────────────────────────────────────────
        Cull Back

        CGPROGRAM
        #pragma surface surf BlinnPhong alphatest:_Cutoff
        #pragma target 2.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _DetailAlbedo;
        sampler2D _DetailNormal;
        fixed4    _Color;
        half      _BumpScale;
        half      _Shininess;
        half      _DetailScale;

        struct Input {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_DetailAlbedo;
            float2 uv_DetailNormal;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 tex      = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            fixed3 detail   = tex2D(_DetailAlbedo, IN.uv_DetailAlbedo).rgb * 2.0;
            tex.rgb        *= lerp(fixed3(1,1,1), detail, _DetailScale);

            o.Albedo        = tex.rgb;
            o.Alpha         = tex.a;
            o.Gloss         = tex.a;
            o.Specular      = _Shininess;

            fixed3 n        = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            fixed3 dn       = UnpackNormal(tex2D(_DetailNormal, IN.uv_DetailNormal));
            n.xy           *= _BumpScale;
            dn.xy          *= _DetailScale;
            o.Normal        = normalize(fixed3(n.xy + dn.xy, n.z));
        }
        ENDCG
    }

    FallBack "Legacy Shaders/Transparent/Cutout/Bumped Specular"
}
