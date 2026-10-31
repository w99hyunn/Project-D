Shader "DoHwa/UI/EquipmentLens"
{
 Properties { _MainTex("Texture",2D)="white"{} _Shadow("Shadow",Float)=0 _Focus("Focus",Range(0,1))=0 _Selected("Selected",Range(0,1))=0 _Occupied("Occupied",Range(0,1))=0 _Press("Press",Range(0,1))=0
 [HideInInspector] _StencilComp("Stencil Comparison",Float)=8
 [HideInInspector] _Stencil("Stencil ID",Float)=0
 [HideInInspector] _StencilOp("Stencil Operation",Float)=0
 [HideInInspector] _StencilWriteMask("Stencil Write Mask",Float)=255
 [HideInInspector] _StencilReadMask("Stencil Read Mask",Float)=255
 [HideInInspector] _ColorMask("Color Mask",Float)=15
 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
 ColorMask [_ColorMask]
 Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
 #include "UnityCG.cginc"
 #include "UnityUI.cginc"
 struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float2 localPosition:TEXCOORD1;};
 float4 _ClipRect;
 float _Focus,_Selected,_Occupied,_Press,_Shadow,_EffectTime;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;o.localPosition=v.vertex.xy;return o;}
 fixed4 frag(v2f i):SV_Target{
 float clipAlpha=1;
 #ifdef UNITY_UI_CLIP_RECT
 clipAlpha=UnityGet2DClipping(i.localPosition,_ClipRect);
 #endif
 float2 p=(i.uv-.5)*2;float r=length(p);if(_Shadow>.5)return fixed4(.22,.27,.34,exp(-r*r*5)*.24*i.color.a*clipAlpha);float edge=.79;float aa=max(fwidth(r),.006);
 float inside=1-smoothstep(edge-aa,edge+aa,r);
 float well=1-smoothstep(.685-aa,.685+aa,r);
 float light=saturate(dot(normalize(p+float2(.0001,0)),normalize(float2(-.6,.8)))*.5+.5);
 float focus=saturate(_Focus+_Selected);
 float activity=saturate(_Occupied*.45+focus);
 float angle=atan2(p.y,p.x);
 float sweep=pow(saturate(cos(angle-_EffectTime*.65)),24);
 float breath=.5+.5*sin(_EffectTime*1.45);
 float halo=exp(-abs(r-edge)*18)*(.055+activity*(.12+.035*breath));
 float movingLight=sweep*activity;
 float lower=saturate(-p.y)*inside;
 float castShadow=exp(-dot(p-float2(0,-.055),p-float2(0,-.055))*3.5)*.18;
 float alpha=inside+(halo+castShadow)*(1-inside);
 float3 gold=lerp(float3(.98,.53,.10),float3(1,.77,.30),saturate(p.y*.5+.65));
 float3 cool=lerp(float3(.63,.69,.77),float3(.89,.93,.97),saturate(p.y*.5+.6));
 float3 fill=lerp(cool,gold,_Occupied);
 // Two warped wave fields create flowing internal contours rather than a rotating decal.
 float time=_EffectTime*.58;
 float2 flow=p*4.3;
 flow+=float2(sin(p.y*5.2+time),cos(p.x*4.6-time*.8))*.55;
 float wave=sin(flow.x+flow.y*.7+time)+sin(flow.y*1.4-flow.x*.5-time*.73)*.55;
 float wave2=sin(flow.y-flow.x*.85-time*.9)+cos(flow.x*1.2+time*.6)*.45;
 float strands=pow(saturate(1-abs(wave)*1.35),3);
 float crossing=pow(saturate(1-abs(wave2)*1.8),4);
 float energy=(strands*.20+crossing*.10)*(.42+_Occupied*.58+_Focus*.22);
 float edgeFade=1-smoothstep(.54,.685,r);
 float3 energyColor=lerp(float3(.80,.94,1),float3(1,.96,.62),_Occupied);
 fill=lerp(fill,energyColor,saturate(energy*edgeFade));
 fill*=1-(.5+.5*sin(wave*2+time*.4))*.055*edgeFade;
 float warm=_Occupied;
 float innerGlow=exp(-abs(r-.65)*17);
 fill=lerp(fill,float3(1,.98,.78),innerGlow*warm*(.65+.12*breath));
 fill=lerp(fill,float3(.89,.96,1),_Selected*(1-_Occupied)*.32);
 float caustic=exp(-pow((r-(.59+.025*sin(angle*3-_EffectTime*.8)))*45,2));
 fill+=caustic*warm*(.045+.055*movingLight)*float3(1,.86,.49);
 fill+=exp(-pow((p.y-.32-p.x*.2)*6,2))*.035;
 float3 bevel=lerp(float3(.87,.90,.94),float3(1,1,1),light);
 float lip=exp(-abs(r-.78)*150)+exp(-abs(r-.70)*130)*.65;
 bevel=lerp(bevel,float3(1,1,1),saturate(lip+movingLight));
 float3 rgb=lerp(bevel,fill,well);
 float3 outside=lerp(float3(.28,.32,.40),float3(.97,.98,1),saturate(halo/max(halo+castShadow,.001)));
 rgb=lerp(outside,rgb,inside);
 alpha+=exp(-abs(r-edge)*65)*movingLight*.3;
 rgb*=1-_Press*.12;
 alpha*=1-_Press*.12;
 return fixed4(rgb,saturate(alpha)*i.color.a*clipAlpha);
 }
 ENDCG
 }
 }
}


