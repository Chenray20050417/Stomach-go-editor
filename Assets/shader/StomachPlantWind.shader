
Shader "StomachGO/PlantWind"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Base Texture", 2D) = "white" {}

        _WindStrength ("Wind Strength", Range(0,1)) = 0.12
        _WindSpeed ("Wind Speed", Range(0,5)) = 1.2
        _WindVariation ("Wind Variation", Range(0,5)) = 1.5

        _BaseY ("Root Y", Float) = 0
        _PlantHeight ("Plant Height", Float) = 2

        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

       
sampler2D _MainTex;
fixed4 _Color;
  

        half _Metallic;
        half _Glossiness;

        float _WindStrength;
        float _WindSpeed;
        float _WindVariation;
        float _BaseY;
        float _PlantHeight;

        struct Input
        {
            float2 uv_MainTex;
        };

        void vert(inout appdata_full v)
        {
            float height = max(_PlantHeight, 0.001);

            // 根部為 0，頂端為 1
            float normalizedHeight =
                saturate((v.vertex.y - _BaseY) / height);

            // 根部固定，越高擺動越明顯
            float influence =
                normalizedHeight * normalizedHeight;

            // 不同植物的位置產生不同相位
            float3 worldPos =
                mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;

            float phase =
                worldPos.x * 1.37 +
                worldPos.z * 2.17 +
                worldPos.y * 0.73;

            float time = _Time.y * _WindSpeed;

            float waveX = sin(
                time + phase +
                normalizedHeight * _WindVariation
            );

            float waveZ = sin(
                time * 0.73 + phase * 1.31 +
                normalizedHeight * _WindVariation
            );

            v.vertex.x +=
                waveX * _WindStrength * influence;

            v.vertex.z +=
                waveZ * _WindStrength * 0.4 * influence;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c =
                tex2D(_MainTex, IN.uv_MainTex) * _Color;

            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }

        ENDCG
    }

    FallBack "Diffuse"
}
