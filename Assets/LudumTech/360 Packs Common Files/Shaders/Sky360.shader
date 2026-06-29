Shader "Custom/Sky360" {
    Properties {
        _MainTex ("Sky texture", 2D) = "white" {}
        _ScrollX ("Texture scroll speed X", Float) = 1.0
        //_ScrollY ("Texture scroll speed Y", Float) = 0.0
        [HDR] _ColorTint("Color", Color) = (1, 1, 1, 1)   // HDR: allows exposure > 1.0
    }

    SubShader {
        Tags { "Queue"="Geometry+800" "RenderType"="Opaque" }
        LOD 100

        CGINCLUDE
        #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_ST;
        float _ScrollX;
        float _ScrollY;
        float4 _ColorTint;          // stays float4 — HDR values can exceed 1.0

        struct v2f {
            float4 pos   : SV_POSITION;
            float2 uv    : TEXCOORD0;
            float4 color : TEXCOORD1;   // float4 (not fixed4) to preserve HDR range
            UNITY_FOG_COORDS(2)
        };

        v2f vert (appdata_full v) {
            v2f o;
            o.pos   = UnityObjectToClipPos(v.vertex);
            o.uv    = TRANSFORM_TEX(float2(v.texcoord.x, 1.0 - v.texcoord.y), _MainTex)
                    + frac(float2(_ScrollX, _ScrollY) * _Time);
            o.color = _ColorTint;
            UNITY_TRANSFER_FOG(o, o.pos);
            return o;
        }
        ENDCG

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_fog

            float4 frag (v2f i) : SV_Target {  // float4 output (not fixed4) for HDR
                float4 col = tex2D(_MainTex, i.uv) * i.color;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
