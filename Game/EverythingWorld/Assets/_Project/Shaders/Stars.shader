// The night sky's stars: tiny camera-facing points on a big dome that follows the camera. Not fogged
// (they're the sky), faded near the horizon, gently twinkling. _Strength is set by the SkyDirector from
// the current land's mood, so stars fade in and out as you cross between lands.
Shader "Everything World/Stars"
{
    Properties
    {
        _Strength ("Strength", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-50" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Stars"
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
            CBUFFER_END

            // position = the star, uv = corner, uv2.x = size (m), uv2.y = twinkle phase, colour alpha = brightness
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 corner : TEXCOORD0; float2 data : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 corner : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 centerWS = TransformObjectToWorld(i.positionOS.xyz);
                float3 centerVS = TransformWorldToView(centerWS);
                o.positionCS = TransformWViewToHClip(centerVS + float3(i.corner * i.data.x, 0));
                float up = normalize(i.positionOS.xyz).y;                      // the dome is centred on the camera
                float horizon = smoothstep(0.03, 0.25, up);
                float twinkle = 0.75 + 0.25 * sin(_Time.y * (1.3 + frac(i.data.y * 7.1) * 2.0) + i.data.y * 6.2831);
                o.color = float4(i.color.rgb, i.color.a * horizon * twinkle * _Strength);
                o.corner = i.corner;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float r = length(i.corner);
                float a = saturate(1.0 - r);
                a = a * a * a;
                return half4(i.color.rgb * i.color.a * a, 1);
            }
            ENDHLSL
        }
    }
}
