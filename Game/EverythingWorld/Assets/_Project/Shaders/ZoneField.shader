// The shimmering curtain where two lands meet (and round artist-radio areas): invisible from far away,
// fading in as you get close, and always a little see-through. Its colour comes from the mesh's vertex
// colours: each side of a border shows the colour of the land you'd be walking into. The colour's alpha
// says which kind of curtain it is, so each kind has its own pattern: 1 = border between lands (soft
// diagonal stripes), 0.66 = bridge gate (horizontal bands), 0.33 = artist-radio ring (vertical stripes).
// Land borders also get a stripe angle and spacing of their own (from the land's number in the second
// UV channel), so neighbouring lands look different even to someone who can't tell their colours apart.
Shader "Everything World/Zone Field"
{
    Properties
    {
        _MaxAlpha ("Strongest opacity (up close)", Range(0, 1)) = 0.5
        _Near ("Fully faded in within (m)", Float) = 14
        _Far ("Invisible beyond (m)", Float) = 60
        _ShimmerSpeed ("Shimmer speed", Float) = 1.2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Field"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _MaxAlpha, _Near, _Far, _ShimmerSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 motif : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float motif : TEXCOORD2; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.color = i.color;
                o.uv = i.uv; // x = metres along the border, y = 0 at the ground .. 1 at the top
                o.motif = i.motif.x;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float d = distance(i.positionWS, _WorldSpaceCameraPos);
                float near = 1.0 - smoothstep(_Near, _Far, d);
                float h = i.uv.y;
                float body = smoothstep(0.0, 0.06, h) * (1.0 - smoothstep(0.3, 1.0, h)); // brightest low down, fading upward
                float t = _Time.y * _ShimmerSpeed;
                float shimmer = 0.7 + 0.18 * sin(i.uv.x * 0.8 - t * 2.0 + h * 5.0) + 0.12 * sin(i.uv.x * 2.3 + t * 3.1);
                float kind = i.color.a;
                float pattern;
                if (kind > 0.8)
                {
                    // border: diagonal stripes, each land with its own angle (20-70 degrees, either way) and spacing
                    float m = i.motif;
                    float ang = lerp(0.35, 1.22, frac(m * 0.618)) * (frac(m * 0.37) > 0.5 ? 1.0 : -1.0);
                    float freq = 0.22 + frac(m * 0.29) * 0.4;                     // stripes per metre
                    float coord = (i.uv.x * cos(ang) + h * 7.0 * sin(ang)) * freq - t * 0.25;
                    pattern = 0.35 + 0.65 * step(0.5, frac(coord));
                }
                else if (kind > 0.5) pattern = 0.45 + 0.55 * step(0.5, frac(h * 7.0 - t * 0.6));            // gate: bold horizontal bands
                else pattern = 0.3 + 0.7 * step(0.6, frac(i.uv.x * 0.9 + t * 0.15));                       // artist ring: narrow vertical stripes
                return half4(i.color.rgb, saturate(_MaxAlpha * near * body * shimmer * pattern));
            }
            ENDHLSL
        }
    }
}
