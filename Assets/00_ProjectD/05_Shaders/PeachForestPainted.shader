Shader "DoHwa/Peach Forest Painted"
{
    Properties
    {
        _BaseMap("Painted surface atlas", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _AtlasRect("Atlas offset and size", Vector) = (0,0,1,1)
        _ShadeTint("Painted shadow tint", Color) = (0.60,0.64,0.75,1)
        _Cull("Culling", Float) = 2
        _MappingMode("Triplanar mapping", Float) = 0
        _TextureScale("Paint scale", Float) = 0.7
        _DetailContrast("Surface paint contrast", Range(0,1)) = 1
        _SurfaceMidColor("Surface middle tone", Color) = (.25,.25,.25,1)
        _MossCoverage("Soft moss on upper surfaces", Range(0,1)) = 0
        _MossColor("Moss paint", Color) = (.16,.22,.10,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull [_Cull]
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _AtlasRect;
                half4 _ShadeTint;
                float _MappingMode;
                float _TextureScale;
                float _DetailContrast;
                half4 _SurfaceMidColor;
                float _MossCoverage;
                half4 _MossColor;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fog : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                half3 normalOS : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fog = ComputeFogFactor(output.positionCS.z);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                return output;
            }
            half3 Paint(float2 coordinates)
            {
                float2 uv = _AtlasRect.xy + 0.008 + frac(coordinates) * (_AtlasRect.zw - 0.016);
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
            }
            half3 ProjectedPaint(float2 coordinates)
            {
                // A rock occupies one continuous atlas region, including negative coordinates.
                // Wrapping at zero split each projection into four unrelated texture quarters.
                float2 uv = _AtlasRect.xy + 0.008 + saturate(coordinates + 0.5) * (_AtlasRect.zw - 0.016);
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 paint = Paint(input.uv);
                if (_MappingMode > 0.5)
                {
                    half3 weights = pow(abs(normalize(input.normalOS)), 4);
                    weights /= max(0.001, weights.x + weights.y + weights.z);
                    float3 p = input.positionOS * _TextureScale;
                    paint = ProjectedPaint(p.zy) * weights.x + ProjectedPaint(p.xz) * weights.y + ProjectedPaint(p.xy) * weights.z;
                }
                paint = lerp(_SurfaceMidColor.rgb, paint, _DetailContrast);
                paint *= _BaseColor.rgb;
                float3 mossPosition = input.positionOS;
                half mossNoise = sin(mossPosition.x * 3.1 + mossPosition.z * 1.7)
                    * cos(mossPosition.z * 2.6 - mossPosition.x * .8);
                half mossTop = smoothstep(.38, .86, normalize(input.normalOS).y + mossNoise * .16);
                half mossHeight = smoothstep(.24, .76, mossPosition.y + mossNoise * .09);
                half mossMask = mossTop * mossHeight * _MossCoverage;
                paint = lerp(paint, _MossColor.rgb * (.90 + mossNoise * .08), mossMask);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = saturate(dot(normalize(input.normalWS), light.direction));
                half shade = smoothstep(0.12, 0.80, diffuse) * light.shadowAttenuation;
                half3 indirect = SampleSH(normalize(input.normalWS)) * .70;
                half3 direct = light.color * (.15 + .85 * shade);
                half3 color = paint * (indirect + direct) * lerp(_ShadeTint.rgb, half3(1,1,1), shade);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
