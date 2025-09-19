Shader "Minecraft/WaterSurface"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _NormalTex ("Normal", 2D) = "bump" {}
        _Tint ("Tint", Color) = (0.2, 0.5, 0.8, 0.7)
        _WaveSpeed ("Wave Speed", Float) = 0.05
        _WaveScale ("Wave Scale", Float) = 0.15
        _DepthTint ("Depth Tint", Color) = (0.05, 0.2, 0.35, 0.8)
        _DepthFactor ("Depth Factor", Range(0, 5)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 250
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard alpha:fade keepalpha addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalTex;
        float4 _MainTex_ST;
        float4 _Tint;
        float _WaveSpeed;
        float _WaveScale;
        float4 _DepthTint;
        half _DepthFactor;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 viewDir;
        };

        fixed4 SampleTint(float height)
        {
            float depthLerp = saturate(exp(-height * _DepthFactor));
            return lerp(_Tint, _DepthTint, depthLerp);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float time = _Time.y;
            float2 flow = float2(time * _WaveSpeed, time * -_WaveSpeed);
            float2 uv1 = IN.uv_MainTex + flow;
            float2 uv2 = IN.uv_MainTex - flow * 0.75;

            fixed4 albedo = tex2D(_MainTex, uv1) * SampleTint(IN.worldPos.y - _WorldSpaceCameraPos.y);
            fixed3 normal1 = UnpackNormal(tex2D(_NormalTex, uv1));
            fixed3 normal2 = UnpackNormal(tex2D(_NormalTex, uv2));
            fixed3 blendedNormal = normalize(normal1 + normal2 * 0.5);

            o.Albedo = albedo.rgb;
            o.Alpha = albedo.a;
            o.Normal = blendedNormal;
            o.Smoothness = 0.8;
            o.Metallic = 0.0;
            o.Occlusion = 1.0;
        }
        ENDCG
    }

    FallBack Off
}
