Shader "Custom/Landscape Unlit" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Xtiling ("X Tiling", Range(1,10)) = 1
    }
 
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
 
        Pass {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 2.0                     // explicit low-end mobile target
            #include "UnityCG.cginc"
 
            struct appdata_t {
                float4 vertex : POSITION;          // float required for POSITION
                fixed4 color  : COLOR;             // fixed: color is always 0-1
                half2  uv     : TEXCOORD0;         // half: UVs don't need full precision
            };
 
            struct v2f {
                half2  uv     : TEXCOORD0;         // half: saves interpolator bandwidth
                fixed4 color  : COLOR;             // fixed: color is always 0-1
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;       // float required for SV_POSITION
            };
 
            sampler2D _MainTex;
            float4    _MainTex_ST;                 // float required by Unity internals
            fixed4    _Color;                      // fixed: color never exceeds 0-1
            fixed     _Xtiling;                    // fixed: small whole number 1-10
 
            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = half2(v.uv.x * _Xtiling, v.uv.y);
                o.color  = v.color * _Color;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
 
            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                col       *= i.color;
                col.a     *= _Color.a;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
 
    FallBack "Sprites/Default"
}
