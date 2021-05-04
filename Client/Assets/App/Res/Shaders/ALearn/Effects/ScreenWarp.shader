Shader "Unlit/ScreenWarp"
{
    Properties
    {
        _MainTex("RGB：颜色", 2D) = "white" {}
        _Opacity("透明度", Range(0, 1)) = 0.5
        _WrapMidVal("扰动中间值", Range(0, 1)) = 0.5
        _WrapInt("扰动强度", Range(0, 5)) = 1
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

        // 获取背景纹理
        GrabPass {
            "_BGTex"
        }

        Pass
        {
            Name "FORWARD"
            Tags {
                "LightMode" = "ForwardBase"
            }

            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma target 3.0

            uniform sampler2D _MainTex;
            uniform half _Opacity;
            uniform sampler2D _BGTex;
            uniform half _WrapMidVal;
            uniform half _WrapInt;

            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv0: TEXCOORD0;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 grabPos: TEXCOORD1;
            };

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv0 = v.uv0;
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }

            half4 frag (VertexOutput i) : COLOR
            {
                half4 var_MainTex = tex2D(_MainTex, i.uv0);
                i.grabPos.xy += (var_MainTex.b - _WrapMidVal) * _WrapInt * _Opacity;
                half3 var_BGTex = tex2D(_BGTex, i.grabPos).rgb;

                half3 finalRGB = lerp(1.0, var_MainTex.rgb, _Opacity) * var_BGTex;
                half opacity = var_MainTex.a;
                return half4(finalRGB * opacity, opacity);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
