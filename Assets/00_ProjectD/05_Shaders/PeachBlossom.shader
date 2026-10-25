Shader "DoHwa/Peach Blossom"
{
    Properties
    {
        _BaseColor("Petal color", Color) = (1,.6,.66,1)
        _TipColor("Petal tip", Color) = (1,.83,.79,1)
        _Cull("Culling", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
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
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TipColor;
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
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fog : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.uv=input.uv;
                output.positionOS=input.positionOS.xyz;
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 normal=SafeNormalize(input.normalWS)*IS_FRONT_VFACE(facing,1,-1);
                Light light=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half wrap=saturate((dot(normal,light.direction)+.45)/1.45);
                half shade=smoothstep(.1,.85,wrap);
                // Cupped petals retain their own silhouette, root tint and pale edges.
                half tip=smoothstep(.20,1,input.uv.x)*.48;
                half edge=saturate(abs(input.uv.y-.5)*1.6)*.08;
                half3 color=lerp(_BaseColor.rgb,_TipColor.rgb,tip+edge);
                half shadow=lerp(.62,1,light.shadowAttenuation);
                half canopyDepth=lerp(.78,1.03,smoothstep(4.4,7.5,input.positionOS.y));
                half3 indirect=SampleSH(normal)*.70;
                half3 direct=light.color*(.18+.75*shade)*shadow;
                color*=(indirect+direct)*canopyDepth;
                return half4(MixFog(clamp(color,0.0,2.0),input.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}


