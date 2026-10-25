Shader "DoHwa/Peach Forest Ground"
{
    Properties
    {
        _BaseMap("Painted atlas", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _PathColor("Warm worn earth", Color) = (0.42,0.32,0.23,1)
        _MossColor("Leaf litter floor", Color) = (.38,.29,.30,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _PathColor;
                half4 _MossColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; half fog : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p=input.positionWS.xz;
                float2 mirrored=abs(frac(p*0.16)*2-1);
                float2 uv=float2(0.675,0.508)+mirrored*float2(0.310,0.480);
                float2 second=abs(frac(float2(p.x+p.y*.25,p.y-p.x*.2)*0.113+0.37)*2-1);
                float2 uv2=float2(0.675,0.508)+second*float2(0.310,0.480);
                half3 moss=lerp(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb,
                    SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv2).rgb,0.42);
                                moss *= half3(0.75,0.83,0.92);
                half mossLuma=dot(moss,half3(.25,.60,.15));
                // Keep the painted surface grain without carrying its green moss hue.
                moss=_MossColor.rgb*(.72+mossLuma*.80);
                moss*=.97+.035*sin(p.x*.28)*cos(p.y*.19);
                float pathX=1.7*sin(p.y*0.115)-0.9*sin(p.y*0.23);
                float edge=0.10*sin(p.y*2.1)+0.09*sin(p.x*3.7+p.y*1.3);
                half path=1-smoothstep(1.3,2.55,abs(p.x-pathX)+edge);
                float spur=abs(p.y-8.1);
                path=max(path,(1-smoothstep(0.7,1.35,spur))*step(1,p.x)*step(p.x,14));
                half grain=dot(moss,half3(.22,.62,.16));
                half soilVariation=sin(p.x*1.3+p.y*.67)*sin(p.y*1.16-p.x*.51);
                half fineGrain=sin(p.x*59+p.y*37)*sin(p.x*31-p.y*73);
                half3 earth=_PathColor.rgb*(0.84+grain*.76+soilVariation*.045+fineGrain*.018);
                float2 cell=floor(p*3.8);
                float random=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
                float2 center=float2(frac(random*7.13),frac(random*13.67))*.54+.23;
                float distance=length((frac(p*3.8)-center)*float2(1,1.4));
                half pebble=(1-smoothstep(.045,.075,distance))*step(random,.08);
                earth=lerp(earth,earth*.78+half3(.035,.035,.04),pebble);
                half3 paint=lerp(moss,earth,path);
                Light light=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half shade=saturate(dot(normalize(input.normalWS),light.direction))*light.shadowAttenuation;
                half3 indirect=SampleSH(normalize(input.normalWS))*.70;
                half3 color=paint*(indirect+light.color*shade*.85);
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}


