Shader "DoHwa/UI/Menu Edge Shade"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture",2D)="white" {}
        _Color("Shade",Color)=(0.07,0.10,0.16,0.62)
        [Toggle] _LeftEdge("Left edge",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            fixed4 _Color;
            float _LeftEdge;
            v2f vert(appdata_full v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;o.color=v.color*_Color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float edge=lerp(i.uv.x,1-i.uv.x,_LeftEdge);
                i.color.a*=smoothstep(0,1,edge)* (1-smoothstep(.65,1,i.uv.y));
                return i.color;
            }
            ENDCG
        }
    }
}
