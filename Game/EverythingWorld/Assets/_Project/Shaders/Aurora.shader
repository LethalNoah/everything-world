// The northern lights: tall rippling curtains far off in the sky. A ribbon mesh round the camera
// (uv.x = metres along, uv.y = 0 bottom .. 1 top); green low down shading to teal and violet higher up,
// with slowly drifting folds and fine vertical rays. _Strength comes from the SkyDirector.
Shader "Everything World/Aurora"
{
    Properties
    {
        _Strength ("Strength", Range(0, 2)) = 1
        _Low ("Low colour", Color) = (0.25, 1.0, 0.55, 1)
        _High ("High colour", Color) = (0.45, 0.35, 1.0, 1)
        _Pink ("Pink", Color) = (1.0, 0.35, 0.7, 1)
        _Teal ("Teal", Color) = (0.2, 0.9, 1.0, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-52" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
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
                float4 _Low, _High, _Pink, _Teal;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 p = i.positionOS.xyz;
                float t = _Time.y * 0.08;
                p.xz += normalize(p.xz) * sin(i.uv.x * 0.004 + t * 3.0) * 60.0 * i.uv.y; // the curtain sways
                o.positionCS = TransformObjectToHClip(p);
                o.uv = i.uv;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float x = i.uv.x, h = i.uv.y;
                float folds = 0.55 + 0.45 * sin(x * 0.006 + t * 0.15) * sin(x * 0.0021 - t * 0.07 + 1.3);
                float rays = 0.6 + 0.4 * sin(x * 0.35 + sin(x * 0.02 + t * 0.4) * 4.0);
                float shape = smoothstep(0.0, 0.12, h) * pow(1.0 - h, 1.6);           // sharp lower edge, fading upward
                float3 c = lerp(_Low.rgb, _High.rgb, smoothstep(0.15, 0.9, h));
                // the colours drift along the curtains and over time: green, teal, pink, violet
                float drift = sin(x * 0.0011 + t * 0.03) * 0.5 + 0.5;
                float drift2 = sin(x * 0.0017 - t * 0.02 + 2.0) * 0.5 + 0.5;
                c = lerp(c, _Teal.rgb, smoothstep(0.55, 0.95, drift) * (1.0 - h * 0.5));
                c = lerp(c, _Pink.rgb, smoothstep(0.6, 1.0, drift2) * smoothstep(0.2, 0.8, h));
                float gaps = smoothstep(0.15, 0.45, sin(x * 0.0009 + t * 0.02 + 1.0) * 0.5 + 0.5); // breaks in the curtain
                float a = folds * rays * shape * gaps * _Strength * 0.6;
                return half4(c * a, 1);
            }
            ENDHLSL
        }
    }
}
