Shader "Custom/Sky360 VR" {
    Properties {
        _MainTex ("Sky texture", 2D) = "white" {}
        _ColorTint("Color", Color) = (1, 1, 1, 1)
       // _MainTex_ST("Texture Tiling/Offset", Vector) = (1, 1, 0, 0)
    }

    SubShader {
        Tags { "Queue"="Geometry+800" "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float4 _MainTex_ST;
            float4 _ColorTint;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
               o.uv = TRANSFORM_TEX(v.uv * _MainTex_ST.xy + _MainTex_ST.zw, _MainTex);
						o.uv.y *= -1;
						o.color = _ColorTint;
                
                o.color = _ColorTint;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 tex = tex2D(_MainTex, i.uv);
                return tex * i.color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
