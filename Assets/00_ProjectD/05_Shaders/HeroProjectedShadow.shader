Shader "DoHwa/Hero Projected Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        [PerRendererData] _HairMask("Hair Mask", 2D) = "black" {}
        [PerRendererData] _JacketMask("Jacket Mask", 2D) = "black" {}
        [PerRendererData] _BottomMask("Bottom Mask", 2D) = "black" {}
        [PerRendererData] _BootsMask("Boots Mask", 2D) = "black" {}
        _Equipped("Equipped", Vector) = (1,1,1,1)
        _UseMask("Use outfit masks", Float) = 0
        _ShowCovered("Show covered skin", Float) = 0
        _Cutoff("Shadow alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_HairMask); SAMPLER(sampler_HairMask);
            TEXTURE2D(_JacketMask); SAMPLER(sampler_JacketMask);
            TEXTURE2D(_BottomMask); SAMPLER(sampler_BottomMask);
            TEXTURE2D(_BootsMask); SAMPLER(sampler_BootsMask);
            CBUFFER_START(UnityPerMaterial)
            float4 _Equipped;
            float _UseMask, _ShowCovered, _Cutoff;
            CBUFFER_END
            float3 _LightDirection;
            float3 _LightPosition;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half alpha:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif
                // Both sides of the sprite cast; bias must face the casting light.
                normalWS *= dot(normalWS, lightDirection) < 0 ? -1 : 1;
                output.positionCS = ApplyShadowClamping(TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDirection)));
                output.uv = input.uv;
                output.alpha = input.color.a;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * input.alpha;
                if (_UseMask > .5)
                {
                    float4 mask = float4(SAMPLE_TEXTURE2D(_HairMask, sampler_HairMask, input.uv).r,
                        SAMPLE_TEXTURE2D(_JacketMask, sampler_JacketMask, input.uv).g,
                        SAMPLE_TEXTURE2D(_BottomMask, sampler_BottomMask, input.uv).b,
                        SAMPLE_TEXTURE2D(_BootsMask, sampler_BootsMask, input.uv).a) * _Equipped;
                    float covered = max(max(mask.r, mask.g), max(mask.b, mask.a));
                    alpha *= lerp(1 - covered, covered, _ShowCovered);
                }
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
