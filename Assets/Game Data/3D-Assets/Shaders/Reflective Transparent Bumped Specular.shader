Shader "Legacy Shaders/Transparent/Reflective/Bumped Specular" {
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
    _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
    _ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _Cube ("Reflection Cubemap", Cube) = "_Skybox" {}
    _BumpMap ("Normalmap", 2D) = "bump" {}
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    LOD 400

CGPROGRAM
#pragma surface surf BlinnPhong alpha

sampler2D _MainTex;
sampler2D _BumpMap;
samplerCUBE _Cube;

fixed4 _Color;
fixed4 _ReflectColor;
half _Shininess;

struct Input {
    float2 uv_MainTex;
    float2 uv_BumpMap;
    float3 worldRefl;
    INTERNAL_DATA
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
    fixed4 c = tex * _Color;
    o.Albedo = c.rgb;
    
    o.Gloss = tex.a;
    o.Specular = _Shininess;
    
    // Normal map influence
    o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
    
    // Reflection calculation based on the normal map
    float3 worldRefl = WorldReflectionVector (IN, o.Normal);
    fixed4 reflcol = texCUBE (_Cube, worldRefl);
    reflcol *= tex.a; // Mask reflection by the texture alpha
    
    o.Emission = reflcol.rgb * _ReflectColor.rgb;
    o.Alpha = c.a * _ReflectColor.a;
}
ENDCG
}

FallBack "Legacy Shaders/Transparent/VertexLit"
}