Shader "Custom/RoadShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalTex("Normal", 2D) = "bump" {}
        _Smoothness ("Smoothness", 2D) = "white" {}
        _Metallic ("Metallic", 2D) = "white" {}
        _Occlusion("AO", 2D) = "white" {}
        _Alpha("Alpha", 2D) = "white" {}
        _HeightOffset("Height Offset", Range(0, 1)) = 0.01
    }
    SubShader
    {
        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalTex;
        sampler2D _Smoothness;
        sampler2D _Metallic;
        sampler2D _Occlusion;
        sampler2D _Alpha;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalTex;
            float2 uv_Smoothness;
            float2 uv_Metallic;
            float2 uv_Occlusion;
            float2 uv_Alpha;
        };

        fixed4 _Color;
        fixed _HeightOffset;

        void vert(inout appdata_full v) {
            v.vertex.y += _HeightOffset;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_NormalTex, IN.uv_NormalTex));

            // Metallic and smoothness come from slider variables
            o.Metallic = tex2D(_MainTex, IN.uv_Metallic).r;
            o.Smoothness = tex2D(_Metallic, IN.uv_Smoothness).r;
            o.Occlusion = tex2D(_Occlusion, IN.uv_Occlusion).r;
            o.Alpha = tex2D(_Alpha, IN.uv_Alpha).r;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
