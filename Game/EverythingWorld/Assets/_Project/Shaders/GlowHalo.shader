// Soft halos of light round lanterns, lit windows and candles. Each halo is a quad that always faces the
// camera; many are packed into one mesh (position = the halo's centre, uv = the corner, uv2.x = radius in
// metres), so thousands cost about one draw call. Added on top of the scene, faded out up close so a
// lantern right in front of you doesn't become a blob, and dimmed by the fog with distance.
Shader "Everything World/Glow Halo"
{
    Properties
    {
        _Strength ("Strength", Range(0, 4)) = 1
        _NearFade ("Fade out closer than (m)", Float) = 1.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Halo"
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
                float _Strength, _NearFade;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 corner : TEXCOORD0; float2 size : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 corner : TEXCOORD0; float fade : TEXCOORD1; float fog : TEXCOORD2; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 centerWS = TransformObjectToWorld(i.positionOS.xyz);
                float3 centerVS = TransformWorldToView(centerWS);
                float3 posVS = centerVS + float3(i.corner * i.size.x, 0);
                o.positionCS = TransformWViewToHClip(posVS);
                o.color = i.color;
                o.corner = i.corner;
                float d = length(centerVS);
                o.fade = smoothstep(_NearFade * 0.4, _NearFade, d);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float r = length(i.corner);
                float glow = saturate(1.0 - r);
                glow = glow * glow * (0.6 + 0.4 * glow);   // bright core, long soft edge
                float3 c = i.color.rgb * i.color.a * glow * _Strength * i.fade;
                c = MixFogColor(c, half3(0, 0, 0), i.fog); // additive: fades to nothing in fog
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
