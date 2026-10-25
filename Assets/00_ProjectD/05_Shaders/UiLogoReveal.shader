Shader "DoHwa/UI/Logo Reveal"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 motion:TEXCOORD1; };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; float progress:TEXCOORD2; };
            sampler2D _MainTex;
            float4 _TextureSampleAdd, _ClipRect;
            v2f vert(appdata v)
            {
                v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);
                o.color=v.color; o.uv=v.uv; o.progress=v.motion.x; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 source=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
                float front=lerp(-.2,1.3,i.progress);
                float position=i.uv.x+(i.uv.y-.5)*.12;
                float distance=front-position;
                float reveal=smoothstep(-.055,.055,distance);
                float sheen=exp(-pow((distance-.075)/.06,2))*sin(saturate(i.progress)*UNITY_PI);
                float3 tint=lerp(float3(.62,.91,1),float3(1,.76,.88),i.uv.x);
                float lightness=max(source.r,max(source.g,source.b));
                source.rgb=lerp(source.rgb,tint,sheen*.4*lightness);
                source.a*=reveal;
                #ifdef UNITY_UI_CLIP_RECT
                source.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(source.a-.001);
                #endif
                return source;
            }
            ENDCG
        }
    }
}
