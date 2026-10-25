Shader "DoHwa/UI/Main River"
{
    Properties
    {
        [PerRendererData] _MainTex("Background", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Amplitude("Ripple displacement", Range(0,0.012)) = 0.006
        _Speed("Ripple speed", Range(0,2)) = 0.85
        _Reflection("Reflection shimmer", Range(0,0.1)) = 0.035
        _Feather("Shore feather", Range(0.005,0.08)) = 0.025
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Amplitude, _Speed, _Feather, _Reflection;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            // Inset river contour in this background's UV space; shores stay motionless.
            float RiverMask(float2 p)
            {
                const float2 points[20]={
                    float2(.02,.30),float2(.04,.20),float2(.25,.14),float2(.43,.21),
                    float2(.49,.25),float2(.39,.29),float2(.50,.34),float2(.58,.38),
                    float2(.52,.42),float2(.41,.45),float2(.35,.48),float2(.39,.51),
                    float2(.30,.54),float2(.25,.53),float2(.32,.49),float2(.32,.46),
                    float2(.42,.42),float2(.35,.37),float2(.20,.34),float2(.02,.34)};
                float distanceSq=1; bool inside=false;
                [unroll] for(int i=0,j=19;i<20;j=i,i++)
                {
                    float2 a=points[i], b=points[j], edge=b-a, delta=p-a;
                    float2 nearest=delta-edge*saturate(dot(delta,edge)/dot(edge,edge));
                    distanceSq=min(distanceSq,dot(nearest,nearest));
                    if((a.y>p.y)!=(b.y>p.y))
                        if(p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
                }
                return inside?smoothstep(0,_Feather,sqrt(distanceSq)):0;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float mask=RiverMask(i.uv);
                float t=_Time.y*_Speed;
                float depth=1-smoothstep(.15,.55,i.uv.y);
                float phase=i.uv.y*100+i.uv.x*12-t*2;
                float2 wave=float2(sin(phase)+.35*sin(i.uv.y*183+t*1.4),
                    .75*sin(i.uv.x*32+i.uv.y*92-t*1.7));
                float2 uv=i.uv+wave*_Amplitude*mask*lerp(.25,1,depth);
                half4 color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv)*i.color;
                color.rgb*=1+sin(phase)*_Reflection*mask*depth;
                return color;
            }
            ENDHLSL
        }
    }
}
