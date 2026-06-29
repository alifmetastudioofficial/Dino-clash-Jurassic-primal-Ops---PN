Shader "Custom/Landscape Unlit XR" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Xtiling ("X Tiling", Range(1,10)) = 1
    }

    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "PreviewType"="Plane"}
        LOD 100

        Pass {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #ifdef UNITY_PASS_FORWARDADD
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #endif

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            int _Xtiling;

            v2f vert (appdata_t v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = float2(v.uv.x * _Xtiling, v.uv.y);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv.xy);
                col *= i.color;
                col.a *= _Color.a;
                return col;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}