Shader "Custom/NewSurfaceShader"
{
    Properties
    {
        // Add any properties here if needed
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry+1"
        }
        
        Pass
        {
            Stencil
            {
                Ref 1
                Comp always
                Pass replace
            }

            Cull Front

            HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return half4(0,0,0,0);
            }
            ENDHLSL
        }
    }
}
