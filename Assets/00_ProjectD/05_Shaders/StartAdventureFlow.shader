Shader "DoHwa/UI/Start Adventure Flow"
{
Properties {
[PerRendererData] _MainTex("Sprite",2D)="white"{}
_Color("Light color",Color)=(0.30,0.76,1,1)
_Speed("Flow speed",Range(0,2))=.35
_Strength("Intensity",Range(0,2))=.85
_StencilComp("Stencil Comparison",Float)=8
_Stencil("Stencil ID",Float)=0
_StencilOp("Stencil Operation",Float)=0
_StencilWriteMask("Stencil Write Mask",Float)=255
_StencilReadMask("Stencil Read Mask",Float)=255
_ColorMask("Color Mask",Float)=15
[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Alpha clip",Float)=0
}
SubShader {
Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
Blend SrcAlpha OneMinusSrcAlpha
ColorMask [_ColorMask]
Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
#pragma multi_compile_local _ UNITY_UI_ALPHACLIP
#include "UnityCG.cginc"
#include "UnityUI.cginc"
struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float4 world:TEXCOORD1;};
float4 _Color,_ClipRect;float _Speed,_Strength;
v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
float sparkle(float2 p,float2 center,float phase,float aspect){float2 d=abs(p-center)*float2(aspect,1);float pulse=pow(saturate(.5+.5*sin(phase)),5);float core=exp(-dot(d*float2(65,65),d*float2(65,65)));float rays=exp(-d.x*230-d.y*24)+exp(-d.x*24-d.y*230);return (core+rays*.35)*pulse;}
fixed4 frag(v2f i):SV_Target {
float t=_Time.y*_Speed;float2 p=i.uv;float x=p.x*2-1;float y=p.y-.5;
float aspect=length(ddy(p))/max(length(ddx(p)),.00001);
float edge=pow(saturate(1-x*x),2)*smoothstep(0,.15,p.y)*smoothstep(0,.15,1-p.y);
float wave=.012*sin(x*7-t*2)+.006*sin(x*13+t*1.3);
float haze=exp(-y*y*19)*(.32+.045*sin(x*9-t*2));
float ribbon=exp(-pow((y-wave)*5,2))*.10*(.65+.35*sin(x*14+t*2));
float flow=.55+.45*sin(x*8-t*3);
float streak=exp(-pow((y+.035-wave*.35)*80,2))*.045*flow;
float spark=0;
for(int n=0;n<8;n++){float f=n/7.0;float sx=.09+f*.82+.035*sin(t+n*2);float sy=.5+.17*sin(n*4.7+t*.8);spark+=sparkle(p,float2(sx,sy),t*3+n*2.6,aspect);}
float light=(haze+ribbon+streak)*edge;float alpha=saturate((light+spark*.60)*_Strength)*i.color.a;
#ifdef UNITY_UI_CLIP_RECT
alpha*=UnityGet2DClipping(i.world.xy,_ClipRect);
#endif
#ifdef UNITY_UI_ALPHACLIP
clip(alpha-.001);
#endif
float3 color=lerp(_Color.rgb,float3(1,.91,.96),saturate(spark+streak));return float4(color*i.color.rgb,alpha*_Color.a);
}
ENDCG
}
}
}




