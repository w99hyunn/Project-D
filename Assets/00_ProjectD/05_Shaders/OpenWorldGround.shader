Shader "DoHwa/Open World Ground"
{
Properties { _BaseMap("Ground grain",2D)="white"{} }
SubShader{
Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
Pass{
Tags{"LightMode"="UniversalForward"}
HLSLPROGRAM
#pragma vertex Vert
#pragma fragment Frag
#pragma multi_compile_fog
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;};
struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half4 color:COLOR;half fog:TEXCOORD2;};
V Vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normalOS);o.color=i.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
half4 Frag(V i):SV_Target{float2 uv=float2(.675,.508)+abs(frac(i.world.xz*.13)*2-1)*float2(.310,.480);half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;half grain=dot(tex,half3(.25,.6,.15));half3 normal=normalize(i.normal);Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));half3 color=i.color.rgb*(.75+grain*.7)*(SampleSH(normal)*.7+sun.color*saturate(dot(normal,sun.direction))*sun.shadowAttenuation*.85);return half4(MixFog(color,i.fog),1);}
ENDHLSL
}
UsePass "Universal Render Pipeline/Lit/DepthOnly"
}
}
