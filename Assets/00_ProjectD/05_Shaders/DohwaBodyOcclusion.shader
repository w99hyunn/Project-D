Shader "DoHwa/Layered Body"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        [Toggle] _UprightDepth("Upright world occlusion", Float) = 0
        _PaletteStrength("Palette integration", Range(0,1)) = 0
        _ShadowInfluence("Environment shadow influence", Range(0,1)) = .6
        [PerRendererData] _ActorGround("Actor ground", Vector) = (0,0,0,0)
        _EnvironmentStrength("Environment influence", Range(0,1)) = .26
        [PerRendererData] _HairMask("Hair Mask", 2D) = "black" {}
        [PerRendererData] _JacketMask("Jacket Mask", 2D) = "black" {}
        [PerRendererData] _BottomMask("Bottom Mask", 2D) = "black" {}
        [PerRendererData] _BootsMask("Boots Mask", 2D) = "black" {}
        _Equipped("Equipped", Vector) = (1,1,1,1)
        _ShowCovered("Show covered skin", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #define DOHWA_BODY 1
            #include "DohwaSpriteEnvironment.hlsl"
            ENDHLSL
        }
    }
}
