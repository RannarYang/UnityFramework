Shader "Unlit/AlphaAdditive"
{
    Properties
    {
        _MainTex("RGB：颜色", 2D) = "white" {}
        _Opacity("透明度", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { 
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "ForceNoShadowCasting" = "True"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "FORWARD"
            Tags {
                "LightMode" = "ForwardBase"
            }

            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma target 3.0

            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform half _Opacity;

            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv0: TEXCOORD0;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
            };

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                return o;
            }

            half4 frag (VertexOutput i) : COLOR
            {
                half4 var_MainTex = tex2D(_MainTex, i.uv0);
                half3 finalRGB = var_MainTex.rgb;
                half opacity = var_MainTex.a * _Opacity;
                return half4(finalRGB * opacity, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
