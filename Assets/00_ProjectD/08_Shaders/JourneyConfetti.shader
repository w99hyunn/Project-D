Shader "DoHwa/UI/Journey Confetti" {
Properties {
[PerRendererData] _MainTex("Sprite",2D)="white"{}
_Clicked("Clicked",Float)=0
_ClickTime("Click time",Float)=0
_Color("Tint",Color)=(1,.45,.72,1)
_StencilComp("Stencil Comparison",Float)=8
_Stencil("Stencil ID",Float)=0
_StencilOp("Stencil Operation",Float)=0
_StencilWriteMask("Stencil Write Mask",Float)=255
_StencilReadMask("Stencil Read Mask",Float)=255
_ColorMask("Color Mask",Float)=15
}
SubShader {
Tags {"Queue"="Transparent" "RenderType"="Transparent"}
Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
Blend SrcAlpha OneMinusSrcAlpha
ColorMask [_ColorMask]
Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
#include "UnityCG.cginc"
#include "UnityUI.cginc"
struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float4 world:TEXCOORD1;};
float _Clicked,_ClickTime;float4 _Color,_ClipRect;
v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
float hash(float x){return frac(sin(x*127.1)*43758.5453);}
fixed4 frag(v2f i):SV_Target {
float aspect=length(ddy(i.uv))/max(length(ddx(i.uv)),.00001);
float2 p=(i.uv-.5)*float2(aspect,1);
float progress=saturate(i.color.r);float t=_Clicked>.5?_ClickTime:_Time.y;float elapsed=_Clicked>.5?max(0,_Time.y-_ClickTime):0;float shards=0;
for(int n=0;n<32;n++){
float seed=n+1;float side=n%2==0?-1:1;float h=hash(seed);float life=frac(t*(.24+h*.13)+h);
float travel=(1-exp(-elapsed*3.2))/3.2;
float2 center=float2(side*(.27+life*(.35+h*.3)),(hash(seed+7)-.5)*.18+sin(t*1.8+seed)*.025+life*.06);
float2 velocity=float2(side*(.24+h*.13)*(.35+h*.3),cos(t*1.8+seed)*.045+(.24+h*.13)*.06);
center+=velocity*travel+float2(side*(.04+h*.035),(hash(seed+9)-.5)*.025)*(1-(1+elapsed*7)*exp(-elapsed*7));float2 d=p-center;float angle=t*(h-.5)*3+seed+life*5+elapsed*((h-.5)*3+(.24+h*.13)*5);float2 q=float2(cos(angle)*d.x-sin(angle)*d.y,sin(angle)*d.x+cos(angle)*d.y);
float2 size=float2(.0025+h*.003,.0035+h*.0045);size.x*=.35+.65*abs(sin(angle*1.5));float dist=max(abs(q.x)/size.x,abs(q.y)/size.y);float aa=max(fwidth(dist),.1);float shape=1-smoothstep(1-aa,1+aa,dist);
float fade=sin(life*3.14159)*(1-smoothstep(.18+h*.10,.52+h*.25,elapsed));shards+=shape*fade;
}
float flash=exp(-dot(p*float2(2,7),p*float2(2,7)))*(elapsed*25)*exp(-elapsed*25)*.50;
float alpha=saturate(shards+flash)*i.color.a;alpha*=smoothstep(0,.06,i.uv.y)*smoothstep(0,.06,1-i.uv.y);
#ifdef UNITY_UI_CLIP_RECT
alpha*=UnityGet2DClipping(i.world.xy,_ClipRect);
#endif
return float4(lerp(_Color.rgb,float3(1,.95,.98),saturate(shards*.6+flash)),alpha);
}
ENDCG
}
}
}



