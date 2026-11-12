// The glow that shows you can do something with a thing (InteractCues): blue to sit, yellow to use, white to
// pick up, pink to talk. Drawn over the thing itself with its own mesh, brightest round the edges as you look at
// it (so it reads as an outline of light, not a repaint), and also used flat-filled for the small marker shapes.
// The outline is added on top of the scene; the marker shapes are blended over it instead, so their colour stays
// true whatever is behind them (added blue on a red seat came out pink). _Color's alpha is how strongly it shows (it fades in as you come near).
Shader "Everything World/Interact Glow"
{
    Properties
    {
        _Color ("Colour (alpha = strength)", Color) = (1, 1, 1, 1)
        _Fill ("Fill", Range(0, 1)) = 0.15
        _Rim ("Rim", Range(0, 3)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Blend onto the scene (One = added light, OneMinusSrcAlpha = its true colour)", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull (Front = an outline drawn from a slightly bigger copy)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+5" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }
            Blend One [_DstBlend]
            ZWrite Off
            Cull [_Cull]
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Fill, _Rim, _DstBlend, _Cull;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.viewWS = _WorldSpaceCameraPos - ws;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float ndv = abs(dot(normalize(i.normalWS), normalize(i.viewWS)));
                float rim = (1.0 - ndv) * (1.0 - ndv);
                float a = saturate(_Color.a * (_Fill + _Rim * rim));
                return half4(_Color.rgb * a, a);   // premultiplied: right for both blends
            }
            ENDHLSL
        }
    }
}
