Shader "Unlit/OldSchool"
{
    Properties
    {
        _MainCol        ("颜色",        color) =       (1.0, 1.0, 1.0, 1.0)
        _SpecularPow    ("高光次幂",    range(1, 90)) = 30
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "FORWARD"
            Tags {
                "LightMode" = "ForwardBase"
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma target 3.0

            // 输入参数
            uniform float3 _MainCol;
            uniform float _SpecularPow;
            // 输入结构
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            // 输出结构
            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float4 posWS : TEXCOORD0;
                float3 nDirWS : TEXCOORD1;
            };

            // 输入结构 >> 顶点Shader >> 输出结构
            VertexOutput vert (VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;
                o.posCS = UnityObjectToClipPos(v.vertex);
                o.posWS = mul(unity_ObjectToWorld, v.vertex);
                o.nDirWS = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            // 输出结构 >> 像素
            fixed4 frag (VertexOutput i) : COLOR
            {
                // 准备向量
                float3 nDir = i.nDirWS;
                float3 lDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 vDir = normalize(_WorldSpaceCameraPos.xyz - i.posWS.xyz);
                float3 hDir = normalize(vDir + lDir);
                // 准备点积结果(中间数据)
                float nDotl = dot(nDir, lDir);
                float nDoth = dot(nDir, hDir);
                // 光照模型
                float lambert = max(0.0, nDotl);
                float blinnPhong = pow(max(0.0, nDoth), _SpecularPow);
                float3 finalRGB = _MainCol * lambert + blinnPhong;
                // 返回结果
                return float4(finalRGB, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
