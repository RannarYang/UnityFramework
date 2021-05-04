Shader "Unlit/Deferred2"
{
    Properties
    {
    }
    SubShader
    {
        Pass
        {
            ZWrite Off
            Blend One One

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_lightpass
			//代表排除不支持MRT的硬件
			#pragma exclude_renderers norm
			#pragma multi_compile __ UNITY_HDR_ON

            #include "UnityCG.cginc"
			#include "UnityDeferredLibrary.cginc"
			#include "UnityGBuffer.cginc"

            sampler2D _CameraGBufferTexture0;
            sampler2D _CameraGBufferTexture1;
            sampler2D _CameraGBufferTexture2;
            sampler2D _CameraGBufferTexture3;

            struct VertexInput {
                float4 vertex: POSITION;
                float3 normal: NORMAL;
            };

            // struct VertexOutput {
            //     float4 pos : SV_POSITION;
            //     float4 uv : TEXCOORD0;
            //     float3 ray : TEXCOORD1;
            // };

            unity_v2f_deferred vert(VertexInput i) {
                unity_v2f_deferred o;
                o.pos = UnityObjectToClipPos(i.vertex);
                // 计算视口坐标系下的坐标(先计算齐次除法，再计算插值不准确，因为投影空间不是线性的，而插值是线性的)
                o.uv = ComputeScreenPos(o.pos);
                o.ray = UnityObjectToViewPos(i.vertex) * float3(-1,-1,1);
                // _LightAsQuad 当在处理四边形时候，也就是直射光时返回1.否则返回0
                o.ray = lerp(o.ray, i.normal, _LightAsQuad);
                return o;
            }

            fixed4 frag (unity_v2f_deferred i) : SV_TARGET {
                // float3 posWS;
                // float2 uv,
                // half3 lDirWS,
                // float atten,
                // float fadeDist
                // UnityDeferredCalculateLightParams(i, posWS, uv, lDirWS, atten, fadeDist);

                float2 uv = i.uv.xy/i.uv.w;
                // 通过深度和方向重新构建世界坐标
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                depth = Linear01Depth(depth);
                // ray只能表示方向，长度不一定， _ProjectionParams是远平面, 因为xyz都是等比例，所以 _ProjectionParams.z / i.ray.z 就是rayToFarPlane向量和ray向量的比值
                float3 rayToFarPlane = i.ray * (_ProjectionParams.z/i.ray.z);
                float4 posVS = float4(rayToFarPlane * depth, 1);
                float3 posWS = mul(unity_CameraToWorld, posVS).xyz;

                float fadeDist = UnityComputeShadowFadeDistance(posWS, posVS.z);

                

                // 对不同的光进行光衰减计算 包括阴影计算
                #if defined(SPOT) // 聚光灯
                    float3 toLight = _LightPos.xyz - posWS;
                    half3 lDirWS = normalize(toLight);
                    float4 uvCookie = mul(unity_WorldToLight, float4(posWS, 1));
                    // _LightTexture0 正常情况下没有cookie的衰减的采样
                    float4 atten = tex2Dbias(_LightTexture0, float4(uvCookie.xy/uvCookie.w, 0, -8)).w;

                    atten *= uvCookie < 0;
                    atten *= tex2D(_LightTextureB0, dot(toLight, toLight) * _LightPos.w).r;
                    atten *= UnityDeferredComputeShadow(posWS, fadeDist, uv);

                #elif defined(DIRECTIONAL) || defined(DICTIONAL_COOKIE) // 平行光
                    half3 lDirWS = -_LightDir.xyz;
                    float atten = 1.0;

                    atten *= UnityDeferredComputeShadow(posWS, fadeDist, uv);

                #if defined(DICTIONAL_COOKIE)
                    float4 uvCookie = mul(unity_WorldToLight, float4(posWS, 1));
                    atten *= tex2Dbias(_LightTexture0, float4(uvCookie.xy, 0, -8)).w;
                #endif

                #elif defined(POINT) || defined(POINT_COOKIE) // 点光
                    float3 toLight = _LightPos.xyz - posWS;
                    half3 lDirWS = normalize(toLight);
                    float atten = tex2D(_LightTextureB0, dot(toLight, toLight) * _LightPos.w).r;
                    atten * = UnityDeferredComputeShadow(posWS, fadeDist, uv);

                    #if defined(POINT_COOKIE)
                    float4 uvCookie = mul(unity_WorldToLight, float4(posWS, 1));
                    atten *= texCUBEbias(_LightTexture0, float4(uvCookie.xyz, 0, -8)).w;
                    #endif
                #else 
                    half3 lDir = 0;
                    float atten = 0;
                #endif
                
                fixed3 lightColor = _LightColor.rgb * atten;
                half4 gbuffer0 = tex2D(_CameraGBufferTexture0, uv);
                half4 gbuffer1 = tex2D(_CameraGBufferTexture1, uv);
                half4 gbuffer2 = tex2D(_CameraGBufferTexture2, uv);

                fixed3 diffuseColor = gbuffer0.rgb;
                fixed3 specularColor = gbuffer1.rgb;
                float gloss = gbuffer1.a * 50;
                float3 nDirWS = normalize(gbuffer2.xyz * 2 - 1);
                fixed3 vDirWS = normalize(_WorldSpaceCameraPos - posWS);
                fixed3 hDirWS = normalize(lDirWS + vDirWS);

                fixed3 diffuse = lightColor * diffuseColor * max(0, dot(nDirWS, lDirWS));
                fixed3 specular = lightColor * specularColor * pow(max(0, dot(nDirWS, hDirWS)), gloss);
                float4 color = float4(diffuse + specular, 1);
                return color;
                // return 1;
            }

            ENDCG
        }
    }
}
