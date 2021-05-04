Shader "Unlit/Matcap"
{
    Properties
    {
        _NormalMap  ("法线贴图", 2D) = "bump" {}
        _Matcap     ("Matcap", 2D) = "gray" {}
        _FresnelPow ("菲涅尔次幂", Range(0, 5)) = 1
        _EnvSpecInt ("环境镜面反射程度", Range(0, 5)) = 1
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

            uniform sampler2D _NormalMap;
            uniform sampler2D _Matcap;
            uniform float _FresnelPow;
            uniform float _EnvSpecInt;

            struct VertexInput {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float4 tangent  : TANGENT;
                float2 uv0      : TEXCOORD0;
            };

            struct VertexOutput {
                float4 pos  : SV_POSITION;
                float3 posWS: TEXCOORD0;
                float2 uv0  : TEXCOORD1;
                float3 nDirWS : TEXCOORD2;
                float3 tDirWS : TEXCOORD3;
                float3 bDirWS : TEXCOORD4;
            };

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.posWS = mul(unity_ObjectToWorld, v.vertex);
                o.uv0 = v.uv0;
                o.nDirWS = UnityObjectToWorldNormal(v.normal);
                o.tDirWS = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0.0)));
                o.bDirWS = normalize(cross(o.nDirWS, o.tDirWS) * v.tangent.w);
                return o;
            }

            fixed4 frag (VertexOutput i) : COLOR
            {
                // 贴图采样
                // 向量准备
                float3 nDirTS = UnpackNormal(tex2D(_NormalMap, i.uv0));
                float3x3 TBN = float3x3(i.tDirWS, i.bDirWS, i.nDirWS);
                float3 nDirWS = normalize(mul(nDirTS, TBN));
                float3 nDirVS = mul(UNITY_MATRIX_V, float4(nDirWS, 0.0));
                float3 vDirWS = normalize(_WorldSpaceCameraPos.xyz - i.posWS.xyz);
                // 中间量准备
                float2 matcapUV = nDirVS.rg * 0.5 + 0.5;
                float ndotv = dot(nDirWS, vDirWS);
                // 光照模型
                float3 matcap = tex2D(_Matcap, matcapUV);
                float fresnel = pow(1.0 - ndotv, _FresnelPow);
                
                float envSpecialLighting = matcap * fresnel * _EnvSpecInt;
                // 后处理

                // 返回值
                // return fixed4(matcap, 1);
                return envSpecialLighting;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
