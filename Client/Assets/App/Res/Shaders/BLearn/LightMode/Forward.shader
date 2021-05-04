Shader "Unlit/Forward"
{
    Properties
    {
        _Diffuse("Diffuse", Color) = (1.0, 1.0, 1.0, 1.0)
        _Specual("Special", Color) = (1.0, 1.0, 1.0, 1.0)
        _Gloss("Gloss", Range(8.0, 256)) = 20
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags {"LightMode" = "ForwardBase"}

            CGPROGRAM
            #pragma multi_compile_fwdbase
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            uniform fixed4 _Diffuse;
            uniform fixed4 _Specual;
            uniform float _Gloss;

            struct VertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct VertexOutput
            {
                float4 posCS : SV_POSITION;
                float3 nDirWS : TEXCOORD0;
                float3 posWS : TEXCOORD1;
                float3 vertexLight: TEXCOORD2;
            };

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o;
                o.posCS = UnityObjectToClipPos(v.vertex);
                o.nDirWS = UnityObjectToWorldNormal(v.normal);
                o.posWS = mul(unity_ObjectToWorld, v.vertex).xyz;
#ifdef LIGHTMAP_OFF
                float3 shLight = ShadeSH9(float4(v.normal, 1.0));
                o.vertexLight = shLight;

#ifdef VERTEXLIGHT_ON
                float3 vertexLight = Shade4PointLights(
                    Unity_4LightPosX0, Unity_4LightPosY0, Unity_4LightPosZ0, 
                    Unity_LightColor[0].rgb, Unity_LightColor[1].rgb, Unity_LightColor[2].rgb, Unity_LightColor[3].rgb,
                    Unity_4LightAtten0, o.posWS, o.nDirWS
                );
                o.vertexLight += vertexLight;
#endif
#endif
                return o;
            }

            fixed4 frag (VertexOutput i) : SV_Target
            {
                fixed3 nDirWS = normalize(i.nDirWS);
                fixed3 lDirWS = normalize(UnityWorldSpaceLightDir(i.posWS));
                fixed3 vDirWS = normalize(_WorldSpaceCameraPos.xyz - i.posWS.xyz);
                fixed3 hDirWS = normalize(lDirWS + vDirWS);

                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;
                fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * max(0, dot(nDirWS, lDirWS));
                fixed3 specular = _LightColor0.rgb * _Specual.rgb * pow(max(0, dot(nDirWS, hDirWS)), _Gloss);

                return fixed4(ambient + (diffuse + specular) + i.vertexLight, 1);
            }
            ENDCG
        }

        Pass {
            Tags {"LightMode" = "ForwardAdd"}
            Blend One One

            CGPROGRAM
            #pragma multi_compile_fwdadd
            #pragma vertex vert
            #pragma fragment frag

            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            fixed4 _Diffuse;
            fixed4 _Specual;
            float _Gloss;

            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                float3 nDirWS : TEXCOORD0;
                float3 posWS : TEXCOORD1;

                LIGHTING_COORDS(2, 3)
            };

            VertexOutput vert(VertexInput v) {
                VertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.nDirWS = UnityObjectToWorldNormal(v.normal);
                o.posWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(VertexOutput i) : SV_TARGET {
                fixed3 nDirWS = normalize(i.nDirWS);
                fixed3 lDirWS = normalize(UnityWorldSpaceLightDir(i.posWS)); 
                fixed3 vDirWS = normalize(UnityWorldSpaceViewDir(i.posWS));
                fixed3 halfDir = normalize(lDirWS + vDirWS);

                fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * max(0, dot(nDirWS, lDirWS));
                fixed3 specular = _LightColor0.rgb * _Specual.rgb * pow(max(0, dot(vDirWS, halfDir)), _Gloss);
                fixed3 atten = LIGHT_ATTENUATION(i);

                return fixed4((diffuse + specular) * atten, 1.0);
            }

            ENDCG
        }
    }
}
