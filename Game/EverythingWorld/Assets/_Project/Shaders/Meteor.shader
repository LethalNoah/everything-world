// A shooting star: a short bright streak with a fading tail. Drawn by a LineRenderer (uv.x = tail..head,
// uv.y = across), added on top of the sky, not fogged.
Shader "Everything World/Meteor"
{
    Properties { _Fade ("Fade", Range(0, 1)) = 1 }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-49" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
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
                float _Fade;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.color = i.color; o.uv = i.uv;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float along = i.uv.x;                       // 0 = tail, 1 = head
                float across = 1.0 - abs(i.uv.y * 2.0 - 1.0);
                float a = along * along * across * across * _Fade;
                float head = smoothstep(0.85, 1.0, along) * across;     // a bright head on the streak
                return half4(i.color.rgb * (a * 5.0 + head * 6.0 * _Fade), 1);
            }
            ENDHLSL
        }
    }
}
