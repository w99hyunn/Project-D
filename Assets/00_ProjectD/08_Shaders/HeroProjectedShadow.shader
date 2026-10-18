Shader "DoHwa/Hero Projected Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        _Color("Shadow tint", Color) = (.10,.08,.12,.30)
        _GroundPlane("Ground plane", Vector) = (0,1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            // All character layers share one coverage mask, so overlap is shaded once.
            Stencil
            {
                Ref 1
                ReadMask 1
                WriteMask 1
                Comp NotEqual
                Pass Replace
            }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float4 _GroundPlane;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half alpha:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS=TransformObjectToWorld(input.positionOS.xyz);
                Light sun=GetMainLight();
                float height=dot(_GroundPlane.xyz,positionWS)+_GroundPlane.w;
                positionWS-=sun.direction*height/max(.25,dot(_GroundPlane.xyz,sun.direction));
                output.positionCS=TransformWorldToHClip(positionWS);
                output.uv=input.uv;
                output.alpha=input.color.a;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a;
                half opacity=alpha*_Color.a*input.alpha;
                clip(opacity-.001);
                return half4(_Color.rgb,opacity);
            }
            ENDHLSL
        }
    }
}
