Shader "Minecraft/VoxelBlockStandard"
{
    Properties
    {
        _MainTex ("Texture Atlas", 2D) = "white" {}
        _UVData ("UV Offset/Scale", Vector) = (0, 0, 1, 1)
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _AmbientOcclusion ("Ambient Occlusion", Range(0, 1)) = 0.75
        _Smoothness ("Smoothness", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        #pragma multi_compile_instancing

        sampler2D _MainTex;
        float4 _MainTex_ST;
        float4 _UVData;
        fixed4 _Tint;
        half _AmbientOcclusion;
        half _Smoothness;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
            float3 worldPos;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _UVData)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Tint)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 uvData = UNITY_ACCESS_INSTANCED_PROP(Props, _UVData);
            fixed4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _Tint);

            float2 atlasUV = IN.uv_MainTex * uvData.zw + uvData.xy;
            fixed4 c = tex2D(_MainTex, atlasUV) * tint;

            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Metallic = 0;
            o.Smoothness = _Smoothness;
            o.Occlusion = saturate(_AmbientOcclusion);
        }
        ENDCG
    }

    FallBack "Diffuse"
}
