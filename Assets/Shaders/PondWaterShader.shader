Shader "RelicDash/PondWaterShader"
{
    Properties
    {
        [Header(Water Colors)]
        _DeepColor("Deep Water Color", Color) = (0.02, 0.12, 0.18, 0.88)
        _ShallowColor("Shallow Water Color", Color) = (0.08, 0.38, 0.35, 0.65)
        _FoamColor("Shoreline Foam Color", Color) = (0.4, 0.6, 0.65, 0.7)
        
        [Header(Wave Animation)]
        _WaveSpeed("Wave Speed", Float) = 1.6
        _WaveFreq("Wave Frequency", Float) = 2.4
        _WaveHeight("Wave Height", Float) = 0.035
        
        [Header(Surface Properties)]
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.94
        _FresnelPower("Fresnel Reflection Power", Range(1.0, 8.0)) = 3.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "WaterForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

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
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FoamColor;
                float _WaveSpeed;
                float _WaveFreq;
                float _WaveHeight;
                float _Smoothness;
                float _FresnelPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                // Animate water vertex ripples using combined sine & cosine
                float time = _Time.y * _WaveSpeed;
                float wave = sin(time + input.positionOS.x * _WaveFreq) * cos(time * 0.75 + input.positionOS.z * _WaveFreq * 1.2);
                float wave2 = sin(time * 1.3 - (input.positionOS.x + input.positionOS.z) * _WaveFreq * 0.8) * 0.5;
                
                input.positionOS.y += (wave + wave2) * _WaveHeight;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Dynamic normal ripple perturbation
                float time = _Time.y * _WaveSpeed;
                float rippleX = cos(time + input.positionWS.x * _WaveFreq * 2.0) * 0.15;
                float rippleZ = sin(time * 0.9 + input.positionWS.z * _WaveFreq * 2.0) * 0.15;

                float3 perturbedNormal = normalize(input.normalWS + float3(rippleX, 0, rippleZ));
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);

                // Fresnel reflection
                float fresnel = pow(1.0 - saturate(dot(perturbedNormal, viewDir)), _FresnelPower);

                // Lighting
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float NdotL = saturate(dot(perturbedNormal, mainLight.direction));
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(perturbedNormal, halfDir));
                float specular = pow(NdotH, _Smoothness * 128.0) * _Smoothness;

                // Blend deep and shallow color based on view angle and ripples
                float waterBlend = saturate(fresnel * 0.6 + rippleX * 0.5 + 0.3);
                half4 baseColor = lerp(_DeepColor, _ShallowColor, waterBlend);

                // Add specular & moon/torchlight highlights
                half3 litColor = baseColor.rgb * (mainLight.color * (NdotL * 0.7 + 0.3)) + (mainLight.color * specular * 1.5);
                litColor += _FoamColor.rgb * fresnel * 0.4;

                half alpha = lerp(baseColor.a, 0.95, fresnel);

                return half4(litColor, alpha);
            }
            ENDHLSL
        }
    }
}
