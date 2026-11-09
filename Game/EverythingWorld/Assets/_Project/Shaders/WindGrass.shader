// Long grass and bunting that move in the wind. Vertex colour gives the colour; its alpha says how much that
// vertex moves (0 at the root or the string, 1 at the tip). The wind's strength and direction come from the
// globals _EW_Wind (0..1) and _EW_WindDir (x, z), set by the land (the Indie Downs follows each song's energy),
// and gusts roll across the ground as waves. Simple soft lighting from the sun and the sky, with fog.
Shader "Everything World/Wind Grass"
{
    Properties
    {
        _Sway ("Sway (m at full wind)", Float) = 0.45
        _Flutter ("Flutter speed", Float) = 1
        _ShadowTint ("Shadow tint", Color) = (0.62, 0.66, 0.72, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Grass"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Sway, _Flutter;
                float4 _ShadowTint;
            CBUFFER_END
            float _EW_Wind;
            float4 _EW_WindDir;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; float fog : TEXCOORD2; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(i.positionOS.xyz);
                float2 dir = normalize(_EW_WindDir.xz + float2(1e-4, 0));
                float wind = saturate(_EW_Wind);
                float t = _Time.y * _Flutter;
                // waves of gusts rolling downwind, plus a quicker shiver on top
                float wave = sin(t * (1.2 + wind * 1.6) - dot(p.xz, dir) * 0.08) * 0.5 + 0.5;
                float shiver = sin(t * 7.0 + p.x * 1.3 + p.z * 1.7) * 0.15;
                float bend = (0.25 + wind * 0.75) * (0.35 + wave * 0.65 + shiver) * _Sway * i.color.a;
                p.xz += dir * bend;
                p.y -= bend * bend * 0.35;                 // tips dip a little as they bend
                o.positionWS = p;
                o.positionCS = TransformWorldToHClip(p);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.color = i.color;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float lit = smoothstep(0.0, 0.35, saturate(dot(n, sun.direction) * 0.5 + 0.5) - 0.25) * sun.shadowAttenuation;
                float3 ambient = SampleSH(n);
                float3 c = i.color.rgb * (lerp(_ShadowTint.rgb, 1.0, lit) * sun.color * 0.85 + ambient * 0.45);
                c = MixFog(c, i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
