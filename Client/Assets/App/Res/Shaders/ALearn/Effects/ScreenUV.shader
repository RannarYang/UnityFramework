Shader "Unlit/ScreenUV"
{
    Properties
    {
        _MainTex("RGB：颜色", 2D) = "white" {}
        _Opacity("透明度", Range(0, 1)) = 0.5
        _ScreenTex("屏幕纹理", 2D) = "white" {}
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

            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma target 3.0

            uniform sampler2D _MainTex;
            uniform half _Opacity;
            uniform sampler2D _ScreenTex; uniform float4 _ScreenTex_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv0: TEXCOORD0;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 screenUV: TEXCOORD1;
            };

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv0 = v.uv0;

                float3 posVS = UnityObjectToViewPos(v.vertex).xyz;
                float originDst = UnityObjectToViewPos(float4(0.0, 0.0, 0.0, 0.0)).z;
                o.screenUV = posVS.xy / posVS.z;
                o.screenUV *= originDst;
                o.screenUV = o.screenUV * _ScreenTex_ST.xy - frac(_Time.x *_ScreenTex_ST.zw);
                return o;
            }

            half4 frag (VertexOutput i) : COLOR
            {
                half4 var_MainTex = tex2D(_MainTex, i.uv0);
                half4 var_ScreenTex = tex2D(_ScreenTex, i.screenUV).r;

                half3 finalRGB = var_MainTex.rgb;
                half opacity = var_MainTex.a * _Opacity * var_ScreenTex;
                return half4(finalRGB * opacity, opacity);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
