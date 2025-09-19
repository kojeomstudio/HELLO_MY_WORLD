Shader "Minecraft/FoliageWind"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.4
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _WindStrength ("Wind Strength", Float) = 0.2
        _WindFrequency ("Wind Frequency", Float) = 1.5
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        LOD 200
        Cull Off
        AlphaToMask On

        CGPROGRAM
        #pragma surface surf Standard alphatest:_Cutoff addshadow vertex:vert
        #pragma target 3.0
        #pragma multi_compile_instancing

        sampler2D _MainTex;
        float4 _MainTex_ST;
        fixed4 _Tint;
        float _WindStrength;
        float _WindFrequency;

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Tint)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Input
        {
            float2 uv_MainTex;
        };

        void vert(inout appdata_full v)
        {
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float sway = sin(worldPos.x * _WindFrequency + _Time.y * 1.1) + cos(worldPos.z * (_WindFrequency * 0.75) + _Time.y * 1.7);
            sway *= _WindStrength * saturate(v.normal.y + 0.3);
            v.vertex.xyz += float3(sway, 0, sway * 0.5);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * UNITY_ACCESS_INSTANCED_PROP(Props, _Tint);
            clip(c.a - _Cutoff);
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Smoothness = 0.1;
            o.Metallic = 0.0;
            o.Occlusion = 1.0;
        }
        ENDCG
    }

    FallBack "Transparent/Cutout/Diffuse"
}
