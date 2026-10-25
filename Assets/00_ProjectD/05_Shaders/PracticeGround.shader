Shader "DoHwa/Practice Ground"
{
    Properties { _BaseColor ("Ground", Color) = (0.57,0.51,0.53,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.world = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.world);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float2 p = i.world.xz;
                float2 edge = abs(frac(p - 0.5) - 0.5) / max(fwidth(p), 0.0001);
                float grid = 1 - saturate(min(edge.x, edge.y));
                float fade = saturate(1 - distance(i.world, _WorldSpaceCameraPos) / 100);
                return half4(_BaseColor.rgb * (1 - grid * 0.16 * fade), 1);
            }
            ENDHLSL
        }
    }
}
