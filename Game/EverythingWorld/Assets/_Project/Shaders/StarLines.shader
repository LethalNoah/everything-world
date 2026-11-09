// Faint lines joining the stars of a few constellations, so the sky reads as sky (not more snow).
// Thin strips on the star dome; same strength and horizon fade as the stars.
Shader "Everything World/Star Lines"
{
    Properties
    {
        _Strength ("Strength", Range(0, 2)) = 1
        _Color ("Colour", Color) = (0.55, 0.65, 0.95, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-51" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Lines"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Strength;
                float4 _Color;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float horizon : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv; // y: -1..1 across the line
                o.horizon = smoothstep(0.05, 0.3, normalize(i.positionOS.xyz).y);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float across = 1.0 - abs(i.uv.y);
                float a = across * across * 0.22 * _Strength * i.horizon;
                return half4(_Color.rgb * a, 1);
            }
            ENDHLSL
        }
    }
}
