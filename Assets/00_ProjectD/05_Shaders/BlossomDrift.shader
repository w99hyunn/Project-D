Shader "DoHwa/Drifting Blossom"
{
    Properties { _BaseColor("Petal tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; half4 color:COLOR; half fog:TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.uv=input.uv;
                output.color=input.color*_BaseColor;
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                Light sun=GetMainLight();
                half diffuse=abs(dot(SafeNormalize(input.normalWS),sun.direction));
                half3 shade=SampleSH(half3(0,1,0))*.65+sun.color*(.25+diffuse*.55);
                half3 petal=input.color.rgb*lerp(half3(.72,.42,.52),half3(1,1,1),smoothstep(0,.55,input.uv.y));
                return half4(MixFog(clamp(petal*shade,0.0,2.0),input.fog),input.color.a);
            }
            ENDHLSL
        }
    }
}

