Shader "DoHwa/UI/ButtonSheen"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Tint("Tint",Color)=(.94,.98,1,1) _Edge("Edge",Float)=.13 _Phase("Phase",Float)=0 _Strength("Strength",Float)=0 _StencilComp("Stencil Comparison",Float)=8 _Stencil("Stencil ID",Float)=0 _StencilOp("Stencil Operation",Float)=0 _StencilWriteMask("Stencil Write Mask",Float)=255 _StencilReadMask("Stencil Read Mask",Float)=255 _ColorMask("Color Mask",Float)=15 }
 SubShader {
 Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
 Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
 Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
 #include "UnityCG.cginc"
 #include "UnityUI.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 surface:TEXCOORD1;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float2 local:TEXCOORD1;float2 surface:TEXCOORD2;fixed4 color:COLOR;};
 sampler2D _MainTex;float _Phase,_Strength,_Edge;float4 _Tint;float4 _ClipRect;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.local=v.vertex.xy;o.surface=v.surface;o.color=v.color;return o;}
 fixed4 frag(v2f i):SV_Target {
 float2 p=i.surface;
 float stripe=exp(-pow((p.x+p.y*.18-lerp(-.25,1.3,_Phase))*8,2));
 float edge=smoothstep(.32,.49,abs(p.y-.5));
 fixed4 sample=tex2D(_MainTex,i.uv);
 float alpha=sample.a*((stripe*.42+edge*_Edge)*_Strength)*i.color.a;
 #ifdef UNITY_UI_CLIP_RECT
 alpha*=UnityGet2DClipping(i.local,_ClipRect);
 #endif
 float3 rgb=_Tint.rgb;
 return fixed4(rgb,alpha);
 }
 ENDCG
 }
 }
}


