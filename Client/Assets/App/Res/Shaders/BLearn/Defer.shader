Shader "Unlit/Defer"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _Diffuse("Diffuse", Color) = (1.0, 1.0, 1.0, 1.0)
        _Specular("_Specular", Color) = (1.0, 1.0, 1.0, 1.0)
        _Gloss("Gloss", Range(8.0, 256)) = 20
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            Tags {"LightMode" = "Deferred"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform fixed4 _Diffuse;
            uniform fixed4 _Specular;
            uniform float _Gloss;

            struct VertexInput {
                float4 vertex: POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct VertexOutput {
                float4 pos: SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 nDirWS: TEXCOORD1;
                float3 posWS : TEXCOORD2;
            };

            struct DefferedOutput {
                float4 gBuffer0 : SV_TARGET0;
                float4 gBuffer1 : SV_TARGET1;
                float4 gBuffer2 : SV_TARGET2;
                float4 gBuffer3 : SV_TARGET3;
            };

            VertexOutput vert(VertexInput v) {
                VertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.nDirWS = UnityObjectToWorldNormal(v.normal);
                o.posWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;  
            }
            DefferedOutput frag(VertexOutput i) {
                DefferedOutput o;
                fixed3 color = tex2D(_MainTex, i.uv).rgb * _Diffuse.rgb;
                o.gBuffer0.rgb = color;
                o.gBuffer0.a = 1;
                o.gBuffer1.rgb = _Specular.rgb;
                o.gBuffer1.a = _Gloss;
                o.gBuffer2 = float4(normalize(i.nDirWS), 1);
                o.gBuffer3 = fixed4(color, 1);
                return o;
            }

            ENDCG
        }
    }
}
