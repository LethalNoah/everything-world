// The sea. Ripples from layered waves moving across the surface (livelier when the music is energetic or
// the wind is up), clear turquoise in the shallows fading to deep blue, foam washing in along every shore,
// glitter where the sun catches the ripples (it blooms), and a subtle reflection of the sky at grazing angles.
// Uses the camera depth texture to know how deep the water is (enabled in the URP asset).
Shader "Everything World/Water"
{
    Properties
    {
        _Shallow ("Shallow colour", Color) = (0.36, 0.72, 0.72, 1)
        _Deep ("Deep colour", Color) = (0.1, 0.33, 0.45, 1)
        _DepthRange ("Depth to reach deep colour (m)", Float) = 7
        _Foam ("Foam colour", Color) = (0.96, 0.97, 0.95, 1)
        _FoamDepth ("Foam reach (m)", Float) = 1.4
        _Ripple ("Ripple strength", Float) = 0.6
        _Reflect ("Sky reflection", Range(0, 1)) = 0.45
        _Glitter ("Sun glitter", Float) = 3
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-100" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Shallow, _Deep, _Foam;
                float _DepthRange, _FoamDepth, _Ripple, _Reflect, _Glitter;
            CBUFFER_END
            float _EW_Energy, _EW_Wind;      // the song's energy (SurpriseDirector) and the land's wind (the Indie Downs)

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float4 screen : TEXCOORD1; float fog : TEXCOORD2; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.screen = ComputeScreenPos(o.positionCS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // the slope of a few waves travelling in different directions, plus fine ripples on top
            float2 Slope(float2 p, float t, float lively)
            {
                float2 s = 0;
                float4 dirs[4] = { float4(0.8, 0.6, 0.11, 1.0), float4(-0.5, 0.86, 0.17, 1.3), float4(0.2, -0.98, 0.29, 0.8), float4(-0.93, -0.36, 0.53, 1.7) };
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float2 d = dirs[k].xy; float f = dirs[k].z; float sp = dirs[k].w * (0.7 + lively);
                    s += d * f * cos(dot(p, d) * f - t * sp) * (k < 2 ? 1.0 : 0.6);
                }
                return s;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float lively = saturate(max(_EW_Energy, _EW_Wind));
                float3 V = normalize(GetCameraPositionWS() - i.positionWS);
                float dist = length(GetCameraPositionWS() - i.positionWS);
                float near = saturate(1.0 - dist / 600.0);                 // ripples fade out far away (no shimmer)
                float2 slope = Slope(i.positionWS.xz, t, lively) * (_Ripple * (0.6 + 0.8 * lively)) * (0.25 + 0.75 * near);
                slope += Slope(i.positionWS.xz * 3.7 + 11.0, t * 1.6, lively) * 0.18 * near;
                float3 n = normalize(float3(-slope.x, 1.0, -slope.y));

                // how deep the water is here, from the depth of whatever is under it
                float2 uv = i.screen.xy / i.screen.w;
                float sceneZ = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float depth = max(0.0, sceneZ - i.screen.w);
                float deep = saturate(depth / _DepthRange);
                float3 col = lerp(_Shallow.rgb, _Deep.rgb, deep);

                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float shadow = lerp(0.55, 1.0, sun.shadowAttenuation);
                float ndl = saturate(dot(n, sun.direction));
                col *= (SampleSH(n) * 0.6 + sun.color * (0.35 + 0.5 * ndl) * shadow);

                // a subtle reflection of the sky at grazing angles, and glitter where the sun catches it
                float fres = pow(1.0 - saturate(dot(n, V)), 4.0);
                col = lerp(col, unity_FogColor.rgb * 1.05, fres * _Reflect);
                float3 h = normalize(sun.direction + V);
                float glint = pow(saturate(dot(n, h)), 220.0) * _Glitter * shadow;
                col += sun.color * glint;

                // foam washing in along the shore: bands that move toward land and break up
                float foamZone = 1.0 - saturate(depth / _FoamDepth);
                float wobble = sin(i.positionWS.x * 0.35 + t * 0.7) * 0.25 + sin(i.positionWS.z * 0.27 - t * 0.5) * 0.25;
                float bands = frac(depth * 1.3 - t * 0.35 + wobble);
                float foam = saturate(foamZone * 1.6 - 0.2) * smoothstep(0.55, 0.8, bands) + smoothstep(0.75, 1.0, foamZone);
                col = lerp(col, _Foam.rgb * (0.7 + 0.4 * ndl), saturate(foam) * 0.85);

                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
