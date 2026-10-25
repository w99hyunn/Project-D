Shader "DoHwa/UI/IconSoftHalo"
{
 Properties { [PerRendererData] _MainTex("Icon",2D)="white"{} _UvRect("Sprite UV",Vector)=(0,0,1,1) }
 SubShader {
 Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
 Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
 Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 sampler2D _MainTex;float4 _UvRect;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=(v.uv-_UvRect.xy)/(_UvRect.zw-_UvRect.xy);o.color=v.color;return o;}
 float Alpha(float2 uv){float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);return tex2D(_MainTex,lerp(_UvRect.xy,_UvRect.zw,saturate(uv))).a*inside;}
 fixed4 frag(v2f i):SV_Target {
 float2 uv=(i.uv-.5)*(46.0/34.0)+.5;
 float sum=0;float weights=0;
 [unroll]for(int y=-3;y<=3;y++) [unroll]for(int x=-3;x<=3;x++) {float w=exp(-(x*x+y*y)/4.5);sum+=Alpha(uv+float2(x,y)*.036)*w;weights+=w;}
 float halo=saturate(sum/weights*1.8);
 return fixed4(i.color.rgb,halo*i.color.a);
 }
 ENDCG
 }
 }
}
