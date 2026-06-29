Shader "CameraFilterPack/Blur_Blurry" {
Properties 
{
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _Amount ("Blur Amount", Range(0.0, 10.0)) = 2.0
}
SubShader 
{
    Pass
    {
        Cull Off ZWrite Off ZTest Always
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        half _Amount;

        struct appdata_t
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f
        {
            float2 texcoord : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };   

        v2f vert(appdata_t IN)
        {
            v2f OUT;
            OUT.vertex = UnityObjectToClipPos(IN.vertex);
            OUT.texcoord = IN.texcoord;
            return OUT;
        }

        half4 frag(v2f i) : COLOR
        {
            // Set blur step size (reduced for low-end devices)
            half2 step = half2(_Amount / 512.0, _Amount / 512.0);
            
            // 5-sample blur (simpler than 3x3 Gaussian)
            half4 color = tex2D(_MainTex, i.texcoord) * 0.4; // Center
            color += tex2D(_MainTex, i.texcoord + step) * 0.15;
            color += tex2D(_MainTex, i.texcoord - step) * 0.15;
            color += tex2D(_MainTex, i.texcoord + half2(step.x, -step.y)) * 0.15;
            color += tex2D(_MainTex, i.texcoord - half2(step.x, -step.y)) * 0.15;
            
            return color;
        }
        ENDCG
    }
}
}
