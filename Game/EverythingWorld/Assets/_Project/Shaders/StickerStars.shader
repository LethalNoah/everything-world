// Glow-in-the-dark star stickers across the sky, like the ones on a bedroom ceiling (the Bedroom Pop Meadows).
// Flat star shapes on a dome round the camera, added on top of the sky and not dimmed by the fog.
// _Strength (0..1) fades them in as songs go quiet.
Shader "Everything World/Sticker Stars"
{
    Properties
    {
        _Strength ("Strength", Range(0, 2)) = 0
        _Color ("Glow colour", Color) = (0.62, 1, 0.66, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-200" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Stickers"
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
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; };
            Varyings vert(Attributes i) { Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.color = i.color; return o; }
            half4 frag(Varyings i) : SV_Target { return half4(_Color.rgb * i.color.a * _Strength, 1); }
            ENDHLSL
        }
    }
}
