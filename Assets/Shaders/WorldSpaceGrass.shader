Shader "CourierRunner/WorldSpaceGrass"
{
    Properties
    {
        _GrassMap ("Grass", 2D) = "white" {}
        _GrassTint ("Tint", Color) = (0.84, 0.88, 0.78, 1)
        _WorldTiling ("World Tiling", Vector) = (0.1, 0.2, 0, 0)
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
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            TEXTURE2D(_GrassMap); SAMPLER(sampler_GrassMap);
            half4 _GrassTint;
            float4 _WorldTiling;
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.positionWS.xz * _WorldTiling.xy;
                return SAMPLE_TEXTURE2D(_GrassMap, sampler_GrassMap, uv) * _GrassTint;
            }
            ENDHLSL
        }
    }
}
