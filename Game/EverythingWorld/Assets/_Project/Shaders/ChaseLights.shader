// Little lights along a loop with pulses of brightness running round it (the Bedroom Pop Meadows' track).
// Vertex colour = the light's colour; uv.x = how far round the loop it sits (0..1). The global _EW_ChaseHead
// (0..1, moved on by MeadowsLife at a speed that follows the music) is where the pulses are. Bright enough
// to bloom.
Shader "Everything World/Chase Lights"
{
    Properties
    {
        _Base ("Resting glow", Float) = 0.9
        _Peak ("Pulse glow", Float) = 5
        _Pulses ("Pulses round the loop", Float) = 3
        _Width ("Pulse width", Float) = 0.035
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Chase"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Base, _Peak, _Pulses, _Width;
            CBUFFER_END
            float _EW_ChaseHead;
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float u : TEXCOORD0; float fog : TEXCOORD1; };
            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.color = i.color; o.u = i.uv.x;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                // distance (as a fraction of the gap between pulses) from the nearest pulse, just behind it
                float d = frac(i.u * _Pulses - _EW_ChaseHead * _Pulses);
                float behind = 1.0 - d;                                  // pulses travel toward higher u
                float pulse = exp(-pow(min(d, behind) / (_Width * _Pulses), 2.0)) + exp(-d / (_Width * _Pulses * 4.0)) * 0.35;
                float3 c = i.color.rgb * (_Base + _Peak * pulse);
                c = MixFog(c, i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
