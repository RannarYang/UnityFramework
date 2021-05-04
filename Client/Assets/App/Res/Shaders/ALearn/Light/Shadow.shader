Shader "Unlit/Shadow"
{
    Properties
    {
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
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma target 3.0

            struct VertexInput {
                float4 vertex : POSITION;
            };

            struct VertexOutput {
                float4 pos : SV_POSITION;
                LIGHTING_COORDS(0, 1)
            };

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag (VertexOutput i) : COLOR
            {
                float shadow = LIGHT_ATTENUATION(i);
                return fixed4(shadow, shadow, shadow, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
