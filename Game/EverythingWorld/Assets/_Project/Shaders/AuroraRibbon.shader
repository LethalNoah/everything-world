// Aurora ribbons that travel. A ribbon mesh lies along a path (uv.x = how far along, 0..1; vertex colour = its
// colour, alpha fading toward the top edge). Bright stretches move along the path with long fading tails, so
// only part of each ribbon is lit at a time, and the curtain ripples up and down as they pass. _Strength (set by
// SmallHoursLife from the song's energy) brightens the whole thing.
Shader "Everything World/Aurora Ribbon"
{
    Properties
    {
        _Strength ("Strength", Range(0, 3)) = 1
        _Speed ("Travel speed (paths per second)", Float) = 0.018
        _Pulses ("Bright stretches per ribbon", Float) = 2
        _Length ("Length of a bright stretch (0..1)", Float) = 0.12
        _Rest ("Glow between stretches", Range(0, 1)) = 0.06
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Aurora"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Strength, _Speed, _Pulses, _Length, _Rest;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float lit : TEXCOORD1; float fog : TEXCOORD2; };

            // how lit this point along the path is: bright heads moving forward, each with a long tail behind
            float Lit(float u)
            {
                float d = frac((u - _Time.y * _Speed) * _Pulses);       // 0 at a head, rising behind it
                float head = smoothstep(0.0, 0.03, d);                   // a soft front edge
                float tail = exp(-d / max(0.001, _Length * _Pulses));    // fading out behind
                return saturate(head * tail + _Rest);
            }

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(i.positionOS.xyz);
                float lit = Lit(i.uv.x);
                // the curtain lifts and ripples where it's lit
                p.y += i.uv.y * (lit * 1.6 + sin(_Time.y * 0.9 + i.uv.x * 60.0) * 0.6);
                p.y += sin(_Time.y * 0.6 + i.uv.x * 25.0) * 0.5;
                o.positionCS = TransformWorldToHClip(p);
                o.color = i.color; o.uv = i.uv; o.lit = lit;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float shimmer = 0.85 + 0.15 * sin(i.uv.x * 400.0 + _Time.y * 3.0);
                float3 c = i.color.rgb * i.color.a * i.lit * shimmer * _Strength;
                c = MixFogColor(c, half3(0, 0, 0), i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
