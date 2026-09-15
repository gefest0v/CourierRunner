Shader "CourierRunner/ForestPathTransition"
{
    Properties
    {
        _PathMap ("Path", 2D) = "white" {}
        _GrassMap ("Grass", 2D) = "white" {}
        _PathTint ("Path Tint", Color) = (0.82, 0.84, 0.76, 1)
        _GrassTint ("Grass Tint", Color) = (0.84, 0.88, 0.78, 1)
        _WorldTiling ("Grass World Tiling", Vector) = (0.1, 0.2, 0, 0)
        _LengthRepeats ("Path Repeats", Float) = 10
        _GrassLengthRepeats ("Grass Repeats", Float) = 40
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
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };
            TEXTURE2D(_PathMap); SAMPLER(sampler_PathMap);
            TEXTURE2D(_GrassMap); SAMPLER(sampler_GrassMap);
            half4 _PathTint;
            half4 _GrassTint;
            float _LengthRepeats;
            float _GrassLengthRepeats;
            float4 _WorldTiling;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 pathUv = float2(input.uv.y * _LengthRepeats, 1.0 - input.uv.x);
                float2 grassUv = input.positionWS.xz * _WorldTiling.xy;
                half3 path = SAMPLE_TEXTURE2D(_PathMap, sampler_PathMap, pathUv).rgb * _PathTint.rgb;
                half3 grass = SAMPLE_TEXTURE2D(_GrassMap, sampler_GrassMap, grassUv).rgb * _GrassTint.rgb;

                half distanceFromCenter = abs(input.uv.x - 0.5h) * 2.0h;
                half organicOffset = (grass.r - grass.b - 0.15h) * 0.18h;
                half pathWeight = 1.0h - smoothstep(0.38h + organicOffset, 1.0h + organicOffset, distanceFromCenter);
                return half4(lerp(grass, path, pathWeight), 1.0h);
            }
            ENDHLSL
        }
    }
}
