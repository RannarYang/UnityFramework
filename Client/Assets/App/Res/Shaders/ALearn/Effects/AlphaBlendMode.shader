Shader "Unlit/AlphaBlendMode"
{
    Properties
    {
        _MainTex("RGB：颜色", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.BlendMode)]
        _BlendSrc("混合源乘子", int) = 0
        [Enum(UnityEngine.Rendering.BlendMode)]
        _BlendDst("混合目标乘子", int) = 0
        [Enum(UnityEngine.Rendering.BlendOp)]
        _BlendOp("混合算符", int) = 0
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

            BlendOp [_BlendOp]
            Blend [_BlendSrc] [_BlendDst]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma target 3.0

            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform half _Cutoff;

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
                return var_MainTex;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
