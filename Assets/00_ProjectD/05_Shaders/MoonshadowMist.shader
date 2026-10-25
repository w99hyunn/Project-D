Shader "DoHwa/World/Moonshadow Mist"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; };
            V vert(A i) { V o; o.world=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); return o; }
            half4 frag(V i):SV_Target
            {
                float2 p=i.world.xz*.055;
                float t=_Time.y*.035;
                float f=sin(p.x+t+sin(p.y*1.7-t))*.5+.5;
                float veil=smoothstep(.15,.85,f)*(.65+.35*sin(p.y*.8+t));
                return half4(lerp(float3(.10,.18,.25),float3(.35,.49,.51),veil),1);
            }
            ENDHLSL
        }
    }
}
