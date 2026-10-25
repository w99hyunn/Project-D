Shader "DoHwa/Peach Forest Water"
{
    Properties
    {
        _BaseColor("Deep jade", Color) = (.15,.32,.36,1)
        _ShallowColor("Shallow water", Color) = (.32,.46,.39,1)
        _ReflectionColor("Warm sky", Color) = (.70,.64,.70,1)
        _WaveScale("Ripple scale",Float)=2.2
        _WaveSpeed("Current speed",Float)=.85
        _FlowStrength("Visible current",Range(0,1))=.20
        _FoamStrength("Shore foam",Range(0,1))=.30
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShallowColor;
                half4 _ReflectionColor;
                float _WaveScale;
                float _WaveSpeed;
                float _FlowStrength;
                float _FoamStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half fog:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            float Hash(float2 p)
            {
                return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);
            }
            float Noise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);
                f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),
                    lerp(Hash(cell+float2(0,1)),Hash(cell+float2(1,1)),f.x),f.y);
            }
            float FlowNoise(float2 p)
            {
                return Noise(p)*.60+Noise(p*2.07+13.2)*.28+Noise(p*4.13-7.8)*.12;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 uv=GetNormalizedScreenSpaceUV(input.positionCS);
                float raw=SampleSceneDepth(uv);
                float sceneDepth=IsPerspectiveProjection()?LinearEyeDepth(raw,_ZBufferParams):LinearDepthToEyeDepth(raw);
                float surfaceDepth=-TransformWorldToView(input.positionWS).z;
                float depth=max(0,sceneDepth-surfaceDepth);
                clip(depth-.006);
                float t=_Time.y*_WaveSpeed;
                float along=input.positionWS.z;
                float across=input.positionWS.x-(10+2.5*sin(along*.11+.8));
                // Advect along the original curved stream, downstream towards negative world Z.
                float2 flow=float2(across*1.65,along*.65+t);
                float swell=FlowNoise(flow);
                float slopeX=(FlowNoise(flow+float2(.07,0))-swell)*1.9;
                float slopeZ=(FlowNoise(flow+float2(0,.07))-swell)*1.9;
                float bend=.275*cos(along*.11+.8);
                half3 normal=normalize(half3(-slopeX,1,-slopeZ+slopeX*bend));
                half3 view=GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel=pow(1-saturate(dot(view,normal)),2);
                Light light=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 water=lerp(_ShallowColor.rgb,_BaseColor.rgb,smoothstep(.08,1.15,depth));
                half3 bed=SampleSceneColor(uv+normal.xz*.004);
                water=lerp(bed*half3(.73,.87,.83),water,.65);
                water=lerp(water,_ReflectionColor.rgb,fresnel*.45);
                half glint=pow(saturate(dot(normal,normalize(view+light.direction))),75);
                float curl=FlowNoise(float2(across*2.4,along*.34+t*.55)+swell*.8);
                half streak=smoothstep(.61,.77,curl)*smoothstep(.30,.70,swell);
                half shore=(1-smoothstep(.035,.32,depth))*smoothstep(.42,.70,FlowNoise(flow*2.3));
                water*=.78+.22*light.shadowAttenuation;
                water+=glint*light.color*light.shadowAttenuation*.30;
                water=lerp(water,half3(.60,.77,.74),streak*_FlowStrength);
                water=lerp(water,half3(.80,.85,.77),shore*_FoamStrength);
                return half4(MixFog(water,input.fog),lerp(.68,.94,smoothstep(.015,.38,depth)));
            }
            ENDHLSL
        }
    }
}
