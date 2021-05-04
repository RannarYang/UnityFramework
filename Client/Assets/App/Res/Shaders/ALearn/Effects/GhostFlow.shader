Shader "Unlit/GhostFlow"
{
    Properties
    {
        _MainTex("RGB：颜色，A:透贴", 2D) = "white" {}
        _Opacity("透明度", Range(0, 1)) = 0.5
        _NoiseTex("噪声图", 2D) = "white" {}
        _NoiseInt("噪声强度", Range(0, 5)) = 0.5
        _FlowSpeed("流动速度", Range(0, 10)) = 5
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
            uniform sampler2D _NoiseTex; uniform float4 _NoiseTex_ST;
            uniform half _NoiseInt;
            uniform half _FlowSpeed;

            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv0: TEXCOORD0;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv0 = v.uv0;
                o.uv1 = TRANSFORM_TEX(v.uv0, _NoiseTex);
                o.uv1.y = o.uv1.y + frac(_Time.x * _FlowSpeed);
                return o;
            }

            half4 frag (VertexOutput i) : COLOR
            {
                half4 var_MainTex = tex2D(_MainTex, i.uv0);
                half var_NoiseTex = tex2D(_NoiseTex, i.uv1).r;

                half3 finalRGB = var_MainTex.rgb;
                half noise = lerp(1.0, var_NoiseTex * 2, _NoiseInt);
                half opacity = var_MainTex.a * _Opacity * noise;

                return half4(finalRGB * opacity, opacity);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
