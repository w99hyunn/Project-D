Shader "DoHwa/World Telegraph"
{
    Properties { _Color ("Color", Color) = (1,0.25,0.12,0.55) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
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
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            Varyings vert(Attributes input) { Varyings o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz); o.uv=input.uv; return o; }
            half4 frag(Varyings input) : SV_Target
            {
                float r=length(input.uv*2-1);
                float edge=max(fwidth(r),.008);
                float disc=1-smoothstep(.96-edge,.96+edge,r);
                float ring=smoothstep(.87-edge,.87+edge,r)*disc;
                float pulse=.5+.5*sin(r*18-_Time.y*7);
                return half4(_Color.rgb,_Color.a*(ring*.85+disc*.12+disc*pulse*.08));
            }
            ENDHLSL
        }
    }
}
