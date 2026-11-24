Shader "Underwater/WaterSurface"
{
    Properties
    {
        _Color ("Tint", Color) = (0.15, 0.45, 0.55, 0.55)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.4
        _Scroll1 ("Scroll Primary", Vector) = (0.03, 0.02, 0, 0)
        _Scroll2 ("Scroll Secondary", Vector) = (-0.02, 0.025, 0, 0)
        _FresnelPower ("Fresnel Power", Range(0.2, 8)) = 2.2
        _FresnelBias ("Fresnel Bias", Range(0, 1)) = 0.12
        _SpecPower ("Spec Power", Range(8, 256)) = 96
        _SpecIntensity ("Spec Intensity", Range(0, 4)) = 1.8
        _DepthFade ("Depth Softness", Range(0.1, 8)) = 2.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 300
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf WaterLight alpha:fade keepalpha
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        float _BumpScale;
        float4 _Scroll1;
        float4 _Scroll2;
        float _FresnelPower;
        float _FresnelBias;
        float _SpecPower;
        float _SpecIntensity;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
        };

        half4 LightingWaterLight(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half3 n = normalize(s.Normal);
            half3 l = normalize(lightDir);
            half3 v = normalize(viewDir);
            half3 h = normalize(l + v);

            half ndl = saturate(dot(n, l));
            half nh = saturate(dot(n, h));
            half spec = pow(nh, _SpecPower) * _SpecIntensity;

            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * (0.35 + ndl * 0.65) * atten;
            c.rgb += _LightColor0.rgb * spec * atten;
            c.a = s.Alpha;
            return c;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float2 uv1 = IN.uv_MainTex + _Time.y * _Scroll1.xy;
            float2 uv2 = IN.uv_MainTex * 1.37 + _Time.y * _Scroll2.xy;
            fixed4 a = tex2D(_MainTex, uv1);
            fixed4 b = tex2D(_MainTex, uv2);
            fixed4 c = (a * 0.55 + b * 0.45) * _Color;

            float3 n1 = UnpackScaleNormal(tex2D(_BumpMap, uv1), _BumpScale);
            float3 n2 = UnpackScaleNormal(tex2D(_BumpMap, uv2), _BumpScale * 0.75);
            o.Normal = normalize(n1 + n2);

            float fres = saturate(_FresnelBias + pow(1.0 - saturate(dot(normalize(IN.viewDir), float3(0,0,1))), _FresnelPower));
            o.Albedo = c.rgb;
            o.Alpha = saturate(c.a * lerp(0.35, 0.85, fres));
            o.Specular = 0.9;
            o.Gloss = 0.95;
            o.Emission = c.rgb * fres * 0.08;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
