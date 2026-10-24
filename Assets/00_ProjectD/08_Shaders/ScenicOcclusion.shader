Shader "DoHwa/World/Scenic Occlusion"
{
Properties { _BaseMap("Color",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) _Cutoff("Cutoff",Range(0,1))=0.5 }
SubShader {
Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
Cull Off
Pass {
Tags {"LightMode"="UniversalForward"}
HLSLPROGRAM
#pragma vertex Vert
#pragma fragment Frag
#pragma multi_compile_fog
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;float4 _BaseMap_ST;float _Cutoff;
CBUFFER_END
float4 _WorldHero;
struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
V Vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normalOS);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
half4 Frag(V i):SV_Target {
float3 ray=normalize(_WorldSpaceCameraPos-_WorldHero.xyz);float3 delta=(TransformObjectToWorld(float3(0,0,0))+float3(0,14,0))-_WorldHero.xyz;float along=dot(delta,ray);float radial=length(delta-ray*along);
float visibility=1-smoothstep(0,1,along)* (1-smoothstep(10.0,15.0,radial))*_WorldHero.w;
float noise=frac(52.9829189*frac(dot(floor(i.positionCS.xy),float2(.06711056,.00583715))));clip(visibility-noise);
half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;half3 n=normalize(i.normal);Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));half3 color=tex*(SampleSH(n)+sun.color*(.25+.75*saturate(dot(n,sun.direction)))*sun.shadowAttenuation);return half4(MixFog(color,i.fog),1);
}
ENDHLSL
}
UsePass "Universal Render Pipeline/Lit/ShadowCaster"
}
}


