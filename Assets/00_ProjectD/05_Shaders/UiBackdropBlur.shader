Shader "DoHwa/UI/BackdropBlur"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader {
        Cull Off ZWrite Off ZTest Always
        Pass {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Direction;
            half4 frag(v2f_img i) : SV_Target {
                half4 c=tex2D(_MainTex,i.uv)*.227027;
                c+=tex2D(_MainTex,i.uv+_Direction.xy*1.384615)*.316216;
                c+=tex2D(_MainTex,i.uv-_Direction.xy*1.384615)*.316216;
                c+=tex2D(_MainTex,i.uv+_Direction.xy*3.230769)*.070270;
                c+=tex2D(_MainTex,i.uv-_Direction.xy*3.230769)*.070270;
                return c;
            }
            ENDHLSL
        }
    }
}
