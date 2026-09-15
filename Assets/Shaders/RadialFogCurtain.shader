Shader "CourierRunner/RadialFogCurtain"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.55,0.65,0.69,0.15)
        _GrainScale ("Grain Scale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float _GrainScale;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float grain = frac(sin(dot(input.positionWS.xz * _GrainScale, float2(12.9898, 78.233))) * 43758.5453);
                float verticalFade = smoothstep(0.0, 0.16, input.uv.y) * (1.0 - smoothstep(0.83, 1.0, input.uv.y));
                half4 color = _FogColor;
                color.a *= verticalFade * lerp(0.88, 1.08, grain);
                return color;
            }
            ENDHLSL
        }
    }
}
