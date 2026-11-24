Shader "Underwater/DepthGrade"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.2, 0.55, 0.65, 1)
        _Contrast ("Contrast", Range(0.5, 1.5)) = 1.05
        _Saturation ("Saturation", Range(0, 3)) = 1.4
        _Vignette ("Vignette", Range(0, 1.5)) = 0.35
        _Absorb ("Red Absorb", Range(0, 1)) = 0.45
        _Depth ("Depth 01", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Tint;
            float _Contrast;
            float _Saturation;
            float _Vignette;
            float _Absorb;
            float _Depth;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float d = saturate(_Depth);

                // Depth color grade: cool cyan/teal, red absorption (stronger underwater).
                fixed3 tint = lerp(fixed3(1,1,1), _Tint.rgb, d * 0.55);
                col.rgb *= tint;
                col.r *= lerp(1.0, 1.0 - _Absorb, d);

                // Contrast + saturation — saturation applies scene-wide (beach vivid grade).
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;
                float luma = dot(col.rgb, fixed3(0.299, 0.587, 0.114));
                col.rgb = lerp(luma.xxx, col.rgb, _Saturation);

                // Soft vignette stronger underwater
                float2 vc = i.uv * 2.0 - 1.0;
                float vig = saturate(1.0 - dot(vc, vc) * (_Vignette * lerp(0.08, 1.0, d)));
                col.rgb *= vig;

                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
