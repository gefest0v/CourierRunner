Shader "CourierRunner/RotatedForestGround"
{
    Properties
    {
        _BaseMap ("Ground Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _LengthRepeats ("Length Repeats", Float) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            half4 _BaseColor;
            float _LengthRepeats;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Source texture's path runs left-to-right; rotate it into the runner's forward axis.
                float2 rotatedUv = float2(input.uv.y * _LengthRepeats, 1.0 - input.uv.x);
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, rotatedUv) * _BaseColor;
            }
            ENDHLSL
        }
    }
}
