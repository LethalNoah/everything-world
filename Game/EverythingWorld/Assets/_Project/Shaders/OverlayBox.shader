// The dark box behind speech: a flat colour with softened edges, always drawn over the scene (never hidden
// behind a wall), just before the words (Overlay Text).
Shader "Everything World/Overlay Box"
{
    Properties
    {
        _Color ("Colour", Color) = (0.06, 0.06, 0.07, 0.82)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Box"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes i) { Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }

            half4 frag(Varyings i) : SV_Target
            {
                // soften the last bit of each edge, so the panel reads as rounded
                float2 e = min(i.uv, 1 - i.uv) * float2(14, 7);
                return half4(_Color.rgb, _Color.a * saturate(min(e.x, e.y) * 1.6));
            }
            ENDHLSL
        }
    }
}
