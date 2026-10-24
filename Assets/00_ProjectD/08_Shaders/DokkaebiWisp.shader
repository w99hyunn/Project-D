Shader "DoHwa/World/Dokkaebi Wisp"
{
    Properties { _Color("Color",Color)=(0.25,1,0.8,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            V vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; o.color=i.color; return o; }
            half4 frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float glow=pow(saturate(1-dot(p,p)),3);
                return half4(_Color.rgb*i.color.rgb,glow*i.color.a*_Color.a);
            }
            ENDHLSL
        }
    }
}
