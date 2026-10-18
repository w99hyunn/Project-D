Shader "DoHwa/Layered Body"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [PerRendererData] _HairMask ("Hair Mask", 2D) = "black" {}
        [PerRendererData] _JacketMask ("Jacket Mask", 2D) = "black" {}
        [PerRendererData] _BottomMask ("Bottom Mask", 2D) = "black" {}
        [PerRendererData] _BootsMask ("Boots Mask", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Equipped ("Equipped", Vector) = (1,1,1,1)
        _ShowCovered ("Show covered skin", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex, _HairMask, _JacketMask, _BottomMask, _BootsMask;
            fixed4 _Color;
            float4 _Equipped;
            float _ShowCovered;
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(Output i) : SV_Target
            {
                fixed4 pixel = tex2D(_MainTex, i.uv) * i.color;
                float4 mask = float4(tex2D(_HairMask, i.uv).r, tex2D(_JacketMask, i.uv).g,
                    tex2D(_BottomMask, i.uv).b, tex2D(_BootsMask, i.uv).a) * _Equipped;
                float covered = max(max(mask.r, mask.g), max(mask.b, mask.a));
                pixel.a *= lerp(1 - covered, covered, _ShowCovered);
                return pixel;
            }
            ENDCG
        }
    }
}
