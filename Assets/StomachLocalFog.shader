Shader "Custom/StomachLocalFog"
{
    Properties
    {
        [Header(Stomach Material)]
        _MainTex ("Albedo Texture", 2D) = "white" {}
        _Color ("Albedo Color", Color) = (1, 0.35, 0.4, 1)

        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 0.2

        _Metallic ("Metallic", Range(0, 1)) = 0
        _Glossiness ("Smoothness", Range(0, 1)) = 0.3

        [Header(Local Fog)]
        _FogColor ("Fog Color", Color) = (0.25, 0.06, 0.10, 1)

        _FogStart ("Fog Start Distance", Float) = 10
        _FogEnd ("Fog End Distance", Float) = 40

        _FogStrength ("Fog Strength", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;

        fixed4 _Color;
        fixed4 _FogColor;

        half _BumpScale;
        half _Metallic;
        half _Glossiness;

        float _FogStart;
        float _FogEnd;
        float _FogStrength;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;

            float3 worldPos;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // ============================
            // 原本胃壁材質
            // ============================

            fixed4 c =
                tex2D(_MainTex, IN.uv_MainTex)
                * _Color;


            // ============================
            // Camera 距離
            // ============================

            float cameraDistance =
                distance(
                    _WorldSpaceCameraPos,
                    IN.worldPos
                );


            // ============================
            // 計算局部 Fog
            // ============================

            float fogRange =
                max(
                    0.001,
                    _FogEnd - _FogStart
                );

            float fogAmount =
                saturate(
                    (cameraDistance - _FogStart)
                    / fogRange
                );

            fogAmount *= _FogStrength;


            // ============================
            // 胃壁顏色 + Fog
            // ============================

            o.Albedo =
                lerp(
                    c.rgb,
                    _FogColor.rgb,
                    fogAmount
                );


            // ============================
            // Normal Map
            // ============================

            o.Normal =
                UnpackScaleNormal(
                    tex2D(
                        _BumpMap,
                        IN.uv_BumpMap
                    ),
                    _BumpScale
                );


            // ============================
            // 材質參數
            // ============================

            o.Metallic = _Metallic;

            o.Smoothness =
                _Glossiness;

            o.Alpha = c.a;
        }

        ENDCG
    }

    FallBack "Standard"
}