Shader "Custom/BufferShader"
{
    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Fog { Mode off }

            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform StructuredBuffer<float3> buffer;

            struct v2f
            {
                float4  pos : SV_POSITION;
            };

            v2f vert(uint id : SV_VertexID)
            {
                float4 pos = float4(buffer[id], 1);
                v2f OUT;
                OUT.pos = UnityObjectToClipPos(pos);
                return OUT;
            }

            float4 frag(v2f IN) : COLOR
            {
                return float4(1,0,0,1);
            }

            ENDCG
        }
    }
}