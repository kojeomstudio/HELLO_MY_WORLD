Shader "Minecraft/SkyGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.05, 0.25, 0.55, 1)
        _HorizonColor ("Horizon Color", Color) = (0.6, 0.75, 0.9, 1)
        _BottomColor ("Bottom Color", Color) = (0.75, 0.65, 0.5, 1)
        _SunDirection ("Sun Direction", Vector) = (0, 1, 0, 0)
        _SunColor ("Sun Color", Color) = (1, 0.9, 0.7, 1)
        _SunPower ("Sun Power", Float) = 32
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Opaque" }
        Cull Front
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            fixed4 _TopColor;
            fixed4 _HorizonColor;
            fixed4 _BottomColor;
            float4 _SunDirection;
            fixed4 _SunColor;
            float _SunPower;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 worldDir = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
                o.dir = worldDir;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float y = saturate(i.dir.y * 0.5 + 0.5);
                fixed4 sky = lerp(_BottomColor, _TopColor, y);
                sky = lerp(sky, _HorizonColor, exp(-abs(i.dir.y) * 6.0));

                float sunDot = saturate(dot(normalize(_SunDirection.xyz), normalize(i.dir)));
                fixed sun = pow(sunDot, _SunPower);
                sky.rgb += _SunColor.rgb * sun;

                return sky;
            }
            ENDCG
        }
    }

    FallBack Off
}
