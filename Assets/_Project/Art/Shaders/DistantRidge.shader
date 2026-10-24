// Dãy núi bao quanh lòng chảo (T60): bóng núi phẳng màu, không nhận sương mù để vẫn hiện sau màn bụi,
// màu lấy theo màu sương của từng màn nên trông như núi mờ trong khói.
Shader "DBP/DistantRidge"
{
    Properties
    {
        _Bottom("Chân núi", Color) = (0.35, 0.36, 0.34, 1)
        _Top("Đỉnh núi", Color) = (0.45, 0.46, 0.44, 1)
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Bottom; half4 _Top;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float h:TEXCOORD0; };
            Varyings vert(Attributes v)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.h = v.uv.y; return o;
            }
            half4 frag(Varyings i):SV_Target { return lerp(_Bottom, _Top, saturate(i.h)); }
            ENDHLSL
        }
    }
}
