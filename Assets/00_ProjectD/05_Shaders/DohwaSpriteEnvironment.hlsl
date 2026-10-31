#define _SURFACE_TYPE_TRANSPARENT 1
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
TEXTURE2D(_HairMask); SAMPLER(sampler_HairMask);
TEXTURE2D(_JacketMask); SAMPLER(sampler_JacketMask);
TEXTURE2D(_BottomMask); SAMPLER(sampler_BottomMask);
TEXTURE2D(_BootsMask); SAMPLER(sampler_BootsMask);
CBUFFER_START(UnityPerMaterial)
half4 _Color;
float4 _Equipped;
float _ShowCovered;
float _EnvironmentStrength;
float _ShadowInfluence;
float _PaletteStrength;
float _UprightDepth;
float4 _ActorGround;
CBUFFER_END
struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; half3 environment:TEXCOORD1; float fog:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
Varyings Vert(Attributes input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    SetUpSpriteInstanceProperties();
    Varyings output;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    float3 positionWS = TransformObjectToWorld(UnityFlipSprite(input.positionOS, unity_SpriteProps.xy));
    output.positionCS = TransformWorldToHClip(positionWS);
    if (_UprightDepth > .5 && _ActorGround.w > .5)
    {
        // Preserve the visible billboard, but test 3D occlusion against an upright actor plane.
        float3 planeNormal = float3(UNITY_MATRIX_V[2][0], 0, UNITY_MATRIX_V[2][2]);
        float3 ray = positionWS - _WorldSpaceCameraPos;
        float denominator = dot(ray, planeNormal);
        float distanceRatio = dot(_ActorGround.xyz - _WorldSpaceCameraPos, planeNormal) / denominator;
        float3 uprightPosition = _WorldSpaceCameraPos + ray * distanceRatio;
        float4 uprightCS = TransformWorldToHClip(uprightPosition);
        output.positionCS.z = uprightCS.z / uprightCS.w * output.positionCS.w;
    }
    output.fog = ComputeFogFactor(output.positionCS.z);
    output.uv = input.uv;
    output.color = input.color * _Color * unity_SpriteColor;
    // A fixed world-space direction preserves the artist's shading when the camera or sprite rotates.
    Light sun = GetMainLight();
    half3 illumination = max(SampleSH(half3(0,1,0)), 0) + sun.color * .35;
    half peak = max(max(illumination.r, illumination.g), illumination.b);
    half3 chroma = illumination / max(peak, .001);
    half brightness = lerp(.82, 1, saturate(dot(illumination, half3(.2126,.7152,.0722))));
    output.environment = lerp(half3(1,1,1), chroma * brightness, _EnvironmentStrength);
    return output;
}
half4 Frag(Varyings input):SV_Target
{
    half4 pixel = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv) * input.color;
    #if defined(DOHWA_BODY)
        float4 mask = float4(SAMPLE_TEXTURE2D(_HairMask,sampler_HairMask,input.uv).r,
            SAMPLE_TEXTURE2D(_JacketMask,sampler_JacketMask,input.uv).g,
            SAMPLE_TEXTURE2D(_BottomMask,sampler_BottomMask,input.uv).b,
            SAMPLE_TEXTURE2D(_BootsMask,sampler_BootsMask,input.uv).a) * _Equipped;
        float covered = max(max(mask.r,mask.g),max(mask.b,mask.a));
        pixel.a *= lerp(1-covered,covered,_ShowCovered);
    #endif
    float3 groundPosition = _ActorGround.w > .5 ? _ActorGround.xyz : TransformObjectToWorld(float3(0,0,0));
    Light groundLight = GetMainLight(TransformWorldToShadowCoord(groundPosition));
    // Sample one stable receiver point for the whole actor instead of the tilted sprite plane.
    half occlusion = lerp(1, lerp(.65, 1, groundLight.shadowAttenuation), _ShadowInfluence);
    // Compress only the bright end and tint the darkest ink, preserving pixel edges and alpha.
    half luminance = dot(pixel.rgb, half3(.2126,.7152,.0722));
    half inkWeight = 1 - smoothstep(.015, .12, luminance);
    half highlightWeight = smoothstep(.22, .75, luminance);
    half3 integratedPalette = pixel.rgb * (1 - .12 * highlightWeight)
        + half3(.016,.012,.018) * inkWeight;
    pixel.rgb = lerp(pixel.rgb, integratedPalette, _PaletteStrength);
    pixel.rgb *= input.environment * occlusion;
    pixel.rgb = MixFog(pixel.rgb, input.fog);
    return pixel;
}

