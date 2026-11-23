Shader "RelicDash/RelicPulsingEmission"
{
    Properties
    {
        [Header(Color and Surface)]
        _BaseColor("Base Color", Color) = (0.05, 0.4, 0.6, 1.0)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.85
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.2

        [Header(Pulsing Emission)]
        [HDR] _EmissionColor("Pulsing Emission Color", Color) = (0.0, 3.5, 4.0, 1.0)
        _PulseSpeed("Pulse Speed (Frequency)", Float) = 3.2
        _PulseMin("Min Emission Intensity", Float) = 1.2
        _PulseMax("Max Emission Intensity", Float) = 4.8

        [Header(Fresnel Rim Glow)]
        [HDR] _RimColor("Fresnel Rim Color", Color) = (0.4, 0.9, 1.0, 1.0)
        _RimPower("Fresnel Rim Exponent", Range(0.5, 8.0)) = 2.8
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float4 _RimColor;
                float _Smoothness;
                float _Metallic;
                float _PulseSpeed;
                float _PulseMin;
                float _PulseMax;
                float _RimPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInputs.positionCS;
                output.positionWS = vertexInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);

                // Simple Lambertian lighting from main light
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 directLight = mainLight.color * (NdotL * mainLight.distanceAttenuation);
                half3 ambient = half3(0.12, 0.15, 0.2);

                // Pulsing emission calculation: sine wave oscillation
                float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                float emissionIntensity = lerp(_PulseMin, _PulseMax, pulse);
                half3 pulsingEmission = _EmissionColor.rgb * emissionIntensity;

                // Fresnel rim glow
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _RimPower);
                half3 rimGlow = _RimColor.rgb * fresnel * 2.0;

                // Composite surface color
                half3 albedo = _BaseColor.rgb * (directLight + ambient);
                half3 finalColor = albedo + pulsingEmission + rimGlow;

                return half4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
