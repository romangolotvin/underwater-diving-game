Shader "Underwater/FishScales"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.2
        _Glossiness ("Smoothness", Range(0,1)) = 0.72
        _Metallic ("Metallic", Range(0,1)) = 0.08
        _SpecPower ("Spec Power", Range(4,128)) = 48
        _SpecIntensity ("Spec Intensity", Range(0,4)) = 1.6
        _RimColor ("Rim Color", Color) = (0.35, 0.85, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.8
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.55
        _SSSColor ("SSS Color", Color) = (0.2, 0.55, 0.65, 1)
        _SSSStrength ("SSS Strength", Range(0, 1.5)) = 0.45
        _SSSWrap ("SSS Wrap", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf FishLight fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        float _BumpScale;
        half _Glossiness;
        half _Metallic;
        float _SpecPower;
        float _SpecIntensity;
        fixed4 _RimColor;
        float _RimPower;
        float _RimIntensity;
        fixed4 _SSSColor;
        float _SSSStrength;
        float _SSSWrap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 viewDir;
        };

        half4 LightingFishLight(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half3 n = normalize(s.Normal);
            half3 l = normalize(lightDir);
            half3 v = normalize(viewDir);
            half3 h = normalize(l + v);

            half ndl = saturate(dot(n, l));
            half wrap = saturate((dot(n, l) + _SSSWrap) / (1.0 + _SSSWrap));
            half3 diffuse = s.Albedo * (_LightColor0.rgb * (ndl * 0.65 + wrap * _SSSStrength) * atten);
            half3 sss = _SSSColor.rgb * wrap * _SSSStrength * s.Albedo * atten * 0.35;

            half nh = saturate(dot(n, h));
            half spec = pow(nh, _SpecPower) * _SpecIntensity * s.Specular;
            half3 specular = _LightColor0.rgb * spec * atten;

            half fres = pow(1.0 - saturate(dot(n, v)), _RimPower);
            half3 rim = _RimColor.rgb * fres * _RimIntensity;

            half4 c;
            c.rgb = diffuse + sss + specular + rim * atten;
            c.a = s.Alpha;
            return c;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_BumpMap), _BumpScale);
            o.Specular = lerp(0.25, 1.0, _Glossiness);
            o.Gloss = _Glossiness;
            o.Alpha = c.a;
            o.Emission = _Metallic * c.rgb * 0.05;
        }
        ENDCG
    }
    FallBack "Standard"
}
