// Optimized for low-end mobile — vertex fresnel, half precision, reduced loops
// + Water Color, Alpha transparency
// + Fog support

Shader "Custom/uWater_Fast_Mobile"
{
    Properties {
        [NoScaleOffset] _MainTex            ("Base (RGB)", 2D)                          = "white" {}
        [Normal]
        [NoScaleOffset] _BumpMap            ("Normalmap", 2D)                           = "bump"  {}
        [NoScaleOffset] _Cube               ("Cubemap", Cube)                           = ""      {}

        _WaterColor                         ("Water Color & Alpha", Color)              = (0.1, 0.4, 0.7, 0.65)

        _TextureAnimationX                  ("MainTex Anim X",   Range(-10, 10))        = 0.0
        _TextureAnimationY                  ("MainTex Anim Y",   Range(-10, 10))        = 0.0
        _TextureScale                       ("Texture Scale",    Float)                 = 1.0
        _NormalMap0AnimationX               ("Normal1 Anim X",   Range(-10, 10))        = 0.0
        _NormalMap0AnimationY               ("Normal1 Anim Y",   Range(-10, 10))        = 0.0
        _NormalMap0Scale                    ("Normal1 Scale",    Float)                 = 1.0
        _NormalMap1AnimationX               ("Normal2 Anim X",   Range(-10, 10))        = 0.0
        _NormalMap1AnimationY               ("Normal2 Anim Y",   Range(-10, 10))        = 0.0
        _NormalMap1Scale                    ("Normal2 Scale",    Float)                 = 1.0
        _NormalMapOffsets                   ("Normal Offsets (XY / ZW)", Vector)        = (0, 0, 0, 0)
        _ReflectColor                       ("Reflection Color", Color)                 = (1, 1, 1, 1)
        _HorizonColor                       ("Horizon Color",    Color)                 = (1, 1, 1, 1)
        _ReflectionFresnel                  ("Reflection Fresnel",      Float)          = 2.0
        _MinReflectionFresnel               ("Min Reflection Fresnel",  Float)          = 0.5
        _HorizonColorFresnel                ("Horizon Color Fresnel",   Float)          = 2.0
        _NormalStrength                     ("Normal Intensity",        Float)          = 1.0
    }

    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off

        Pass {
            Lighting On

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog              // ← enables Linear, Exp, Exp2 fog modes
            #include "UnityCG.cginc"

            struct v2f {
                half2  uv_MainTex : TEXCOORD0;
                half4  uv_BumpMap : TEXCOORD1;
                half3  viewDir    : TEXCOORD2;
                half2  fresnels   : TEXCOORD3;
                UNITY_FOG_COORDS(4)                // ← reserves TEXCOORD4 for fog factor
                fixed3 ambient    : COLOR0;
                fixed3 points     : COLOR1;
                float4 pos        : SV_POSITION;
            };

            sampler2D   _MainTex;
            sampler2D   _BumpMap;
            samplerCUBE _Cube;
            fixed4  _WaterColor;
            fixed3  _ReflectColor;
            fixed3  _HorizonColor;
            half    _ReflectionFresnel;
            fixed   _MinReflectionFresnel;
            half    _HorizonColorFresnel;
            fixed   _NormalStrength;
            float   _TextureAnimationX,    _TextureAnimationY;
            fixed   _TextureScale;
            float   _NormalMap0AnimationX, _NormalMap0AnimationY;
            fixed   _NormalMap0Scale;
            float   _NormalMap1AnimationX, _NormalMap1AnimationY;
            fixed   _NormalMap1Scale;
            fixed4  _NormalMapOffsets;

            half3 CalcDirAmb(float4 vertex, float3 normal)
            {
                float3 vpos = mul(UNITY_MATRIX_MV, vertex).xyz;
                float3 vN   = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));
                half3  col  = (half3)UNITY_LIGHTMODEL_AMBIENT.xyz;
                for (int i = 0; i < 4; i++) {
                    if (unity_LightPosition[i].w <= 0.0f) {
                        float3 toL = unity_LightPosition[i].xyz - vpos * unity_LightPosition[i].w;
                        float  lsq = dot(toL, toL);
                        toL  *= rsqrt(lsq);
                        half att  = 1.0h / (1.0h + lsq * unity_LightAtten[i].z);
                        half diff = max(0, dot(vN, toL));
                        col += (half3)unity_LightColor[i].rgb * (diff * att);
                    }
                }
                return col;
            }

            half3 CalcPoints(float4 vertex, float3 normal)
            {
                float3 vpos = mul(UNITY_MATRIX_MV, vertex).xyz;
                float3 vN   = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));
                half3  col  = 0;
                for (int i = 0; i < 4; i++) {
                    if (unity_LightPosition[i].w > 0.0f) {
                        float3 toL = unity_LightPosition[i].xyz - vpos * unity_LightPosition[i].w;
                        float  lsq = dot(toL, toL);
                        toL  *= rsqrt(lsq);
                        half att  = 1.0h / (1.0h + lsq * unity_LightAtten[i].z);
                        half diff = max(0, dot(vN, toL));
                        col += (half3)unity_LightColor[i].rgb * (diff * att);
                    }
                }
                return col;
            }

            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.uv_MainTex    = v.texcoord * _TextureScale
                                + float2(_TextureAnimationX,   _TextureAnimationY)  * _Time.x;
                o.uv_BumpMap.xy = v.texcoord * _NormalMap0Scale + _NormalMapOffsets.xy
                                + float2(_NormalMap0AnimationX, _NormalMap0AnimationY) * _Time.x;
                o.uv_BumpMap.zw = v.texcoord * _NormalMap1Scale + _NormalMapOffsets.zw
                                + float2(_NormalMap1AnimationX, _NormalMap1AnimationY) * _Time.x;

                o.ambient = CalcDirAmb(v.vertex, v.normal);
                o.points  = CalcPoints(v.vertex, v.normal);

                float3 viewDir = normalize(WorldSpaceViewDir(v.vertex));
                o.viewDir      = (half3)viewDir;
                o.fresnels.x   = (half)(pow(1.0 - viewDir.y, _ReflectionFresnel) + _MinReflectionFresnel);
                o.fresnels.y   = (half)clamp(pow(1.0 - viewDir.y, _HorizonColorFresnel), 0.0, 1.0);

                UNITY_TRANSFER_FOG(o, o.pos);      // ← calculates fog factor from clip position

                return o;
            }

            fixed4 frag(v2f i) : COLOR
            {
                fixed4 ct  = tex2D(_MainTex, i.uv_MainTex);
                fixed4 pn  = tex2D(_BumpMap, i.uv_BumpMap.xy) + tex2D(_BumpMap, i.uv_BumpMap.zw);
                pn        *= 0.5f;
                fixed3 nrml = UnpackNormal(pn);
                nrml.xy    *= _NormalStrength;
                nrml        = normalize(nrml);

                half3 worldRefl = reflect(i.viewDir, nrml);
                worldRefl.x     = -worldRefl.x;
                worldRefl.y     = abs(worldRefl.y);

                half3 c = ct.rgb * i.ambient + ct.a * i.points;

                fixed4 refl = texCUBE(_Cube, worldRefl);
                c += refl.rgb * _ReflectColor * i.fresnels.x;
                c  = lerp(c, _HorizonColor, i.fresnels.y);
                c *= _WaterColor.rgb;

                fixed4 col = fixed4(c, _WaterColor.a);
                UNITY_APPLY_FOG(i.fogCoord, col);  // ← blends fog color based on distance
                return col;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
